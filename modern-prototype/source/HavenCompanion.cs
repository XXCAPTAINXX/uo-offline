using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fourth;
using Server.Targeting;
using CompanionParty = Server.Engines.PartySystem.Party;

namespace Server.HavenPrototype
{
    public enum CompanionRole { Warrior, Caster, Archer }
    // First ServUO vertical slice, not a deserializer for existing ModernUO saves.
    public partial class HavenCompanion : BaseCreature
    {
        private static readonly string[] GivenNames = { "Alden", "Bram", "Corin", "Darian", "Elias", "Finn", "Gareth", "Jonas", "Kellan", "Luca", "Marek", "Nolan", "Orin", "Rowan", "Silas", "Tobin" };
        private static readonly string[] FamilyNames = { "Ashford", "Blackwater", "Driftwood", "Fairwind", "Greywake", "Hawthorne", "Ironwood", "Keelward", "Marsh", "Northwood", "Reed", "Saltmere", "Seabrook", "Thorne", "Westfall", "Wick" };
        private static string RecruitName()
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mobile in World.Mobiles.Values)
                if (mobile is HavenCompanion && !mobile.Deleted && mobile.Name != null) used.Add(mobile.Name);
            int start = Utility.Random(GivenNames.Length * FamilyNames.Length);
            for (int offset = 0; offset < GivenNames.Length * FamilyNames.Length; offset++)
            {
                int index = (start + offset) % (GivenNames.Length * FamilyNames.Length);
                string name = GivenNames[index / FamilyNames.Length] + " " + FamilyNames[index % FamilyNames.Length];
                if (!used.Contains(name)) return name;
            }
            return GivenNames[Utility.Random(GivenNames.Length)] + " " + FamilyNames[Utility.Random(FamilyNames.Length)];
        }
        private Mobile _owner;
        private BaseAI _companionAI;
        private CompanionRole _role;
        private Item _roleSword, _roleShield, _roleBow;
        private Timer _missionTimer;
        private DateTime _missionDue;
        private int _missionMinutes;
        private int _pendingGold;
        private int _completedMissions;
        private string _lastReport = "No missions completed yet.";
        private DateTime _nextHeal;

        public Mobile BoundOwner { get { return _owner; } }
        public CompanionRole Role { get { return _role; } }
        public bool OnMission { get { return _missionDue != DateTime.MinValue; } }
        public DateTime MissionDue { get { return _missionDue; } }
        public int PendingGold { get { return _pendingGold; } }
        public int CompletedMissions { get { return _completedMissions; } }
        public string LastReport { get { return _lastReport; } }
        public override bool CanAutoStable { get { return false; } }
        public override bool AllowNewPetFriend { get { return false; } }
        public override bool KeepsItemsOnDeath { get { return true; } }
        protected override BaseAI ForcedAI
        {
            get {
                if (_companionAI == null)
                    _companionAI = _role == CompanionRole.Caster ? (BaseAI)new HavenCompanionMageAI(this) :
                        _role == CompanionRole.Archer ? (BaseAI)new HavenCompanionArcherAI(this) : new HavenCompanionAI(this);
                return _companionAI;
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("c", AccessLevel.Player, Open);
            CommandSystem.Register("companion", AccessLevel.Player, Open);
        }

        private static void Open(CommandEventArgs e)
        {
            var companion = Claim(e.Mobile);
            if (companion != null) companion.Show(e.Mobile);
        }

        public static HavenCompanion Claim(Mobile owner)
        {
            var account = owner == null ? null : owner.Account as Account;
            if (owner == null || owner.Deleted || !owner.Player || account == null || !owner.Alive || owner.Map == null || owner.Map == Map.Internal) return null;
            string key = "HavenPrototype.Companion:" + owner.Serial.Value;
            int serial;
            HavenCompanion companion = null;
            if (Int32.TryParse(account.GetTag(key), out serial)) companion = World.FindMobile((Serial)serial) as HavenCompanion;
            if (companion != null && !companion.Deleted)
            {
                if (companion._owner != owner || (companion.ControlMaster != null && companion.ControlMaster != owner)) return null;
                return companion;
            }
            if (owner.Followers >= owner.FollowersMax) { owner.SendMessage("Your companion needs one free follower slot."); return null; }
            companion = new HavenCompanion();
            companion._owner = owner;
            if (!companion.SetControlMaster(owner)) { companion.Delete(); return null; }
            companion.IsBonded = true;
            companion.MoveToWorld(owner.Location, owner.Map);
            companion.SetOrder(owner, OrderType.Follow);
            account.SetTag(key, companion.Serial.Value.ToString());
            return companion;
        }

        [Constructable]
        public HavenCompanion() : base(AIType.AI_Melee, FightMode.Aggressor, 12, 1, 0.2, 0.4)
        {
            Name = RecruitName();
            Title = "the faithful companion";
            Body = 0x190;
            Hue = 0x83EA;
            Tamable = false;
            ControlSlots = 1;
            MinTameSkill = 0;
            SetStr(100); SetDex(80); SetInt(100); SetHits(180); SetMana(100);
            SetDamage(8, 12);
            SetResistance(ResistanceType.Physical, 40);
            SetResistance(ResistanceType.Fire, 25); SetResistance(ResistanceType.Cold, 25);
            SetResistance(ResistanceType.Poison, 25); SetResistance(ResistanceType.Energy, 25);
            SetSkill(SkillName.Swords, 75); SetSkill(SkillName.Tactics, 75);
            SetSkill(SkillName.Anatomy, 65); SetSkill(SkillName.MagicResist, 65);
            SetSkill(SkillName.Magery, 100); SetSkill(SkillName.Meditation, 100);
            SetSkill(SkillName.EvalInt, 100); SetSkill(SkillName.Archery, 100); SetSkill(SkillName.Wrestling, 100);
            AddItem(new ChainChest { Movable = false }); AddItem(new ChainLegs { Movable = false });
            AddItem(new Boots { Movable = false }); AddItem(_roleShield = new MetalShield { Movable = false });
            AddItem(_roleSword = new Longsword { Movable = false });
            var cloak = new Cloak(0x59B) { Movable = false }; cloak.Attributes.RegenMana = 8; AddItem(cloak);
            AddItem(new HavenCompanionPack());
            SetSkill(SkillName.Mining, 50); SetSkill(SkillName.Lumberjacking, 50); SetSkill(SkillName.AnimalLore, 50);
            EnsureResourceLedger();
        }

        public bool SetRole(Mobile from, CompanionRole role)
        {
            if (!CanCommand(from) || role < CompanionRole.Warrior || role > CompanionRole.Archer || Spell != null ||
                Combatant != null || from.Combatant != null || Aggressors.Count > 0 || Aggressed.Count > 0 || from.Aggressors.Count > 0 || from.Aggressed.Count > 0) return false;
            if (role == _role) return true;
            var hands = new List<Item>();
            var one = FindItemOnLayer(Layer.OneHanded); var two = FindItemOnLayer(Layer.TwoHanded);
            if (one != null) hands.Add(one); if (two != null) hands.Add(two);
            int count = 0, weight = 0;
            foreach (var item in hands)
            {
                if (!Backpack.CheckHold(this, item, false, true, count, weight)) return false;
                count += item.TotalItems + 1; weight += item.TotalWeight + item.PileWeight;
            }
            foreach (var item in hands) Backpack.DropItem(item);
            if (role == CompanionRole.Warrior)
            {
                if (_roleSword == null || _roleSword.Deleted) _roleSword = new Longsword { Movable = false };
                if (_roleShield == null || _roleShield.Deleted) _roleShield = new MetalShield { Movable = false };
                AddItem(_roleSword); AddItem(_roleShield);
            }
            else if (role == CompanionRole.Archer)
            {
                if (_roleBow == null || _roleBow.Deleted) _roleBow = new Bow { Movable = false };
                AddItem(_roleBow);
            }
            _role = role; RangeFight = role == CompanionRole.Warrior ? 1 : 6;
            _companionAI = null; ChangeAIType(AIType.AI_Melee);
            return SetOrder(from, OrderType.Follow);
        }

        public bool IsOwner(Mobile from) { return from != null && !from.Deleted && from == _owner && !Deleted; }
        public bool CanCommand(Mobile from)
        {
            return IsOwner(from) && Controlled && ControlMaster == from && from.Alive && Alive && !IsDeadPet && !OnMission && from.Map == Map && Map != Map.Internal && from.InRange(this, 14) && from.InLOS(this);
        }
        internal void PrepareFollowSpeed()
        {
            if (_owner == null || !Controlled || ControlMaster != _owner || OnMission) return;
            // Native AdjustSpeeds recalculates from Dex on order changes. Apply a travel pace
            // here so all roles and existing saved companions can keep up with their owner.
            double pace = _owner.Mounted || _owner.Flying ? 0.10 : 0.15;
            ActiveSpeed = Math.Min(ActiveSpeed, pace);
            CurrentSpeed = ActiveSpeed;
        }
        public bool CanOpenPack(Mobile from) { return CanCommand(from) && from.InRange(this, 2); }
        public override bool CheckControlChance(Mobile from) { return IsOwner(from); }
        public override bool CanBeControlledBy(Mobile from) { return IsOwner(from); }
        public override double GetControlChance(Mobile from, bool useBaseSkill) { return IsOwner(from) ? 1.0 : 0.0; }
        public override bool CanTransfer(Mobile from) { return false; }
        public override bool CanBeRenamedBy(Mobile from) { return IsOwner(from); }
        public override void OnRelease(Mobile from) { }
        public override OrderType ControlOrder
        {
            get { return base.ControlOrder; }
            set
            {
                if (value == OrderType.Release || value == OrderType.Transfer || value == OrderType.Friend || value == OrderType.Unfriend || value == OrderType.Drop) return;
                if (base.ControlOrder != value) base.ControlOrder = value;
            }
        }

        public bool SetOrder(Mobile from, OrderType order)
        {
            if (!CanCommand(from) || (order != OrderType.Follow && order != OrderType.Guard && order != OrderType.Stay && order != OrderType.Stop)) return false;
            var casting = Spell as Server.Spells.Spell;
            if (casting != null) casting.Disturb(Server.Spells.DisturbType.NewCast);
            if (Target != null) Target.Cancel(this, TargetCancelType.Canceled);
            Combatant = null; FocusMob = null; Warmode = false;
            ControlTarget = order == OrderType.Stay || order == OrderType.Stop ? null : from;
            ControlOrder = order;
            return true;
        }

        public bool Attack(Mobile from, Mobile target)
        {
            if (!CanCommand(from) || target == null || target == this || target == from || target.Deleted || !target.Alive || target.Map != Map ||
                !from.InRange(target, 12) || !InRange(target, 12) || !from.InLOS(target) || !InLOS(target) || !from.CanBeHarmful(target, false) || !CanBeHarmful(target, false)) return false;
            from.DoHarmful(target);
            ControlTarget = target; ControlOrder = OrderType.Attack; Combatant = target;
            return true;
        }

        internal bool ValidAutomaticTarget(Mobile target)
        {
            var creature = target as BaseCreature;
            return _owner != null && target != null && target != this && target != _owner && !target.Deleted && target.Alive &&
                creature != null && !creature.Controlled && !creature.Summoned && !creature.Tamable && !creature.IsDeadPet &&
                target.Map == Map && _owner.Map == Map && _owner.InRange(target, 10) && InRange(target, 12) && InLOS(target) && CanSee(target) &&
                IsHostileNotoriety(target) && _owner.CanBeHarmful(target, false) && CanBeHarmful(target, false);
        }

        private bool IsHostileNotoriety(Mobile target)
        {
            int notoriety = Notoriety.Compute(_owner, target);
            return notoriety == Notoriety.Enemy || notoriety == Notoriety.Murderer || notoriety == Notoriety.CanBeAttacked;
        }

        public bool JoinOwnerParty(Mobile from)
        {
            if (!CanCommand(from)) return false;
            var party = CompanionParty.Get(from);
            var current = CompanionParty.Get(this);
            if (current != null) return current == party;
            if (party != null && (party.Leader != from || party.Count >= CompanionParty.Capacity)) return false;
            if (party == null) { party = new CompanionParty(from); from.Party = party; }
            party.Add(this);
            return CompanionParty.Get(this) == party;
        }

        internal Mobile ClosestHostile()
        {
            if (_owner == null || _owner.Map != Map || Map == Map.Internal) return null;
            Mobile best = null;
            double distance = Double.MaxValue;
            var nearby = _owner.GetMobilesInRange(10);
            try { foreach (Mobile target in nearby) if (ValidAutomaticTarget(target)) { double d = target.GetDistanceToSqrt(_owner); if (d < distance) { best = target; distance = d; } } }
            finally { nearby.Free(); }
            return best;
        }

        public bool HealOwner(Mobile from)
        {
            if (!CanCommand(from) || from.Hits >= from.HitsMax || from.Poisoned || MortalStrike.IsWounded(from) || Spell != null || DateTime.UtcNow < _nextHeal || Mana < 11) return false;
            var spell = new CompanionHealSpell(this, from);
            if (!spell.Cast()) return false;
            _nextHeal = DateTime.UtcNow.AddSeconds(8);
            return true;
        }

        public override void OnThink()
        {
            if (_owner != null && _owner.NetState != null) RecoverFromDeath(DateTime.UtcNow);
            base.OnThink();
            if (_owner != null && _owner.NetState != null)
            {
                if (_owner.Hits < _owner.HitsMax * 0.65 && HealOwner(_owner)) return;
                if (Hits < HitsMax * 0.8) HealSelf();
            }
        }

        internal bool HealSelf()
        {
            if (Deleted || !Alive || IsDeadPet || OnMission || IsStabled || Map == null || Map == Map.Internal ||
                Hits >= HitsMax || Poisoned || MortalStrike.IsWounded(this) || Spell != null ||
                DateTime.UtcNow < _nextHeal || Mana < 11) return false;
            if (!new CompanionHealSpell(this, this).Cast()) return false;
            _nextHeal = DateTime.UtcNow.AddSeconds(4);
            return true;
        }

        private DateTime _reviveAt;
        internal bool RecoverFromDeath(DateTime now)
        {
            if (!IsDeadPet) { _reviveAt = DateTime.MinValue; return false; }
            if (_reviveAt == DateTime.MinValue) _reviveAt = now.AddSeconds(5);
            if (_owner == null || _owner.Deleted || ControlMaster != _owner || IsStabled || OnMission ||
                Map == null || Map == Map.Internal || _owner.Map != Map || !InRange(_owner, 18) || now < _reviveAt) return false;
            ResurrectPet();
            Hits = HitsMax; Stam = StamMax; Mana = ManaMax;
            Combatant = null; FocusMob = null; Warmode = false;
            ControlTarget = _owner; ControlOrder = OrderType.Follow;
            _reviveAt = DateTime.MinValue;
            AIObject.Activate();
            _owner.SendMessage("Your companion has recovered and is ready to help again.");
            return true;
        }

        public override bool HandlesOnSpeech(Mobile from) { return CanCommand(from); }
        public override void OnSpeech(SpeechEventArgs e)
        {
            // Do not swallow "all" speech: other pets must hear it too.
            if (!CanCommand(e.Mobile)) return;
            string speech = e.Speech.Trim().ToLowerInvariant();
            string prefix = Name.ToLowerInvariant() + " ";
            if (speech.StartsWith("all ")) speech = speech.Substring(4);
            else if (speech.StartsWith(prefix)) speech = speech.Substring(prefix.Length);
            else return;
            switch (speech)
            {
                case "follow me": SetOrder(e.Mobile, OrderType.Follow); break;
                case "guard me": SetOrder(e.Mobile, OrderType.Guard); break;
                case "stay": case "stop": SetOrder(e.Mobile, OrderType.Stay); break;
                case "heal me": HealOwner(e.Mobile); break;
                case "kill": case "attack": AIObject.BeginPickTarget(e.Mobile, OrderType.Attack); break;
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<Server.ContextMenus.ContextMenuEntry> list)
        {
            // The prototype's order gump deliberately omits release, transfer and drop-all.
        }
        public override void OnDoubleClick(Mobile from) { Show(from); }
        public void Show(Mobile from, bool expanded = false)
        {
            if (!IsOwner(from)) return;
            from.CloseGump(typeof(CompanionGump));
            from.CloseGump(typeof(CompanionActivityGump));
            from.CloseGump(typeof(CompanionResourceMissionGump));
            from.CloseGump(typeof(CompanionMissionTimerGump));
            from.CloseGump(typeof(CompanionCombatBarGump));
            if (OnMission && !expanded) from.SendGump(new CompanionMissionTimerGump(this,from));
            else from.SendGump(new CompanionGump(this));
        }
        public override bool IsSnoop(Mobile from) { return !CanOpenPack(from) && base.IsSnoop(from); }
        public void OpenPack(Mobile from)
        {
            if (CanOpenPack(from)) Backpack.DisplayTo(from);
            else if (IsOwner(from)) from.SendMessage("Come within two tiles to use the prototype's pack.");
        }
        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return CanOpenPack(from) && item != Backpack && item.IsChildOf(Backpack);
        }
        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            return CanOpenPack(from) && (target == Backpack || target.IsChildOf(Backpack));
        }
        public override bool OnDragDrop(Mobile from, Item item)
        {
            return CanOpenPack(from) && Backpack.TryDropItem(from, item, false);
        }

        public bool StartMission(Mobile from, int minutes)
        {
            return StartMission(from, minutes, CompanionMission.Supply);
        }
        public bool StartMission(Mobile from, int minutes, CompanionMission kind)
        {
            if (!CanCommand(from) || Combatant != null || from.Combatant != null || Aggressors.Count > 0 || Aggressed.Count > 0 ||
                from.Aggressors.Count > 0 || from.Aggressed.Count > 0 || Spell != null ||
                (minutes != 5 && minutes != 15 && minutes != 30) || _pendingGold > Int32.MaxValue - minutes * 100) return false;
            if (!PrepareResourceMission(kind, minutes)) return false;
            _missionMinutes = minutes;
            _missionDue = DateTime.UtcNow.AddMinutes(minutes);
            Combatant = null; ControlTarget = null; ControlOrder = OrderType.Stay;
            Internalize();
            ScheduleMission();
            return true;
        }

        private void ScheduleMission()
        {
            if (_missionTimer != null) _missionTimer.Stop();
            _missionTimer = null;
            if (!OnMission || Deleted) return;
            var delay = _missionDue - DateTime.UtcNow;
            _missionTimer = Timer.DelayCall(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, CompleteDueMission);
        }
        private void CompleteDueMission()
        {
            if (Deleted || !OnMission) return;
            if (DateTime.UtcNow < _missionDue) { ScheduleMission(); return; }
            int gold = _missionMinutes * 100;
            _missionDue = DateTime.MinValue;
            _missionTimer = null;
            _completedMissions++;
            _pendingGold += gold;
            _lastReport = FinishResourceMission() + " " + _missionMinutes + " minutes; " + gold + " gold earned. Completed runs: " + _completedMissions + ".";
            _lastReport += " " + HavenMarks.Award(_owner,_missionMinutes*2) + " Haven Marks earned.";
            DeliverRewards();
            _missionReturnPending = true;
            if (_owner != null && _owner.NetState != null) _owner.SendMessage(_lastReport);
            CheckMissionReturn();
        }
        public void DeliverRewards()
        {
            DeliverResourceRewards();
            if (Backpack == null || _pendingGold <= 0) return;
            foreach (Item item in Backpack.Items)
            {
                var stack = item as Gold;
                if (stack == null || stack.Amount >= 60000) continue;
                int amount = Math.Min(60000 - stack.Amount, _pendingGold);
                stack.Amount += amount; _pendingGold -= amount;
                if (_pendingGold == 0) return;
            }
            while (_pendingGold > 0)
            {
                var gold = new Gold(Math.Min(60000, _pendingGold));
                if (!Backpack.CheckHold(this, gold, false, true)) { gold.Delete(); return; }
                Backpack.DropItem(gold);
                _pendingGold -= gold.Amount;
            }
        }

        public bool Recall(Mobile from)
        {
            if (!IsOwner(from) || !from.Alive || !Alive || IsDeadPet || IsStabled || from.Map == null || from.Map == Map.Internal ||
                from.Combatant != null || Combatant != null || from.Aggressors.Count > 0 || from.Aggressed.Count > 0 || Aggressors.Count > 0 || Aggressed.Count > 0) return false;
            if (!Controlled && !SetControlMaster(from)) return false;
            if (ControlMaster != from) return false;
            if (OnMission)
            {
                if (DateTime.UtcNow >= _missionDue) CompleteDueMission();
                else
                {
                    if (_missionTimer != null) _missionTimer.Stop();
                    _missionTimer = null;
                    _missionDue = DateTime.MinValue;
                    _scheduledResources.Clear();
                    _lastReport = _missionKind + " mission recalled early. No completion rewards or mission training were awarded.";
                    from.SendMessage(_lastReport);
                }
            }
            MoveToWorld(from.Location, from.Map);
            DeliverRewards();
            if (!SetOrder(from, OrderType.Follow)) return false;
            AIObject.NextMove = Core.TickCount;
            AIObject.Activate();
            ClearMissionReturn();
            return true;
        }

        public override void OnAfterDelete()
        {
            ClearMissionReturn();
            if (_missionTimer != null) _missionTimer.Stop();
            _missionTimer = null;
            base.OnAfterDelete();
        }
        public HavenCompanion(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(3);
            writer.Write(_owner); writer.Write(_missionDue); writer.Write(_missionMinutes);
            writer.Write(_pendingGold); writer.Write(_completedMissions); writer.Write(_lastReport);
            writer.Write((int)_role); writer.Write(_roleSword); writer.Write(_roleShield); writer.Write(_roleBow);
            SerializeResourceMissions(writer);
            writer.Write(_missionReturnPending);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            _owner = reader.ReadMobile(); _missionDue = reader.ReadDateTime(); _missionMinutes = reader.ReadInt();
            _pendingGold = reader.ReadInt(); _completedMissions = reader.ReadInt(); _lastReport = reader.ReadString();
            if (version >= 1) { _role = (CompanionRole)reader.ReadInt(); _roleSword = reader.ReadItem(); _roleShield = reader.ReadItem(); _roleBow = reader.ReadItem(); }
            else { _roleSword = FindItemOnLayer(Layer.OneHanded); _roleShield = FindItemOnLayer(Layer.TwoHanded); }
            if (_role < CompanionRole.Warrior || _role > CompanionRole.Archer) _role = CompanionRole.Warrior;
            if (version >= 2) DeserializeResourceMissions(reader);
            _missionReturnPending = version >= 3 ? reader.ReadBool() :
                !OnMission && _completedMissions > 0 && Map == Map.Internal && !IsStabled;
            _companionAI = null; ChangeAIType(AIType.AI_Melee);
            ScheduleMission();
            if (_missionReturnPending) ScheduleMissionReturn();
        }

        private sealed class CompanionHealSpell : GreaterHealSpell
        {
            private readonly HavenCompanion _companion;
            private readonly Mobile _recipient;
            public CompanionHealSpell(HavenCompanion companion, Mobile recipient) : base(companion, null) { _companion = companion; _recipient = recipient; }
            public override void OnCast()
            {
                if ((_recipient == _companion && _companion.Alive && !_companion.IsDeadPet && !_companion.OnMission && !_companion.IsStabled) || _companion.CanCommand(_recipient)) Target(_recipient);
                else FinishSequence();
            }
        }
        internal sealed class AttackTarget : Target
        {
            private readonly HavenCompanion _companion;
            public AttackTarget(HavenCompanion companion) : base(12, false, TargetFlags.Harmful) { _companion = companion; }
            protected override void OnTarget(Mobile from, object targeted) { _companion.Attack(from, targeted as Mobile); }
        }
    }

    public class HavenCompanionAI : MeleeAI
    {
        private readonly HavenCompanion _companion;
        public HavenCompanionAI(HavenCompanion companion) : base(companion) { _companion = companion; }
        public override bool DoOrderFollow() { _companion.PrepareFollowSpeed(); return base.DoOrderFollow(); }
        public override void EndPickTarget(Mobile from, IDamageable target, OrderType order)
        {
            if (order == OrderType.Attack) _companion.Attack(from, target as Mobile);
            else if (_companion.CanCommand(from)) base.EndPickTarget(from, target, order);
        }
        public override bool DoOrderGuard()
        {
            return CompanionGuard.Run(this, _companion);
        }
    }

    internal static class CompanionGuard
    {
        public static bool Run(BaseAI ai, HavenCompanion companion)
        {
            var owner = companion.BoundOwner;
            // Continuing an existing order must not require the owner to remain in command range/LOS.
            if (!companion.IsOwner(owner) || !companion.Controlled || companion.ControlMaster != owner ||
                !owner.Alive || !companion.Alive || companion.IsDeadPet || companion.OnMission ||
                owner.Map != companion.Map || companion.Map == Map.Internal) return true;
            var target = companion.ClosestHostile(); companion.Combatant = target; companion.FocusMob = target;
            if (target != null) { ai.Action = ActionType.Combat; return ai.Think(); }
            // Native OnCurrentOrderChanged clears ControlTarget when Guard is selected.
            // Restore it before native following, otherwise Follow changes the order to None.
            companion.ControlTarget = owner;
            companion.Warmode = false; return ai.DoOrderFollow();
        }
    }
    public class HavenCompanionMageAI : MageAI
    {
        private readonly HavenCompanion _companion;
        public HavenCompanionMageAI(HavenCompanion companion) : base(companion) { _companion = companion; }
        public override bool SmartAI { get { return true; } }
        public override bool DoOrderGuard() { return CompanionGuard.Run(this, _companion); }
        public override bool DoOrderFollow() { _companion.PrepareFollowSpeed(); return base.DoOrderFollow(); }
        public override void EndPickTarget(Mobile from, IDamageable target, OrderType order)
        {
            if (order == OrderType.Attack) _companion.Attack(from, target as Mobile);
            else if (_companion.CanCommand(from)) base.EndPickTarget(from, target, order);
        }
    }
    public class HavenCompanionArcherAI : ArcherAI
    {
        private readonly HavenCompanion _companion;
        public HavenCompanionArcherAI(HavenCompanion companion) : base(companion) { _companion = companion; }
        public override bool DoOrderGuard() { return CompanionGuard.Run(this, _companion); }
        public override bool DoOrderFollow() { _companion.PrepareFollowSpeed(); return base.DoOrderFollow(); }
        public override void EndPickTarget(Mobile from, IDamageable target, OrderType order)
        {
            if (order == OrderType.Attack) _companion.Attack(from, target as Mobile);
            else if (_companion.CanCommand(from)) base.EndPickTarget(from, target, order);
        }
    }

    public class HavenCompanionPack : Backpack
    {
        [Constructable]
        public HavenCompanionPack() { Name = "companion's pack"; Movable = false; }
        public override int DefaultMaxItems { get { return 1000; } }
        public override int DefaultMaxWeight { get { return 0; } }
        public override bool Security { get { return false; } }
        public override void OnDoubleClick(Mobile from) { var c = RootParent as HavenCompanion; if (c != null) c.OpenPack(from); }
        public override bool IsAccessibleTo(Mobile from) { var c = RootParent as HavenCompanion; return c != null && c.CanOpenPack(from); }
        public override bool CheckItemUse(Mobile from, Item item) { return IsAccessibleTo(from); }
        public HavenCompanionPack(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class CompanionGump : Gump
    {
        private readonly HavenCompanion _companion;
        public CompanionGump(HavenCompanion companion) : base(50, 50)
        {
            _companion = companion;
            AddBackground(0, 0, 480, 460, 0xA28);
            AddLabel(24, 18, 0, companion.Name + " — Companion");
            Button(330,18,12,"Combat");
            AddLabel(24, 45, 0, "HP " + companion.Hits + "/" + companion.HitsMax + "    Mana " + companion.Mana + "/" + companion.ManaMax);
            AddLabel(24, 72, 0, companion.OnMission ? companion.MissionKind + ": " + Math.Max(0, Math.Ceiling((companion.MissionDue - DateTime.UtcNow).TotalMinutes)) + " minutes left" : companion.Role + " | Orders: " + companion.ControlOrder);
            Button(24, 110, 1, "Follow"); Button(250, 110, 2, "Guard");
            Button(24, 148, 3, "Stay"); Button(250, 148, 4, "Attack...");
            Button(24, 186, 5, "Heal me"); Button(250, 186, 6, "Open pack");
            Button(24, 224, 7, "Recall"); Button(250, 224, 8, "Roles / missions");
            AddLabel(24, 265, 0, "Last mission report");
            Button(250, 262, 10, "Join my party");
            AddHtml(24, 291, 430, 76, "<BASEFONT COLOR=#202020>" + companion.LastReport + "</BASEFONT>", false, true);
            AddLabel(24, 379, 0, "Gold waiting for pack space: " + companion.PendingGold);
            Button(24, 417, 9, "Refresh / collect"); Button(330, 417, 0, "Close");
            if(companion.OnMission) Button(330,72,11,"Minimize");
        }
        private void Button(int x, int y, int id, string text) { AddButton(x, y, 0xFA5, 0xFA7, id, GumpButtonType.Reply, 0); AddLabel(x + 34, y, 0, text); }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;
            if (!_companion.IsOwner(from) || info.ButtonID == 0) return;
            bool ok = true;
            switch (info.ButtonID)
            {
                case 1: ok = _companion.SetOrder(from, OrderType.Follow); break;
                case 2: ok = _companion.SetOrder(from, OrderType.Guard); break;
                case 3: ok = _companion.SetOrder(from, OrderType.Stay); break;
                case 4: if (_companion.CanCommand(from)) from.Target = new HavenCompanion.AttackTarget(_companion); else ok = false; break;
                case 5: ok = _companion.HealOwner(from); break;
                case 6: _companion.OpenPack(from); break;
                case 7: ok = _companion.Recall(from); break;
                case 8: from.SendGump(new CompanionActivityGump(_companion)); return;
                case 9: _companion.DeliverRewards(); break;
                case 10: ok = _companion.JoinOwnerParty(from); break;
                case 11: _companion.Show(from); return;
                case 12: _companion.ShowCombatBar(from); return;
            }
            if (!ok) from.SendMessage("That action is unavailable. Check distance, combat, health or mission status.");
            _companion.Show(from);
        }
    }
    public class CompanionActivityGump : Gump
    {
        private readonly HavenCompanion _companion;
        public CompanionActivityGump(HavenCompanion companion) : base(70,70)
        {
            _companion = companion; AddBackground(0,0,500,460,0xA28);
            AddLabel(24,20,0,companion.Name + " - missions");
            AddLabel(24,55,0,"Current role: " + companion.Role);
            Button(24,100,1,"Warrior"); Button(185,100,2,"Caster"); Button(340,100,3,"Archer");
            AddHtml(24,145,450,60,"<BASEFONT COLOR=#202020>Change roles outside combat. All roles can heal you. Caster uses native Magery; Archer uses a bow.</BASEFONT>",false,false);
            AddLabel(24,225,0,"Supply mission - 100 gold per minute");
            Button(24,265,10,"5 minutes"); Button(185,265,11,"15 minutes"); Button(340,265,12,"30 minutes");
            AddLabel(24,315,0,"Missions finish while offline. Use Recall on return.");
            Button(24,355,20,"Resource missions"); Button(275,355,21,"Resource ledger");
            Button(24,410,0,"Back");
        }
        private void Button(int x,int y,int id,string label) { AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0); AddLabel(x+34,y,0,label); }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile; if (!_companion.IsOwner(from)) return;
            bool ok = true;
            if (info.ButtonID >= 1 && info.ButtonID <= 3) ok = _companion.SetRole(from,(CompanionRole)(info.ButtonID-1));
            else if (info.ButtonID >= 10 && info.ButtonID <= 12) ok = _companion.StartMission(from,info.ButtonID == 10 ? 5 : info.ButtonID == 11 ? 15 : 30);
            else if (info.ButtonID == 20) { from.SendGump(new CompanionResourceMissionGump(_companion,5)); return; }
            else if (info.ButtonID == 21) { _companion.OpenResourceLedger(from); return; }
            if (!ok) from.SendMessage("Unavailable: check distance, combat, casting, mission status and pack space.");
            _companion.Show(from);
        }
    }
}


