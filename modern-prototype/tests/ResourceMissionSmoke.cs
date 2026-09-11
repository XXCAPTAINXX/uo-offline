using System;
using System.IO;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class ResourceMissionSmoke
{
    private static HavenCompanion _fixture;
    private static void Require(bool value) { if(!value) throw new Exception("Mission resource conservation failed"); }
    private static void Due(HavenCompanion companion)
    {
        typeof(HavenCompanion).GetField("_missionDue",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(companion,DateTime.UtcNow.AddSeconds(-1));
        typeof(HavenCompanion).GetMethod("CompleteDueMission",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(companion,null);
    }
    public static void Run(Action<string,Action> check,bool reload)
    {
        if(reload)
        {
            check("resource mission rewards and ledger persist without replay",()=> {
                var restored=World.FindMobile((Serial)Int32.Parse(File.ReadAllText("resource-mission-fixture.txt"))) as HavenCompanion;
                Require(restored!=null && restored.CompletedMissions==8 && !restored.OnMission && restored.ResourceLedger.Balance(0)==HavenResourceLedger.MaxBalance && restored.PendingResources==150);
                int before=restored.ResourceLedger.Balance(0); restored.DeliverRewards(); Require(restored.ResourceLedger.Balance(0)==before && restored.PendingResources==150);
                Require(restored.ResourceLedger.Balance(36)==5 && restored.MissionKind==CompanionMission.Mining);
            });
            return;
        }
        var owner=new PlayerMobile { Name="resource missions fixture",Player=true,Body=0x190 };
        owner.RawStr=100; owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
        new Account("resource-mission-fixture",Guid.NewGuid().ToString("N"))[0]=owner;
        var companion=HavenCompanion.Claim(owner);
        _fixture=companion;
        var playerLedger=new HavenResourceLedger(); owner.Backpack.DropItem(playerLedger);
        check("companion begins with accessible ledger",()=>Require(companion.ResourceLedger!=null && companion.ResourceLedger.CanUse(owner)));
        check("owner can use ledger in companion pack without snooping",()=> {
            var stranger=new PlayerMobile {Player=true,Body=0x190};
            Require(!companion.IsSnoop(owner) && companion.ResourceLedger.IsAccessibleTo(owner) && companion.ResourceLedger.CheckItemUse(owner));
            Require(companion.IsSnoop(stranger) && !companion.ResourceLedger.IsAccessibleTo(stranger));
            stranger.Delete();
        });
        check("resource mission snapshots tier and scales by duration",()=> {
            companion.Skills.Mining.Base=99;
            Require(companion.StartMission(owner,15,CompanionMission.Mining)); companion.Skills.Mining.Base=50;
            Require(CompanionMissionTimerGump.Remaining(companion).StartsWith("Mining  15:"));
            Due(companion); Require(companion.ResourceLedger.Balance(8)==300 && companion.Skills.Mining.Base==53 && companion.PendingResources==0);
            Require(companion.Recall(owner) && companion.LastReport.Contains("300 Valorite ingots"));
        });
        check("companion ledger transfers to owner's book",()=> {
            Require(companion.ResourceLedger.TransferAll(owner,playerLedger) && playerLedger.Balance(8)==300 && companion.ResourceLedger.Balance(8)==0);
        });
        check("wood and leather missions unlock higher resource types",()=> {
            companion.Skills.Lumberjacking.Base=100; Require(companion.StartMission(owner,30,CompanionMission.Lumber)); Due(companion); Require(companion.ResourceLedger.Balance(15)==600); companion.Recall(owner);
            companion.Skills.AnimalLore.Base=100; Require(companion.StartMission(owner,5,CompanionMission.Leather)); Due(companion); Require(companion.ResourceLedger.Balance(19)==50); companion.Recall(owner);
        });
        check("regional missions enforce skill and award native catalog resources",()=> {
            companion.Skills.Magery.Base=50; companion.Skills.Tactics.Base=50; Require(!companion.StartMission(owner,5,CompanionMission.Abyss));
            companion.Skills.Magery.Base=100; Require(companion.StartMission(owner,5,CompanionMission.Malas)); Due(companion); Require(companion.ResourceLedger.Balance(29)==10 && companion.ResourceLedger.Balance(35)==10); companion.Recall(owner);
            Require(companion.StartMission(owner,5,CompanionMission.Abyss)); Due(companion); Require(companion.ResourceLedger.Balance(36)==5 && companion.ResourceLedger.Balance(46)==5); companion.Recall(owner);
        });
        check("full pack still receives resources into existing ledger",()=> {
            companion.Backpack.MaxItems=1;
            Require(companion.StartMission(owner,5,CompanionMission.Mining)); Due(companion); Require(companion.ResourceLedger.Balance(0)==100 && companion.PendingResources==0);
            companion.Backpack.MaxItems=1000; companion.Recall(owner);
            Require(companion.CompletedMissions==6);
        });
        check("ledger overflow remains pending and delivers only available room",()=> {
            Require(companion.ResourceLedger.Credit(0,HavenResourceLedger.MaxBalance-100));
            Require(companion.StartMission(owner,5,CompanionMission.Mining)); Due(companion);
            Require(companion.PendingResources==100 && companion.ResourceLedger.Balance(0)==HavenResourceLedger.MaxBalance); companion.Recall(owner);
            Require(companion.ResourceLedger.Withdraw(owner,0,50,true)); companion.DeliverRewards();
            Require(companion.PendingResources==50 && companion.ResourceLedger.Balance(0)==HavenResourceLedger.MaxBalance);
            Require(companion.StartMission(owner,5,CompanionMission.Mining));
        });
        File.WriteAllText("resource-mission-fixture.txt",companion.Serial.Value.ToString());
    }
    public static void PrepareForSave()
    {
        if(_fixture!=null && _fixture.OnMission)
            typeof(HavenCompanion).GetField("_missionDue",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(_fixture,DateTime.UtcNow.AddSeconds(-1));
    }
}
