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
    { Theme = Math.Clamp(theme, 0, 2); Visible = false; Movable = false; Weight = 0; Name = "island trial theme"; }
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
        { if (item is HavenTrialTheme theme) { theme.Theme = Math.Clamp(value, 0, 2); return; } }
        holder.AddItem(new HavenTrialTheme(value));
    }
    internal static string Label(int theme) => theme switch { 1 => "Earth and ore", 2 => "Wild beasts", _ => "Woodland" };
    internal static void Dress(HavenTrialCreature creature, int theme)
    {
        var boss = creature.TrialStage == 4;
        if (theme == 1)
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
    internal static Item Resource(int theme, bool boss) => theme switch
    {
        1 => boss ? new IronIngot(Utility.RandomMinMax(200, 350)) : new IronOre(Utility.RandomMinMax(1, 3)),
        2 => new Hides(boss ? Utility.RandomMinMax(150, 250) : Utility.RandomMinMax(5, 12)),
        _ => boss ? new Board(Utility.RandomMinMax(200, 350)) : new Log(Utility.RandomMinMax(5, 12))
    };
}
