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
    public enum CompanionRole { Warrior, Caster, Archer, Bard, Healer }
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
                        _role == CompanionRole.Archer ? (BaseAI)new HavenCompanionArcherAI(this) : _role == CompanionRole.Healer ? (BaseAI)new HavenCompanionHealerAI(this) : new HavenCompanionAI(this);
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
            EnsureProgressionCaps();
            Name = RecruitName();
            Title = "the faithful companion";
            Body = 0x190;
            Hue = 0x83EA;
            Tamable = false;
            ControlSlots = 0;
            MinTameSkill = 0;
            SetStr(200); SetDex(150); SetInt(200); SetHits(400); SetStam(150); SetMana(300);
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
            EnsureEvolvingEquipment();
        }

        public bool SetRole(Mobile from, CompanionRole role)
        {
            if (!CanCommand(from) || role < CompanionRole.Warrior || role > CompanionRole.Healer || Spell != null ||
                HavenPreview.TravelCombatSeconds(this)>0 || HavenPreview.TravelCombatSeconds(from)>0) return false;
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
            StopTamingAssist();ClearRoleSupport();_role = role;if(role==CompanionRole.Bard)EnsureBardTools(); EnsureEvolvingEquipment(); RangeFight = role == CompanionRole.Warrior ? 1 : 6;
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
        public bool CanOpenPack(Mobile from) { return CanCommand(from) && from.InRange(this, 12); }
        public override void OnSkillChange(SkillName name,double oldBase)
        {
            base.OnSkillChange(name,oldBase);
            if (_owner == null || World.Loading || (name != SkillName.AnimalTaming && name != SkillName.AnimalLore)) return;
            var skill=Skills[name];
            if (skill.Base < oldBase)
            {
                System.IO.File.AppendAllText("companion-skill-protection.log",DateTime.UtcNow.ToString("O")+" companion="+Serial+" skill="+name+" blocked "+oldBase+" -> "+skill.Base+"\n"+Environment.StackTrace+"\n");
                skill.Base=oldBase;
            }
        }
        public override bool CheckControlChance(Mobile from) { return IsOwner(from); }
        public override bool CanBeControlledBy(Mobile from) { return IsOwner(from); }
        public override double GetControlChance(Mobile from, bool useBaseSkill) { return IsOwner(from) ? 1.0 : 0.0; }
        public override bool CanTransfer(Mobile from) { return false; }
        public override bool CanBeRenamedBy(Mobile from) { return IsOwner(from); }
        public override void OnRelease(Mobile from) { }
        // Mission parking and native pet release must never scatter player storage.
        public override void DropBackpack() { }
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
            StopTamingAssist();
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
            if(Role==CompanionRole.Healer){if(IsOwner(from))from.SendMessage("Your healer stays with the group. Choose a combat role to attack.");return false;}
            if (!CanCommand(from) || target == null || target == this || target == from || target.Deleted || !target.Alive || target.Map != Map ||
                !from.InRange(target, 12) || !InRange(target, 12) || !from.InLOS(target) || !InLOS(target) || !from.CanBeHarmful(target, false)) return false;
            _checkingPetOrder=true;try{if(!CanBeHarmful(target,false))return false;}finally{_checkingPetOrder=false;}
            StopTamingAssist();from.DoHarmful(target);_explicitPetTarget=WildCustomPet(target)?target:null;
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

        public bool HealOwner(Mobile from) { return IsOwner(from) && SupportPatient(from); }
        internal bool SupportPatient(Mobile patient) {
            if(Deleted||!Alive||IsDeadPet||OnMission||IsStabled||patient==null||patient.Deleted||Map==null||Map==Map.Internal||patient.Map!=Map||!InRange(patient,12)||!InLOS(patient)||DateTime.UtcNow<_nextHeal||Mana<10)return false;
            var pet=patient as BaseCreature;
            bool allowed=patient==this||patient==_owner||(pet!=null&&(pet.ControlMaster==_owner||pet.ControlMaster==this))||(_owner!=null&&patient.Player&&CompanionParty.Get(_owner)!=null&&CompanionParty.Get(_owner).Contains(patient));
            if(!allowed)return false;
            if(!patient.Alive) {
                if(!patient.Player||patient.Map==null||!patient.Map.CanFit(patient.Location,16,false,false)||patient.Region.IsPartOf("Khaldun")||patient.HasGump(typeof(ResurrectGump)))return false;
                patient.SendGump(new ResurrectGump(patient,this));_nextHeal=DateTime.UtcNow.AddSeconds(10);
            } else if(pet!=null&&pet.IsDeadPet) {
                if((pet.ControlMaster!=_owner&&pet.ControlMaster!=this)||!patient.Map.CanFit(patient.Location,16,false,false))return false;pet.ResurrectPet();_nextHeal=DateTime.UtcNow.AddSeconds(10);
            } else if(patient.Poisoned) {if(!patient.CurePoison(this))return false;_nextHeal=DateTime.UtcNow.AddSeconds(2);}
            else {if(patient.Hits>=patient.HitsMax||MortalStrike.IsWounded(patient))return false;patient.Heal(30+(int)(Skills.Healing.Base/5)+(Role==CompanionRole.Healer?15:0),this);_nextHeal=DateTime.UtcNow.AddSeconds(2);}
            AwardRoleHelp();Mana-=10;patient.FixedEffect(0x376A,10,16);patient.PlaySound(0x1F2);CheckSkill(SkillName.Healing,0,125);return true;
        }

        public override void OnThink()
        {
            if (_owner != null && _owner.NetState != null) RecoverFromDeath(DateTime.UtcNow);
            RecoverResources(DateTime.UtcNow);
            ThinkAssignedPets();
            RespectWildPets();
            ThinkTamingAssist();ThinkRoleSupport();ThinkBardCombat();
            if (_owner != null && _owner.NetState != null) MaintainSpellweaver();
            base.OnThink();
            if (_owner != null && _owner.NetState != null)
            {
                if (!TryBandage(_owner)) TryBandage(this);
                if (Role==CompanionRole.Healer ? HealMostUrgent() : (HealOwner(_owner) || SupportPatient(this))) return;
                var patients=GetMobilesInRange(12);try{foreach(Mobile patient in patients)if(SupportPatient(patient))break;}finally{patients.Free();}
            }
        }

        internal bool TryBandage(Mobile patient)
        {
            if (Deleted || !Alive || IsDeadPet || OnMission || IsStabled || Backpack == null ||
                patient == null || patient.Deleted || (patient != this && patient != _owner) ||
                Map == null || Map == Map.Internal || patient.Map != Map || !InRange(patient, HavenCompanionAccess.BandageRange(this, patient)) || !InLOS(patient) ||
                BandageContext.GetContext(this) != null || MortalStrike.IsWounded(patient) ||
                (patient.Alive && !patient.Poisoned && patient.Hits >= patient.HitsMax)) return false;
            var bandages = Backpack.FindItemByType(typeof(Bandage), true) as Bandage;
            if (bandages == null || bandages.Deleted || bandages.Amount < 1) return false;
            if (BandageContext.BeginHeal(this, patient) == null) return false;
            bandages.Consume(1);
            return true;
        }

        private DateTime _nextResourceRecovery;
        internal void RecoverResources(DateTime now)
        {
            if (Deleted || !Alive || IsDeadPet || OnMission || IsStabled || Map == null || Map == Map.Internal || now < _nextResourceRecovery) return;
            _nextResourceRecovery = now.AddSeconds(3);
            if (!Poisoned) Hits = Math.Min(HitsMax, Hits + 4);
            Stam = Math.Min(StamMax, Stam + 12);
            Mana = Math.Min(ManaMax, Mana + 8);
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
            if (OnMission && !expanded) from.SendGump(new CompanionMissionTimerGump(this,from));
            else { from.SendGump(new CompanionGump(this)); ShowAwayTimer(from); }
        }
        public override bool IsSnoop(Mobile from) { return !CanOpenPack(from) && base.IsSnoop(from); }
        public void OpenPack(Mobile from)
        {
            if (ShowAwayTimer(from)) return;
            if (CanOpenPack(from)) Backpack.DisplayTo(from);
            else if (IsOwner(from)) from.SendMessage("Come within twelve tiles and line of sight to use your companion's pack.");
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
            return StartMissionCore(from,minutes,kind,false);
        }
        internal bool StartOfflineMission(int minutes,CompanionMission kind){return StartMissionCore(_owner,minutes,kind,true);}
        private bool StartMissionCore(Mobile from,int minutes,CompanionMission kind,bool offline)
        {
            string reason = MissionStartError(from, minutes, kind,offline);
            if (reason != null) { if (from != null) from.SendMessage(reason); return false; }
            if (!PrepareResourceMission(kind, minutes)) { from.SendMessage("Cannot start: collect pending resources or pet tickets first; reward storage is full."); return false; }
            ClearMissionReturn();
            _missionMinutes = minutes;
            _missionDue = DateTime.UtcNow.AddMinutes(minutes);
            Combatant = null; ControlTarget = null; ControlOrder = OrderType.Stay;
            StopTamingAssist();ClearRoleSupport();ParkAssignedPets();
            Internalize();
            EnsureProgressionCaps();
            ScheduleMission();
            return true;
        }

        public string MissionStartError(Mobile from, int minutes, CompanionMission kind){return MissionStartError(from,minutes,kind,false);}
        internal string MissionStartError(Mobile from,int minutes,CompanionMission kind,bool offline)
        {
            if (!IsOwner(from)) return "This is not your companion.";
            if(offline&&(from.NetState!=null||HavenOfflineMissionPlan.Find(this)?.Enabled!=true))return "Offline missions must be enabled and the owner logged out.";
            if(IsStabled)return "Recall your companion from the stables first.";
            if (OnMission) return "Your companion is already on a mission. Recall early to cancel it first.";
            if (!from.Alive || !Alive || IsDeadPet) return "You and your companion must be alive to start a mission.";
            if (!Controlled || ControlMaster != from) return "Your companion is not currently under your control.";
            if (!offline&&(Map == Map.Internal || from.Map != Map || !from.InRange(this,14))) return "Move within 14 tiles of your companion before starting a mission.";
            if (!offline&&!from.InLOS(this)) return "Move into sight of your companion before starting a mission.";
            if (HavenPreview.TravelCombatSeconds(this)>0 || HavenPreview.TravelCombatSeconds(from)>0) return "Wait 15 seconds after the last combat action before sending a mission.";
            if (Spell != null) return "Your companion is casting. Try again when the spell finishes.";
            if (minutes != 5 && minutes != 15 && minutes != 30 && minutes != 60) return "Choose a 5, 15, 30 or 60 minute mission.";
            if (kind < CompanionMission.Supply || kind > CompanionMission.AbyssIngredients) return "That mission is unavailable.";
            if(HavenRegionalMissions.Valid(kind)&&!HavenRegionalMissions.CanStart(this,kind))return "This route needs "+HavenRegionalMissions.Requirement(kind)+" Magic Resistance AND a combat skill (Tactics, Magery or Archery).";
            if (_pendingGold > Int32.MaxValue - minutes * 100) return "Collect your companion's pending gold before starting another mission.";
            if ((kind == CompanionMission.Malas || kind == CompanionMission.Abyss) && Math.Max(Skills.Magery.Base,Skills.Tactics.Base) < (kind == CompanionMission.Malas ? 60 : 80)) return "Your companion needs " + (kind == CompanionMission.Malas ? "60" : "80") + " trained Magery or Tactics for this route.";
            if (HavenPetMissions.Valid(kind) && !HavenPetMissions.CanStart(this,kind)) return "Your companion needs " + HavenPetMissions.Requirements[(int)kind-6].ToString("0.0") + " trained Animal Taming AND Animal Lore for this pet.";
            if (HavenPetMissions.Valid(kind) && PendingPetTickets >= 50) return "Collect pending pet tickets before starting another taming mission.";
            return null;
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
            // Recall deliberately has no distance, line-of-sight or same-map requirement.
            if (!IsOwner(from)) return false;
            string blocked = !from.Alive ? "You must be alive to recall your companion." :
                !Alive || IsDeadPet ? "Your companion needs resurrection before Recall." :
                IsStabled ? "Your companion is stabled; reclaim him before Recall." :
                from.Map == null || from.Map == Map.Internal ? "Your current location is unavailable for Recall." :
                HavenPreview.TravelCombatSeconds(from)>0 ? "Your combat is still active; Recall is available after it clears." :
                HavenPreview.TravelCombatSeconds(this)>0 ? "Your companion's combat is still active; Recall is available after it clears." : null;
            if (blocked != null) { from.SendMessage(blocked); return false; }
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
                    CancelPetMission();
                    _lastReport = _missionKind + " mission recalled early. No completion rewards or mission training were awarded.";
                    from.SendMessage(_lastReport);
                }
            }
            from.CloseGump(typeof(CompanionMissionTimerGump));
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
            ClearRoleSupport();
            ClearMissionReturn();
            CancelPetMission();
            foreach(var ticket in _pendingPets.ToArray())if(ticket!=null&&!ticket.Deleted){if(_owner!=null&&!_owner.Deleted&&_owner.BankBox!=null)_owner.BankBox.DropItem(ticket);else ticket.Delete();}_pendingPets.Clear();
            if (_missionTimer != null) _missionTimer.Stop();
            _missionTimer = null;
            base.OnAfterDelete();
        }
        public HavenCompanion(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(5);
            writer.Write(_owner); writer.Write(_missionDue); writer.Write(_missionMinutes);
            writer.Write(_pendingGold); writer.Write(_completedMissions); writer.Write(_lastReport);
            writer.Write((int)_role); writer.Write(_roleSword); writer.Write(_roleShield); writer.Write(_roleBow);
            SerializeResourceMissions(writer);
            writer.Write(_missionReturnPending);
            SerializePetMissions(writer);
            writer.Write(_roleTrainingMinutes);
        }
        public void EnsureProgressionCaps() { EnsureFreeFollowerSlots(); Skills.AnimalTaming.Base=Math.Max(50,Skills.AnimalTaming.Base); for (int i = 0; i < Skills.Length; i++) Skills[i].Cap = Math.Max(125.0, Skills[i].Cap); Skills.Cap = Math.Max(Skills.Cap, Skills.Length * 1250); RawStr=Math.Max(RawStr,200); RawDex=Math.Max(RawDex,150); RawInt=Math.Max(RawInt,200); HitsMaxSeed=Math.Max(HitsMaxSeed,400); StamMaxSeed=Math.Max(StamMaxSeed,150); ManaMaxSeed=Math.Max(ManaMaxSeed,300); }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            _owner = reader.ReadMobile(); _missionDue = reader.ReadDateTime(); _missionMinutes = reader.ReadInt();
            _pendingGold = reader.ReadInt(); _completedMissions = reader.ReadInt(); _lastReport = reader.ReadString();
            if (version >= 1) { _role = (CompanionRole)reader.ReadInt(); _roleSword = reader.ReadItem(); _roleShield = reader.ReadItem(); _roleBow = reader.ReadItem(); }
            else { _roleSword = FindItemOnLayer(Layer.OneHanded); _roleShield = FindItemOnLayer(Layer.TwoHanded); }
            if (_role < CompanionRole.Warrior || _role > CompanionRole.Healer) _role = CompanionRole.Warrior;
            if (version >= 2) DeserializeResourceMissions(reader);
            _missionReturnPending = version >= 3 ? reader.ReadBool() :
                !OnMission && _completedMissions > 0 && Map == Map.Internal && !IsStabled;
            if(version>=4)DeserializePetMissions(reader);
            if(version>=5)_roleTrainingMinutes=Math.Max(0,reader.ReadDouble());
            _companionAI = null; ChangeAIType(AIType.AI_Melee);
            EnsureProgressionCaps();
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
            if(companion.Role==CompanionRole.Healer){companion.Combatant=null;companion.ControlTarget=owner;return ai.DoOrderFollow();}
            var target = companion.ClosestHostile(); companion.Combatant = target; companion.FocusMob = target;
            if (target != null) { ai.Action = ActionType.Combat; return ai.Think(); }
            // Native OnCurrentOrderChanged clears ControlTarget when Guard is selected.
            // Restore it before native following, otherwise Follow changes the order to None.
            companion.ControlTarget = owner;
            companion.Warmode = false; return ai.DoOrderFollow();
        }
    }
    public partial class HavenCompanionMageAI : MageAI
    {
        private readonly HavenCompanion _companion;
        public HavenCompanionMageAI(HavenCompanion companion) : base(companion) { _companion = companion; }
        public override bool SmartAI { get { return false; } }
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

    public class CompanionGump : HavenMenuGump
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
            Button(24, 224, 7, "Recall"); Button(250, 224, 8, "Missions");
            Button(24,262,15,"Roles");
            AddLabel(24,294,0,"Last mission report");
            Button(250, 262, 10, "Join my party");
            Button(250, 379, 13, "Stats / skills");
            AddHtml(24, 320, 430, 48, "<BASEFONT COLOR=#202020>" + companion.LastReport + "</BASEFONT>", false, true);
            AddLabel(24, 379, 0, "Pending gold: " + companion.PendingGold);
            Button(24, 417, 9, "Refresh / collect"); Button(220,417,14,"Pets"); Button(330, 417, 0, "Close");
            if(companion.OnMission) Button(330,72,11,"Minimize");
        }
        private void Button(int x, int y, int id, string text) { FlatButton(x,y,id==12||id==11||id==0?120:id==14?90:190,id,text); }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;
            if (!_companion.IsOwner(from) || info.ButtonID == 0) return;
            _companion.ShowAwayTimer(from);
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
                case 15: from.SendGump(new HavenCompanionRolesGump(_companion)); return;
                case 8: from.SendGump(new CompanionActivityGump(_companion)); return;
                case 9: _companion.DeliverRewards(); break;
                case 10: ok = _companion.JoinOwnerParty(from); break;
                case 11: _companion.Show(from); return;
                case 12: _companion.ShowCombatBar(from); return;
                case 13: from.SendGump(new HavenCompanionStatsGump(_companion)); return;
                case 14: HavenCompanionPetsGump.Show(from,_companion); return;
            }
            if (!ok) from.SendMessage("That action is unavailable. Check distance, combat, health or mission status.");
            _companion.Show(from);
        }
    }
}
