using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.CustomBots;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenFellowshipBoard : Item
{
    [Constructible] public HavenFellowshipBoard() : base(0x1E5F) { Name = "Fellowship Board — guild recruits"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    { if (from.Map == Map && from.InRange(this, 3)) { from.CloseGump<HavenGuildRosterGump>(); from.SendGump(new HavenGuildRosterGump(from)); } }
}

public sealed class HavenFellowshipGump : Gump
{
    private readonly HavenGuildCrew _crew;
    private readonly int _tab, _page;
    private readonly List<Item> _items = new();
    private static readonly string[] Tabs = { "Orders", "Skills", "Equipment", "Products", "History" };
    internal static void Open(Mobile from, HavenGuildCrew crew, int tab = 0, int page = 0)
    {
        if (crew?.Deleted != false || !crew.CanRead(from)) { return; }
        from.CloseGump<HavenFellowshipGump>(); from.SendGump(new HavenFellowshipGump(crew, tab, page));
    }
    private HavenFellowshipGump(HavenGuildCrew crew, int tab, int page) : base(65, 65)
    {
        _crew = crew; _tab = Math.Clamp(tab, 0, 4); _page = Math.Max(0, page);
        var worker = crew.Worker;
        AddBackground(0, 0, 625, 495, 9270); AddLabel(25, 20, 1152, $"Fellowship — {worker?.Name ?? "unclaimed guild goods"}");
        AddLabel(25, 48, 2101, $"Completed jobs: {crew.CompletedJobs:N0} · Crafted items: {crew.CraftsCompleted:N0}");
        for (var i = 0; i < Tabs.Length; i++) { AddButton(20 + i * 119, 82, 4005, 4007, 10 + i); AddLabel(54 + i * 119, 82, _tab == i ? 1152 : 2101, Tabs[i]); }
        if (_tab == 0)
        {
            var status = !crew.ValidGuild ? "Recruit unavailable" : !crew.AutoWork ? "Work paused" : BotPlayerParty.InPlayerParty(worker) ? "Adventuring with party" : $"{crew.Job}: next job in {Math.Max(0, (crew.Due - Core.Now).TotalMinutes):F1} minutes";
            AddLabel(25, 125, 1152, status);
            AddLabel(25, 157, 2101, "Five-minute expeditions; no work while fighting or in a player party.");
            var jobs = Enum.GetValues<HavenGuildJob>();
            for (var i = 0; i < jobs.Length; i++)
            { AddButton(25 + i % 2 * 285, 197 + i / 2 * 38, 4005, 4007, 100 + i); AddLabel(60 + i % 2 * 285, 197 + i / 2 * 38, 1152, jobs[i].ToString()); }
            AddButton(25, 325, 4005, 4007, 1); AddLabel(60, 325, 1152, crew.AutoWork ? "Pause work" : "Resume work");
            AddButton(310, 325, 4005, 4007, 2); AddLabel(345, 325, 1152, "Invite to party");
            AddButton(25, 363, 4005, 4007, 3); AddLabel(60, 363, 1152, "Resource balances");
            AddButton(310, 363, 4005, 4007, 4); AddLabel(345, 363, 1152, "Treasurer access");
            AddLabel(25, 400, 2101, "Officers give orders. The leader grants treasury access per recruit.");
        }
        else if (_tab == 1 && worker != null)
        {
            AddLabel(25, 125, 1152, $"STR {worker.RawStr}   DEX {worker.RawDex}   INT {worker.RawInt}   Stat cap {worker.StatCap}");
            var skills = new List<Skill>(); foreach (var skill in worker.Skills) { if (skill.Base > 0) { skills.Add(skill); } }
            skills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            _page = Math.Min(_page, Math.Max(0, (skills.Count - 1) / 9));
            for (var i = 0; i < 9 && _page * 9 + i < skills.Count; i++)
            { var skill = skills[_page * 9 + i]; AddLabel(25, 166 + i * 27, 2101, skill.Name); AddLabel(325, 166 + i * 27, 1152, $"{skill.Base:F1} / {skill.Cap:F1}"); }
        }
        else if (_tab is 2 or 3)
        {
            var all = new List<Item>();
            if (_tab == 2 && worker != null)
            {
                foreach (var item in worker.Items) { if (Equipment(item)) { all.Add(item); } }
                if (worker.Backpack != null) { foreach (var item in worker.Backpack.Items) { if (Equipment(item)) { all.Add(item); } } }
            }
            else if (_tab == 3) { foreach (var item in crew.Products) { if (item?.Deleted == false && item.Parent == crew) { all.Add(item); } } }
            _page = Math.Min(_page, Math.Max(0, (all.Count - 1) / 7));
            AddHtml(25, 120, 575, 38, _tab == 2 ? "<BASEFONT COLOR=#FFFFFF>Inspect equipped and spare gear. Only added gear may be withdrawn.</BASEFONT>" : "<BASEFONT COLOR=#FFFFFF>Crafted from guild materials. Treasury permission is needed to withdraw.</BASEFONT>");
            for (var i = 0; i < 7 && _page * 7 + i < all.Count; i++)
            {
                var item = all[_page * 7 + i]; _items.Add(item); var y = 163 + i * 33;
                AddItem(25, y, item.ItemID, item.Hue); AddItemProperty(item.Serial);
                AddLabelCropped(70, y, 425, 23, 1152, $"{HavenMarketDirectory.Describe(item)}{(item.Parent == worker ? " (worn)" : "")}");
                if (_tab == 3 || !crew.OriginalEquipment.Contains(item)) { AddButton(540, y, 4005, 4007, 200 + i); }
            }
            if (_tab == 2) { AddButton(25, 407, 4005, 4007, 5); AddLabel(60, 407, 1152, "Give / equip an item"); }
        }
        else if (_tab == 4)
        {
            var lines = new List<string>(); for (var i = crew.History.Count - 1; i >= 0; i--) { lines.Add(System.Net.WebUtility.HtmlEncode(crew.History[i])); }
            AddHtml(25, 125, 575, 300, $"<BASEFONT COLOR=#FFFFFF>{string.Join("<BR><BR>", lines)}</BASEFONT>", false, true);
        }
        AddButton(25, 448, 4014, 4016, 6); AddLabel(60, 448, 1152, "Previous");
        AddButton(170, 448, 4005, 4007, 7); AddLabel(205, 448, 1152, "Next");
        AddButton(310, 448, 4005, 4007, 8); AddLabel(345, 448, 1152, "Roster");
        AddButton(490, 448, 4017, 4019, 0); AddLabel(525, 448, 1152, "Close");
    }
    private static bool Equipment(Item item) => item?.Deleted == false && !item.IsVirtualItem && item is BaseWeapon or BaseArmor or BaseClothing or BaseJewel or Spellbook;
    internal static bool GiveEquipment(HavenGuildCrew crew, Mobile from, Item item)
    {
        var worker = crew.Worker;
        if (!crew.CanManage(from) || !crew.ValidGuild || worker.Combatant != null || worker.Map != from.Map || !from.InRange(worker, 3) ||
            from.Backpack == null || item?.IsChildOf(from.Backpack) != true || !Equipment(item) || !item.Movable || worker.Backpack == null) { return false; }
        var old = worker.FindItemOnLayer(item.Layer);
        if (old != null && !worker.Backpack.TryDropItem(worker, old, false)) { return false; }
        if (worker.EquipItem(item)) { crew.Log($"Equipped {HavenMarketDirectory.Describe(item)}"); return true; }
        if (old != null) { worker.EquipItem(old); }
        return false;
    }
    internal static bool TakeEquipment(HavenGuildCrew crew, Mobile from, Item item)
    {
        if (!crew.CanWithdraw(from) || !crew.ValidGuild || crew.Worker.Combatant != null || crew.Worker.Map != from.Map || !from.InRange(crew.Worker, 3) ||
            !Equipment(item) || item.RootParent != crew.Worker || crew.OriginalEquipment.Contains(item) || from.Backpack == null) { return false; }
        if (!from.Backpack.TryDropItem(from, item, true)) { return false; }
        crew.Log($"Returned equipment: {HavenMarketDirectory.Describe(item)}"); return true;
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile; if (info.ButtonID == 0 || _crew.Deleted || !_crew.CanRead(from)) { return; }
        if (info.ButtonID is >= 10 and <= 14) { Open(from, _crew, info.ButtonID - 10); return; }
        if (info.ButtonID is 6 or 7) { Open(from, _crew, _tab, _page + (info.ButtonID == 6 ? -1 : 1)); return; }
        if (info.ButtonID == 8) { from.SendGump(new HavenGuildRosterGump(from)); return; }
        if (info.ButtonID == 3) { from.SendGump(new HavenGuildCrewGump(_crew)); return; }
        if (info.ButtonID == 2 && _crew.Worker != null) { BotPlayerParty.TryRecruitToPlayer(from, _crew.Worker, 1); }
        else if (info.ButtonID is >= 100 and < 106) { _crew.SetJob(from, (HavenGuildJob)(info.ButtonID - 100)); }
        else if (info.ButtonID == 1 && _crew.CanManage(from)) { _crew.AutoWork = !_crew.AutoWork; }
        else if (info.ButtonID == 4 && _crew.Guild?.Leader == from)
        { from.SendMessage("Target a guild member to grant or revoke withdrawal access for this recruit."); from.Target = new CrewTarget(_crew, false); return; }
        else if (info.ButtonID == 5 && _crew.CanManage(from))
        { from.SendMessage("Stand near your recruit and target equipment in your backpack."); from.Target = new CrewTarget(_crew, true); return; }
        else if (info.ButtonID >= 200 && info.ButtonID < 200 + _items.Count)
        {
            var item = _items[info.ButtonID - 200]; var success = _tab == 3 ? HavenGuildCrafting.TakeProduct(_crew, from, item) : TakeEquipment(_crew, from, item);
            if (!success) { from.SendMessage("Could not withdraw. Check permissions, space, ownership and distance to the recruit."); }
        }
        Open(from, _crew, _tab, _page);
    }
    private sealed class CrewTarget : Target
    {
        private readonly HavenGuildCrew _crew; private readonly bool _equipment;
        public CrewTarget(HavenGuildCrew crew, bool equipment) : base(12, false, TargetFlags.None) { _crew = crew; _equipment = equipment; }
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_crew.Deleted) { return; }
            var success = _equipment ? targeted is Item item && GiveEquipment(_crew, from, item) : targeted is Mobile member && _crew.SetTreasurer(from, member);
            from.SendMessage(success ? "Fellowship updated." : "Could not apply that change. Check permissions, ownership and distance.");
            Open(from, _crew, _equipment ? 2 : 0);
        }
    }
}
