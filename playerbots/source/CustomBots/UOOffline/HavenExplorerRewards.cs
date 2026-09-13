using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenGoldenShovel : Shovel
{
    [Constructible] public HavenGoldenShovel() : base(1000) { Name = "The Gilded Pathfinder"; Hue = 0x8A5; LootType = LootType.Blessed; }
    public override void OnDoubleClick(Mobile from)
    {
        if (!HavenPortableTravel.CanUse(from, this)) { from.SendMessage("Keep the shovel in your pack; leave combat before travelling."); return; }
        from.SendMessage("Target a decoded, unfinished treasure map in your backpack."); from.Target = new MapTarget(this);
    }
    internal bool Travel(Mobile from, TreasureMap map)
    {
        if (map?.Deleted != false || map.Completed || map.Decoder == null || from.Backpack == null || !map.IsChildOf(from.Backpack) || map.ChestMap == null) { return false; }
        if (map.ChestLocation.X < 0 || map.ChestLocation.Y < 0 || map.ChestLocation.X >= map.ChestMap.Width || map.ChestLocation.Y >= map.ChestMap.Height) { return false; }
        var p = new Point3D(map.ChestLocation.X, map.ChestLocation.Y, map.ChestMap.GetAverageZ(map.ChestLocation.X, map.ChestLocation.Y));
        return HavenPortableTravel.Go(from, this, map.ChestMap, p, 2, true);
    }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Target a decoded map to travel beside its dig site."}"); list.Add($"{"The map's normal dig, guardians and loot remain unchanged."}"); }
    private sealed class MapTarget : Target
    {
        private readonly HavenGoldenShovel _shovel;
        public MapTarget(HavenGoldenShovel shovel) : base(-1, false, TargetFlags.None) { _shovel = shovel; }
        protected override void OnTarget(Mobile from, object target)
        { if (target is not TreasureMap map || !_shovel.Travel(from, map)) { from.SendMessage("The map must be decoded, unfinished and in your pack, with a safe travel destination."); } }
    }
}

[SerializationGenerator(0)]
public partial class HavenEndlessBandage : Bandage
{
    [Constructible] public HavenEndlessBandage() { Name = "Mercy's Endless Bandage"; Stackable = false; Hue = 0x481; LootType = LootType.Blessed; }
    public override void Consume(int amount = 1) { }
    public override void OnDoubleClick(Mobile from)
    { if (from.Backpack != null && IsChildOf(from.Backpack)) { base.OnDoubleClick(from); } else { from.SendMessage("Keep the endless bandage in your pack."); } }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Unlimited uses; normal Healing and Veterinary checks apply."}"); }
}

[SerializationGenerator(0)]
public partial class HavenResourceSatchel : Bag
{
    [Constructible] public HavenResourceSatchel() { Name = "Gatherer's Resource Satchel"; Hue = 0x59B; Weight = 2; LootType = LootType.Blessed; }
    public override void OnDoubleClick(Mobile from) => HavenStorageMenu.DisplayTo(from,this);
    public override int DefaultMaxWeight => 20000;
    public override bool OnDragDropInto(Mobile from, Item item, Point3D p) => Accepts(item) && base.OnDragDropInto(from, item, p);
    public override bool TryDropItem(Mobile from, Item item, bool message, bool sound) => Accepts(item) && base.TryDropItem(from, item, message, sound);
    internal static bool Accepts(Item item) => item is not Container &&
        (HavenResourceCatalog.Index(item) >= 0 || item is BaseOre or Bandage or Fish or RawFishSteak || IsGem(item));
    private static bool IsGem(Item item) => item is Amber or Amethyst or Citrine or Diamond or Emerald or Ruby or
        Sapphire or StarSapphire or Tourmaline or BlueDiamond or BrilliantAmber or DarkSapphire or EcruCitrine or
        FireRuby or PerfectEmerald or ArcaneGem;
    public override bool TryDropItem(Mobile from, Item item, bool message)
    {
        if (!Accepts(item)) { if (message) { from.SendMessage("This satchel holds crafting resources, gems, fish and bandages, not equipment or other bags."); } return false; }
        return base.TryDropItem(from, item, message);
    }
    public override int GetTotal(TotalType type)
    { var raw = base.GetTotal(type); return type == TotalType.Weight ? (raw + 9) / 10 : raw; }
    public override void UpdateTotal(Item sender, TotalType type, int delta)
    {
        var before = GetTotal(TotalType.Weight);
        base.UpdateTotal(sender, type, delta);
        if (type == TotalType.Weight && sender != this && !sender.IsVirtualItem && delta != 0)
        { base.UpdateTotal(this, type, GetTotal(type) - before - delta); }
    }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Holds crafting resources, gems, fish and bandages."}"); list.Add($"{"Resource weight reduced by 90%; normal item count limits apply."}"); }
}
