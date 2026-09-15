using System.Linq;
using Server;
using Server.Items;
namespace Server.HavenPrototype {
 public partial class HavenCompanion {
  internal bool _deliveringMissionRewards;
  internal bool PlaceMissionReward(Item item){
   if(Backpack==null||item==null||item.Deleted)return false;
   _deliveringMissionRewards=true;
   try{if(item.IsChildOf(Backpack)){Backpack.DropItem(item);return true;}return Backpack.TryDropItem(this,item,false);}finally{_deliveringMissionRewards=false;}
  }
  internal void UnpackMissionSupplies(){
   if(Backpack==null)return;
   foreach(var bag in Backpack.Items.OfType<Bag>().Where(b=>b.Name=="taming mission bonus supplies").ToArray()){
    foreach(var item in bag.Items.ToArray())PlaceMissionReward(item);
    if(bag.Items.Count==0)bag.Delete();
   }
  }
 }
}

