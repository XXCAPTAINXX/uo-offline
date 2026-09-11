using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.HavenPrototype
{


public partial class HavenAbyssTrial : Item
{
    internal static readonly Point3D Site = new Point3D(526, 758, -92);
    internal static readonly Point3D Landing = new Point3D(527, 758, -92);
    private static readonly Point2D[] Neighbors = { new Point2D(1, 0), new Point2D(-1, 0), new Point2D(0, 1), new Point2D(0, -1) };
    internal static readonly HashSet<HavenAbyssTrial> Registry = new HashSet<HavenAbyssTrial>();
    public Mobile Challenger;
    public List<HavenAbyssGuardian> Guardians = new List<HavenAbyssGuardian>();
    public HavenAncientHellhound Hound;
    public int Kills;
    public int Stage;
    public DateTime Expires;
    public DateTime NextStart;
    private Timer _timer;

    [Constructable]
    public HavenAbyssTrial() : base(0xE31)
    { Name = "Brazier of the Ancient Hunt"; Movable = false; Light = LightType.Circle300; }
    internal static bool TerrainReady() => Map.TerMur.CanSpawnMobile(Site) && Map.TerMur.CanSpawnMobile(Landing);
    internal void Register() { Registry.Add(this); }
    
    private void Recover()
    {
        Register();
        // An interrupted ritual cannot award a second hound after a restart.
        Finish();
    }
    internal static bool Go(Mobile from)
    {
        if (from?.Deleted != false || !from.Alive || from.Criminal || from.Spell != null || SpellHelper.CheckCombat(from) ||
            !SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom)) { return false; }
        foreach (var trial in Registry)
        {
            if (trial.Deleted || trial.Map != Map.TerMur || trial.Location != Site) { continue; }
            for (var radius = 0; radius <= 2; radius++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    for (var dy = -radius; dy <= radius; dy++)
                    {
                        if (radius > 0 && Math.Abs(dx) != radius && Math.Abs(dy) != radius) { continue; }
                        var landing = new Point3D(Landing.X + dx, Landing.Y + dy, Landing.Z);
                        if (!Map.TerMur.CanSpawnMobile(landing) || !SpellHelper.CheckTravel(from, Map.TerMur, landing, TravelCheckType.RecallTo)) { continue; }
                        BaseCreature.TeleportPets(from, landing, Map.TerMur); from.MoveToWorld(landing, Map.TerMur); from.PlaySound(0x1FE);
                        from.SendMessage("Ancient Hunt: use the brazier. This is a Haven encounter; the full Stygian Abyss remains under development.");
                        return true;
                    }
                }
            }
            return false;
        }
        return false;
    }
    internal bool Start(Mobile from)
    {
        if (Deleted || !Registry.Contains(this) || Stage != 0 || DateTime.UtcNow < NextStart || from?.Deleted != false || !from.Alive ||
            from.Map != Map || !from.InRange(this, 3) || !from.InLOS(this) || from.Skills.AnimalTaming.Base < 110 || from.Skills.AnimalLore.Base < 110) { return false; }
        Challenger = from; Kills = 0; Stage = 1; Expires = DateTime.UtcNow + TimeSpan.FromMinutes(15);
        Spawn();
        if (Guardians.Count == 0) { Finish(); NextStart = DateTime.UtcNow; return false; }
        _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Tick);
        from.SendMessage("Defeat two waves of three guardians. An ancient hellhound will emerge to be tamed. Stay within 28 tiles of the brazier.");
        return true;
    }
    internal void Tick()
    {
        if (Deleted || Stage == 0) { return; }
        if (Stage == 3)
        {
            if (Hound?.Deleted != false || Hound.Controlled || Hound.Owners.Count > 0 || DateTime.UtcNow >= Expires) { Finish(); }
            return;
        }
        if (Challenger?.Deleted != false || !Challenger.Alive || Challenger.Map != Map || !Challenger.InRange(this, 28) || DateTime.UtcNow >= Expires)
        { Finish(); return; }
        foreach (var guardian in Guardians)
        {
            if (guardian?.Deleted == false && (guardian.Map != Map || !guardian.InRange(this, 18)) &&
                TrySpawnPoint(out var point)) { guardian.MoveToWorld(point, Map); }
        }
        Spawn();
    }
    // Connected, flat floor cells only; avoids placing the pet across lava or on another level.
    internal bool TrySpawnPoint(out Point3D point)
    {
        point = default;
        if (Map == null || Map == Map.Internal) { return false; }
        var pending = new Queue<Point3D>(); var visited = new HashSet<Point2D>(); var choices = new List<Point3D>();
        var root = new Point3D(X + 1, Y, Z);
        if (!Map.CanFit(root, 16, false, false)) { return false; }
        pending.Enqueue(root); visited.Add(new Point2D(root));
        while (pending.Count > 0)
        {
            var p = pending.Dequeue();
            if (!Utility.InRange(p, Location, 3) && Map.CanSpawnMobile(p) &&
                Map.LineOfSight(new Point3D(root.X, root.Y, root.Z + 16), new Point3D(p.X, p.Y, p.Z + 16)))
            { choices.Add(p); }
            foreach (var delta in Neighbors)
            {
                var x = p.X + delta.X; var y = p.Y + delta.Y;
                if (Math.Abs(x - X) > 9 || Math.Abs(y - Y) > 9 || x < 0 || y < 0 || x >= Map.Width || y >= Map.Height ||
                    !visited.Add(new Point2D(x, y))) { continue; }
                var next = new Point3D(x, y, Map.GetAverageZ(x, y));
                if (Math.Abs(next.Z - p.Z) <= 2 && Map.CanFit(next, 16, false, false)) { pending.Enqueue(next); }
            }
        }
        if (choices.Count == 0) { return false; }
        point = choices[Utility.Random(choices.Count)]; return true;
    }
    internal void Spawn()
    {
        if ((Stage < 1 || Stage > 2)) { return; }
        for (var i = Guardians.Count - 1; i >= 0; i--)
        { if (Guardians[i]?.Deleted != false) { Guardians.RemoveAt(i);  } }
        while (Guardians.Count < 3 - Kills)
        {
            if (!TrySpawnPoint(out var point)) { return; }
            var mob = new HavenAbyssGuardian(Stage) { Trial = this, Home = Location, RangeHome = 12 };
            Guardians.Add(mob);  mob.MoveToWorld(point, Map); mob.Combatant = Challenger;
        }
    }
    internal void Defeated(HavenAbyssGuardian guardian)
    {
        if ((Stage < 1 || Stage > 2) || !Guardians.Remove(guardian)) { return; }
        guardian.Trial = null; Kills++; 
        if (Kills < 3) { return; }
        if (Stage == 1) { Stage = 2; Kills = 0; return; }
        if (!TrySpawnPoint(out var point)) { Finish(); return; }
        var hound = new HavenAncientHellhound();
        HavenPetMissions.ApplyRarity(hound, RollRarity(Utility.RandomDouble()));
        Hound = hound; Stage = 3; Expires = DateTime.UtcNow + TimeSpan.FromMinutes(20);
        hound.Home = Location; hound.RangeHome = 8; hound.MoveToWorld(point, Map);
        Challenger?.SendMessage("An ancient hellhound has emerged! You have 20 minutes to tame it. Use Animal Lore to inspect its rarity and stats.");
    }
    internal static int RollRarity(double roll) => roll < .50 ? 1 : roll < .85 ? 2 : 3;
    internal void Finish()
    {
        _timer?.Stop(); _timer = null;
        foreach (var mob in Guardians) { if (mob?.Deleted == false) { mob.Trial = null; mob.Delete(); } }
        Guardians.Clear();
        if (Hound?.Deleted == false && !Hound.Controlled && Hound.Owners.Count == 0) { Hound.Delete(); }
        Hound = null; Challenger = null; Kills = 0; Stage = 0;
        NextStart = DateTime.UtcNow + TimeSpan.FromMinutes(10); 
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map == Map && from.InRange(this, 3) && from.InLOS(this))
        { from.CloseGump(typeof(HavenAbyssTrialGump)); from.SendGump(new HavenAbyssTrialGump(this, from)); }
    }
    public HavenAbyssTrial(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Challenger);w.Write(Guardians.Count);foreach(var g in Guardians)w.Write(g);w.Write(Hound);w.Write(Kills);w.Write(Stage);w.Write(Expires);w.Write(NextStart);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Challenger=r.ReadMobile();int count=r.ReadInt();if(count<0||count>3)throw new InvalidOperationException("Invalid guardian count");for(int i=0;i<count;i++){var g=r.ReadMobile() as HavenAbyssGuardian;if(g!=null)Guardians.Add(g);}Hound=r.ReadMobile() as HavenAncientHellhound;Kills=r.ReadInt();Stage=r.ReadInt();Expires=r.ReadDateTime();NextStart=r.ReadDateTime();Timer.DelayCall(TimeSpan.Zero,Recover);}
    public override void OnDelete() { Finish(); Registry.Remove(this); base.OnDelete(); }
    public static void Initialize()
    {
        CommandSystem.Register("abyss", AccessLevel.Player, e =>
        { if(!Go(e.Mobile))e.Mobile.SendMessage("Ancient Hunt travel unavailable: check combat, criminal status and encounter installation."); });
        CommandSystem.Register("HavenAbyssSetup", AccessLevel.GameMaster, e =>
        {
            if (Registry.Count > 0) { e.Mobile.SendMessage("An Ancient Hunt brazier already exists; it was preserved."); return; }
            if (!TerrainReady()) { e.Mobile.SendMessage("The verified Abyss floor is unavailable or blocked. Nothing was placed."); return; }
            var trial = new HavenAbyssTrial(); trial.MoveToWorld(Site, Map.TerMur); trial.Register();
            e.Mobile.SendMessage("Ancient Hunt installed. Players can use [abyss or the Wayfarer's Atlas.");
        });
    }
}


