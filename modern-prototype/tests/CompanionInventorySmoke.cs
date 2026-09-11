using System;
using System.Linq;
using System.IO;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class CompanionInventorySmoke {
 public static void Initialize(){if(File.Exists("INVENTORY-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(3),Run);}
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText("inventory-checks.log","PASS "+label+"\n");}
 static void Run(){try{
 Check(HavenCompanionProgression.TamingTraining(500,5,0.99)==50,"five minute early tame training gives five points");
 Check(HavenCompanionProgression.TamingTraining(1000,5,0.99)==10,"normal training above100");
 Check(HavenCompanionProgression.TamingTraining(1200,5,0.99)==0,"over120 remains twenty times slower");
 var owner=new MissionSafetyPlayer{Player=true,Body=0x190};owner.AddItem(new Backpack());new Account("inventory-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
 owner.Criminal=true;Check(HavenPreview.CanTravel(owner),"criminal status alone does not block Haven travel");Check(owner.Criminal,"travel eligibility preserves criminal status");
 var opponent=new Horse();opponent.MoveToWorld(owner.Location,owner.Map);owner.Combatant=opponent;Check(!HavenPreview.CanTravel(owner),"active combat blocks Haven travel");owner.Combatant=null;owner.Aggressors.Clear();owner.Aggressed.Clear();
 var incoming=AggressorInfo.Create(opponent,owner,false);owner.Aggressors.Add(incoming);Check(!HavenPreview.CanTravel(owner),"recent incoming combat blocks Haven travel");owner.Aggressors.Remove(incoming);
 var outgoing=AggressorInfo.Create(owner,opponent,false);owner.Aggressed.Add(outgoing);Check(!HavenPreview.CanTravel(owner),"recent outgoing combat blocks Haven travel");owner.Aggressed.Remove(outgoing);
 Check(HavenPreview.CanTravel(owner),"travel resumes when combat clears even while criminal");owner.Criminal=false;opponent.Delete();
 var home=HavenStarterHome.Claim(owner);Check(home!=null,"create owned home for travel check");
 var homeEnemy=new Horse();homeEnemy.MoveToWorld(owner.Location,owner.Map);owner.Combatant=homeEnemy;owner.Criminal=true;
 Check(HavenStarterHome.Travel(owner),"home travel works during combat while criminal");Check(owner.Criminal,"home does not erase criminal status");Check(owner.Map==home.Map&&owner.InRange(home,8),"home travel reaches owned lodge");
 owner.Combatant=null;owner.Aggressors.Clear();owner.Aggressed.Clear();owner.Criminal=false;homeEnemy.Delete();home.Delete();owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
 var c=HavenCompanion.Claim(owner);
 c.Skills.AnimalTaming.Base=73; c.Skills.AnimalTaming.Base=50;Check(c.Skills.AnimalTaming.Base==73,"earned taming cannot reset downward");
var bag=new Bag();var gold=new Gold(200);bag.DropItem(gold);c.Backpack.DropItem(bag);
 Check(c.Backpack.MaxItems==1000&&c.Backpack.MaxWeight==0,"1000 item unlimited-weight companion pack");
 foreach(CompanionRole role in new[]{CompanionRole.Warrior,CompanionRole.Caster,CompanionRole.Archer,CompanionRole.Bard,CompanionRole.Healer}){
 Check(c.SetRole(owner,role),"role "+role);Check(!c.AIObject.DoOrderRelease()&&c.ControlMaster==owner,"native release blocked "+role);c.DropBackpack();Check(bag.Parent==c.Backpack&&gold.Parent==bag,"inventory retained "+role);
 }
 c.MoveToWorld(new Point3D(2000,1500,0),Map.Trammel);Check(c.Recall(owner)&&c.Map==owner.Map&&c.Location==owner.Location,"recall from another facet at any distance");
 c.MoveToWorld(new Point3D(1500,1500,0),owner.Map);Check(c.Recall(owner)&&c.Location==owner.Location,"recall across the same facet");
 Check(c.StartMission(owner,30,CompanionMission.Mining),"mission dispatch");Check(bag.Parent==c.Backpack&&gold.Parent==bag,"nested items retained while away");Check(c.Recall(owner),"early recall");Check(bag.Parent==c.Backpack&&gold.Parent==bag&&gold.Amount==200,"same items return unchanged");
 var horse=new Horse();horse.MoveToWorld(owner.Location,owner.Map);horse.SetControlMaster(owner);Check(horse.AIObject.DoOrderRelease()&&!horse.Controlled,"ordinary pet release unchanged");horse.Delete();
 var source=HavenMiniChamp.Find();Check(source!=null,"existing verified mini camp");var camp=new HavenMiniChamp();camp.MoveToWorld(source.Location,source.Map);owner.MoveToWorld(camp.Location,camp.Map);c.MoveToWorld(owner.Location,owner.Map);
 Check(camp.Begin(owner,3),"start combined challenge");var foesField=typeof(HavenMiniChamp).GetField("_foes",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var spawn=typeof(HavenMiniChamp).GetMethod("SpawnWave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 for(int stage=0;stage<4;stage++){var foes=(System.Collections.Generic.List<Mobile>)foesField.GetValue(camp);Check(foes.Count==(stage==3?3:15),"challenge population stage "+stage);Check(System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(foes,x=>x.Name)).Count()==3,"all three variants present");camp.Credit((HavenMiniEnemy)foes[0],owner,1);foreach(var foe in foes.ToArray()){camp.Defeated((HavenMiniEnemy)foe);foe.Delete();}if(stage<3)spawn.Invoke(camp,null);}
 Check(!camp.Active,"challenge completes only after all bosses");var pirate=new HavenMiniPrize(owner,1);Check(pirate.FindItemByType(typeof(Cannonball))!=null&&pirate.FindItemByType(typeof(PowderCharge))!=null&&pirate.FindItemByType(typeof(FuseCord))!=null,"native ship ammunition in corsair prize");pirate.Delete();camp.Delete();
 c.Criminal=true;owner.Criminal=false;owner.DoBeneficial(c);Check(!owner.Criminal,"owner care of criminal companion does not create a flag");
 owner.Criminal=true;var expiry=typeof(Mobile).GetField("m_ExpireCriminal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var timer=(Timer)expiry.GetValue(owner);var crimeCount=owner.Crimes;owner.DoBeneficial(c);Check(owner.Criminal&&owner.Crimes==crimeCount,"owner care preserves existing expiry");
 var stranger=new PlayerMobile{Player=true};stranger.Criminal=true;Check(owner.IsBeneficialCriminal(stranger),"unrelated criminal aid remains criminal");
 c.Criminal=false;owner.Criminal=false;c.Internalize();c.CriminalAction(false);Check(owner.Criminal,"actual companion crime still flags owner");
 owner.Criminal=false;c.Criminal=false;c.MoveToWorld(owner.Location,owner.Map);stranger.Delete();
c.Delete();owner.Delete();File.AppendAllText("inventory-checks.log","COMPLETE\n");
 }catch(Exception e){File.AppendAllText("inventory-checks.log","FAIL "+e+"\n");}Core.Kill(false);}
}
