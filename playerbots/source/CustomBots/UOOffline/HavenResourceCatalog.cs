using System;
using Server.Items;

namespace Server.UOOffline;

public static class HavenResourceCatalog
{
    public sealed record Entry(Type Type, string Name, string Group, CraftResource Resource = CraftResource.None)
    {
        public Item Create(int amount) => (Item)Activator.CreateInstance(Type, amount);
    }
    // Serialized ledger balances use these indices. Append entries; never reorder existing ones.
    public static readonly Entry[] Entries =
    [
        new(typeof(IronIngot), "Iron ingots", "Metal", CraftResource.Iron),
        new(typeof(DullCopperIngot), "Dull copper ingots", "Metal", CraftResource.DullCopper),
        new(typeof(ShadowIronIngot), "Shadow iron ingots", "Metal", CraftResource.ShadowIron),
        new(typeof(CopperIngot), "Copper ingots", "Metal", CraftResource.Copper),
        new(typeof(BronzeIngot), "Bronze ingots", "Metal", CraftResource.Bronze),
        new(typeof(GoldIngot), "Gold ingots", "Metal", CraftResource.Gold),
        new(typeof(AgapiteIngot), "Agapite ingots", "Metal", CraftResource.Agapite),
        new(typeof(VeriteIngot), "Verite ingots", "Metal", CraftResource.Verite),
        new(typeof(ValoriteIngot), "Valorite ingots", "Metal", CraftResource.Valorite),
        new(typeof(Log), "Regular logs", "Wood", CraftResource.RegularWood),
        new(typeof(OakLog), "Oak logs", "Wood", CraftResource.OakWood),
        new(typeof(AshLog), "Ash logs", "Wood", CraftResource.AshWood),
        new(typeof(YewLog), "Yew logs", "Wood", CraftResource.YewWood),
        new(typeof(HeartwoodLog), "Heartwood logs", "Wood", CraftResource.Heartwood),
        new(typeof(BloodwoodLog), "Bloodwood logs", "Wood", CraftResource.Bloodwood),
        new(typeof(FrostwoodLog), "Frostwood logs", "Wood", CraftResource.Frostwood),
        new(typeof(Board), "Regular boards", "Wood", CraftResource.RegularWood),
        new(typeof(OakBoard), "Oak boards", "Wood", CraftResource.OakWood),
        new(typeof(AshBoard), "Ash boards", "Wood", CraftResource.AshWood),
        new(typeof(YewBoard), "Yew boards", "Wood", CraftResource.YewWood),
        new(typeof(HeartwoodBoard), "Heartwood boards", "Wood", CraftResource.Heartwood),
        new(typeof(BloodwoodBoard), "Bloodwood boards", "Wood", CraftResource.Bloodwood),
        new(typeof(FrostwoodBoard), "Frostwood boards", "Wood", CraftResource.Frostwood),
        new(typeof(Leather), "Regular leather", "Leather", CraftResource.RegularLeather),
        new(typeof(SpinedLeather), "Spined leather", "Leather", CraftResource.SpinedLeather),
        new(typeof(HornedLeather), "Horned leather", "Leather", CraftResource.HornedLeather),
        new(typeof(BarbedLeather), "Barbed leather", "Leather", CraftResource.BarbedLeather),
        new(typeof(Hides), "Regular hides", "Leather", CraftResource.RegularLeather),
        new(typeof(SpinedHides), "Spined hides", "Leather", CraftResource.SpinedLeather),
        new(typeof(HornedHides), "Horned hides", "Leather", CraftResource.HornedLeather),
        new(typeof(BarbedHides), "Barbed hides", "Leather", CraftResource.BarbedLeather),
        new(typeof(BlackPearl), "Black pearl", "Reagents"),
        new(typeof(Bloodmoss), "Bloodmoss", "Reagents"),
        new(typeof(Garlic), "Garlic", "Reagents"),
        new(typeof(Ginseng), "Ginseng", "Reagents"),
        new(typeof(MandrakeRoot), "Mandrake root", "Reagents"),
        new(typeof(Nightshade), "Nightshade", "Reagents"),
        new(typeof(SulfurousAsh), "Sulfurous ash", "Reagents"),
        new(typeof(SpidersSilk), "Spider's silk", "Reagents"),
        new(typeof(BatWing), "Bat wings", "Reagents"),
        new(typeof(GraveDust), "Grave dust", "Reagents"),
        new(typeof(DaemonBlood), "Daemon blood", "Reagents"),
        new(typeof(NoxCrystal), "Nox crystals", "Reagents"),
        new(typeof(PigIron), "Pig iron", "Reagents"),
        new(typeof(Arrow), "Arrows", "Supplies"),
        new(typeof(Bolt), "Bolts", "Supplies"),
        new(typeof(Feather), "Feathers", "Supplies"),
        new(typeof(Bone), "Bones", "Supplies"),
        new(typeof(Cloth), "Undyed cloth", "Supplies"),
        new(typeof(UncutCloth), "Undyed uncut cloth", "Supplies"),
        new(typeof(BoltOfCloth), "Undyed cloth bolts", "Supplies")
    ];
    public static CraftResource Material(Item item) => item switch
    {
        BaseIngot ingot => ingot.Resource, Log log => log.Resource, Board board => board.Resource,
        BaseLeather leather => leather.Resource, BaseHides hides => hides.Resource, _ => CraftResource.None
    };
    public static int Index(Item item)
    {
        if (item?.Deleted != false || item is not ICommodity { IsDeedable: true } || !item.Stackable ||
            item.Name != null || item.LootType != LootType.Regular) { return -1; }
        var material = Material(item);
        if (item.Hue != (material == CraftResource.None ? 0 : CraftResources.GetHue(material))) { return -1; }
        for (var i = 0; i < Entries.Length; i++)
        {
            var entry = Entries[i];
            if (entry.Resource != material) { continue; }
            if (entry.Type == item.GetType() || item.GetType() == typeof(Log) && typeof(Log).IsAssignableFrom(entry.Type) ||
                item.GetType() == typeof(Board) && typeof(Board).IsAssignableFrom(entry.Type)) { return i; }
        }
        return -1;
    }
}
