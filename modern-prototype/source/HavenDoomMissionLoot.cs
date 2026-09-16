using System.Linq;
using Server.Items;
namespace Server.HavenPrototype {
 public class HavenDoomMissionLoot:Container {
  private Mobile _owner;
  public HavenDoomMissionLoot(Mobile owner,Item artifact):base(0xE76){
   _owner=owner;Name="Recovered Doom artifact";Visible=false;Movable=false;
   Internalize();DropItem(artifact);
  }
  public HavenDoomMissionLoot(Serial serial):base(serial){}
  public static HavenDoomMissionLoot[] For(Mobile owner){return World.Items.Values.OfType<HavenDoomMissionLoot>().Where(x=>!x.Deleted&&x._owner==owner).ToArray();}
  public static int Pending(Mobile owner){return For(owner).Sum(x=>x.Items.Count);}
  public static int DeliverTo(HavenCompanion companion){
   if(companion==null||companion.Deleted||companion.Backpack==null||companion.BoundOwner==null)return 0;
   int count=0;foreach(var parcel in For(companion.BoundOwner)){
    foreach(var item in parcel.Items.ToArray())if(companion.PlaceMissionReward(item))count++;
    if(parcel.Items.Count==0)parcel.Delete();
   }
   if(count>0&&companion.BoundOwner.NetState!=null){companion.BoundOwner.SendMessage(count+" Doom artifact(s) delivered to your companion's backpack.");HavenAdvancedRewards.CelebrateDrop(companion.BoundOwner);}
   return count;
  }
  public static int Collect(Mobile owner){
   if(owner==null||owner.Deleted||!owner.Alive||owner.Backpack==null)return 0;
   int collected=0;
   foreach(var parcel in For(owner)){
    foreach(var item in parcel.Items.ToArray())if(owner.Backpack.TryDropItem(owner,item,false))collected++;
    if(parcel.Items.Count==0)parcel.Delete();
   }
   if(collected>0)HavenAdvancedRewards.CelebrateDrop(owner);
   owner.SendMessage("Collected "+collected+" Doom artifacts. Protected returns waiting: "+Pending(owner)+".");
   return collected;
  }
  public override void Serialize(GenericWriter writer){base.Serialize(writer);writer.Write(0);writer.Write(_owner);}
  public override void Deserialize(GenericReader reader){base.Deserialize(reader);reader.ReadInt();_owner=reader.ReadMobile();}
 }
}

