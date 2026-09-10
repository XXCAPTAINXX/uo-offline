using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenTrialTheme : Item
{
    [SerializableField(0)] private int _theme;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenTrialTheme(int theme = 0) : base(1)
    { Theme = Math.Clamp(theme, 0, 3); Visible = false; Movable = false; Weight = 0; Name = "island trial theme"; }
    internal static int Get(Item holder)
    {
        foreach (var item in holder.Items) { if (item is HavenTrialTheme theme) { return theme.Theme; } }
        return 0;
    }
    internal static int Get(Mobile holder)
    {
        foreach (var item in holder.Items) { if (item is HavenTrialTheme theme) { return theme.Theme; } }
        return 0;
    }
    internal static void Set(Item holder, int value)
    {
        foreach (var item in holder.Items)
        { if (item is HavenTrialTheme theme) { theme.Theme = Math.Clamp(value, 0, 3); return; } }
        holder.AddItem(new HavenTrialTheme(value));
    }
    internal static string Label(int theme) => theme switch { 1 => "Earth and ore", 2 => "Wild beasts", 3 => "Blackwake pirates", _ => "Woodland" };
    internal static void Dress(HavenTrialCreature creature, int theme)
    {
        var boss = creature.TrialStage == 4;
        if (theme == 3)
        {
            creature.Name = boss ? "Captain Blackwake, the island raider" : creature.TrialStage switch
            { 1 => "a Blackwake deckhand", 2 => "a Blackwake raider", _ => "a Blackwake first mate" };
            creature.Body = 400; creature.Hue = 0x83EA;
            creature.AddItem(new TricorneHat(0x455)); creature.AddItem(new Shirt(boss ? 0x66D : 0x455));
            creature.AddItem(new LongPants(0x455)); creature.AddItem(new Boots());
        }
        else if (theme == 1)
        {
            creature.Name = boss ? "Stoneback, the quarry champion" : creature.TrialStage switch
            { 1 => "a restless earth wisp", 2 => "a copperstone elemental", _ => "an ironstone guardian" };
            creature.Body = 14; creature.Hue = creature.TrialStage == 2 ? 0x96D : 0x973;
        }
        else if (theme == 2)
        {
            creature.Name = boss ? "Razorhide, the beast champion" : creature.TrialStage switch
            { 1 => "a wild woodland wolf", 2 => "a bristling forest boar", _ => "a fierce woodland bear" };
            creature.Body = boss || creature.TrialStage == 3 ? 211 : creature.TrialStage == 2 ? 290 : 225;
            creature.Hue = 0;
        }
    }
    internal static CommodityDeed ResourceDeed(int theme, bool boss)
    {
        var deed = new CommodityDeed();
        var resource = Resource(theme, boss);
        if (!deed.SetCommodity(resource))
        { resource.Delete(); deed.Delete(); throw new InvalidOperationException("Trial resource must be deedable."); }
        return deed;
    }
    internal static Item Resource(int theme, bool boss) => theme switch
    {
        1 => boss ? new IronIngot(Utility.RandomMinMax(200, 350)) : new IronIngot(Utility.RandomMinMax(1, 3)),
        2 => new Hides(boss ? Utility.RandomMinMax(150, 250) : Utility.RandomMinMax(5, 12)),
        _ => boss ? new Board(Utility.RandomMinMax(200, 350)) : new Log(Utility.RandomMinMax(5, 12))
    };
}
