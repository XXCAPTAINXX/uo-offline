using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPetAbilities : Item
{
    [SerializableField(0)] private List<int> _learned = new();
    private DateTime _nextSong;
    private DateTime _nextAttack;
    private DateTime _nextPool;
    private HavenPetPool _pool;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenPetAbilities() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "trained pet abilities"; }
    // School, special, weapon move, area effect. Costs are in tenths of training points.
    internal static readonly string[] Names = ["Magery", "Poisoning", "Discordance", "Peacemaking", "Healing", "Fire breath",
        "Armor Ignore", "Bleed Attack", "Concussion Blow", "Crushing Blow", "Disarm", "Dismount", "Double Strike",
        "Mortal Strike", "Paralyzing Blow", "Feint", "Whirlwind Attack", "Frenzied Whirlwind", "Elemental pool (custom pets)"];
    internal static int Category(int id) => id < 4 ? 0 : id < 6 ? 1 : id < 16 ? 2 : 3;
    internal static int Cost(int id) => id < 4 ? 5000 : id == 4 ? 1000 : id == 5 || id >= 16 ? 2000 : 1000;
    internal static HavenPetAbilities Find(BaseCreature pet) => pet?.Backpack?.FindItemByType<HavenPetAbilities>();
    internal static WeaponAbility Move(int id) => id switch
    {
        6 => WeaponAbility.ArmorIgnore, 7 => WeaponAbility.BleedAttack, 8 => WeaponAbility.ConcussionBlow,
        9 => WeaponAbility.CrushingBlow, 10 => WeaponAbility.Disarm, 11 => WeaponAbility.Dismount,
        12 => WeaponAbility.DoubleStrike, 13 => WeaponAbility.MortalStrike, 14 => WeaponAbility.ParalyzingBlow,
        15 => WeaponAbility.Feint, 16 => WeaponAbility.WhirlwindAttack, 17 => WeaponAbility.FrenziedWhirlwind, _ => null
    };
    internal static bool Knows(BaseCreature pet, int id) => id == 4 ? HavenPetTraining.HealingRank(pet) > 0 : Find(pet)?.Learned.Contains(id) == true;
    internal static bool HasRoom(BaseCreature pet, int id)
    {
        var count = HavenPetTraining.HealingRank(pet) > 0 ? 1 : 0;
        var categoryCount = Category(id) == 1 ? count : 0;
        var abilities = Find(pet);
        if (abilities != null)
        {
            foreach (var existing in abilities.Learned)
            {
                count++;
                if (Category(existing) == Category(id)) { categoryCount++; }
            }
        }
        return count < 3 && categoryCount < (Category(id) == 2 ? 2 : 1);
    }
    internal static bool Learn(Mobile owner, BaseCreature pet, int id)
    {
        if (id < 0 || id >= Names.Length || id == 18 && !HavenTamingMissions.IsCustomPet(pet) || !HavenPetTraining.Owned(owner, pet) || Knows(pet, id) || !HasRoom(pet, id)) { return false; }
        var training = HavenPetTraining.Find(pet);
        if (training == null || !training.CanSpend(owner, pet, Cost(id))) { return false; }
        if (id == 4) { return training.LearnHealing(owner, pet); }
        var record = Find(pet);
        if (record == null) { record = new HavenPetAbilities(); pet.Backpack.DropItem(record); }
        record.Learned.Add(id); record.MarkDirty();
        if (id == 0)
        {
            pet.Skills.Magery.Base = Math.Max(30, pet.Skills.Magery.Base);
            pet.Skills.EvalInt.Base = Math.Max(30, pet.Skills.EvalInt.Base);
            pet.AI = AIType.AI_Mage;
        }
        else if (id == 1) { pet.Skills.Poisoning.Base = Math.Max(30, pet.Skills.Poisoning.Base); }
        else if (id is 2 or 3)
        {
            pet.Skills.Musicianship.Base = Math.Max(30, pet.Skills.Musicianship.Base);
            var skill = id == 2 ? SkillName.Discordance : SkillName.Peacemaking;
            pet.Skills[skill].Base = Math.Max(30, pet.Skills[skill].Base);
        }
        training.Spend(owner, pet, Cost(id)); pet.InvalidateProperties();
        owner.SendMessage($"{pet.Name} learned {Names[id]}.");
        return true;
    }
    public static WeaponAbility SelectMove(BaseCreature pet)
    {
        if (!pet.Controlled || pet.Summoned || pet.IsDeadPet) { return null; }
        var record = Find(pet);
        if (record == null) { return null; }
        WeaponAbility chosen = null; var count = 0;
        foreach (var id in record.Learned)
        {
            var move = Move(id);
            if (move != null && Utility.Random(++count) == 0) { chosen = move; }
        }
        return chosen;
    }
    private static bool CanAct(BaseCreature pet, Mobile target) => pet.Controlled && !pet.Summoned && !pet.IsDeadPet && pet.Alive &&
        target?.Deleted == false && target.Alive && target.Map == pet.Map && pet.InLOS(target) && pet.CanBeHarmful(target, false);
    public static void OnAttack(BaseCreature pet, Mobile target)
    {
        if (!pet.Controlled || pet.Summoned || pet.IsDeadPet || pet is HavenCompanion) { return; }
        var record = Find(pet);
        if (record == null || Core.Now < record._nextAttack || !CanAct(pet, target) || !pet.InRange(target, 8)) { return; }
        record._nextAttack = Core.Now + TimeSpan.FromSeconds(10);
        if (record.Learned.Contains(1) && pet.InRange(target, 2) && pet.CheckTargetSkill(SkillName.Poisoning, target, 0, 120))
        {
            var skill = pet.Skills.Poisoning.Value;
            target.ApplyPoison(pet, skill >= 100 ? Poison.Deadly : skill >= 80 ? Poison.Greater : skill >= 50 ? Poison.Regular : Poison.Lesser);
        }
        if (record.Learned.Contains(5) && pet.Mana >= 10)
        {
            pet.Mana -= 10;
            pet.MovingEffect(target, 0x36D4, 7, 0, false, false);
            AOS.Damage(target, pet, Math.Clamp(pet.Hits / 10, 10, 60), 0, 100, 0, 0, 0);
        }
    }
    public static void Think(BaseCreature pet)
    {
        if (!pet.Controlled || pet.Summoned || pet.IsDeadPet || pet is HavenCompanion) { return; }
        var record = Find(pet);
        if (record == null || pet.Combatant is not Mobile enemy || !CanAct(pet, enemy) || !pet.InRange(enemy, 12)) { return; }
        if (record.Learned.Contains(5)) { OnAttack(pet, enemy); }
        if (record.Learned.Contains(18) && HavenTamingMissions.IsCustomPet(pet) && Core.Now >= record._nextPool && pet.Mana >= 15 &&
            pet.ControlMaster is PlayerMobile owner && HavenPetTraining.Owned(owner, pet) && enemy is BaseCreature { Controlled: false, Summoned: false } && enemy is not BaseVendor)
        {
            record._nextPool = Core.Now + TimeSpan.FromSeconds(18);
            record._pool?.Delete(); pet.Mana -= 15;
            record._pool = new HavenPetPool(pet, owner);
            record._pool.MoveToWorld(enemy.Location, enemy.Map);
            record._pool.Start();
        }
        if (Core.Now < record._nextSong) { return; }
        record._nextSong = Core.Now + TimeSpan.FromSeconds(15);
        var discord = record.Learned.Contains(2);
        if (!discord && !record.Learned.Contains(3)) { return; }
        var instrument = pet.Backpack.FindItemByType<Lute>();
        if (instrument == null) { instrument = new Lute { Movable = false }; pet.Backpack.DropItem(instrument); }
        instrument.UsesRemaining = Math.Max(100, instrument.UsesRemaining);
        if (discord) { Discordance.OnPickedInstrument(pet, instrument); }
        else { Peacemaking.OnPickedInstrument(pet, instrument); }
        pet.Target?.Invoke(pet, enemy);
    }
    public static void AddProperties(BaseCreature pet, IPropertyList list)
    {
        var record = Find(pet);
        if (record == null) { return; }
        foreach (var id in record.Learned)
        { if (id >= 0 && id < Names.Length) { list.Add($"{"Trained ability:"} {Names[id]}"); } }
    }
    internal void ClearPool(HavenPetPool pool) { if (_pool == pool) { _pool = null; } }
    public override void OnAfterDelete()
    {
        _pool?.Delete(); _pool = null; base.OnAfterDelete();
    }
}

