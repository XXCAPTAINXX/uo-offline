using System;
using ModernUO.Serialization;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using CompanionParty = Server.Engines.PartySystem.Party;

namespace Server.UOOffline;

public static class HavenCompanions
{
    public static void Initialize() => CommandSystem.Register("Companion", AccessLevel.Player, e =>
    {
        var companion = ClaimOrRecall(e.Mobile);
        if (companion != null) { HavenCompanionGump.DisplayTo(e.Mobile, companion); }
    });

    internal static HavenCompanion ClaimOrRecall(Mobile from)
    {
        if (from?.Deleted != false || !from.Player || from.Account is not Account account ||
            from.Map == null || from.Map == Map.Internal) { return null; }
        var key = $"HavenCompanion:{from.Serial}";
        var tag = account.GetTag(key);
        HavenCompanion companion = null;
        var created = false;
        if (Serial.TryParse(tag, null, out var serial)) { companion = World.FindMobile(serial) as HavenCompanion; }
        if (companion?.Deleted == false)
        {
            if (companion.BoundOwner != from || (companion.ControlMaster != null && companion.ControlMaster != from))
            {
                from.SendMessage("This companion belongs to another adventurer.");
                return null;
            }
            if (companion.IsStabled)
            {
                if (from is not PlayerMobile player || companion.StabledBy != from || player.Stabled?.Contains(companion) != true)
                {
                    from.SendMessage("Your companion's stable record needs attention from a game master.");
                    return null;
                }
            }
            else if (companion.Map == Map.Internal)
            {
                from.SendMessage("Your companion is stabled or stored. Claim them from the stable or use their pet statue first.");
                return null;
            }
        }
        else
        {
            if (!from.Alive)
            {
                from.SendMessage("Visit the healer before claiming your first companion.");
                return null;
            }
            if (from.Followers >= from.FollowersMax)
            {
                from.SendMessage("Your companion needs one free follower slot.");
                return null;
            }
            companion = new HavenCompanion { BoundOwner = from };
            created = true;
        }
        if (!companion.Controlled && !companion.SetControlMaster(from))
        {
            if (created) { companion.Delete(); }
            return null;
        }
        if (companion.IsStabled && from is PlayerMobile owner)
        {
            owner.RemoveStabled(companion);
            owner.AutoStabled?.Remove(companion);
            companion.IsStabled = false;
            companion.StabledBy = null;
        }
        companion.IsBonded = true;
        companion.UpdateTraining(Core.Now);
        companion.Loyalty = BaseCreature.MaxLoyalty;
        companion.MoveToWorld(from.Location, from.Map);
        companion.ControlTarget = from;
        companion.ControlOrder = OrderType.Follow;
        account.SetTag(key, companion.Serial.ToString());
        from.SendMessage("Your permanent companion is here. Double-click them or use [companion for orders, roles, support and storage.");
        if (companion.IsDeadPet) { from.SendMessage("Your companion has fallen. Visit Mira at New Haven bank or a stable master for free resurrection."); }
        return companion;
    }
}

public enum HavenCompanionRole { Fighter, Healer, Bard }

[SerializationGenerator(0)]
public partial class HavenCompanion : BaseCreature
{
    [SerializableField(0)]
    private Mobile _boundOwner;

    [SerializableField(1)]
    private double _trainingMinutes;

    [SerializableField(2)]
    private DateTime _lastTraining = Core.Now;

    [SerializableField(3)]
    private HavenCompanionRole _role;

    private DateTime _nextSupport = Core.Now;
    private DateTime _nextTraining = Core.Now;
    private DateTime _nextCombatTraining = Core.Now;
    private DateTime _nextBuff = Core.Now;

    public double Mastery => 75.0 + Math.Sqrt(Math.Max(0, TrainingMinutes)) / 2.0;
    public double TrainingLevel => 1.0 + Math.Floor(Math.Sqrt(Math.Max(0, TrainingMinutes) / 60.0));

