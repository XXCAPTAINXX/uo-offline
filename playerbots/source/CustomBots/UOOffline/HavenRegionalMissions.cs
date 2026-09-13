using System;
using System.Linq;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Network;
namespace Server.UOOffline;

public static class HavenRegionalMissions
{
    internal static readonly HavenExpeditionKind[] Kinds=[HavenExpeditionKind.MalasReagents,HavenExpeditionKind.DoomBones,HavenExpeditionKind.AbyssEssences,HavenExpeditionKind.AbyssIngredients];
    internal static bool IsRegional(HavenExpeditionKind kind)=>Kinds.Contains(kind);
    internal static string Name(HavenExpeditionKind kind)=>kind switch
    {
        HavenExpeditionKind.MalasReagents=>"Malas: necromantic reagents",HavenExpeditionKind.DoomBones=>"Malas: Doom bones",
        HavenExpeditionKind.AbyssEssences=>"Stygian Abyss: essences",HavenExpeditionKind.AbyssIngredients=>"Stygian Abyss: rare ingredients",
        _=>HavenTamingMissions.IsTaming(kind)?$"Tame {HavenTamingMissions.PetName(kind)}":kind.ToString()
    };
    internal static int Requirement(HavenExpeditionKind kind)=>kind switch
    { HavenExpeditionKind.MalasReagents=>40,HavenExpeditionKind.DoomBones=>60,HavenExpeditionKind.AbyssEssences=>80,HavenExpeditionKind.AbyssIngredients=>100,_=>0 };
    internal static double Skill(HavenCompanion c)=>c==null?0:Math.Min(c.Skills.MagicResist.Base,Math.Max(c.Skills.Tactics.Base,Math.Max(c.Skills.Magery.Base,c.Skills.Archery.Base)));
    internal static bool CanStart(HavenCompanion c,HavenExpeditionKind kind)=>!IsRegional(kind)||Skill(c)>=Requirement(kind);
    internal static string Description(HavenExpeditionKind kind)=>kind switch
    {
        HavenExpeditionKind.MalasReagents=>"Bat wings, grave dust, daemon blood, nox crystals and pig iron.",
        HavenExpeditionKind.DoomBones=>"Daemon bones for Doom and reagent uses; ordinary bones.",
        HavenExpeditionKind.AbyssEssences=>"Virtue essences for the Abyss artifice station.",
        _=>"Rare ingredients including claws, faery dust and crystalline blackrock."
    };
    internal static void Add(Bag bag,HavenExpeditionKind kind,int minutes,int percent)
    {
        if(kind==HavenExpeditionKind.MalasReagents)
        { foreach(var type in new[]{typeof(BatWing),typeof(GraveDust),typeof(DaemonBlood),typeof(NoxCrystal),typeof(PigIron)}) { AddResource(bag,type,HavenMissionDuration.Scale(minutes*3,percent)); } }
        else if(kind==HavenExpeditionKind.DoomBones)
        { AddResource(bag,typeof(DaemonBone),HavenMissionDuration.Scale(minutes*2,percent));AddResource(bag,typeof(Bone),HavenMissionDuration.Scale(minutes*5,percent)); }
        else
        {
            var essences=kind==HavenExpeditionKind.AbyssEssences;
            var pool=HavenResourceCatalog.Entries.Where(e=>e.Group=="Abyss"&&e.Type.Name.StartsWith("Essence")==essences).ToArray();
            var amount=Math.Max(1,HavenMissionDuration.Scale(minutes,percent)/(essences?2:5));var first=Utility.Random(pool.Length);
            for(var i=0;i<3;i++) { AddResource(bag,pool[(first+i)%pool.Length].Type,amount); }
        }
    }
    internal static void AddResource(Bag bag,Type type,int amount)
    {
        var resource=HavenResourceCatalog.Entries.First(e=>e.Type==type).Create(amount);var deed=new CommodityDeed();
        if(!deed.SetCommodity(resource)||HavenResourceCatalog.Index(resource)<0) { deed.Delete();resource.Delete();throw new InvalidOperationException("Mission material is not supported by the resource book."); }
        bag.DropItem(deed);
    }
}

