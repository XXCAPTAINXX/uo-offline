using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModernUO.Serialization;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenMissionSnapshot
{
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Int { get; set; }
    public double Level { get; set; }
    public Dictionary<string,double> Skills { get; set; } = new();
    public Dictionary<uint,HavenMissionGear> Gear { get; set; } = new();
}
public sealed class HavenMissionGear
{
    public string Name { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
    public string Stats { get; set; }
}
public sealed class HavenMissionReceipt
{
    public string Name { get; set; }
    public long Amount { get; set; }
    public int Icon { get; set; }
    public int Hue { get; set; }
    public string Stats { get; set; }
    public string Destination { get; set; }
}
public sealed class HavenMissionReport
{
    public DateTime Started { get; set; }
    public DateTime Ended { get; set; }
    public string Outcome { get; set; } = "In progress";
    public int Runs { get; set; }
    public int EarlyReturns { get; set; }
    public int Minutes { get; set; }
    public int TrainingMinutes { get; set; }
    public int EarlierRuns { get; set; }
    public Dictionary<string,int> Missions { get; set; } = new();
    public HavenMissionSnapshot Before { get; set; }
    public HavenMissionSnapshot After { get; set; }
    public List<HavenMissionReceipt> Loot { get; set; } = new();
}

[SerializationGenerator(0)]
public partial class HavenMissionJournal : Item
{
    [SerializableField(0)] private HavenCompanion _companion;
    [SerializableField(1)] private string _data = "[]";
    [SerializableField(2)] private bool _active;
    [SerializableField(3)] private bool _unread;
    private Timer _timer;
    public override bool IsVirtualItem => true;
    [Constructible] public HavenMissionJournal():base(1) { Name="companion field journal"; Visible=false; Movable=false; Weight=0; }
    internal List<HavenMissionReport> Reports => JsonSerializer.Deserialize<List<HavenMissionReport>>(Data) ?? new();
    private void Store(List<HavenMissionReport> reports) { Data=JsonSerializer.Serialize(reports); }
    internal static HavenMissionJournal Find(HavenCompanion companion) => companion?.Backpack?.FindItemByType<HavenMissionJournal>();
    internal static HavenMissionJournal Begin(HavenCompanion companion,DateTime now)
    {
        var journal=Find(companion);
        var firstJournal=journal==null;
        if(journal==null) { journal=new HavenMissionJournal { Companion=companion }; companion.Backpack.DropItem(journal); }
        if(journal.Active) { return journal; }
        companion.UpdateTraining(now);
        var reports=journal.Reports;
        reports.Insert(0,new HavenMissionReport { Started=now,Before=Snapshot(companion),After=Snapshot(companion),
            EarlierRuns=firstJournal ? companion.Backpack.FindItemByType<HavenCompanionGearAssignment>()?.Completed ?? 0 : 0 });
        if(reports.Count>10) { reports.RemoveRange(10,reports.Count-10); }
        journal.Active=true; journal.Unread=false; journal.Store(reports); journal.Schedule(); return journal;
    }
    internal static string ItemName(Item item) => item.Name ?? item.DefaultName ?? Regex.Replace(item.GetType().Name,"([a-z])([A-Z])","$1 $2");
    internal static string Details(Item item)
    {
        var rows=new List<string>();
        if(item is IAosItem aos)
        {
            foreach(var attribute in Enum.GetValues<AosAttribute>())
            { if(aos.Attributes[attribute]!=0) { rows.Add($"{attribute}: {aos.Attributes[attribute]}"); } }
        }
        if(item is BaseWeapon weapon)
        {
            rows.Add($"Damage {weapon.MinDamage}-{weapon.MaxDamage}; speed {weapon.Speed}; slayers {weapon.Slayer}/{weapon.Slayer2}");
            foreach(var attribute in Enum.GetValues<AosWeaponAttribute>())
            { if(weapon.WeaponAttributes[attribute]!=0) { rows.Add($"{attribute}: {weapon.WeaponAttributes[attribute]}"); } }
        }
        if(item is BaseArmor armor) { rows.Add($"Resists P/F/C/P/E: {armor.PhysicalResistance}/{armor.FireResistance}/{armor.ColdResistance}/{armor.PoisonResistance}/{armor.EnergyResistance}"); }
        if(item is BaseJewel jewel)
        { for(var i=0;i<5;i++) { if(jewel.SkillBonuses.GetBonus(i)!=0) { rows.Add($"{jewel.SkillBonuses.GetSkill(i)}: {jewel.SkillBonuses.GetBonus(i):F1}"); } } }
        return string.Join("; ",rows);
    }
    internal static HavenMissionSnapshot Snapshot(HavenCompanion companion)
    {
        var snapshot=new HavenMissionSnapshot { Str=companion.RawStr,Dex=companion.RawDex,Int=companion.RawInt,Level=companion.TrainingLevel };
        foreach(var skill in companion.Skills) { snapshot.Skills[skill.Name]=skill.Base; }
        foreach(var item in companion.Items)
        {
            if(item is not IAosItem) { continue; }
            var experience=item switch { IEvolvingStarterWeapon weapon=>weapon.Experience,HavenLevelingCape cape=>cape.Experience,ApprenticeGrimoire book=>book.Experience,_=>HavenGearExperience.Find(item)?.Experience ?? 0 };
            snapshot.Gear[item.Serial.Value]=new HavenMissionGear { Name=ItemName(item),Level=HavenCompanionGearGrowth.Level(item),Experience=experience,Stats=Details(item) };
        }
        return snapshot;
    }
    internal void Receipt(Item item,string destination)
    {
        var deed=item as CommodityDeed;
        var resource=deed?.Commodity ?? item;
        var name=ItemName(resource)+(deed!=null ? " (resource deed)" : "");
        var stats=Details(resource); var reports=Reports; var report=reports[0];
        var row=report.Loot.Find(r=>r.Name==name&&r.Stats==stats&&r.Hue==resource.Hue&&r.Destination==destination);
        if(row==null) { row=new HavenMissionReceipt { Name=name,Icon=resource.ItemID,Hue=resource.Hue,Stats=stats,Destination=destination };report.Loot.Add(row); }
        row.Amount+=resource.Amount; Store(reports);
    }
    internal void Completed(HavenExpeditionKind kind,int minutes,int training,bool full)
    {
        var reports=Reports; var report=reports[0]; report.Runs++;report.Minutes+=minutes;report.TrainingMinutes+=training;
        if(!full) { report.EarlyReturns++; }
        var name=HavenRegionalMissions.Name(kind);report.Missions[name]=report.Missions.GetValueOrDefault(name)+1;
        report.After=Snapshot(Companion); Store(reports);
    }
    internal void Finish(DateTime now,string outcome)
    {
        if(!Active) { return; }
        Companion.UpdateTraining(now);
        var reports=Reports; reports[0].Ended=now;reports[0].Outcome=outcome;reports[0].After=Snapshot(Companion);
        Active=false;Unread=true;Store(reports);Schedule();
    }
    internal bool MayShow(Mobile owner) => !Deleted && Companion?.BoundOwner==owner && owner?.NetState!=null && owner.Map!=null && owner.Map!=Map.Internal;
    internal void Show(Mobile owner)
    {
        if(!MayShow(owner)) { return; }
        Unread=false;owner.CloseGump<HavenMissionReportGump>();owner.SendGump(new HavenMissionReportGump(this));
    }
    [AfterDeserialization(false)] private void Schedule()
    { _timer?.Stop();_timer=null;if(!Deleted&&(Active||Unread)) { _timer=Timer.DelayCall(TimeSpan.FromSeconds(5),Pulse); } }
    private void Pulse()
    {
        if(Companion?.Deleted!=false || Companion.BoundOwner?.Deleted!=false) { Delete();return; }
        var owner=Companion.BoundOwner;var idle=Companion.Backpack.FindItemByType<HavenCompanionIdleMissions>();
        var busy=Companion.Expedition!=null || Companion.Backpack.FindItemByType<HavenCompanionGearAssignment>()?.Running==true;
        if(Active&&!busy&&owner.NetState!=null&&idle?.ReportingAfk!=true) { Finish(Core.Now,"Returned to your side"); }
        if(Unread&&!busy&&MayShow(owner)) { Show(owner); }
        Schedule();
    }
    public static void Initialize()
    {
        CommandSystem.Register("CompanionReport",AccessLevel.Player,e=>
        {
            var journal=Find(HavenCompanionGearAssignment.Find(e.Mobile));
            if(journal==null) { e.Mobile.SendMessage("No mission report yet. A report is recorded when your companion goes on a mission."); }
            else { journal.Show(e.Mobile); }
        });
        // Start exact tracking for assignments that were already running before this update.
        Timer.DelayCall(TimeSpan.FromSeconds(10),()=>
        {
            foreach(var account in Accounts.GetAccounts())
            {
                for(var i=0;i<account.Length;i++)
                {
                    var companion=HavenCompanionGearAssignment.Find(account[i]);
                    if(companion!=null&&(companion.Expedition!=null||companion.Backpack.FindItemByType<HavenCompanionGearAssignment>()?.Running==true))
                    { Begin(companion,Core.Now); }
                }
            }
        });
    }
    public override void OnDelete() { _timer?.Stop();_timer=null;Companion=null;base.OnDelete(); }
}

public sealed class HavenMissionReportGump : Gump
{
    private readonly HavenMissionJournal _journal; private readonly int _tab,_page,_history;
    public HavenMissionReportGump(HavenMissionJournal journal,int tab=0,int page=0,int history=0):base(45,45)
    {
        _journal=journal;_tab=Math.Clamp(tab,0,3);
        var reports=journal.Reports;_history=Math.Clamp(history,0,reports.Count-1);var r=reports[_history];
        AddBackground(0,0,680,540,5054);AddBackground(12,12,656,516,3000);
        AddLabel(28,22,0,$"{journal.Companion.Name} — Field Report");
        AddLabel(28,48,0,$"{r.Started.ToLocalTime():g} | {r.Outcome}");
        var tabs=new[]{"Summary","Loot","Skills","Gear"};
        for(var i=0;i<4;i++) { AddButton(28+i*157,80,4005,4007,10+i);AddLabel(63+i*157,82,0,tabs[i]); }
        var rows=new List<string>();
        if(_tab==0)
        {
            rows.Add($"Runs: {r.Runs} ({r.EarlyReturns} early returns). Mission time: {r.Minutes} minutes.");
            rows.Add($"Level: {r.Before.Level} → {r.After.Level}");
            rows.Add($"Strength {r.Before.Str} → {r.After.Str}; Dexterity {r.Before.Dex} → {r.After.Dex}; Intelligence {r.Before.Int} → {r.After.Int}");
            rows.Add($"Mission training: {r.TrainingMinutes} reward minutes; {r.TrainingMinutes*10} gear XP offered (item caps apply).");
            rows.Add("Stat and skill changes include natural training during this reporting period.");
            rows.AddRange(r.Missions.Select(p=>$"{p.Key}: {p.Value} run(s)"));
            if(r.EarlierRuns>0) { rows.Add($"{r.EarlierRuns} earlier runs occurred before this report began; their item details were not recorded."); }
            rows.Add("Loot lists actual quantities and delivery locations. Gear lists before/after equipment stats. Last 10 reports are retained.");
        }
        else if(_tab==1)
        { rows.AddRange(r.Loot.Select(x=>$"{x.Amount:N0} × {x.Name} — {x.Destination}"+(string.IsNullOrEmpty(x.Stats)?"":$"\n{x.Stats}"))); }
        else if(_tab==2)
        { rows.AddRange(r.After.Skills.Where(p=>Math.Abs(p.Value-r.Before.Skills.GetValueOrDefault(p.Key))>.001).OrderBy(p=>p.Key).Select(p=>$"{p.Key}: {r.Before.Skills.GetValueOrDefault(p.Key):F1} → {p.Value:F1} ({p.Value-r.Before.Skills.GetValueOrDefault(p.Key):+0.0;-0.0;0})")); }
        else
        {
            foreach(var entry in r.After.Gear)
            {
                var after=entry.Value;var before=r.Before.Gear.GetValueOrDefault(entry.Key);
                rows.Add($"{after.Name}\nLevel {before?.Level ?? 1} → {after.Level}; XP {before?.Experience ?? 0} → {after.Experience}\nBefore: {before?.Stats ?? "Not equipped"}\nAfter: {after.Stats}");
            }
        }
        if(rows.Count==0) { rows.Add("Nothing recorded in this category yet."); }
        var perPage=_tab==3?3:_tab==1?5:10;var pages=Math.Max(1,(rows.Count+perPage-1)/perPage);_page=Math.Clamp(page,0,pages-1);
        AddHtml(28,120,623,335,string.Join("<BR><BR>",rows.Skip(_page*perPage).Take(perPage).Select(x=>WebUtility.HtmlEncode(x).Replace("\n","<BR>"))),false,true);
        AddButton(28,470,4014,4016,1);AddLabel(63,472,0,"Previous");AddLabel(270,472,0,$"Page {_page+1}/{pages}");AddButton(540,470,4005,4007,2);AddLabel(575,472,0,"Next");
        AddButton(28,505,4014,4016,3);AddLabel(63,507,0,"Older report");AddButton(225,505,4005,4007,4);AddLabel(260,507,0,"Newer report");AddButton(540,505,4017,4019,0);AddLabel(575,507,0,"Close");
    }
    public override void OnResponse(NetState sender,in RelayInfo info)
    {
        if(info.ButtonID==0||!_journal.MayShow(sender.Mobile)) { return; }
        var tab=info.ButtonID is >=10 and <=13 ? info.ButtonID-10:_tab;
        var page=info.ButtonID==1?_page-1:info.ButtonID==2?_page+1:0;
        var history=info.ButtonID==3?_history+1:info.ButtonID==4?_history-1:_history;
        sender.Mobile.SendGump(new HavenMissionReportGump(_journal,tab,page,history));
    }
}