public sealed class HavenPetAbilitiesGump : Gump
{
    private readonly BaseCreature _pet;
    private readonly int _page;
    public static void DisplayTo(Mobile owner, BaseCreature pet, int page = 0)
    {
        if (!HavenPetTraining.Owned(owner, pet)) { return; }
        owner.CloseGump<HavenPetAbilitiesGump>(); owner.SendGump(new HavenPetAbilitiesGump(pet, page));
    }
    internal HavenPetAbilitiesGump(BaseCreature pet, int page) : base(25, 30)
    {
        _pet = pet; _page = Math.Clamp(page, 0, 1);
        AddBackground(0, 0, 700, 540, 5054); AddImageTiled(12, 12, 676, 516, 2624);
        AddLabel(25, 20, 53, "PET ABILITIES");
        AddLabel(25, 49, 1152, "Up to 3 trained choices: 1 school, 1 special, 2 moves, 1 area effect.");
        AddLabel(25, 73, 1152, "Rarity powers stay separate. Purchases spend points immediately.");
        AddLabel(25, 98, 53, $"Available points: {HavenPetTraining.Get(pet).Points:F1}");
        string[] categories = ["Magic", "Special", "Move", "Area"];
        for (var row = 0; row < 10 && _page * 10 + row < HavenPetAbilities.Names.Length; row++)
        {
            var id = _page * 10 + row; var y = 136 + row * 31;
            AddLabel(25, y, 53, categories[HavenPetAbilities.Category(id)]);
            AddLabel(110, y, 1152, HavenPetAbilities.Names[id]);
            AddLabel(350, y, 1152, $"{HavenPetAbilities.Cost(id) / 10} points");
            if (HavenPetAbilities.Knows(pet, id)) { AddLabel(505, y, 53, "Learned"); }
            else if (id == 18 && !HavenTamingMissions.IsCustomPet(pet)) { AddLabel(505, y, 1152, "Custom pets only"); }
            else if (HavenPetAbilities.HasRoom(pet, id))
            { AddButton(500, y, 4005, 4007, 100 + id); AddLabel(535, y, 1152, "Learn"); }
            else { AddLabel(505, y, 1152, "Choice limit"); }
        }
        AddLabel(25, 461, 1152, "Magery uses spellcasting AI; bard and poison skills improve with use.");
        AddButton(25, 498, 4005, 4007, 1); AddLabel(60, 498, 1152, "Back");
        AddButton(240, 498, 4005, 4007, 2); AddLabel(275, 498, 1152, _page == 0 ? "Next page" : "Previous page");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var owner = sender.Mobile;
        if (info.ButtonID == 0 || !HavenPetTraining.Owned(owner, _pet)) { return; }
        if (info.ButtonID == 1) { HavenPetTrainingGump.DisplayTo(owner, _pet); return; }
        if (info.ButtonID >= 100 && !HavenPetAbilities.Learn(owner, _pet, info.ButtonID - 100))
        { owner.SendMessage("Ability not learned. Check training completion, points, choice limits and a spare follower slot."); }
        DisplayTo(owner, _pet, info.ButtonID == 2 ? 1 - _page : _page);
    }
}
