using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class CompanionSkinningSmoke
{
 public static void Run(Action<string> log)
 {
  var owner=new PlayerMobile{Player=true};owner.AddItem(new Backpack());owner.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);owner.Hits=owner.HitsMax;
  var c=new HavenCompanion();typeof(HavenCompanion).GetField("_owner",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,owner);c.SetControlMaster(owner);c.MoveToWorld(owner.Location,owner.Map);c.ControlOrder=OrderType.Follow;c.ControlTarget=owner;c.Hits=c.HitsMax;
  var animal=new Cow();animal.MoveToWorld(owner.Location,owner.Map);animal.RegisterDamage(1,owner);animal.Kill();var corpse=animal.Corpse as Corpse;
  if(corpse==null||!c.CanSkinCorpse(corpse))throw new Exception("Earned corpse not eligible");
  c.Hits--;if(c.SkinCorpse(corpse)||corpse.Carved)throw new Exception("Harvest while injured");c.Hits=c.HitsMax;
  var enemy=new Ogre();enemy.MoveToWorld(owner.Location,owner.Map);owner.Combatant=enemy;if(c.SkinCorpse(corpse))throw new Exception("Harvest during combat");owner.Combatant=null;
  var equipment=new Longsword();corpse.DropItem(equipment);
  if(!c.SkinCorpse(corpse)||!corpse.Carved||equipment.Parent!=c.Backpack)throw new Exception("Harvest or equipment loot failed");
  var ledger=c.Backpack.Items.OfType<HavenResourceLedger>().First();int leather=Array.IndexOf(HavenResources.Types,typeof(Leather)),meat=Array.IndexOf(HavenResources.Types,typeof(RawRibs));
  int count=ledger.Balance(leather);if(count<=0||ledger.Balance(meat)<=0)throw new Exception("Meat/leather not credited to ledger");
  if(c.SkinCorpse(corpse)||ledger.Balance(leather)!=count)throw new Exception("Duplicate harvest");
  var tiger=new WildTiger();tiger.MoveToWorld(owner.Location,owner.Map);tiger.RegisterDamage(1,owner);tiger.Kill();var tigerCorpse=tiger.Corpse as Corpse;
  if(!c.CanSkinCorpse(tigerCorpse)||!c.SkinCorpse(tigerCorpse)||ledger.Balance(Array.IndexOf(HavenResources.Types,typeof(TigerPelt)))<=0)throw new Exception("Tiger pelts not harvested into ledger");
  tigerCorpse.Delete();
  var other=new Cow();other.MoveToWorld(owner.Location,owner.Map);other.Kill();if(c.CanSkinCorpse(other.Corpse as Corpse))throw new Exception("Stranger corpse eligible");
  log("PASS earned animal corpse carved; leather, meat and tiger pelts ledgered; equipment looted; injured/combat harvesting blocked; repeated harvest cannot duplicate; unearned corpse rejected");
  c.Delete();owner.Delete();enemy.Delete();corpse.Delete();if(other.Corpse!=null)other.Corpse.Delete();
 }
}