[SerializationGenerator(0)]
public partial class HavenMissionRoute : Item
{
    [SerializableField(0)] private HavenCompanion _companion;
    [SerializableField(1)] private int _focus=-1;
    [SerializableField(2)] private bool _offlineArmed;
    [SerializableField(3)] private int _offlineMinutes=5;
    private Timer _timer;
    public override bool IsVirtualItem=>true;
    [Constructible] public HavenMissionRoute():base(1) { Name="companion mission route";Visible=false;Movable=false;Weight=0; }
    internal static HavenMissionRoute Get(HavenCompanion companion)
    {
        var route=companion.Backpack.FindItemByType<HavenMissionRoute>();
        if(route==null) { route=new HavenMissionRoute { Companion=companion };companion.Backpack.DropItem(route); }return route;
    }
    internal static HavenExpeditionKind OfflineKind(HavenCompanion c) { var focus=c.Backpack.FindItemByType<HavenMissionRoute>()?.Focus??-1;return focus<0?HavenExpeditionKind.Grind:(HavenExpeditionKind)focus; }
    internal static int Duration(HavenCompanion c)=>c.Backpack.FindItemByType<HavenMissionRoute>()?.OfflineMinutes??5;
    internal void Arm() { OfflineMinutes=HavenMissionDuration.Selected(Companion);OfflineArmed=true;Schedule(); }
    [AfterDeserialization(false)] private void Schedule() { _timer?.Stop();_timer=null;if(!Deleted&&OfflineArmed) { _timer=Timer.DelayCall(TimeSpan.FromSeconds(5),Pulse); } }
    private void Pulse()
    {
        if(Companion?.Deleted!=false||Companion.BoundOwner?.Deleted!=false) { Delete();return; }
        if(Companion.BoundOwner.NetState==null)
        { if(HavenRegionalMissions.CanStart(Companion,OfflineKind(Companion))) { HavenCompanionGearAssignment.Begin(Companion,Core.Now); }OfflineArmed=false; }
        Schedule();
    }
    public override void OnDelete() { _timer?.Stop();_timer=null;Companion=null;base.OnDelete(); }
}

public sealed class HavenRegionalMissionGump : Gump
{
    private readonly HavenCompanion _companion;
    internal HavenRegionalMissionGump(HavenCompanion companion):base(60,60)
    {
        _companion=companion;var route=HavenMissionRoute.Get(companion);
        AddBackground(0,0,620,535,5054);AddBackground(12,12,596,511,3000);AddLabel(25,24,0,"Regional resource missions");
        AddLabel(25,50,0,$"Combat / resist rating: {HavenRegionalMissions.Skill(companion):F1}. Book-compatible deeds.");
        for(var i=0;i<4;i++)
        {
            var kind=HavenRegionalMissions.Kinds[i];var y=90+i*72;
            AddLabel(25,y,0,$"{HavenRegionalMissions.Name(kind)} — needs {HavenRegionalMissions.Requirement(kind)}");AddHtml(25,y+23,410,40,HavenRegionalMissions.Description(kind));
            AddButton(445,y,4005,4007,10+i);AddLabel(480,y+2,0,"Send");AddButton(445,y+30,4005,4007,20+i);AddLabel(480,y+32,0,"AFK focus");
        }
        AddHtml(25,380,565,40,$"<BASEFONT COLOR=#181818>AFK focus: {(route.Focus<0?"Mixed cycle":HavenRegionalMissions.Name((HavenExpeditionKind)route.Focus))}</BASEFONT>");
        AddButton(25,410,4005,4007,1);AddLabel(60,412,0,"Mixed cycle");AddButton(210,410,4005,4007,2);AddLabel(245,412,0,route.OfflineArmed?"Cancel offline order":"Run this focus after logout");
        AddButton(495,495,4017,4019,0);AddLabel(530,497,0,"Close");
        AddButton(25,447,4005,4007,3);AddLabel(60,449,0,"Cycle gathering / loot focus");
        AddLabel(25,495,0,"Loot, ore, wood, leather or reagents");
    }
    public override void OnResponse(NetState state,in RelayInfo info)
    {
        var owner=state.Mobile;if(info.ButtonID==0||_companion.Deleted||_companion.BoundOwner!=owner) { return; }
        if(_companion.Backpack.FindItemByType<HavenCompanionGearAssignment>()?.Running==true) { owner.SendMessage("Let your offline companion return before changing the route.");return; }
        var route=HavenMissionRoute.Get(_companion);
        if(info.ButtonID==1) { route.Focus=-1; }
        else if(info.ButtonID==3) { route.Focus=route.Focus<0||route.Focus>=4?0:route.Focus+1; }
        else if(info.ButtonID==2) { if(route.OfflineArmed) { route.OfflineArmed=false; }else { route.Arm();owner.SendMessage("Offline missions will start after logout and stop when you return."); } }
        else if(info.ButtonID is >=10 and <=13 or >=20 and <=23)
        {
            var kind=HavenRegionalMissions.Kinds[info.ButtonID%10];
            if(!HavenRegionalMissions.CanStart(_companion,kind)) { owner.SendMessage("Your companion needs the displayed combat and magic resistance skills first."); }
            else if(info.ButtonID<20) { owner.SendGump(new HavenMissionDurationGump(_companion,kind));return; }
            else { route.Focus=(int)kind; }
        }
        owner.SendGump(new HavenRegionalMissionGump(_companion));
    }
}
