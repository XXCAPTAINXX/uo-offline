using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Items;
using Server.Multis;

namespace Server.UOOffline;

public enum HavenCraftStationKind { Smithing, Tailoring, Carpentry, Fletching, Tinkering, Alchemy, Cooking, Inscription, Glassblowing, Masonry, Cartography }

// ModernUO adaptation of the public veteran power-tool behavior (5,000 rechargeable uses).
// Existing client art is used because the later machine graphics are absent from this shard's data.
[SerializationGenerator(0)]
public partial class HavenCraftStation : BaseTool
{
    [SerializableField(0)] private HavenCraftStationKind _kind;
    public const int Capacity = 5000;
    public override bool BreakOnDepletion => false;
    [Constructible] public HavenCraftStation() : this(HavenCraftStationKind.Smithing) { }
    [Constructible] public HavenCraftStation(HavenCraftStationKind kind) : base(500,0xB90)
    { Kind=kind; Name=Names[(int)kind]; Movable=false; }
    internal static readonly string[] Names = { "Smithing press", "Sewing machine", "Spinning lathe", "Bow stringer", "Tinker bench", "Alchemy station", "BBQ smoker", "Enchanted writing desk", "Glass kiln", "Enchanted sculpting tool", "Map charter" };
    public override CraftSystem CraftSystem => Kind switch
    {
        HavenCraftStationKind.Smithing => DefBlacksmithy.CraftSystem,
        HavenCraftStationKind.Tailoring => DefTailoring.CraftSystem,
        HavenCraftStationKind.Carpentry => DefCarpentry.CraftSystem,
        HavenCraftStationKind.Fletching => DefBowFletching.CraftSystem,
        HavenCraftStationKind.Tinkering => DefTinkering.CraftSystem,
        HavenCraftStationKind.Alchemy => DefAlchemy.CraftSystem,
        HavenCraftStationKind.Cooking => DefCooking.CraftSystem,
        HavenCraftStationKind.Inscription => DefInscription.CraftSystem,
        HavenCraftStationKind.Glassblowing => DefGlassblowing.CraftSystem,
        HavenCraftStationKind.Masonry => DefMasonry.CraftSystem,
        _ => DefCartography.CraftSystem
    };
    internal bool CanOperate(Mobile from)
    {
        var house=BaseHouse.FindHouseAt(this);
        return !Deleted && Parent == null && from?.Deleted == false && from.Alive && from.Map==Map &&
            from.InRange(this,2) && Math.Abs(from.Z-Z)<=8 && from.InLOS(this) && house?.IsInside(from)==true &&
            (house.IsOwner(from)||house.IsCoOwner(from)||house.IsGuildMember(from));
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (!CanOperate(from)) { from.SendMessage("Stand beside a station in your house or guild workshop."); return; }
        if (UsesRemaining<=0) { from.SendMessage("Refill this station by dropping an ordinary matching crafting tool on it."); return; }
        var error=CraftSystem.CanCraft(from,this,null);
        if (error>0) { from.SendLocalizedMessage(error); return; }
        CraftItem.ShowCraftMenu(from,CraftSystem,this,null);
    }
    internal bool Recharge(Mobile from, Item item)
    {
        if (!CanOperate(from) || item is not BaseTool tool || tool==this || tool.Deleted || tool.UsesRemaining<=0 ||
            tool.CraftSystem!=CraftSystem || !Ordinary(tool) || tool.UsesRemaining>Capacity-UsesRemaining) { return false; }
        UsesRemaining+=tool.UsesRemaining;
        tool.Delete();
        return true;
    }
    private static bool Ordinary(BaseTool tool) => tool.GetType()==typeof(SmithHammer) || tool.GetType()==typeof(Tongs) ||
        tool.GetType()==typeof(SewingKit) || tool.GetType()==typeof(Saw) || tool.GetType()==typeof(DovetailSaw) ||
        tool.GetType()==typeof(FletcherTools) || tool.GetType()==typeof(TinkerTools) || tool.GetType()==typeof(MortarPestle) ||
        tool.GetType()==typeof(Skillet) || tool.GetType()==typeof(ScribesPen) || tool.GetType()==typeof(Blowpipe) ||
        tool.GetType()==typeof(MalletAndChisel) || tool.GetType()==typeof(MapmakersPen);
    public override bool OnDragDrop(Mobile from, Item item)
    {
        if (Recharge(from,item)) { from.SendMessage($"Station refilled: {UsesRemaining:N0} / {Capacity:N0} uses."); return true; }
        from.SendMessage("Use an ordinary matching tool whose charges fit within the 5,000-use capacity. Special tools are kept intact.");
        return false;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Drop matching ordinary tools here to refill; capacity"} {Capacity:N0}.");
        list.Add($"{"Uses your crafting skill and backpack materials; special techniques still require learning."}");
    }
}
