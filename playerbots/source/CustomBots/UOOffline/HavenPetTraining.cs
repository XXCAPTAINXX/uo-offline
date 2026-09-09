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
    private readonly int _category;
    private static readonly string[] Categories = ["Stats", "Resists", "Magic skill caps", "Combat skill caps", "Abilities"];
    public static void DisplayTo(Mobile owner, BaseCreature pet, bool confirmFinish = false, int category = 0)
    {
        if (!HavenPetTraining.Owned(owner, pet)) { return; }
        owner.CloseGump<HavenPetTrainingGump>(); owner.SendGump(new HavenPetTrainingGump(pet, confirmFinish, category));
    }
    internal HavenPetTrainingGump(BaseCreature pet, bool confirmFinish = false, int category = 0) : base(20, 30)
    {
        _pet = pet; _confirmFinish = confirmFinish; _category = Math.Clamp(category, 0, 4);
        var record = HavenPetTraining.Get(pet);
        AddBackground(0, 0, 700, 570, 5054); AddImageTiled(12, 12, 676, 546, 2624);
        AddLabel(225, 24, 53, "ANIMAL TRAINING");
        AddHtml(28, 53, 640, 24, $"<BASEFONT COLOR=#FFFFFF>{Utility.FixHtml(pet.Name)}</BASEFONT>");
        AddLabel(28, 82, 1152, $"Slots {pet.ControlSlots}/{HavenPetTraining.MaxSlots(pet)}   Progress {record.Progress / 100.0:F1}%   Available points {record.Points:F1}");
        AddAlphaRegion(24, 122, 184, 324); AddAlphaRegion(220, 122, 456, 324);
        AddLabel(42, 136, 53, "CATEGORIES"); AddLabel(238, 136, 53, "SELECTIONS");
        for (var i = 0; i < Categories.Length; i++)
        {
            AddButton(34, 173 + i * 42, 4005, 4007, 1000 + i);
            AddLabel(67, 175 + i * 42, i == _category ? 53 : 1152, Categories[i]);
        }
        if (_category is 0 or 1)
        {
            AddLabel(440, 166, 53, "+1 / cost"); AddLabel(555, 166, 53, "+10 / cost");
            var first = _category == 0 ? 0 : 6;
            var end = _category == 0 ? 6 : 11;
            for (var i = first; i < end; i++)
            {
                var y = 198 + (i - first) * 35;
                AddLabel(238, y, 1152, HavenPetTraining.Labels[i]);
                AddLabel(385, y, 1152, $"{HavenPetTraining.Value(pet, i)}");
                AddButton(442, y, 4005, 4007, 100 + i); AddLabel(475, y + 2, 1152, $"{HavenPetTraining.Weights[i] / 10.0:F1}");
                AddButton(555, y, 4005, 4007, 200 + i); AddLabel(588, y + 2, 1152, $"{HavenPetTraining.Weights[i]}");
            }
        }
        else if (_category is 2 or 3)
        {
            AddLabel(238, 169, 53, "Skill                    Current / cap");
            var first = _category == 2 ? 5 : 0;
            var end = _category == 2 ? HavenPetTraining.TrainableSkills.Length : 5;
            for (var i = first; i < end; i++)
            {
                var skill = pet.Skills[HavenPetTraining.TrainableSkills[i]];
                var y = 200 + (i - first) * 36;
                AddButton(236, y, 4005, 4007, 300 + i);
                AddLabel(270, y + 2, 1152, skill.Name);
                AddLabel(460, y + 2, 1152, $"{skill.Base:F1} / {skill.Cap:F1}");
                AddTooltip(1042971, "Apply the lowest matching power scroll in your backpack that raises this skill cap. Skill points still train through use.");
            }
            AddHtml(238, 391, 410, 42, "<BASEFONT COLOR=#FFFFFF>Consumes the matching scroll and training points. Select a skill to apply its next available scroll.</BASEFONT>");
        }
        else
        {
            AddButton(236, 194, 4005, 4007, 910); AddLabel(270, 196, 1152, "Learn Healing - 100 points");
            AddHtml(238, 234, 410, 100, "<BASEFONT COLOR=#FFFFFF>Teaches your pet to heal itself and its owner. Healing and Anatomy start at a minimum of 20 and improve through use.</BASEFONT>");
            AddLabel(238, 342, 53, record.Healing > 0 ? "Healing learned" : "Healing not yet learned");
        }
        AddHtml(28, 456, 644, 36, "<BASEFONT COLOR=#FFFFFF>Train through combat to 100%, then spend points. The first purchase adds one follower slot. Purchases apply immediately.</BASEFONT>");
        Button(28, 501, 900, "Begin training"); Button(228, 501, 901, confirmFinish ? "Confirm finish" : "Finish stage");
        Button(442, 501, 903, "Info"); Button(557, 501, 0, "Close");
        Button(28, 535, 904, "Animal Lore"); Button(228, 535, 902, "Refresh");
        if (confirmFinish) { AddLabel(380, 537, 53, "Finishing discards leftover points."); }
    }
    private void Button(int x, int y, int id, string text) { AddButton(x, y, 4005, 4007, id); AddLabel(x + 33, y + 2, 1152, text); }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var owner = sender.Mobile; var button = info.ButtonID;
        if (button == 0 || !HavenPetTraining.Owned(owner, _pet)) { return; }
        if (button is >= 1000 and <= 1004) { DisplayTo(owner, _pet, category: button - 1000); return; }
        if (button == 904) { HavenAnimalLoreGump.DisplayTo(owner, _pet); return; }
        if (button == 903)
        {
            owner.SendMessage("Begin a stage, train through combat to 100%, then spend the available points. Your first upgrade requires one spare follower slot. Finish discards leftover points. Skill upgrades require matching scrolls.");
            DisplayTo(owner, _pet, category: _category); return;
        }
        var record = HavenPetTraining.Get(_pet); var success = false;
        if (button == 901 && !_confirmFinish) { DisplayTo(owner, _pet, true, _category); return; }
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
        DisplayTo(owner, _pet, category: _category);
    }
}
