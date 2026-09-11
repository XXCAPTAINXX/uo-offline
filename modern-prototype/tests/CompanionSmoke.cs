using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;
using Server.Network;

// Opt-in only. Test accounts and fixtures never come from a player's world.
public static class CompanionSmoke
{
    private static int _failures;
    private static HavenCompanion _companion;
    private static PlayerMobile _owner;
    private static PlayerMobile _stranger;
    private static Orc _enemy;
    private static int _hitsBefore;
    private static double _distanceBefore;
    private static int _healBefore;
    private const string Report = "companion-checks.log";
    public static void Initialize()
    {
        if (File.Exists("COMPANION-TEST-ONLY")) EventSink.ServerStarted += () => Timer.DelayCall(TimeSpan.FromSeconds(2), Run);
    }
    private static void Check(string name, Action test)
    {
        try { test(); File.AppendAllText(Report, "PASS " + name + Environment.NewLine); }
        catch (Exception ex) { _failures++; File.AppendAllText(Report, "FAIL " + name + ": " + ex + Environment.NewLine); }
    }
    private static void Require(bool result, string message) { if (!result) throw new Exception(message); }
    private static void Field(string name, object value) { typeof(HavenCompanion).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_companion, value); }
    private static void FinishMission()
    {
        typeof(HavenCompanion).GetMethod("CompleteDueMission", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_companion, null);
    }
    private static void ClearCombat()
    {
        _companion.Combatant = null; _owner.Combatant = null;
        _companion.Aggressors.Clear(); _companion.Aggressed.Clear();
        _owner.Aggressors.Clear(); _owner.Aggressed.Clear();
        _companion.MoveToWorld(_owner.Location, _owner.Map);
        _companion.SetOrder(_owner, OrderType.Stay);
    }
    private static void Run()
    {
        bool reload = File.Exists("companion-fixtures.txt");
        File.AppendAllText(Report, "PHASE " + (reload ? "reload" : "fresh") + Environment.NewLine);
        PreviewSmoke.Run(Check, reload);
        ResourceSmoke.Run(Check, reload);
        ResourceMissionSmoke.Run(Check, reload);
        ShadowguardSmoke.Run(Check, reload);
        DoomNavalSmoke.Run(Check, reload);
        if (reload)
        {
            var ids = File.ReadAllLines("companion-fixtures.txt");
            _owner = World.FindMobile((Serial)Int32.Parse(ids[0])) as PlayerMobile;
            _companion = World.FindMobile((Serial)Int32.Parse(ids[1])) as HavenCompanion;
            Check("owner and companion references persist", () => Require(_companion != null && _owner != null && _companion.BoundOwner == _owner && _companion.ControlMaster == _owner, "Ownership mismatch"));
            Check("role and native AI persist", () => Require(_companion.Role == CompanionRole.Archer && _companion.AIObject is HavenCompanionArcherAI && _companion.Weapon is Bow, "Role lost"));
            Check("overdue mission completes while owner is offline", () => Require(!_companion.OnMission && _companion.CompletedMissions == 3, "Mission did not recover"));
            Check("mission report persists", () => Require(_companion.LastReport.Contains("Completed runs: 3"), "Report absent"));
            Check("inventory and stacked gold persist", () => Require(_companion.Backpack.FindItemsByType(typeof(Gold), false).Length == 1 && _companion.Backpack.GetAmount(typeof(Gold)) == 2500, "Unexpected rewards"));
            Check("restart does not duplicate rewards", () => { FinishMission(); Require(_companion.CompletedMissions == 3 && _companion.Backpack.GetAmount(typeof(Gold)) == 2500, "Reward replay" ); });
            // Offline PlayerMobiles load on Map.Internal until native login restores them.
            Check("simulate native login location restoration", () => {
                _owner.MoveToWorld(new Point3D(Int32.Parse(ids[2]), Int32.Parse(ids[3]), Int32.Parse(ids[4])), Map.Trammel);
                Require(_owner.Alive, "Owner died in fixture");
            });
            Check("claim after login returns same companion", () => Require(HavenCompanion.Claim(_owner) == _companion, "Duplicate companion"));
            Check("owner can recall recovered companion", () => Require(_companion.Recall(_owner) && _companion.Map == _owner.Map, "Recall failed"));
            Check("reloaded role responds to guard command", () => Require(_companion.SetOrder(_owner, OrderType.Guard) && _companion.AIObject.DoOrderGuard(), "Guard failed after restart"));
            Done(); return;
        }
        Check("create player fixture on traversable modern map", () => {
            _owner = new PlayerMobile { Name = "prototype owner", Body = 0x190, Player = true };
            _owner.RawStr = 100; _owner.RawDex = 100; _owner.RawInt = 100; _owner.Hits = _owner.HitsMax;
            var account = new Account("companion-fixture", Guid.NewGuid().ToString("N"));
            account[0] = _owner;
            bool placed = false;
            for (int x = 500; x < 4500 && !placed; x += 50)
                for (int y = 500; y < 3500 && !placed; y += 50)
                {
                    int z = Map.Trammel.GetAverageZ(x, y);
                    bool clear = true;
                    for (int dx = 0; dx <= 5 && clear; dx++) for (int dy = 0; dy <= 2 && clear; dy++) clear = Map.Trammel.CanSpawnMobile(x + dx, y + dy, z);
                    if (clear) { _owner.MoveToWorld(new Point3D(x, y, z), Map.Trammel); placed = true; }
                }
            Require(placed, "No open fixture site");
            _stranger = new PlayerMobile { Name = "stranger", Body = 0x190, Player = true };
            _stranger.MoveToWorld(_owner.Location, _owner.Map);
        });
        if (_owner == null || _owner.Map == Map.Internal) { Done(); return; }
        Check("claim binds owner and one follower slot", () => { _companion = HavenCompanion.Claim(_owner); Require(_companion != null && _companion.BoundOwner == _owner && _owner.Followers == 1 && _companion.IsBonded, "Claim failed"); });
        if (_companion == null) { Done(); return; }
        Check("repeat claim does not duplicate", () => Require(HavenCompanion.Claim(_owner) == _companion && _owner.Followers == 1, "Duplicate"));
        Check("stranger cannot issue orders", () => Require(!_companion.SetOrder(_stranger, OrderType.Guard), "Unauthorized command"));
        Check("transfer release and drop-all blocked", () => {
            var before = _companion.ControlOrder;
            _companion.ControlOrder = OrderType.Release; _companion.ControlOrder = OrderType.Drop;
            Require(_companion.ControlOrder == before && !_companion.CanTransfer(_stranger), "Permanent ownership broken");
        });
        Check("pet speech guard stay and follow", () => {
            _companion.OnSpeech(new SpeechEventArgs(_owner, "all guard me", MessageType.Regular, 0, new int[0]));
            Require(_companion.ControlOrder == OrderType.Guard, "Guard speech ignored");
            _companion.OnSpeech(new SpeechEventArgs(_owner, "all stay", MessageType.Regular, 0, new int[0]));
            Require(_companion.ControlOrder == OrderType.Stay, "Stay speech ignored");
            _companion.OnSpeech(new SpeechEventArgs(_owner, "Alden Ashford follow me", MessageType.Regular, 0, new int[0]));
            Require(_companion.ControlOrder == OrderType.Follow, "Named follow ignored");
        });
        Check("owner can deposit and withdraw nested items", () => {
            var bag = new Bag(); var ruby = new Ruby(); bag.DropItem(ruby);
            Require(_companion.OnDragDrop(_owner, bag), "Deposit denied");
            Require(_companion.CheckNonlocalLift(_owner, ruby) && _companion.CheckNonlocalDrop(_owner, new Gold(1), bag), "Nested access denied");
            Require(!_companion.CheckNonlocalLift(_stranger, ruby) && !_companion.Backpack.IsAccessibleTo(_stranger), "Stranger access");
            bag.Delete();
        });
        Check("remote pack access and stale command denied", () => {
            var at = _stranger.Location; _stranger.MoveToWorld(new Point3D(at.X + 20, at.Y, at.Z), _owner.Map);
            Require(!_companion.CanOpenPack(_stranger), "Remote intrusion");
            var origin = _owner.Location; _owner.MoveToWorld(new Point3D(origin.X + 20, origin.Y, origin.Z), _owner.Map);
            Require(!_companion.SetOrder(_owner, OrderType.Guard) && !_companion.CanOpenPack(_owner), "Range not rechecked");
            _owner.MoveToWorld(origin, _companion.Map);
        });
        Check("companion joins owner party idempotently", () => Require(_companion.JoinOwnerParty(_owner) && _companion.JoinOwnerParty(_owner) && Server.Engines.PartySystem.Party.Get(_owner).Count == 2, "Party join incorrect"));
        Check("all kill shares native target with ordinary pet", () => {
            _owner.Skills.AnimalTaming.Base = 120; _owner.Skills.AnimalLore.Base = 120;
            var dog = new Dog(); dog.SetControlMaster(_owner); dog.Loyalty = BaseCreature.MaxLoyalty; dog.MoveToWorld(_owner.Location, _owner.Map);
            var enemy = new Orc(); enemy.MoveToWorld(new Point3D(_owner.X + 2, _owner.Y, _owner.Z), _owner.Map);
            _owner.Target = null;
            _companion.OnSpeech(new SpeechEventArgs(_owner, "all kill", MessageType.Regular, 0, new int[0]));
            var cursor = _owner.Target; dog.AIObject.BeginPickTarget(_owner, OrderType.Attack);
            Require(cursor != null && cursor == _owner.Target, "Cursor replaced");
            cursor.Invoke(_owner, enemy);
            Require(_companion.ControlTarget == enemy && dog.ControlTarget == enemy, "Not both commanded");
            dog.Delete(); enemy.Delete(); ClearCombat();
        });
        Check("guard chooses hostile and excludes wild tameable", () => {
            _enemy = new Orc(); _enemy.MoveToWorld(new Point3D(_owner.X + 2, _owner.Y, _owner.Z), _owner.Map);
            var tameable = new GreatHart(); tameable.MoveToWorld(new Point3D(_owner.X + 1, _owner.Y, _owner.Z), _owner.Map);
            Require(!_companion.ValidAutomaticTarget(tameable), "Wild tameable selected");
            Require(_companion.ClosestHostile() == _enemy, "Hostile not selected");
            _companion.SetOrder(_owner, OrderType.Guard); _companion.AIObject.DoOrderGuard();
            Require(_companion.Combatant == _enemy && _companion.ControlOrder == OrderType.Guard, "Guard attack failed");
            tameable.Delete(); _enemy.Delete(); ClearCombat();
        });
        Check("follow order starts native movement", () => {
            _companion.MoveToWorld(new Point3D(_owner.X + 5, _owner.Y, _owner.Z), _owner.Map);
            _companion.SetOrder(_owner, OrderType.Follow); _distanceBefore = _companion.GetDistanceToSqrt(_owner);
            _companion.AIObject.DoOrderFollow();
        });
        Timer.DelayCall(TimeSpan.FromSeconds(3), AfterFollow);
    }
    private static void AfterFollow()
    {
        Check("native follow closes distance", () => Require(_companion.GetDistanceToSqrt(_owner) < _distanceBefore, "No movement"));
        ClearCombat();
        Check("native greater heal starts", () => { _owner.Hits = 30; _healBefore = _owner.Hits; Require(_companion.HealOwner(_owner), "Heal not cast"); });
        Timer.DelayCall(TimeSpan.FromSeconds(4), AfterHeal);
    }
    private static void AfterHeal()
    {
        Check("native spell heals owner and spends mana", () => Require(_owner.Hits > _healBefore + 10 && _companion.Mana < _companion.ManaMax, "No effective heal"));
        _owner.Hits = _owner.HitsMax;
        Check("direct attack starts native combat", () => {
            _enemy = new Orc(); _enemy.SetHits(3000); _enemy.Hits = _enemy.HitsMax;
            _enemy.MoveToWorld(new Point3D(_owner.X + 1, _owner.Y, _owner.Z), _owner.Map);
            Require(_companion.Attack(_owner, _enemy), "Attack rejected");
            _hitsBefore = _enemy.Hits;
            _companion.AIObject.DoOrderAttack();
        });
        Timer.DelayCall(TimeSpan.FromSeconds(12), AfterAttack);
    }
    private static void AfterAttack()
    {
        Check("native melee damages target", () => Require(_enemy.Hits < _hitsBefore, "No melee damage"));
        Check("companion damage credit belongs to real owner", () => Require(_enemy.GetLootingRights().Any(x => x.m_Mobile == _owner && x.m_HasRight), "Owner lacks loot rights"));
        _enemy.Delete(); ClearCombat();
        RoleSmoke.Run(_companion, _owner, _stranger, Check, AfterRoles);
    }
    private static void AfterRoles()
    {
        Check("gold rewards merge existing pile", () => { _companion.Backpack.DropItem(new Gold(100)); Field("_pendingGold", 900); _companion.DeliverRewards(); Require(_companion.Backpack.GetAmount(typeof(Gold)) == 1000 && _companion.Backpack.FindItemsByType(typeof(Gold), false).Length == 1, "Gold fragmentation"); });
        Check("full pack queues rewards without loss", () => {
            var gold = _companion.Backpack.FindItemByType(typeof(Gold)); gold.Delete();
            _companion.Backpack.MaxItems = 1; var filler = new Bag(); _companion.Backpack.DropItem(filler);
            Field("_pendingGold", 1000); _companion.DeliverRewards(); Require(_companion.PendingGold == 1000, "Reward lost");
            filler.Delete(); _companion.Backpack.MaxItems = 1000; _companion.DeliverRewards();
            Require(_companion.PendingGold == 0 && _companion.Backpack.GetAmount(typeof(Gold)) == 1000, "Pending delivery failed");
        });
        Check("mission rejects invalid length and stranger", () => Require(!_companion.StartMission(_owner, 0) && !_companion.StartMission(_stranger, 5), "Invalid mission accepted"));
        Check("mission blocks duplicate starts and early rewards", () => {
            Require(_companion.StartMission(_owner, 5), "Mission not started");
            Require(!_companion.StartMission(_owner, 5) && _companion.Map == Map.Internal, "Duplicate mission or not away");
            FinishMission(); Require(_companion.CompletedMissions == 0 && _companion.OnMission, "Early payout");
        });
        Check("due mission rewards once and reports", () => {
            Field("_missionDue", DateTime.UtcNow.AddSeconds(-1)); FinishMission(); FinishMission();
            Require(_companion.CompletedMissions == 1 && _companion.Backpack.GetAmount(typeof(Gold)) == 1500 && _companion.LastReport.Contains("500 gold"), "Bad completion");
            Require(_companion.Recall(_owner), "Return failed");
        });
        Check("second reward keeps one gold pile", () => {
            Require(_companion.StartMission(_owner, 5), "Second mission failed");
            Field("_missionDue", DateTime.UtcNow.AddSeconds(-1)); FinishMission(); _companion.Recall(_owner);
            Require(_companion.CompletedMissions == 2 && _companion.Backpack.GetAmount(typeof(Gold)) == 2000 && _companion.Backpack.FindItemsByType(typeof(Gold), false).Length == 1, "Stack regression");
        });
        Check("save pending mission and companion", () => {
            Require(_companion.StartMission(_owner, 5), "Pending mission start failed");
            Field("_missionDue", DateTime.UtcNow.AddSeconds(1));
            ResourceMissionSmoke.PrepareForSave();
            World.Save(false, false);
            File.WriteAllLines("companion-fixtures.txt", new[] { _owner.Serial.Value.ToString(), _companion.Serial.Value.ToString(), _owner.X.ToString(), _owner.Y.ToString(), _owner.Z.ToString() });
        });
        Done();
    }
    private static void Done()
    {
        File.AppendAllText(Report, "COMPLETE failures=" + _failures + Environment.NewLine);
        Timer.DelayCall(TimeSpan.FromMilliseconds(100), () => Core.Kill(false));
    }
}
