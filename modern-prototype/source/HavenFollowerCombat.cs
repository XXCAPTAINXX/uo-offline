using System.Linq;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public static class HavenFollowerCombat
 {
  public static void Clear(BaseCreature pet)
  {
   if(!HavenPreview.Enabled||pet==null||pet.Deleted||(!pet.Controlled&&!pet.Summoned))return;
   if(HavenFollowerMovement.CanPass(pet,pet.Combatant as Mobile))pet.Combatant=null;
   if(HavenFollowerMovement.CanPass(pet,pet.FocusMob as Mobile))pet.FocusMob=null;
   if(pet.ControlOrder==OrderType.Attack&&HavenFollowerMovement.CanPass(pet,pet.ControlTarget as Mobile)){pet.ControlTarget=pet.ControlMaster;pet.ControlOrder=OrderType.Follow;}
   if(HavenFollowerMovement.CanPass(pet,pet.BardTarget)){pet.BardTarget=null;pet.BardProvoked=false;}
   var friends=pet.Aggressors.Select(a=>a.Attacker).Concat(pet.Aggressed.Select(a=>a.Defender)).Where(m=>HavenFollowerMovement.CanPass(pet,m)).Distinct().ToArray();
   foreach(var friend in friends){pet.RemoveAggressor(friend);pet.RemoveAggressed(friend);friend.RemoveAggressor(pet);friend.RemoveAggressed(pet);}
  }
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)foreach(var pet in World.Mobiles.Values.OfType<BaseCreature>().ToArray())Clear(pet);};}
 }
}
