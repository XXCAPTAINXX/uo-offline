using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Commands;
using Server.CustomBots;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

// Guild records own work and output; bots retain their native skills and equipment.
[SerializationGenerator(0)]
public partial class HavenGuildCrew : Item
{
    [SerializableField(0)] private PlayerBot _worker;
    [SerializableField(1)] private Mobile _recruiter;
    [SerializableField(2)] private Guild _guild;
    [SerializableField(3)] private DateTime _due;
    [SerializableField(4)] private long _completedJobs;
    [SerializableField(5)] private bool _autoWork = true;
    [SerializableField(6)] private List<long> _resources = new();
    [SerializableField(7)] private List<Item> _originalEquipment = new();
    [SerializableField(8)] private HavenGuildJob _job;
    [SerializableField(9)] private List<string> _history = new();
    [SerializableField(10)] private List<Item> _products = new();
    [SerializableField(11)] private List<Mobile> _treasurers = new();
    [SerializableField(12)] private long _craftsCompleted;
    private Timer _timer;
    internal static readonly Dictionary<PlayerBot, HavenGuildCrew> Registry = new();
    [Constructible]
    public HavenGuildCrew() : base(0xFF1) { Name = "Fellowship work ledger"; LootType = LootType.Blessed; Weight = 1; }
    internal static bool Retained(PlayerBot bot) => bot != null && Registry.TryGetValue(bot, out var crew) && !crew.Deleted;
    internal static bool Working(PlayerBot bot) => Registry.TryGetValue(bot, out var crew) && crew.AutoWork && crew.ValidGuild && !BotPlayerParty.InPlayerParty(bot) && bot.Alive && bot.Combatant == null;
    internal bool ValidGuild => Guild != null && !Guild.Disbanded && Worker?.Deleted == false && Worker.Guild == Guild;
    internal static bool Officer(Mobile from, Guild guild) => guild?.Disbanded == false && from?.Guild == guild &&
        (guild.Leader == from || from is PlayerMobile player && player.GuildRank.GetFlag(RankFlags.CanPromoteDemote));
    internal bool CanManage(Mobile from) => Officer(from, Guild) || from != null && Recruiter == from && !ValidGuild;
    internal bool CanWithdraw(Mobile from) => from != null && (Guild?.Disbanded == false && from.Guild == Guild && (Guild.Leader == from || Treasurers.Contains(from)) || Recruiter == from && !ValidGuild);
    internal bool CanRead(Mobile from) => CanManage(from) || ValidGuild && from?.Guild == Guild;
    public override bool CheckLift(Mobile from, Item item, ref LRReason reason) => item == this && base.CheckLift(from, item, ref reason);
    public override bool CheckItemUse(Mobile from, Item item) => item == this && CanRead(from);
    public override bool CheckTarget(Mobile from, Target target, object targeted) => targeted == this && CanRead(from);
    internal void Log(string text)
    { History.Add($"{Core.Now:MM-dd HH:mm} UTC {text}"); while (History.Count > 20) { History.RemoveAt(0); } this.MarkDirty(); }
    internal bool SetJob(Mobile from, HavenGuildJob job)
    {
        if (!CanManage(from) || !Enum.IsDefined(job)) { return false; }
        Job = job; Due = Core.Now + TimeSpan.FromMinutes(5); Log($"Order: {job}"); return true;
    }
    internal bool SetTreasurer(Mobile from, Mobile member)
    {
        if (Guild?.Disbanded != false || Guild.Leader != from || member?.Deleted != false || member.Guild != Guild || member == from) { return false; }
        if (!Treasurers.Remove(member)) { Treasurers.Add(member); } this.MarkDirty(); return true;
    }
    internal static bool CanParty(Mobile from, PlayerBot bot)
    {
        if (!Registry.TryGetValue(bot, out var crew)) { return true; }
        if (!crew.ValidGuild || from.Guild != crew.Guild) { return false; }
        var count = 0;
        if (Server.Engines.PartySystem.Party.Get(from) is { } party)
        {
            foreach (var member in party.Members) { if (member.Mobile is PlayerBot other && other != bot && Retained(other)) { count++; } }
            foreach (var candidate in party.Candidates) { if (candidate is PlayerBot other && other != bot && Retained(other)) { count++; } }
        }
        return count < 2;
    }

