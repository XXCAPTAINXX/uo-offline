using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.Engines.PartySystem;
using Server.Engines.Shadowguard;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class ShadowguardSmoke
{
    private static void Require(bool value) { if(!value) throw new Exception("Shadowguard progression assertion failed"); }
    public static void Run(Action<string,Action> check,bool reload)
    {
        if(reload)
        {
            check("Shadowguard reload consumes orphaned player records without corrupting saved data",()=> {
                var restored=ShadowguardController.Instance;
                Require(restored!=null && !restored.Deleted && restored.Encounters.Count==0 && restored.Addons.Count==1 && restored.Instances.Count==17);
                Require(restored.Table==null || restored.Table.Count==0);
            });
            return;
        }
        var owner=new PlayerMobile { Name="Shadowguard fixture",Player=true,Body=0x190,Blessed=true };
        owner.RawStr=100; owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(505,2192,25),Map.TerMur);
        new Account("shadowguard-fixture",Guid.NewGuid().ToString("N"))[0]=owner;
        var companion=HavenCompanion.Claim(owner);
        var controller=new ShadowguardController(); controller.MoveToWorld(new Point3D(501,2192,50),Map.TerMur);
        check("Shadowguard roof denies owner missing rooms",()=> {
            Require(companion.JoinOwnerParty(owner) && !controller.CanTryEncounter(owner,EncounterType.Roof));
        });
        check("Shadowguard companion shares qualified owner's room eligibility",()=> {
            controller.AddToTable(owner,EncounterType.Required);
            Require(controller.CanTryEncounter(owner,EncounterType.Roof) && !controller.Table.ContainsKey(companion));
        });
        check("Shadowguard still requires every real party member's rooms",()=> {
            var other=new PlayerMobile { Player=true }; other.MoveToWorld(owner.Location,owner.Map);
            var party=Party.Get(owner); party.Add(other);
            Require(!controller.CanTryEncounter(owner,EncounterType.Roof));
            controller.AddToTable(other,EncounterType.Required); Require(controller.CanTryEncounter(owner,EncounterType.Roof));
            party.Remove(other); other.Delete();
        });
        check("native Shadowguard bar advances all pirate waves and grants room credit",()=> {
            controller.CompleteRoof(owner);
            var encounter=new BarEncounter(controller.Instances[0]); encounter.PartyLeader=owner; encounter.StartTime=DateTime.UtcNow; encounter.HasBegun=true;
            controller.AddEncounter(encounter); encounter.Setup(); encounter.CheckAddon(); encounter.AddPlayer(owner);
            for(int wave=0;wave<5;wave++)
            {
                Require(encounter.Pirates.Count>0);
                foreach(var pirate in encounter.Pirates.ToArray()) pirate.Kill();
            }
            Require(encounter.Completed && encounter.Wave==5);
            encounter.Reset();
            Require(controller.HasCompletedEncounter(owner,EncounterType.Bar) && !controller.Instances[0].InUse && owner.Location==controller.KickLocation);
        });
        check("native Shadowguard roof sequences four bosses and consumes room credit",()=> {
            controller.AddToTable(owner,EncounterType.Required);
            var encounter=new RoofEncounter(controller.Instances[13]); encounter.PartyLeader=owner; encounter.StartTime=DateTime.UtcNow; encounter.HasBegun=true;
            controller.AddEncounter(encounter); encounter.Setup(); encounter.AddPlayer(owner);
            // Skip only the introduction delay; deaths and progression use native callbacks.
            typeof(RoofEncounter).GetMethod("SpawnBoss",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(encounter,null);
            var types=new System.Collections.Generic.HashSet<Type>();
            for(int i=0;i<4;i++) { var boss=encounter.CurrentBoss; Require(boss!=null && types.Add(boss.GetType())); boss.Kill(); }
            Require(encounter.Completed && types.Count==4 && !controller.HasCompletedEncounter(owner,EncounterType.Bar));
            encounter.Reset(); Require(!controller.Instances[13].InUse && owner.Location==controller.KickLocation);
        });
    }
}