    [Constructible]
    public HavenCompanion() : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        Name = "Alden Ashford";
        Title = "the faithful companion";
        Body = 0x190;
        Hue = 0x83EA;
        ControlSlots = 1;
        SkillsCap = int.MaxValue;
        Tamable = false;
        MinTameSkill = 0;
        SetStr(100);
        SetDex(80);
        SetInt(60);
        SetHits(180);
        SetDamage(8, 12);
        SetResistance(ResistanceType.Physical, 40);
        SetResistance(ResistanceType.Fire, 25);
        SetResistance(ResistanceType.Cold, 25);
        SetResistance(ResistanceType.Poison, 25);
        SetResistance(ResistanceType.Energy, 25);
        SetSkill(SkillName.Swords, 75);
        SetSkill(SkillName.Tactics, 75);
        SetSkill(SkillName.Anatomy, 65);
        SetSkill(SkillName.MagicResist, 65);
        AddItem(new ChainChest { Movable = false });
        AddItem(new ChainLegs { Movable = false });
        AddItem(new Boots { Movable = false });
        AddItem(new Longsword { Movable = false });
        AddItem(new MetalShield { Movable = false });
        AddItem(new Cloak(0x59B) { Movable = false });
        AddItem(new HavenCompanionPack());
    }

    internal void UpdateTraining(DateTime now)
    {
        if (now <= LastTraining) { return; }
        TrainingMinutes += (now - LastTraining).TotalMinutes;
        LastTraining = now;
        ApplyGrowth();
    }

    private void ApplyGrowth()
    {
        // Native skill storage is a ushort in tenths. Mastery remains separate
        // and continues powering combat/support after the native display fills.
        foreach (var skill in Skills)
        {
            skill.Cap = 6553.5;
            skill.Base = Math.Min(6553.5, Mastery);
        }
        var bonus = (int)Math.Min(100000000, TrainingLevel * 2);
        var currentHits = Hits;
        SetHits(180 + bonus * 2);
        Hits = Math.Min(currentHits, HitsMax);
        SetDamage(8 + bonus / 2, 12 + bonus);
    }

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);
        if (damage > 0 && Core.Now >= _nextCombatTraining)
        {
            _nextCombatTraining = Core.Now + TimeSpan.FromSeconds(10);
            TrainingMinutes += 1;
        }
    }

    public override void OnDoubleClick(Mobile from) => HavenCompanionGump.DisplayTo(from, this);

    public override bool CheckNonlocalLift(Mobile from, Item item) =>
        from == BoundOwner && item.IsChildOf(Backpack) && from.Map == Map && from.InRange(this, 3);

    public override bool CheckNonlocalDrop(Mobile from, Item item, Item target) =>
        from == BoundOwner && (target == Backpack || target.IsChildOf(Backpack)) && from.Map == Map && from.InRange(this, 3);

    public override bool OnDragDrop(Mobile from, Item dropped) =>
        from == BoundOwner && from.Map == Map && from.InRange(this, 3) && Backpack.TryDropItem(from, dropped, false);

    internal bool Support(Mobile patient)
    {
        if (IsDeadPet || !Alive || patient?.Deleted != false || patient.Map != Map || !InRange(patient, 3) ||
            !InLOS(patient) || Core.Now < _nextSupport || Mana < 10) { return false; }
        if (patient != BoundOwner && (CompanionParty.Get(BoundOwner)?.Contains(patient) != true || !patient.Player)) { return false; }
        if (!patient.Alive)
        {
            if (!patient.Map.CanFit(patient.Location, 16, false, false)) { return false; }
            patient.CloseGump<ResurrectGump>();
            patient.SendGump(new ResurrectGump(patient, ResurrectMessage.Healer));
            _nextSupport = Core.Now + TimeSpan.FromSeconds(30);
        }
        else
        {
            if (patient.Poisoned) { patient.CurePoison(this); }
            else
            {
                if (patient.Hits >= patient.HitsMax) { return false; }
                patient.Heal((int)Math.Min(100000000, (Role == HavenCompanionRole.Healer ? 25 : 10) + TrainingLevel));
            }
            _nextSupport = Core.Now + TimeSpan.FromSeconds(Role == HavenCompanionRole.Healer ? 8 : 20);
        }
        Mana -= 10;
        patient.FixedEffect(0x376A, 10, 16);
        patient.PlaySound(0x1F2);
        return true;
    }

    internal void JoinParty(Mobile from)
    {
        if (from != BoundOwner) { return; }
        var party = CompanionParty.Get(from);
        if (party == null)
        {
            if (from.Party != null) { from.SendMessage("Respond to your pending party invitation first."); return; }
            party = new CompanionParty(from);
            from.Party = party;
        }
        if (party.Contains(this)) { from.SendMessage("I am already in your party."); return; }
        if (party.Leader != from || party.Count >= CompanionParty.Capacity)
        {
            from.SendMessage("You must lead a party with a free space to add your companion.");
            return;
        }
        CompanionParty.Get(this)?.Remove(this);
        party.Add(this);
    }

    internal void BardSupport(Mobile patient)
    {
        if (!patient.Alive || patient.Map != Map || !InRange(patient, 8) || !InLOS(patient)) { return; }
        var amount = 5 + (int)Math.Log2(1 + Math.Max(0, TrainingMinutes) / 60);
        patient.RemoveStatMod("HavenCompanionSongStr");
        patient.RemoveStatMod("HavenCompanionSongDex");
        patient.RemoveStatMod("HavenCompanionSongInt");
        patient.AddStatMod(new StatMod(StatType.Str, "HavenCompanionSongStr", amount, TimeSpan.FromSeconds(20)));
        patient.AddStatMod(new StatMod(StatType.Dex, "HavenCompanionSongDex", amount, TimeSpan.FromSeconds(20)));
        patient.AddStatMod(new StatMod(StatType.Int, "HavenCompanionSongInt", amount, TimeSpan.FromSeconds(20)));
    }

    public override bool KeepsItemsOnDeath => true;
    public override bool CanDrop => false;
    public override bool DeleteOnRelease => false;
    public override bool CanBeDamaged() => (Controlled || BoundOwner == null) && base.CanBeDamaged();
    public override TimeSpan BondingAbandonDelay => TimeSpan.FromDays(36500);
    public override bool CheckControlChance(Mobile m) => m == BoundOwner;
    public override bool CanBeControlledBy(Mobile m) => m == BoundOwner;

    public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness) =>
        target?.Player != true && target is not BaseCreature { ControlMaster.Player: true } &&
        base.CanBeHarmful(target, message, ignoreOurBlessedness);

    public override void OnThink()
    {
        base.OnThink();
        if (Controlled) { Loyalty = MaxLoyalty; }
        if (Core.Now >= _nextTraining)
        {
            _nextTraining = Core.Now + TimeSpan.FromMinutes(1);
            UpdateTraining(Core.Now);
        }
        if (IsDeadPet || BoundOwner?.NetState == null || BoundOwner.Map != Map || !InRange(BoundOwner, 12)) { return; }
        if (ControlOrder == OrderType.Attack && (ControlTarget == null || ControlTarget.Deleted || !ControlTarget.Alive))
        {
            Combatant = null;
            ControlTarget = BoundOwner;
            ControlOrder = OrderType.Follow;
        }
        if (ControlOrder is OrderType.Follow or OrderType.Guard && BoundOwner.Combatant is Mobile enemy &&
            enemy.Alive && InRange(enemy, 10) && CanBeHarmful(enemy, false))
        {
            Combatant = enemy;
            ControlTarget = enemy;
            ControlOrder = OrderType.Attack;
        }
        Support(BoundOwner);
        var party = CompanionParty.Get(BoundOwner);
        if (party?.Contains(this) == true)
        {
            foreach (var member in party.Members)
            {
                if (member.Mobile.Player) { Support(member.Mobile); }
            }
        }
        if (Role == HavenCompanionRole.Bard && Core.Now >= _nextBuff && Mana >= 5)
        {
            _nextBuff = Core.Now + TimeSpan.FromSeconds(10);
            Mana -= 5;
            BardSupport(BoundOwner);
            if (party?.Contains(this) == true)
            {
                foreach (var member in party.Members)
                {
                    if (member.Mobile.Player) { BardSupport(member.Mobile); }
                }
            }
            PlaySound(0x45);
        }
    }

    public override void OnDelete()
    {
        CompanionParty.Get(this)?.Remove(this);
        BoundOwner = null;
        base.OnDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenCompanionPack : Backpack
{
    [Constructible]
    public HavenCompanionPack()
    {
        Name = "Companion shared pack";
        Movable = false;
        LootType = LootType.Blessed;
    }
    public override int DefaultMaxItems => 1000;
    public override int DefaultMaxWeight => 50000;
    public override bool CheckContentDisplay(Mobile from) =>
        Parent is HavenCompanion companion && companion.BoundOwner == from && from.Map == companion.Map && from.InRange(companion, 3);
    public override void OnSnoop(Mobile from) { }
}
