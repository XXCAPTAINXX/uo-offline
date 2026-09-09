using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
namespace Server.UOOffline;

// Slot stages and weighted attribute limits follow Animal Training. Combat pacing is shard-specific.
[SerializationGenerator(0)]
public partial class HavenPetTraining : Item
{
    [SerializableField(0)] private int _progress;
    [SerializableField(1)] private int _pointsTenths;
    [SerializableField(2)] private int _healing;
    [SerializableField(3)] private bool _active;
    [SerializableField(4)] private bool _advanced;
    [SerializableField(5)] private int _initialSlots;
    [SerializableField(6)] private Dictionary<int, int> _targets = new();
    public override bool IsVirtualItem => true;
    public double Points => PointsTenths / 10.0;
    [Constructible]
    public HavenPetTraining() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "animal training record"; }
    internal static HavenPetTraining Find(BaseCreature pet) => pet?.Backpack?.FindItemByType<HavenPetTraining>();
    internal static HavenPetTraining Get(BaseCreature pet)
    {
        var record = Find(pet);
        if (record != null) { return record; }
        if (pet.Backpack == null) { pet.AddItem(new Backpack()); }
        record = new HavenPetTraining { InitialSlots = pet.ControlSlots };
        pet.Backpack.DropItem(record); return record;
    }
    public static int HealingRank(BaseCreature pet) => Find(pet)?.Healing ?? 0;
    internal static bool Owned(Mobile owner, BaseCreature pet) => owner?.Deleted == false && owner.Alive && pet?.Deleted == false &&
        pet.Alive && pet is not HavenCompanion && pet.Controlled && !pet.Summoned && !pet.IsDeadPet && pet.ControlMaster == owner &&
        pet.Map != Map.Internal && pet.Map == owner.Map && owner.InRange(pet, 12) && owner.InLOS(pet);
    public static int MaxSlots(BaseCreature pet)
    {
        if (HavenTamingMissions.IsCustomPet(pet)) { return 5; }
        return pet switch
        {
            SwampDragon => pet.ControlSlots,
            Horse or PackHorse or PackLlama or RidableLlama or ForestOstard or DesertOstard or DireWolf => 3,
            Chicken or Bird => 2,
            Bull or FrenziedOstard => 4,
            Dragon or WhiteWyrm or Drake or Beetle or FireBeetle or Nightmare or Unicorn or Kirin or CuSidhe or RuneBeetle => 5,
            _ => Math.Max(pet.ControlSlots, 3)
        };
    }
    internal bool Begin(Mobile owner, BaseCreature pet)
    {
        if (!Owned(owner, pet) || Find(pet) != this || Active || pet.ControlSlots >= MaxSlots(pet)) { return false; }
        Active = true; Advanced = false; Progress = 0; _targets.Clear();
        PointsTenths = pet.ControlSlots == InitialSlots && InitialSlots == 1
            ? MaxSlots(pet) == 2 ? 25560 : MaxSlots(pet) == 3 ? 23810 : 15010 : 15010;
        pet.InvalidateProperties(); return true;
    }
    public static void AwardDamage(BaseCreature target, Mobile attacker, int damage)
    {
        if (damage <= 0 || attacker is not BaseCreature pet || pet is HavenCompanion || pet.Summoned || pet.IsDeadPet ||
            !pet.Controlled || pet.ControlMaster is not PlayerMobile owner || owner is Server.CustomBots.PlayerBot ||
            !Owned(owner, pet) || target == pet || target.Controlled || target.Summoned || target.IsDeadPet) { return; }
        var record = Find(pet);
        if (record == null || !record.Active || record.Progress >= 10000) { return; }
        if (pet.Skills.Wrestling.Base - target.Skills.Wrestling.Base > 50) { return; }
        var serial = (int)target.Serial.Value;
        record._targets.TryGetValue(serial, out var earned);
        var limit = pet.ControlSlots < 3 ? 5000 : 2500;
        var gain = Math.Min(Math.Min(damage, target.HitsMax), Math.Max(0, limit - earned));
        if (gain <= 0) { return; }
        record._targets[serial] = earned + gain;
        record.Progress = Math.Min(10000, record.Progress + gain);
        if (record.Progress == 10000) { owner.SendMessage($"{pet.Name} has completed combat training. Use Animal Lore or [pettrain to choose upgrades."); }
        pet.InvalidateProperties();
    }
    internal bool CanSpend(Mobile owner, BaseCreature pet, int cost) => Owned(owner, pet) && Find(pet) == this && Active &&
        Progress == 10000 && cost > 0 && cost <= PointsTenths && (Advanced || owner.Followers + 1 <= owner.FollowersMax);
    private void Spend(Mobile owner, BaseCreature pet, int cost)
    {
        if (!Advanced)
        {
            pet.RemoveFollowers(); pet.ControlSlots++; pet.AddFollowers(); Advanced = true;
            pet.MinTameSkill = Math.Min(120, pet.MinTameSkill + 21);
            owner.SendMessage($"{pet.Name} now uses {pet.ControlSlots} follower slots.");
        }
        PointsTenths -= cost;
        if (PointsTenths == 0) { Finish(owner, pet); }
        pet.InvalidateProperties();
    }
    internal bool Finish(Mobile owner, BaseCreature pet)
    {
        if (!Owned(owner, pet) || Find(pet) != this || !Active || !Advanced || Progress < 10000) { return false; }
        Active = false; PointsTenths = 0; _targets.Clear();
        if (pet.ControlSlots == 5 && owner.FollowersMax < 6)
        {
            // FollowersMax is already persisted on Mobile. A floor, rather than +=, prevents repeat rewards.
            owner.FollowersMax = 6;
            owner.SendMessage("Master Trainer earned! Your permanent follower capacity is now 6 (+1). This reward does not stack.");
        }
        pet.InvalidateProperties(); return true;
    }
    internal static readonly string[] Labels = ["Strength", "Dexterity", "Intelligence", "Maximum health", "Maximum stamina", "Maximum mana", "Physical resist", "Fire resist", "Cold resist", "Poison resist", "Energy resist"];
    internal static readonly int[] Weights = [30, 1, 5, 30, 5, 5, 30, 30, 30, 30, 30];
    private static readonly int[] Limits = [700, 150, 700, 1100, 150, 1500, 80, 80, 80, 80, 80];
    internal static int Value(BaseCreature pet, int option) => option switch
    {
        0 => pet.RawStr, 1 => pet.RawDex, 2 => pet.RawInt, 3 => pet.HitsMaxSeed > 0 ? pet.HitsMaxSeed : pet.RawStr, 4 => pet.StamMaxSeed > 0 ? pet.StamMaxSeed : pet.RawDex, 5 => pet.ManaMaxSeed > 0 ? pet.ManaMaxSeed : pet.RawInt,
        6 => pet.BasePhysicalResistance, 7 => pet.BaseFireResistance, 8 => pet.BaseColdResistance, 9 => pet.BasePoisonResistance, 10 => pet.BaseEnergyResistance, _ => 0
    };
    internal bool Upgrade(Mobile owner, BaseCreature pet, int option, int amount)
    {
        if (option < 0 || option >= Labels.Length || amount is not (1 or 10)) { return false; }
        var value = Value(pet, option);
        var cost = amount * Weights[option];
        if (value + amount > Limits[option] || !CanSpend(owner, pet, cost)) { return false; }
        var start = option < 3 ? 0 : option < 6 ? 3 : 6;
        var end = start == 6 ? 11 : start + 3;
        var weight = cost;
        for (var i = start; i < end; i++) { weight += Value(pet, i) * Weights[i]; }
        if (weight > (start == 0 ? 23000 : start == 3 ? 33000 : 10950)) { return false; }
        // Freeze implicit vitals before raising raw stats, so they cannot bypass the separate vital budget.
        if (pet.HitsMaxSeed <= 0) { pet.HitsMaxSeed = pet.RawStr; }
        if (pet.StamMaxSeed <= 0) { pet.StamMaxSeed = pet.RawDex; }
        if (pet.ManaMaxSeed <= 0) { pet.ManaMaxSeed = pet.RawInt; }
        switch (option)
        {
            case 0: pet.RawStr += amount; break;
            case 1: pet.RawDex += amount; break;
            case 2: pet.RawInt += amount; break;
            case 3: pet.SetHits(value + amount); break;
            case 4: pet.SetStam(value + amount); break;
            case 5: pet.SetMana(value + amount); break;
            case 6: pet.PhysicalResistanceSeed += amount; break;
            case 7: pet.FireResistSeed += amount; break;
            case 8: pet.ColdResistSeed += amount; break;
            case 9: pet.PoisonResistSeed += amount; break;
            case 10: pet.EnergyResistSeed += amount; break;
        }
        Spend(owner, pet, cost); return true;
    }
    internal bool LearnHealing(Mobile owner, BaseCreature pet)
    {
        if (Healing > 0 || pet.CanHealOwner || !CanSpend(owner, pet, 1000)) { return false; }
        Healing = 1;
        pet.Skills.Healing.Base = Math.Max(20, pet.Skills.Healing.Base);
        pet.Skills.Anatomy.Base = Math.Max(20, pet.Skills.Anatomy.Base);
        Spend(owner, pet, 1000); return true;
    }
    internal static readonly SkillName[] TrainableSkills = [SkillName.Wrestling, SkillName.Tactics, SkillName.Anatomy, SkillName.Healing,
        SkillName.MagicResist, SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.Focus];
    internal bool RaiseCap(Mobile owner, BaseCreature pet, SkillName skill, Item scroll)
    {
        if (!Array.Exists(TrainableSkills, s => s == skill) || scroll?.Deleted != false || owner.Backpack == null || !scroll.IsChildOf(owner.Backpack)) { return false; }
        var cap = scroll switch { PowerScroll p when p.Skill == skill => p.Value, HavenPetPowerScroll p when p.Skill == skill => p.Cap, _ => 0 };
        var current = pet.Skills[skill];
        if (cap is not (105 or 110 or 115 or 120) || cap <= current.Cap) { return false; }
        var weight = skill is SkillName.Wrestling or SkillName.Tactics or SkillName.EvalInt ? 10 : skill == SkillName.Magery ? 5 : 1;
        var cost = (int)Math.Ceiling((cap - current.Cap) * 10 * weight);
        if (!CanSpend(owner, pet, cost)) { return false; }
        current.Cap = cap; scroll.Delete(); Spend(owner, pet, cost); return true;
    }
    public static void AddProperties(BaseCreature pet, IPropertyList list)
    {
        var record = Find(pet); if (record == null) { return; }
        list.Add($"{"Animal training:"} {pet.ControlSlots}{" / "}{MaxSlots(pet)}{" slots; combat progress "}{record.Progress / 100.0:F1}{"%"}");
        if (record.Active) { list.Add($"{"Training points:"} {record.Points:F1}"); }
        if (record.Healing > 0) { list.Add($"{"Learned Healing; automatically heals self and owner"}"); }
    }
    public static void Initialize() => CommandSystem.Register("PetTrain", AccessLevel.Player, e =>
    { e.Mobile.SendMessage("Select your nearby pet to open Animal Training. Dismount first."); e.Mobile.Target = new TrainingTarget(); });
    private sealed class TrainingTarget : Target
    {
        public TrainingTarget() : base(12, false, TargetFlags.None) { }
        protected override void OnTarget(Mobile from, object targeted)
        { if (targeted is BaseCreature pet && Owned(from, pet)) { HavenPetTrainingGump.DisplayTo(from, pet); } }
    }
}
public sealed class HavenPetTrainingGump : Gump
{
    private readonly BaseCreature _pet;
    private readonly bool _confirmFinish;
    public static void DisplayTo(Mobile owner, BaseCreature pet, bool confirmFinish = false)
    {
        if (!HavenPetTraining.Owned(owner, pet)) { return; }
        owner.CloseGump<HavenPetTrainingGump>(); owner.SendGump(new HavenPetTrainingGump(pet, confirmFinish));
    }
    internal HavenPetTrainingGump(BaseCreature pet, bool confirmFinish = false) : base(30, 40)
    {
        _pet = pet; _confirmFinish = confirmFinish; var record = HavenPetTraining.Get(pet);
        AddBackground(0, 0, 590, 535, 5054); AddBackground(10, 10, 570, 515, 3000);
        AddLabel(22, 22, 0, $"Animal Training: {pet.Name}");
        AddLabel(22, 47, 0, $"Slots {pet.ControlSlots}/{HavenPetTraining.MaxSlots(pet)} | Progress {record.Progress / 100.0:F1}% | Points {record.Points:F1}");
        AddHtml(22, 74, 540, 42, "Train in combat to 100%, then buy upgrades. Your first purchase adds one follower slot. Changes are permanent.");
        AddPage(0);
        AddButton(22, 480, 4005, 4007, 900); AddLabel(55, 482, 0, "Begin training");
        AddButton(195, 480, 4005, 4007, 901); AddLabel(228, 482, 0, confirmFinish ? "Confirm: discard leftover points" : "Finish stage");
        AddButton(22, 507, 4005, 4007, 902); AddLabel(55, 509, 0, "Refresh");
        AddButton(470, 507, 4005, 4007, 0); AddLabel(503, 509, 0, "Close");
        AddPage(1);
        AddLabel(22, 116, 0, "Attribute / current"); AddLabel(288, 116, 0, "+1"); AddLabel(410, 116, 0, "+10");
        for (var i = 0; i < HavenPetTraining.Labels.Length; i++)
        {
            var y = 140 + i * 26;
            AddLabel(22, y, 0, $"{HavenPetTraining.Labels[i]}: {HavenPetTraining.Value(pet, i)}");
            AddButton(285, y, 4005, 4007, 100 + i); AddLabel(320, y + 2, 0, $"{HavenPetTraining.Weights[i] / 10.0:F1} pts");
            AddButton(405, y, 4005, 4007, 200 + i); AddLabel(440, y + 2, 0, $"{HavenPetTraining.Weights[i]} pts");
        }
        AddButton(22, 447, 4005, 4007, 0, GumpButtonType.Page, 2); AddLabel(55, 449, 0, "Skills and Healing");
        AddPage(2);
        AddButton(22, 120, 4005, 4007, 910); AddLabel(55, 122, 0, "Learn Healing: 100 points (skill starts at 20)");
        AddHtml(22, 153, 535, 42, "Skill upgrades consume a matching power scroll from your backpack. They raise caps; skills still train through use.");
        for (var i = 0; i < HavenPetTraining.TrainableSkills.Length; i++)
        {
            var s = pet.Skills[HavenPetTraining.TrainableSkills[i]];
            AddButton(22, 205 + i * 25, 4005, 4007, 300 + i);
            AddLabel(55, 207 + i * 25, 0, $"{s.Name}: {s.Base:F1} / {s.Cap:F1} - apply next scroll");
        }
        AddButton(22, 447, 4005, 4007, 0, GumpButtonType.Page, 1); AddLabel(55, 449, 0, "Stats and resists");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var owner = sender.Mobile; var button = info.ButtonID;
        if (button == 0 || !HavenPetTraining.Owned(owner, _pet)) { return; }
        var record = HavenPetTraining.Get(_pet); var success = false;
        if (button == 901 && !_confirmFinish) { DisplayTo(owner, _pet, true); return; }
        if (button == 900) { success = record.Begin(owner, _pet); }
        else if (button == 901) { success = record.Finish(owner, _pet); }
        else if (button == 910) { success = record.LearnHealing(owner, _pet); }
        else if (button is >= 100 and < 111) { success = record.Upgrade(owner, _pet, button - 100, 1); }
        else if (button is >= 200 and < 211) { success = record.Upgrade(owner, _pet, button - 200, 10); }
        else if (button is >= 300 and < 309)
        {
            var skill = HavenPetTraining.TrainableSkills[button - 300];
            Item selected = null; double best = 121;
            foreach (var item in owner.Backpack.FindItemsByType<Item>())
            {
                var cap = item switch { PowerScroll p when p.Skill == skill => p.Value, HavenPetPowerScroll p when p.Skill == skill => p.Cap, _ => 0 };
                if (cap > _pet.Skills[skill].Cap && cap < best) { selected = item; best = cap; }
            }
            success = record.RaiseCap(owner, _pet, skill, selected);
        }
        else if (button == 902) { success = true; }
        if (!success) { owner.SendMessage("No change: check progress, points, stat limits, matching scrolls, and room for the extra follower slot."); }
        DisplayTo(owner, _pet);
    }
}
