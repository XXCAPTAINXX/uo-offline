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
   var existing=World.Items.Values.OfType<HavenFreePetHitchingPost>().FirstOrDefault(x=>!x.Deleted&&x.GetType()==typeof(HavenFreePetHitchingPost));if(existing!=null)return existing;
   Point3D point;if(!HavenPreview.FindLanding(new HavenPreview.Destination("Pet shrinking",Map.Trammel,3513,2580,Map.Trammel.GetAverageZ(3513,2580)),out point)){Console.WriteLine("Haven: no clear tile for free pet shrinking post.");return null;}
   var post=new HavenFreePetHitchingPost();post.MoveToWorld(point,Map.Trammel);return post;
  }
  internal static bool CanUseSource(Mobile p,Item source){
   if(source==null||source.Deleted||p==null||p.Deleted||!p.Alive)return false;
   if(source is HavenPetLeash)return p.Backpack!=null&&source.IsChildOf(p.Backpack)&&HavenResources.Accessible(p,source);
   if(source is HavenHouseHitchingPost&&!((HavenHouseHitchingPost)source).CanUse(p))return false;
   return source.Parent==null&&p.Map==source.Map&&p.InRange(source.Location,3)&&p.InLOS(source);
  }
  internal static void BeginShrink(Mobile p,Item source){if(!CanUseSource(p,source)){p.SendMessage("Keep a leash in your pack or stand beside an accessible post.");return;}p.SendMessage("Target a living pet you control to shrink it for free.");p.Target=new PetTarget(source);}
  public override void OnDoubleClick(Mobile p){BeginShrink(p,this);}
  public bool Shrink(Mobile p,BaseCreature pet){return ShrinkFrom(this,p,pet);}
  internal static bool ShrinkFrom(Item source,Mobile p,BaseCreature pet){
   if(!CanUseSource(p,source)||pet==null||pet.Deleted||!pet.Controlled||pet.ControlMaster!=p||pet.Map!=p.Map||!p.InRange(pet,3)||!p.InLOS(pet)){p?.SendMessage("Stand beside a pet you control and use your leash or post.");return false;}
   if(pet is HavenCompanion||pet.Body.IsHuman||pet.IsDeadPet||!pet.Alive||pet.Summoned){p.SendMessage("Only living, non-summoned animal pets can be shrunk.");return false;}
   if((pet is PackHorse||pet is PackLlama||pet is Beetle)&&pet.Backpack!=null&&pet.Backpack.Items.Count>0){p.SendMessage("Unload your pack pet before shrinking it.");return false;}
   var enemy=pet.Combatant as Mobile;if(enemy!=null&&enemy.Map==pet.Map&&pet.InRange(enemy,12)){p.SendMessage("Your pet must finish fighting before it can be shrunk.");return false;}
   var ticket=HavenPetTicket.Store(pet,p,p.Backpack);if(ticket==null){p.SendMessage("Make room in your backpack for the pet ticket.");return false;}
   p.SendMessage(pet.IsBonded?"Your bonded pet is safe in your pet book. Use [petbook to inspect or release it.":"Your exact pet is stored in its claim ticket. Double-click the ticket to bring it back.");return true;
  }
  class PetTarget:Target {readonly Item _post;public PetTarget(Item post):base(3,false,TargetFlags.None){_post=post;}protected override void OnTarget(Mobile p,object target){ShrinkFrom(_post,p,target as BaseCreature);}}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Double-click: shrink your pet for free");list.Add("Bonded pets go to your pet book; other pets become tickets");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
}
