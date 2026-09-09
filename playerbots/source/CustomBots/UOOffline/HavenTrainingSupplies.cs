using System;
using System.Linq;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenTrainingStone : Item
{
    [Constructible]
    public HavenTrainingStone() : base(0xED4) { Name = "Training Supplies"; Hue = 0x489; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    {
        if (HavenShopAccess.CanUse(from, this)) { from.SendGump(new HavenListGump(this, new Menu())); }
    }
    internal sealed class Menu : ItemListMenu, IHavenShop
    {
        internal static readonly SkillName[] Skills = [SkillName.Swords, SkillName.Fencing, SkillName.Macing, SkillName.Archery,
            SkillName.Tactics, SkillName.Anatomy, SkillName.Healing, SkillName.MagicResist, SkillName.Magery, SkillName.EvalInt,
            SkillName.Meditation, SkillName.Spellweaving, SkillName.Necromancy, SkillName.SpiritSpeak, SkillName.Chivalry,
            SkillName.Musicianship, SkillName.Discordance, SkillName.Peacemaking, SkillName.Provocation, SkillName.AnimalTaming,
            SkillName.AnimalLore, SkillName.Veterinary, SkillName.Blacksmith, SkillName.Tailoring, SkillName.Carpentry,
            SkillName.Alchemy, SkillName.Inscribe];
        private static ItemListEntry[] EntriesForShop() => new[] { new ItemListEntry("Shard mastery manual — 1,000 gold", 0xEFA, 0x489) }
            .Concat(Skills.SelectMany(skill => new[] { new ItemListEntry($"{skill} 105 scroll — 2,500 gold", 0x14F0), new ItemListEntry($"{skill} 110 scroll — 7,500 gold", 0x14F0) })).ToArray();
        public Menu() : base("Training supplies — 105/110 scrolls and shard mastery", EntriesForShop()) { }
        public Item CreateItem(int index) => index == 0 ? new HavenMasteryManual() : new PowerScroll(Skills[(index - 1) / 2], (index - 1) % 2 == 0 ? 105 : 110);
        public override void OnResponse(NetState state, int index)
        {
            var from = state.Mobile;
            if (index < 0 || index >= Entries.Length || from.Backpack == null) { return; }
            var item = CreateItem(index);
            var price = index == 0 ? 1000 : (index - 1) % 2 == 0 ? 2500 : 7500;
            if (!from.Backpack.CheckHold(from, item, false)) { item.Delete(); from.SendMessage("Make room in your backpack."); return; }
            if (!HavenEconomy.TryPay(from, price)) { item.Delete(); from.SendMessage($"You need {price:N0} gold."); return; }
            from.Backpack.DropItem(item);
            from.SendMessage("Your training item is in your backpack. Normal skill-cap requirements apply.");
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenMasteryManual : Item
{
    [SerializableField(0)] private Mobile _boundTo;
    private static readonly SkillName[] FocusSkills = [SkillName.Swords, SkillName.Archery, SkillName.Magery, SkillName.Musicianship, SkillName.Healing, SkillName.AnimalTaming];
    private static readonly string[] FocusNames = ["Warrior", "Archer", "Caster", "Bard", "Healer", "Beastmaster"];
    [Constructible]
    public HavenMasteryManual() : base(0xEFA) { Name = "shard mastery manual"; Hue = 0x489; Weight = 1; LootType = LootType.Blessed; }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack != null && IsChildOf(from.Backpack) && (BoundTo == null || BoundTo == from)) { from.SendGump(new FocusGump(this)); }
    }
    internal bool Activate(Mobile from, int focus)
    {
        if (Deleted || from.Backpack == null || !IsChildOf(from.Backpack) || BoundTo != null && BoundTo != from ||
            focus < 0 || focus >= FocusSkills.Length || !from.Alive) { return false; }
        var skill = from.Skills[FocusSkills[focus]].Value;
        if (focus == 0) { skill = Math.Max(skill, Math.Max(from.Skills.Fencing.Value, from.Skills.Macing.Value)); }
        if (focus == 2) { skill = Math.Max(skill, from.Skills.Spellweaving.Value); }
        if (skill < 90) { from.SendMessage("This focus requires at least 90 in its skill."); return false; }
        BoundTo = from;
        var power = Math.Min(5, 2 + (int)((skill - 90) / 10));
        from.RemoveStatMod("HavenMasteryStr"); from.RemoveStatMod("HavenMasteryDex"); from.RemoveStatMod("HavenMasteryInt");
        var strength = focus is 0 or 5 ? power * 2 : focus == 4 ? power : 0;
        var dexterity = focus == 1 ? power * 3 : focus is 0 or 3 or 5 ? power : 0;
        var intelligence = focus == 2 ? power * 3 : focus is 3 or 4 or 5 ? power * 2 : 0;
        from.AddStatMod(new StatMod(StatType.Str, "HavenMasteryStr", strength, TimeSpan.FromMinutes(30)));
        from.AddStatMod(new StatMod(StatType.Dex, "HavenMasteryDex", dexterity, TimeSpan.FromMinutes(30)));
        from.AddStatMod(new StatMod(StatType.Int, "HavenMasteryInt", intelligence, TimeSpan.FromMinutes(30)));
        from.SendMessage($"{FocusNames[focus]} focus active for 30 minutes: +{strength} Str, +{dexterity} Dex, +{intelligence} Int. Only one mastery focus applies.");
        return true;
    }
    public override void OnDelete() { BoundTo = null; base.OnDelete(); }
    private sealed class FocusGump : Gump
    {
        private readonly HavenMasteryManual _manual;
        public FocusGump(HavenMasteryManual manual) : base(40, 40)
        {
            _manual = manual; AddBackground(0, 0, 390, 350, 5054);
            AddLabel(22, 20, 0, "Shard mastery manual");
            AddHtml(22, 50, 345, 58, "Choose one 30-minute focus. Requires 90 skill.<BR>Stronger bonuses at 100, 110 and 120.<BR>These are this shard's custom mastery bonuses.");
            for (var i = 0; i < FocusNames.Length; i++) { AddButton(22, 120 + i * 33, 4005, 4007, i + 1); AddLabel(62, 122 + i * 33, 0, $"{FocusNames[i]} — {FocusSkills[i]}"); }
        }
        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID > 0) { _manual.Activate(state.Mobile, info.ButtonID - 1); }
        }
    }
}
