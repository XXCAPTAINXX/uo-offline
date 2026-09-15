using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;

namespace Server.UOOffline;

public static class HavenPortableTravel
{
    internal static bool CanUse(Mobile from, Item item) => from?.Deleted == false && from.Alive &&
        from.Backpack != null && item?.Deleted == false && item.IsChildOf(from.Backpack) &&
        !from.Criminal && from.Spell == null && !SpellHelper.CheckCombat(from);
    internal static bool Go(Mobile from, Item item, Map map, Point3D target, int radius = 3, bool adjacent = false)
    {
        if (!CanUse(from, item) || map == null || map == Map.Internal ||
            !SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom, out _)) { return false; }
        for (var r = adjacent ? 1 : 0; r <= radius; r++)
        for (var dx = -r; dx <= r; dx++)
        for (var dy = -r; dy <= r; dy++)
        {
            if (r > 0 && Math.Abs(dx) != r && Math.Abs(dy) != r) { continue; }
            var x = target.X + dx; var y = target.Y + dy;
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) { continue; }
            var z = r == 0 ? target.Z : map.GetAverageZ(x, y);
            var p = new Point3D(x, y, z);
            if (!map.CanSpawnMobile(p) || BaseHouse.FindHouseAt(p, map, 16) != null ||
                !SpellHelper.CheckTravel(from, map, p, TravelCheckType.RecallTo, out _)) { continue; }
            BaseCreature.TeleportPets(from, p, map); from.MoveToWorld(p, map); from.PlaySound(0x1FE); return true;
        }
        return false;
    }
}

public sealed class HavenTravelGump : Gump
{
    private readonly OfflineTravelBook _book;
    private readonly int _category, _facet, _page;
    private readonly List<(string Name, int Label, Point3D Point, Map Map)> _entries;
    private static readonly PMList[] Facets = { PMList.Trammel, PMList.Felucca, PMList.Ilshenar, PMList.Malas, PMList.Tokuno, PMList.TerMur };
    public HavenTravelGump(OfflineTravelBook book, int category = 0, int facet = 0, int page = 0) : base(60, 50)
    {
        _book = book; _category = Math.Clamp(category, 0, 1); _facet = Math.Clamp(facet, 0, Facets.Length - 1);
        _entries = Entries(_category, _facet); _page = Math.Clamp(page, 0, Math.Max(0, (_entries.Count - 1) / 10));
        AddBackground(0, 0, 610, 475, 9270); AddImageTiled(15, 15, 580, 57, 2624);
        AddItem(30, 28, 0x22C5, 0x8A5); AddLabel(80, 26, 1152, "The Wayfarer's Atlas");
        AddLabel(80, 49, 2101, "Towns, dungeon entrances and Haven Commons");
        for (var i = 0; i < 2; i++)
        { AddButton(25 + i * 155, 85, 4005, 4007, 10 + i); AddLabel(60 + i * 155, 85, _category == i ? 1152 : 2101, i == 0 ? "Towns" : "Dungeons"); }
        AddButton(420, 85, 4005, 4007, 12); AddLabel(455, 85, 1152, "Commons");
        AddLabel(30, 115, 2101, Facets[_facet].Map.Name);
        AddButton(420, 115, 4005, 4007, 13); AddLabel(455, 115, 2101, "Abyss routes");
        for (var row = 0; row < 10 && _page * 10 + row < _entries.Count; row++)
        {
            var entry = _entries[_page * 10 + row]; var y = 143 + row * 24;
            AddButton(30, y, 4005, 4007, 100 + row);
            if (entry.Label > 0) { AddHtmlLocalized(68, y, 485, 22, entry.Label, 0xFFFFFF); }
            else { AddLabel(68, y, 1152, entry.Name); }
        }
        if (_entries.Count == 0) { AddLabel(35, 160, 2101, "No verified dungeon entrances for this facet yet."); }
        for (var i = 0; i < Facets.Length; i++)
        { AddButton(24 + i * 96, 398, 4005, 4007, 20 + i); AddLabel(59 + i * 96, 398, _facet == i ? 1152 : 2101, i == 5 ? "Ter Mur" : Facets[i].Map.Name); }
        AddButton(25, 439, 4014, 4016, 30); AddLabel(60, 439, 1152, "Previous");
        AddLabel(223, 439, 2101, $"Page {_page + 1} / {Math.Max(1, (_entries.Count + 9) / 10)}");
        AddButton(365, 439, 4005, 4007, 31); AddLabel(400, 439, 1152, "Next");
        AddButton(505, 439, 4017, 4019, 0); AddLabel(540, 439, 1152, "Close");
    }
    internal static List<(string Name, int Label, Point3D Point, Map Map)> Entries(int category, int facet)
    {
        var entries = new List<(string, int, Point3D, Map)>(); var list = Facets[Math.Clamp(facet, 0, Facets.Length - 1)];
        if (category == 0)
        {
            if (list.Map == Map.Tokuno)
            {
                foreach (var den in HavenSnowBearDen.Registry)
                {
                    if (!den.Deleted && den.Map == Map.Tokuno)
                    { entries.Add(("Frostbound bear den — northern snowfields", 0, den.Location, Map.Tokuno)); break; }
                }
            }
            if (list.Map == Map.Trammel)
            {
                entries.Add(("New Haven bank and recovery", 0, HavenRecovery.BankLocation, Map.Trammel));
                if (HavenFrontierHub.Registry.Count == 1) { entries.Add(("Chelonia — tortoises and corsair expeditions", 0, HavenChelonia.Landing, Map.Trammel)); }
            }
            foreach (var entry in list.Entries) { entries.Add((null, entry.Number, entry.Location, list.Map)); }
        }
        else
        {
            if (HavenFrontierHub.Registry.Count == 1 && list.Map == HavenFrontierSupport.ShadowMap)
            {
                entries.Add(("Shadowguard — Eodon fortress", 0, HavenFrontierSupport.ShadowLanding, HavenFrontierSupport.ShadowMap));
            }
            if (list.Map == Map.Trammel && HavenFrontierHub.Registry.Count == 1)
            {
                entries.Add(("Blackthorn — captains and rift beacons", 0, HavenFrontierSupport.RiftLanding, Map.Trammel));
            }
            foreach (var entry in UOOfflineDungeonPortal.Destinations)
            { if (entry.Map == list.Map) { entries.Add((entry.Name, 0, entry.Location, entry.Map)); } }
        }
        return entries;
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (info.ButtonID == 0 || !HavenPortableTravel.CanUse(from, _book)) { return; }
        var category = _category; var facet = _facet; var page = _page;
        if (info.ButtonID is 10 or 11) { category = info.ButtonID - 10; page = 0; }
        else if (info.ButtonID >= 20 && info.ButtonID < 20 + Facets.Length) { facet = info.ButtonID - 20; page = 0; }
        else if (info.ButtonID == 30) { page--; }
        else if (info.ButtonID == 31) { page++; }
        else if (info.ButtonID == 12) { if (SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom, out _) && HavenCommunityCenter.Travel(from)) { return; } }
        else if (info.ButtonID == 13)
        { HavenAbyssExpedition.Open(from); return; }
        else if (info.ButtonID >= 100 && info.ButtonID < 110 && _page * 10 + info.ButtonID - 100 < _entries.Count)
        {
            var e = _entries[_page * 10 + info.ButtonID - 100];
            if (HavenPortableTravel.Go(from, _book, e.Map, e.Point)) { return; }
            from.SendMessage("That destination is blocked or travel is restricted here.");
        }
        from.SendGump(new HavenTravelGump(_book, category, facet, page));
    }
}
