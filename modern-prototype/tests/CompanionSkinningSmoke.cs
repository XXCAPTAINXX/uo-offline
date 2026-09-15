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
  if(!c.SkinCorpse(corpse)||!corpse.Carved||!c.Backpack.Items.Any(i=>i is BaseLeather))throw new Exception("Harvest did not yield leather");
  int count=c.Backpack.Items.OfType<BaseLeather>().Sum(i=>i.Amount);if(c.SkinCorpse(corpse)||c.Backpack.Items.OfType<BaseLeather>().Sum(i=>i.Amount)!=count)throw new Exception("Duplicate harvest");
  var other=new Cow();other.MoveToWorld(owner.Location,owner.Map);other.Kill();if(c.CanSkinCorpse(other.Corpse as Corpse))throw new Exception("Stranger corpse eligible");
  log("PASS earned animal corpse carved and leather gathered; injured/combat harvesting blocked; repeated harvest cannot duplicate; unearned corpse rejected");
  c.Delete();owner.Delete();enemy.Delete();corpse.Delete();if(other.Corpse!=null)other.Corpse.Delete();
 }
}
