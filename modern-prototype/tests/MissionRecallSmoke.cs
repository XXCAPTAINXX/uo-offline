using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class MissionRecallSmoke
{
    private static void Require(bool value) { if(!value) throw new Exception("Mission recall regression"); }
    private static object Call(HavenCompanion c,string method) {return typeof(HavenCompanion).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);}
    public static void Run(Action<string,Action> check,bool reload)
    {
        if(reload) {
            check("cancelled mission remains cancelled after reload",()=>{
                var restored=World.Mobiles.Values.OfType<HavenCompanion>().Single(x=>x.Name=="Recall fixture");
                Require(!restored.OnMission && restored.CompletedMissions==0 && restored.Backpack.GetAmount(typeof(Gold))==0);
                Call(restored,"CompleteDueMission");Require(restored.CompletedMissions==0);
            });return;
        }
        var owner=new PlayerMobile {Player=true,Body=0x190,RawStr=100};owner.AddItem(new Backpack());
        new Account("early-recall-fixture",Guid.NewGuid().ToString("N"))[0]=owner;
        owner.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
        var c=HavenCompanion.Claim(owner);c.Name="Recall fixture";c.Skills.Magery.Base=100;
        foreach(CompanionMission kind in Enum.GetValues(typeof(CompanionMission))) {
            check("early recall cancels "+kind+" without rewards and restores Follow",()=>{
                Require(c.StartMission(owner,5,kind));
                var stranger=new PlayerMobile {Player=true};Require(!c.Recall(stranger) && c.OnMission);stranger.Delete();
                Require(c.Recall(owner) && !c.OnMission && c.ControlOrder==OrderType.Follow && c.ControlTarget==owner && c.Map==owner.Map);
                Call(c,"CompleteDueMission");Require(c.CompletedMissions==0 && c.Backpack.GetAmount(typeof(Gold))==0 && c.PendingResources==0);
                Require(Enumerable.Range(0,HavenResources.Types.Length).All(id=>c.ResourceLedger.Balance(id)==0));
            });
        }
        check("automatic placement returns completed mission once",()=>{
            var temp=HavenCompanion.Claim(owner);Require(temp==c);
            typeof(HavenCompanion).GetField("_missionReturnPending",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,true);
            c.Internalize();Require((bool)Call(c,"PlaceAfterMission"));
            Require(c.ControlOrder==OrderType.Follow && c.ControlTarget==owner && !(bool)Call(c,"PlaceAfterMission"));
        });
    }
}