public partial class HavenAbyssGuardian : HellHound
{
    public HavenAbyssTrial Trial;
    [Constructable] public HavenAbyssGuardian() : this(1) { }
    public HavenAbyssGuardian(int stage)
    {
        Name = stage == 1 ? "a cinder sentinel" : "an ancient flame sentinel";
        Body = stage == 1 ? 98 : 15; Tamable = false;
        SetStr(160); SetDex(90); SetInt(80); SetHits(stage == 1 ? 180 : 280);
        SetDamage(7, 11); SetSkill(SkillName.Wrestling, 75); SetSkill(SkillName.Tactics, 75);
    }
    public HavenAbyssGuardian(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Trial);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Trial=r.ReadItem() as HavenAbyssTrial;}
    public override void OnDeath(Container corpse) { Trial?.Defeated(this); base.OnDeath(corpse); }
    public override void OnAfterDelete() { Trial = null; base.OnAfterDelete(); }
}

public sealed class HavenAbyssTrialGump : Gump
{
    private readonly HavenAbyssTrial _trial;
    public HavenAbyssTrialGump(HavenAbyssTrial trial, Mobile from) : base(80, 70)
    {
        _trial = trial;
        AddBackground(0, 0, 470, 350, 0xA28); AddLabel(30, 25, 0, "The Ancient Hunt");
        AddHtml(30, 65, 410, 130, "<BASEFONT COLOR=#342B23>Defeat two waves of guardians to reveal a wild Ancient Hellhound. Tame it before it leaves.<BR><BR>Innate Healing, fire breath, high Dexterity and stamina. Rare, Epic or Legendary; Legendary starts with one follower slot.</BASEFONT>",false,false);
        AddLabel(30, 200, 0, $"Taming {from.Skills.AnimalTaming.Base:F1} / 110    Lore {from.Skills.AnimalLore.Base:F1} / 110");
        AddLabel(30, 232, 0, trial.Stage == 0 ? DateTime.UtcNow < trial.NextStart ? $"Ready in {Math.Ceiling((trial.NextStart - DateTime.UtcNow).TotalMinutes)} minutes" : "Ready to begin" :
            trial.Stage == 3 ? "A hellhound is waiting to be tamed" : $"Wave {trial.Stage} / 2 — {trial.Kills} / 3 defeated");
        AddButton(30, 285, 4005, 4007, 1,GumpButtonType.Reply,0); AddLabel(65, 285, 0, "Begin hunt");
        AddButton(305, 285, 4017, 4019, 0,GumpButtonType.Reply,0); AddLabel(340, 285, 0, "Close");
    }
    public override void OnResponse(NetState sender, RelayInfo info)
    {
        if (info.ButtonID != 1) { return; }
        if (!_trial.Start(sender.Mobile)) { sender.Mobile.SendMessage("The hunt is unavailable. Check your Taming and Lore, distance, and the brazier's cooldown."); }
    }
}

}
