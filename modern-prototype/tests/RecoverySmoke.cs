using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;
public static class RecoverySmoke {
    private static void Require(bool ok){if(!ok)throw new Exception("Recovery regression");}
    public static void Run(Action<string,Action> check,bool reload){
        HavenRecovery.Ensure();
        check("Haven recovery NPCs are present once",()=>{HavenRecovery.Ensure();Require(World.Mobiles.Values.OfType<HavenPlazaHealer>().Count()==1 && World.Mobiles.Values.OfType<HavenRecoverySteward>().Count()==1);});
        if(reload)return;
        var steward=HavenRecovery.Steward();var owner=new PlayerMobile {Player=true,Name="Recovery fixture",Body=0x190,RawStr=100};owner.AddItem(new Backpack());new Account("recovery-fixture",Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(steward.Location,steward.Map);
        var c=HavenCompanion.Claim(owner);var weapon=c.FindItemOnLayer(Layer.OneHanded);
        check("companion baseline regeneration uses three-second cadence and resource caps",()=>{var now=DateTime.UtcNow.AddMinutes(1);c.Hits=20;c.Stam=20;c.Mana=20;c.RecoverResources(now);Require(c.Hits==24 && c.Stam==32 && c.Mana==28);c.RecoverResources(now.AddSeconds(2));Require(c.Hits==24 && c.Mana==28);c.Hits=c.HitsMax-1;c.Mana=c.ManaMax-1;c.Stam=c.StamMax-1;c.RecoverResources(now.AddSeconds(3));Require(c.Hits==c.HitsMax && c.Mana==c.ManaMax && c.Stam==c.StamMax);});
        check("Healing and Veterinary cap at two seconds for all dex and resurrection",()=>{foreach(int dex in new[]{10,80,150,300}){owner.RawDex=dex;foreach(bool dead in new[]{false,true}){Require(BandageContext.GetDelay(owner,owner,dead,SkillName.Healing).TotalSeconds<=2);Require(BandageContext.GetDelay(owner,c,dead,SkillName.Healing).TotalSeconds<=2);Require(BandageContext.GetDelay(owner,c,dead,SkillName.Veterinary).TotalSeconds<=2);}}});
        check("dead companion returns from another facet with gear and Follow",()=>{c.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);c.Kill();Require(c.IsDeadPet && HavenRecovery.RecoverCompanion(owner) && !c.IsDeadPet && c.ControlOrder==OrderType.Follow && c.Map==owner.Map && weapon.Parent==c);});
        check("companion recovers after five seconds near owner, with full resources",()=>{c.Kill();var now=DateTime.UtcNow;Require(c.IsDeadPet && !c.RecoverFromDeath(now) && !c.RecoverFromDeath(now.AddSeconds(4)));Require(c.RecoverFromDeath(now.AddSeconds(5)) && !c.IsDeadPet && c.Hits==c.HitsMax && c.Mana==c.ManaMax && c.Stam==c.StamMax && c.ControlOrder==OrderType.Follow && weapon.Parent==c);Require(!c.RecoverFromDeath(now.AddSeconds(6)));});
        check("automatic recovery waits for nearby owner",()=>{c.Kill();var now=DateTime.UtcNow;c.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);Require(!c.RecoverFromDeath(now) && !c.RecoverFromDeath(now.AddSeconds(6)) && c.IsDeadPet);c.MoveToWorld(owner.Location,owner.Map);Require(c.RecoverFromDeath(now.AddSeconds(7)));});
        var stranger=new PlayerMobile {Player=true,Body=0x190,RawStr=100};stranger.MoveToWorld(steward.Location,steward.Map);
        var pet=new Dog();pet.SetControlMaster(owner);pet.IsBonded=true;pet.MoveToWorld(steward.Location,steward.Map);pet.Kill();
        var otherPet=new Dog();otherPet.SetControlMaster(stranger);otherPet.IsBonded=true;otherPet.MoveToWorld(steward.Location,steward.Map);otherPet.Kill();
        check("pet clinic resurrects only owner's nearby ghosts",()=>{Require(HavenRecovery.ResurrectPets(owner)==1 && !pet.IsDeadPet && otherPet.IsDeadPet);});
        var dagger=new Dagger();owner.Backpack.DropItem(dagger);owner.Kill();var corpse=owner.Corpse as Corpse;
        check("player healer accepts ghost and corpse recall preserves contents",()=>{Require(!owner.Alive && World.Mobiles.Values.OfType<HavenPlazaHealer>().Single().CheckResurrect(owner));owner.Resurrect();Require(corpse!=null);corpse.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);int count=corpse.TotalItems;Require(HavenRecovery.RecallCorpse(owner) && corpse.Map==owner.Map && corpse.Location==owner.Location && corpse.TotalItems==count);});
        check("corpse recall denies other owners and remote use",()=>{stranger.Corpse=corpse;Require(!HavenRecovery.RecallCorpse(stranger));owner.Internalize();Require(!HavenRecovery.RecallCorpse(owner));});
        c.Delete();pet.Delete();otherPet.Delete();if(corpse!=null)corpse.Delete();owner.Delete();stranger.Delete();
    }
}
