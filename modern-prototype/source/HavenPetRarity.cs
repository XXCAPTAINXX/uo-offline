using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype
{
    // Persistent identity for rarity, independent of the disposable claim ticket.
    public class HavenPetRarity : Item
    {
        public int Tier { get; private set; }
        public static string Label(int tier){return new[]{"Normal","Rare","Epic","Legendary"}[Math.Max(0,Math.Min(3,tier))];}
        public HavenPetRarity(int tier):base(1){Tier=Math.Max(0,Math.Min(3,tier));Visible=false;Movable=false;Weight=0;Name="pet rarity";}
        public HavenPetRarity(Serial serial):base(serial){}
        public static HavenPetRarity Find(BaseCreature pet){return pet.Backpack==null?null:pet.Backpack.FindItemByType(typeof(HavenPetRarity),true) as HavenPetRarity;}
        public static void Attach(BaseCreature pet,int tier){if(Find(pet)!=null)return;if(pet.Backpack==null)pet.AddItem(new Backpack());pet.Backpack.DropItem(new HavenPetRarity(tier));pet.InvalidateProperties();}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Tier);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Tier=Math.Max(0,Math.Min(3,r.ReadInt()));}
    }
}
