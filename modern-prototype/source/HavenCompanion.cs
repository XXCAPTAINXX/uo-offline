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
    // First ServUO vertical slice, not a deserializer for existing ModernUO saves.
    public class HavenCompanion : BaseCreature
    {
        private Mobile _owner;
        private HavenCompanionAI _companionAI;
        private Timer _missionTimer;
        private DateTime _missionDue;
        private int _missionMinutes;
        private int _pendingGold;
        private int _completedMissions;
        private string _lastReport = "No missions completed yet.";
        private DateTime _nextHeal;

        public Mobile BoundOwner { get { return _owner; } }
        public bool OnMission { get { return _missionDue != DateTime.MinValue; } }
        public DateTime MissionDue { get { return _missionDue; } }
        public int PendingGold { get { return _pendingGold; } }
        public int CompletedMissions { get { return _completedMissions; } }
        public string LastReport { get { return _lastReport; } }
        public override bool CanAutoStable { get { return false; } }
        public override bool AllowNewPetFriend { get { return false; } }
        public override bool KeepsItemsOnDeath { get { return true; } }
        protected override BaseAI ForcedAI { get { return _companionAI ?? (_companionAI = new HavenCompanionAI(this)); } }

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
            Name = "Alden Ashford";
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
            AddItem(new ChainChest { Movable = false }); AddItem(new ChainLegs { Movable = false });
            AddItem(new Boots { Movable = false }); AddItem(new MetalShield { Movable = false });
            AddItem(new Longsword { Movable = false }); AddItem(new Cloak(0x59B) { Movable = false });
            AddItem(new HavenCompanionPack());
        }

        public bool IsOwner(Mobile from) { return from != null && !from.Deleted && from == _owner && !Deleted; }
        public bool CanCommand(Mobile from)
        {
            return IsOwner(from) && Controlled && ControlMaster == from && from.Alive && Alive && !IsDeadPet && !OnMission && from.Map == Map && Map != Map.Internal && from.InRange(this, 14) && from.InLOS(this);
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
            base.OnThink();
            if (_owner != null && _owner.NetState != null && _owner.Hits < _owner.HitsMax * 0.65) HealOwner(_owner);
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
        public void Show(Mobile from)
        {
            if (!IsOwner(from)) return;
            from.CloseGump(typeof(CompanionGump));
            from.SendGump(new CompanionGump(this));
        }
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
            if (!CanCommand(from) || Combatant != null || from.Combatant != null || Aggressors.Count > 0 || Aggressed.Count > 0 ||
                from.Aggressors.Count > 0 || from.Aggressed.Count > 0 || (minutes != 5 && minutes != 15 && minutes != 30)) return false;
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
            _lastReport = "Supply run completed: " + _missionMinutes + " minutes; " + gold + " gold earned. Completed runs: " + _completedMissions + ".";
            DeliverRewards();
            if (_owner != null && _owner.NetState != null) { _owner.SendMessage(_lastReport); _owner.SendMessage("Use [c and Recall to bring your companion back."); }
        }
        public void DeliverRewards()
        {
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
            if (!IsOwner(from) || !from.Alive || !Alive || IsDeadPet || IsStabled || OnMission || from.Map == null || from.Map == Map.Internal ||
                from.Combatant != null || Combatant != null || from.Aggressors.Count > 0 || from.Aggressed.Count > 0 || Aggressors.Count > 0 || Aggressed.Count > 0) return false;
            if (!Controlled && !SetControlMaster(from)) return false;
            if (ControlMaster != from) return false;
            MoveToWorld(from.Location, from.Map);
            DeliverRewards();
            return SetOrder(from, OrderType.Follow);
        }

        public override void OnAfterDelete()
        {
            if (_missionTimer != null) _missionTimer.Stop();
            _missionTimer = null;
            base.OnAfterDelete();
        }
        public HavenCompanion(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(_owner); writer.Write(_missionDue); writer.Write(_missionMinutes);
            writer.Write(_pendingGold); writer.Write(_completedMissions); writer.Write(_lastReport);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            _owner = reader.ReadMobile(); _missionDue = reader.ReadDateTime(); _missionMinutes = reader.ReadInt();
            _pendingGold = reader.ReadInt(); _completedMissions = reader.ReadInt(); _lastReport = reader.ReadString();
            ScheduleMission();
        }

        private sealed class CompanionHealSpell : GreaterHealSpell
        {
            private readonly HavenCompanion _companion;
            private readonly Mobile _recipient;
            public CompanionHealSpell(HavenCompanion companion, Mobile recipient) : base(companion, null) { _companion = companion; _recipient = recipient; }
            public override void OnCast()
            {
                if (_companion.CanCommand(_recipient)) Target(_recipient);
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
        public override void EndPickTarget(Mobile from, IDamageable target, OrderType order)
        {
            if (order == OrderType.Attack) _companion.Attack(from, target as Mobile);
            else if (_companion.CanCommand(from)) base.EndPickTarget(from, target, order);
        }
        public override bool DoOrderGuard()
        {
            if (!_companion.CanCommand(_companion.BoundOwner)) return true;
            var target = _companion.ClosestHostile();
            _companion.Combatant = target;
            _companion.FocusMob = target;
            if (target != null) { Action = ActionType.Combat; return Think(); }
            _companion.Warmode = false;
            return DoOrderFollow();
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
            AddLabel(24, 18, 0, "Alden Ashford — ServUO prototype");
            AddLabel(24, 45, 0, "HP " + companion.Hits + "/" + companion.HitsMax + "    Mana " + companion.Mana + "/" + companion.ManaMax);
            AddLabel(24, 72, 0, companion.OnMission ? "Supply run: " + Math.Max(0, Math.Ceiling((companion.MissionDue - DateTime.UtcNow).TotalMinutes)) + " minutes left" : "Orders: " + companion.ControlOrder);
            Button(24, 110, 1, "Follow"); Button(250, 110, 2, "Guard");
            Button(24, 148, 3, "Stay"); Button(250, 148, 4, "Attack...");
            Button(24, 186, 5, "Heal me"); Button(250, 186, 6, "Open pack");
            Button(24, 224, 7, "Recall"); Button(250, 224, 8, "5-minute supply run");
            AddLabel(24, 265, 0, "Last mission report");
            Button(250, 262, 10, "Join my party");
            AddHtml(24, 291, 430, 76, "<BASEFONT COLOR=#202020>" + companion.LastReport + "</BASEFONT>", false, true);
            AddLabel(24, 379, 0, "Gold waiting for pack space: " + companion.PendingGold);
            Button(24, 417, 9, "Refresh / collect"); Button(330, 417, 0, "Close");
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
                case 8: ok = _companion.StartMission(from, 5); break;
                case 9: _companion.DeliverRewards(); break;
                case 10: ok = _companion.JoinOwnerParty(from); break;
            }
            if (!ok) from.SendMessage("That action is unavailable. Check distance, combat, health or mission status.");
            _companion.Show(from);
        }
    }
}