    internal static HavenGuildCrew Recruit(Mobile from, PlayerBot bot)
    {
        if (from?.Guild is not Guild guild || !Officer(from, guild) || bot?.Deleted != false || !bot.Alive ||
            bot.Guild != null || bot.Kills >= 5 || bot.LoggingOut || bot.LifecycleExempt || bot.Combatant != null ||
            bot.Map != from.Map || !from.InRange(bot, 6) || Retained(bot) || BotPartyManager.IsInParty(bot) || BotPlayerParty.InPlayerParty(bot)) { return null; }
        var count = 0; foreach (var record in Registry.Values) { if (record.Guild == guild) { count++; } }
        if (count >= 4 || from.Backpack == null) { return null; }
        var crew = new HavenGuildCrew { Worker = bot, Recruiter = from, Guild = guild, Due = Core.Now + TimeSpan.FromMinutes(5) };
        foreach (var item in bot.Items) { crew.OriginalEquipment.Add(item); }
        foreach (var item in bot.Backpack.FindItemsByType<Item>()) { crew.OriginalEquipment.Add(item); }
        if (!from.Backpack.TryDropItem(from, crew, false)) { crew.Delete(); return null; }
        guild.AddMember(bot); bot.BotGuildIndex = -1; crew.Log($"{bot.Name} recruited"); crew.Schedule(); return crew;
    }
    [AfterDeserialization]
    private void Schedule()
    {
        _timer?.Stop(); _timer = null;
        if (Deleted) { return; }
        if (Worker?.Deleted == false) { Registry[Worker] = this; }
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(30), Pulse);
    }
    private void Pulse()
    {
        if (Worker?.Deleted == true) { Registry.Remove(Worker); Worker = null; AutoWork = false; }
        Work(Core.Now); Schedule();
    }
    internal void Work(DateTime now)
    {
        if (!ValidGuild || !AutoWork || !Worker.Alive || Worker.Combatant != null || BotPlayerParty.InPlayerParty(Worker))
        { Due = now + TimeSpan.FromMinutes(5); return; }
        if (now < Due) { return; }
        // Credit one job and advance its due time on the same main-loop turn.
        // No loop over missed intervals: downtime never manufactures a backlog.
        Due = now + TimeSpan.FromMinutes(5);
        if (Job is HavenGuildJob.Craft or HavenGuildJob.Auto && HavenGuildCrafting.TryWork(this))
        { CompletedJobs++; ImproveStats(); return; }
        if (Job == HavenGuildJob.Train)
        {
            var trained = Worker.Skills[HavenGuildCrafting.BestSkill(Worker)]; trained.Base = Math.Min(trained.Cap, trained.Base + .2);
            CompletedJobs++; ImproveStats(); Log($"Training: {trained.Name} {trained.Base:F1}"); return;
        }
        long total = 0; foreach (var balance in Resources) { total += balance; }
        if (total >= 100000) { return; }
        var kind = Job switch
        {
            HavenGuildJob.Mine => HavenExpeditionKind.Ore, HavenGuildJob.Wood => HavenExpeditionKind.Wood,
            HavenGuildJob.Hunt => HavenExpeditionKind.Leather,
            HavenGuildJob.Craft => HavenGuildCrafting.GatherFor(Worker),
            _ => Worker.Skills.Mining.Base >= Worker.Skills.Lumberjacking.Base ? HavenExpeditionKind.Ore : HavenExpeditionKind.Wood
        };
        var skillName = kind == HavenExpeditionKind.Ore ? SkillName.Mining : SkillName.Lumberjacking;
        var skill = Worker.Skills[skillName];
        if (kind == HavenExpeditionKind.Leather || Job == HavenGuildJob.Auto && skill.Base < 20)
        {
            kind = HavenExpeditionKind.Leather;
            skillName = SkillName.Tactics;
            skill = Worker.Skills[skillName];
        }
        var index = HavenMissionResources.SelectIndex(kind, skill.Base, Utility.RandomDouble());
        while (Resources.Count <= index) { Resources.Add(0); }
        var amount = Math.Min(100, 100000 - total);
        var basic = kind == HavenExpeditionKind.Ore ? 0 : kind == HavenExpeditionKind.Wood ? 9 : 23;
        if (basic != index) { Resources[basic] += amount / 2; Resources[index] += amount - amount / 2; }
        else { Resources[index] += amount; }
        this.MarkDirty();
        CompletedJobs++;
        skill.Base = Math.Min(skill.Cap, skill.Base + 0.1);
        ImproveStats(); Log($"Gathered {amount} resources: {HavenResourceCatalog.Entries[basic].Name} / {HavenResourceCatalog.Entries[index].Name}");
    }
    internal void ImproveStats()
    {
        if (CompletedJobs % 12 != 0 || Worker.RawStr + Worker.RawDex + Worker.RawInt >= Worker.StatCap) { return; }
        if (Worker.RawInt <= Worker.RawDex && Worker.RawInt <= Worker.RawStr) { Worker.RawInt++; }
        else if (Worker.RawDex <= Worker.RawStr) { Worker.RawDex++; }
        else { Worker.RawStr++; }
    }
    internal bool Withdraw(Mobile from, int index, int amount)
    {
        if (!CanWithdraw(from) || from.Backpack == null || index < 0 || index >= Resources.Count || index >= HavenResourceCatalog.Entries.Length || amount <= 0 || amount > 60000 || Resources[index] < amount) { return false; }
        var resource = HavenResourceCatalog.Entries[index].Create(amount); var deed = new CommodityDeed();
        if (!deed.SetCommodity(resource)) { deed.Delete(); resource.Delete(); return false; }
        if (!from.Backpack.TryDropItem(from, deed, false)) { deed.Delete(); return false; }
        Resources[index] -= amount; this.MarkDirty(); Log($"Withdrew {amount} {HavenResourceCatalog.Entries[index].Name}"); return true;
    }
    internal void Dismiss(Mobile from)
    {
        if (!CanManage(from)) { return; }
        AutoWork = false;
        if (Worker != null)
        {
            var recipient = CanWithdraw(from) ? from : Guild?.Leader;
            if (recipient?.Deleted != false) { return; }
            var destination = recipient == from ? from.Backpack : recipient.BankBox;
            if (destination == null) { return; }
            var cargo = new List<Item>();
            foreach (var item in Worker.Backpack.FindItemsByType<Item>()) { cargo.Add(item); }
            foreach (var item in cargo)
            { if (!item.IsVirtualItem && !OriginalEquipment.Contains(item)) { destination.DropItem(item); } }
            foreach (var item in Worker.Items.ToArray())
            { if (!item.IsVirtualItem && !OriginalEquipment.Contains(item) && item.Layer is not Layer.Backpack and not Layer.Bank and not Layer.Hair and not Layer.FacialHair and not Layer.Mount) { destination.DropItem(item); } }
            Registry.Remove(Worker);
            if (Worker.Guild == Guild) { Guild.RemoveMember(Worker); }
            Worker = null;
        }
        from.SendMessage("Recruit released. Added equipment was returned to your pack if you have treasury access, or to the guild leader's bank. The ledger retains unclaimed resources.");
    }
    public override void OnDoubleClick(Mobile from)
    { if (CanRead(from)) { HavenFellowshipGump.Open(from, this); } }
    public override void OnDelete()
    {
        _timer?.Stop(); _timer = null;
        var owner = Guild?.Leader?.Deleted == false ? Guild.Leader : Recruiter;
        if (owner?.Deleted == false)
        {
            var bank = owner.BankBox;
            foreach (var product in Products.ToArray()) { if (product?.Deleted == false && product.Parent == this) { bank.DropItem(product); } }
            if (Worker?.Deleted == false)
            {
                var cargo = new List<Item>();
                foreach (var item in Worker.Backpack.FindItemsByType<Item>()) { cargo.Add(item); }
                foreach (var item in cargo)
                { if (!item.IsVirtualItem && !OriginalEquipment.Contains(item)) { bank.DropItem(item); } }
                foreach (var item in Worker.Items.ToArray())
                { if (!item.IsVirtualItem && !OriginalEquipment.Contains(item) && item.Layer is not Layer.Backpack and not Layer.Bank and not Layer.Hair and not Layer.FacialHair and not Layer.Mount) { bank.DropItem(item); } }
            }
            for (var i = 0; i < Resources.Count && i < HavenResourceCatalog.Entries.Length; i++)
            {
                while (Resources[i] > 0)
                {
                    var amount = (int)Math.Min(Resources[i], 60000);
                    var resource = HavenResourceCatalog.Entries[i].Create(amount); var deed = new CommodityDeed();
                    if (deed.SetCommodity(resource)) { bank.DropItem(deed); }
                    else { deed.Delete(); bank.DropItem(resource); }
                    Resources[i] -= amount;
                }
            }
        }
        if (Worker != null) { Registry.Remove(Worker); if (Worker.Guild == Guild && Guild != null) { Guild.RemoveMember(Worker); } }
        Worker = null; Recruiter = null; Guild = null; OriginalEquipment.Clear(); Resources.Clear(); Products.Clear(); Treasurers.Clear(); History.Clear(); base.OnDelete();
    }
}

