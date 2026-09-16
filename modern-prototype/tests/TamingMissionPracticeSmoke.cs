using System;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class TamingMissionPracticeSmoke
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run(Action<string> log)
    {
        var p=new PlayerMobile{Player=true,Body=0x190,RawStr=100};p.AddItem(new Backpack());
        new Account("mission-practice-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;
        p.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
        var c=HavenCompanion.Claim(p);c.MoveToWorld(p.Location,p.Map);
        try
        {
            c.Skills.AnimalTaming.Base=80;c.Skills.AnimalLore.Base=80;
            c.Skills.Peacemaking.Base=80;c.Skills.Musicianship.Base=80;
            c.Skills.Peacemaking.SetLockNoRelay(SkillLock.Up);c.Skills.Musicianship.SetLockNoRelay(SkillLock.Up);
            Check(c.StartMission(p,5,CompanionMission.TameHorse),"Taming mission dispatch");
            Check(c.Recall(p),"Early mission recall");
            Check(c.Skills.Peacemaking.BaseFixedPoint==800&&c.Skills.Musicianship.BaseFixedPoint==800,"Early recall awarded bard practice");
            Check(c.StartMission(p,5,CompanionMission.TameHorse),"Second dispatch");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(HavenCompanion).GetField("_missionDue",flags).SetValue(c,DateTime.UtcNow.AddSeconds(-1));
            var complete=typeof(HavenCompanion).GetMethod("CompleteDueMission",flags);complete.Invoke(c,null);
            Check(c.Skills.Peacemaking.BaseFixedPoint==803&&c.Skills.Musicianship.BaseFixedPoint==801,"Completed trip practice");
            Check(c.LastReport.Contains("Peacemaking +0.3")&&c.LastReport.Contains("Musicianship +0.1"),"Mission report missing bard practice");
            complete.Invoke(c,null);c.DeliverPetTickets();c.DeliverPetTickets();
            Check(c.Skills.Peacemaking.BaseFixedPoint==803&&c.Skills.Musicianship.BaseFixedPoint==801,"Repeated completion/collection awarded practice");
            Check(c.Recall(p),"Return after completed trip");
            Check(c.StartMission(p,30,CompanionMission.TameHorse),"Long taming mission dispatch");
            c.Skills.Peacemaking.Cap=120;c.Skills.Peacemaking.Base=119.9;
            c.Skills.Musicianship.SetLockNoRelay(SkillLock.Down);
            typeof(HavenCompanion).GetField("_missionDue",flags).SetValue(c,DateTime.UtcNow.AddSeconds(-1));complete.Invoke(c,null);
            Check(c.Skills.Peacemaking.BaseFixedPoint==1200&&c.Skills.Musicianship.BaseFixedPoint==801,"Long trip bypassed cap or down lock");
            Check(c.LastReport.Contains("Peacemaking +0.1")&&!c.LastReport.Contains("Musicianship +"),"Report claimed blocked gains");
            var book=HavenPetBook.Ensure(p);var horse=new Horse();horse.SetControlMaster(p);horse.MoveToWorld(p.Location,p.Map);
            var ticket=HavenPetTicket.Store(horse,p,book);Check(ticket!=null,"Ordinary horse storage");
            Check(HavenPetExchange.Eligible(p,ticket,book)&&HavenPetBulkExchangeConfirm.Eligible(p,ticket,book),"Starter protection blocks ordinary horses");
            int credits=HavenPetExchange.Balance(p),value=HavenPetExchange.Value(ticket);
            Check(HavenPetExchange.Exchange(p,ticket,book)&&HavenPetExchange.Balance(p)==credits+value&&horse.Deleted,"Ordinary horse exchange failed");
            log("PASS actual 30-minute completion obeys cap/down lock and reports actual gain; ordinary horses remain exchangeable");
            log("PASS actual taming dispatch: early recall grants no bard skills; due completion grants once; mission report shows both gains; repeated completion/collection grants nothing");
        }
        finally{c.Delete();p.Delete();}
    }
}
