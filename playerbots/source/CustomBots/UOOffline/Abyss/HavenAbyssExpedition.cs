using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenAbyssExpedition : Item
{
    internal static readonly HashSet<HavenAbyssExpedition> Registry = new();
    [SerializableField(0)] private List<HavenAbyssMiniChamp> _sites = new();
    [SerializableField(1)] private List<Item> _fixtures = new();
    [Constructible]
    public HavenAbyssExpedition() : base(0xBD2) { Name = "Abyss expedition wayboard"; Movable = false; }
    [AfterDeserialization] private void Register() => Registry.Add(this);
    public static void Configure() => CommandSystem.Register("HavenAbyssRestore", AccessLevel.Administrator, e =>
    {
        var expedition = Install();
        e.Mobile.SendMessage(expedition == null ? "One of the verified Abyss sites is blocked. No restoration was installed." : "Abyss crafting expeditions installed: 13 mini-champions, expedition routes and the Fire Temple footbridge. Use [abyss.");
    });
    internal static HavenAbyssExpedition Install()
    {
        foreach (var existing in Registry) { if (!existing.Deleted && existing.Map == Map.TerMur) { return existing; } }
        var positions = new List<Point3D>();
        foreach (var site in HavenAbyssCatalog.Sites)
        {
            if (!HavenAbyssMiniChamp.TryPoint(site.Center, Map.TerMur, 20, out var center) ||
                !HavenAbyssMiniChamp.ConnectedPoint(center, Map.TerMur, 18, 4, out _)) { return null; }
            positions.Add(center);
        }
        var board = new HavenAbyssExpedition(); board.MoveToWorld(new Point3D(528, 758, -92), Map.TerMur); board.Register();
        for (var i = 0; i < positions.Count; i++)
        {
            var spawn = new HavenAbyssMiniChamp(); spawn.Setup(i, positions[i]); board.Sites.Add(spawn);
        }
        var bridge = new HavenAbyssFootbridge(); bridge.MoveToWorld(new Point3D(526, 760, -90), Map.TerMur); board.Fixtures.Add(bridge);
        var forge = new HavenAbyssArtifice(); forge.MoveToWorld(new Point3D(528, 757, -92), Map.TerMur); board.Fixtures.Add(forge);
        board.MarkDirty(); return board;
    }
    internal static void Open(Mobile from)
    {
        foreach (var expedition in Registry)
        {
            if (!expedition.Deleted && expedition.Map == Map.TerMur)
            { from.CloseGump<HavenAbyssGuide>(); from.SendGump(new HavenAbyssGuide(expedition)); return; }
        }
        if (!HavenAbyssTrial.Go(from)) { from.SendMessage("The Abyss expeditions are not installed yet, or travel is restricted."); }
    }
    internal bool Travel(Mobile from, int index)
    {
        if (Deleted || !Registry.Contains(this) || from?.Deleted != false || !from.Alive || from.Criminal || from.Spell != null ||
            SpellHelper.CheckCombat(from) || !SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom, out _)) { return false; }
        if (index < 0 || index >= Sites.Count || Sites[index]?.Deleted != false) { return false; }
        var site = Sites[index];
        if (!HavenAbyssMiniChamp.ConnectedPoint(site.Location, Map, 18, 10, out var point) ||
            !SpellHelper.CheckTravel(from, Map, point, TravelCheckType.RecallTo, out _)) { return false; }
        BaseCreature.TeleportPets(from, point, Map); from.MoveToWorld(point, Map); from.PlaySound(0x1FE); return true;
    }
    public override void OnDoubleClick(Mobile from)
    { if (from.Map == Map && from.InRange(this, 4) && from.InLOS(this)) { Open(from); } }
    public override void OnDelete()
    {
        Registry.Remove(this);
        foreach (var site in Sites) { site?.Delete(); }
        foreach (var fixture in Fixtures) { fixture?.Delete(); }
        Sites.Clear(); Fixtures.Clear(); base.OnDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenAbyssFootbridge : BaseAddon
{
    public override BaseAddonDeed Deed => null;
    [Constructible]
    public HavenAbyssFootbridge()
    {
        Name = "Fire Temple stone footbridge"; Movable = false;
        for (var x = -1; x <= 1; x++)
        {
            for (var y = 0; y <= 12; y++)
            {
                var rise=y is 0 or 12 ? 0 : y is 1 or 11 ? 2 : 3;
                AddComponent(new AddonComponent(0x519), x, y, rise-TileData.ItemTable[0x519].CalcHeight);
            }
        }
    }
}

public sealed class HavenAbyssGuide : Gump
{
    private readonly HavenAbyssExpedition _expedition;
    public HavenAbyssGuide(HavenAbyssExpedition expedition) : base(60, 40)
    {
        _expedition = expedition; AddBackground(0, 0, 610, 530, 9270); AddLabel(25, 22, 1152, "Abyss expedition routes");
        AddHtml(25, 52, 560, 48, "<BASEFONT COLOR=#FFFFFF>Defeat each site's waves for crafting essences and rare materials.<BR>Choose a route. Travel brings following pets and requires leaving combat.</BASEFONT>");
        for (var i = 0; i < expedition.Sites.Count; i++)
        {
            var site = expedition.Sites[i]; if (site?.Deleted != false || site.Definition == null) { continue; }
            var y = 106 + i * 27; AddButton(25, y, 4005, 4007, 100 + i); AddLabel(64, y, 1152, site.Definition.Name);
            AddLabel(345, y, 2101, site.Definition.Essence.Name.Replace("Essence", ""));
            AddLabel(465, y, 2101, site.Active ? $"Wave {site.Wave + 1}/{site.Definition.Waves.Length}" : "Ready soon");
        }
        AddButton(25, 480, 4005, 4007, 1); AddLabel(63, 480, 1152, "Ancient Hunt");
        AddButton(225, 480, 4005, 4007, 2); AddLabel(264, 480, 1152, "Haven bank");
        AddButton(475, 480, 4017, 4019, 0); AddLabel(515, 480, 1152, "Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || _expedition.Deleted || !HavenAbyssExpedition.Registry.Contains(_expedition)) { return; }
        if (info.ButtonID >= 100 && _expedition.Travel(from, info.ButtonID - 100)) { return; }
        if (info.ButtonID == 1 && HavenAbyssTrial.Go(from)) { return; }
        if (info.ButtonID == 2)
        { HavenRecovery.GoToBank(from); return; }
        else { from.SendMessage("That route is blocked or travel is restricted."); }
        HavenAbyssExpedition.Open(from);
    }
}