public class HavenGuildCrewGump : Gump
{
    private readonly HavenGuildCrew _crew;
    private readonly int _page;
    public HavenGuildCrewGump(HavenGuildCrew crew, int page = 0) : base(80, 80)
    {
        _crew = crew; AddBackground(0, 0, 480, 510, 9270);
        AddLabel(20, 20, 1152, $"Fellowship — {crew.Worker?.Name ?? "unclaimed resources"}");
        AddLabel(20, 50, 0, $"Gathering jobs completed: {crew.CompletedJobs}");
        AddLabel(20, 76, 0, "Jobs take five minutes; joining a party pauses work.");
        AddButton(20, 110, 4005, 4007, 1); AddLabel(55, 110, 0, crew.AutoWork ? "Pause automatic work" : "Resume automatic work");
        AddButton(240, 110, 4005, 4007, 2); AddLabel(275, 110, 0, "Invite to party");
        AddLabel(20, 148, 0, "Withdraw amount:"); AddTextEntry(180, 145, 100, 24, 0, 1, "100");
        var indices = new List<int>();
        for (var i = 0; i < crew.Resources.Count; i++) { if (crew.Resources[i] > 0) { indices.Add(i); } }
        _page = Math.Clamp(page, 0, Math.Max(0, (indices.Count - 1) / 8));
        for (var row = 0; row < 8 && _page * 8 + row < indices.Count; row++)
        {
            var i = indices[_page * 8 + row];
            AddButton(20, 180 + row * 30, 4005, 4007, 100 + i);
            AddLabel(55, 180 + row * 30, 0, $"{HavenResourceCatalog.Entries[i].Name}: {crew.Resources[i]:N0}");
        }
        AddButton(20, 425, 4014, 4016, 10); AddLabel(55, 425, 0, "Previous");
        AddButton(160, 425, 4005, 4007, 11); AddLabel(195, 425, 0, "Next");
        AddButton(310, 425, 4005, 4007, 12); AddLabel(345, 425, 0, "Roster");
        AddButton(20, 455, 4005, 4007, 3); AddLabel(55, 455, 0, "Dismiss recruit");
        AddButton(315, 455, 4017, 4019, 0); AddLabel(350, 455, 0, "Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (_crew.Deleted || !_crew.CanRead(from) || info.ButtonID == 0) { return; }
        if (info.ButtonID == 12) { from.SendGump(new HavenGuildRosterGump(from)); return; }
        if (info.ButtonID is 10 or 11) { from.SendGump(new HavenGuildCrewGump(_crew, _page + (info.ButtonID == 10 ? -1 : 1))); return; }
        if (info.ButtonID == 2 && _crew.Worker != null) { BotPlayerParty.TryRecruitToPlayer(from, _crew.Worker, 1); }
        else if (info.ButtonID >= 100 && int.TryParse(info.GetTextEntry(1), out var amount))
        { _crew.Withdraw(from, info.ButtonID - 100, amount); }
        else if (_crew.CanManage(from))
        {
            if (info.ButtonID == 1) { _crew.AutoWork = !_crew.AutoWork; }
            else if (info.ButtonID == 3) { _crew.Dismiss(from); }
        }
        from.SendGump(new HavenGuildCrewGump(_crew, _page));
    }
}

public class HavenGuildRosterGump : Gump
{
    private readonly HavenGuildCrew[] _crews;
    public HavenGuildRosterGump(Mobile from) : base(80, 80)
    {
        _crews = HavenGuildCrew.Registry.Values.Where(crew => crew.CanRead(from)).Take(4).ToArray();
        AddBackground(0, 0, 460, 290, 9270); AddLabel(20, 20, 1152, "Fellowship roster");
        AddLabel(20, 47, 0, "Four guild recruits; up to two in an adventuring party.");
        for (var i = 0; i < _crews.Length; i++)
        {
            var crew = _crews[i]; AddButton(20, 85 + i * 36, 4005, 4007, i + 1);
            AddLabel(55, 85 + i * 36, 0, $"{crew.Worker?.Name} — {(crew.AutoWork ? "auto work" : "available")}");
        }
        if (_crews.Length == 0) { AddLabel(20, 90, 0, "Guild leaders: use [guildcrew recruit near a bot."); }
        AddButton(320, 244, 4017, 4019, 0); AddLabel(355, 244, 0, "Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    { if (info.ButtonID > 0 && info.ButtonID <= _crews.Length) { _crews[info.ButtonID - 1].OnDoubleClick(sender.Mobile); } }
}

public static class HavenGuildCrewCommands
{
    public static void Initialize()
    {
        CommandSystem.Register("GuildCrew", AccessLevel.Player, e =>
        {
            if (e.ArgString.Trim().Equals("recruit", StringComparison.OrdinalIgnoreCase))
            { e.Mobile.Target = new RecruitTarget(); e.Mobile.SendMessage("Target a nearby available bot to recruit for your guild."); return; }
            e.Mobile.SendGump(new HavenGuildRosterGump(e.Mobile));
        });
    }
    private sealed class RecruitTarget : Target
    {
        public RecruitTarget() : base(6, false, TargetFlags.None) { }
        protected override void OnTarget(Mobile from, object targeted)
        { if (targeted is not PlayerBot bot || HavenGuildCrew.Recruit(from, bot) == null) { from.SendMessage("Recruitment requires the guild leader, a willing unaffiliated bot, backpack space, and fewer than four recruits."); } }
    }
}
