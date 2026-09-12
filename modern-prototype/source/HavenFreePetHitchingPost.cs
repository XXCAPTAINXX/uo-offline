using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
namespace Server.HavenPrototype {
 public class HavenFreePetHitchingPost:Item {
  public HavenFreePetHitchingPost():base(0x14E7){Name="free pet shrinking post";Movable=false;Hue=0x59B;}
  public HavenFreePetHitchingPost(Serial serial):base(serial){}
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(3),()=>{Ensure();});};}
  public static HavenFreePetHitchingPost Ensure(){
   var existing=World.Items.Values.OfType<HavenFreePetHitchingPost>().FirstOrDefault(x=>!x.Deleted);if(existing!=null)return existing;
   Point3D point;if(!HavenPreview.FindLanding(new HavenPreview.Destination("Pet shrinking",Map.Trammel,3513,2580,Map.Trammel.GetAverageZ(3513,2580)),out point)){Console.WriteLine("Haven: no clear tile for free pet shrinking post.");return null;}
   var post=new HavenFreePetHitchingPost();post.MoveToWorld(point,Map.Trammel);return post;
  }
  bool Near(Mobile p){return !Deleted&&p!=null&&!p.Deleted&&p.Alive&&p.Map==Map&&p.InRange(Location,3)&&p.InLOS(this);}
  public override void OnDoubleClick(Mobile p){if(!Near(p)){p.SendMessage("Stand within three tiles of the post.");return;}p.SendMessage("Target a living pet you control to shrink it for free.");p.Target=new PetTarget(this);}
  public bool Shrink(Mobile p,BaseCreature pet){
   if(!Near(p)||pet==null||pet.Deleted||!pet.Controlled||pet.ControlMaster!=p||pet.Map!=p.Map||!p.InRange(pet,3)||!p.InLOS(pet)){p?.SendMessage("Stand beside the post and a pet you control.");return false;}
   if(pet is HavenCompanion||pet.Body.IsHuman||pet.IsDeadPet||!pet.Alive||pet.Summoned){p.SendMessage("Only living, non-summoned animal pets can be shrunk.");return false;}
   if((pet is PackHorse||pet is PackLlama||pet is Beetle)&&pet.Backpack!=null&&pet.Backpack.Items.Count>0){p.SendMessage("Unload your pack pet before shrinking it.");return false;}
   var enemy=pet.Combatant as Mobile;if(enemy!=null&&enemy.Map==pet.Map&&pet.InRange(enemy,12)){p.SendMessage("Your pet must finish fighting before it can be shrunk.");return false;}
   var ticket=HavenPetTicket.Store(pet,p,p.Backpack);if(ticket==null){p.SendMessage("Make room in your backpack for the pet ticket.");return false;}
   p.SendMessage("Your exact pet is stored in its claim ticket. Double-click the ticket to bring it back.");return true;
  }
  class PetTarget:Target {readonly HavenFreePetHitchingPost _post;public PetTarget(HavenFreePetHitchingPost post):base(3,false,TargetFlags.None){_post=post;}protected override void OnTarget(Mobile p,object target){_post.Shrink(p,target as BaseCreature);}}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Double-click: shrink your pet for free");list.Add("Exact pet and training preserved in a claim ticket");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
}
