using System;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public static class HavenPortableTravel
    {
        public static bool CanUse(Mobile from, Item item) { return HavenPreview.CanTravel(from) && from.Spell==null && item!=null&&!item.Deleted&&from.Backpack!=null&&item.IsChildOf(from.Backpack)&&HavenResources.Accessible(from,item); }
        public static bool Go(Mobile from,Map map,Point3D target,int radius) {
            if(!HavenPreview.CanTravel(from)||from.Spell!=null||map==null||map==Map.Internal||!SpellHelper.CheckTravel(from,TravelCheckType.RecallFrom))return false;
            for(int r=1;r<=radius;r++)for(int dx=-r;dx<=r;dx++)for(int dy=-r;dy<=r;dy++){
                if(Math.Abs(dx)!=r&&Math.Abs(dy)!=r)continue;int x=target.X+dx,y=target.Y+dy;
                if(x<0||y<0||x>=map.Width||y>=map.Height)continue;
                var p=new Point3D(x,y,map.GetAverageZ(x,y));
                if(!map.CanSpawnMobile(p)||BaseHouse.FindHouseAt(p,map,16)!=null||!SpellHelper.CheckTravel(from,map,p,TravelCheckType.RecallTo))continue;
                BaseCreature.TeleportPets(from,p,map);from.MoveToWorld(p,map);from.PlaySound(0x1FE);return true;
            }return false;
        }
        public static bool ValidMap(Mobile from,TreasureMap map) { return map!=null&&!map.Deleted&&!map.Completed&&map.Decoder!=null&&from.Backpack!=null&&map.IsChildOf(from.Backpack)&&HavenResources.Accessible(from,map)&&map.Facet!=null&&map.Facet!=Map.Internal&&map.ChestLocation.X>=0&&map.ChestLocation.Y>=0&&map.ChestLocation.X<map.Facet.Width&&map.ChestLocation.Y<map.Facet.Height; }
    }
    public class HavenGoldenShovel : Shovel
    {
        [Constructable] public HavenGoldenShovel():base(1000){Name="The Gilded Pathfinder";Hue=0x8A5;LootType=LootType.Blessed;}
        public HavenGoldenShovel(Serial serial):base(serial){}
        public override void OnDoubleClick(Mobile from){if(!HavenPortableTravel.CanUse(from,this)){from.SendMessage("Keep the shovel in your pack and leave combat before travelling.");return;}from.SendMessage("Target a decoded, unfinished treasure map in your backpack.");from.Target=new MapTarget(this);}
        public bool Travel(Mobile from,TreasureMap map){return HavenPortableTravel.CanUse(from,this)&&HavenPortableTravel.ValidMap(from,map)&&HavenPortableTravel.Go(from,map.Facet,new Point3D(map.ChestLocation,0),2);}
        public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Target a decoded map to travel beside its dig site.");list.Add("Normal digging, guardians and treasure remain unchanged.");}
        sealed class MapTarget:Target{readonly HavenGoldenShovel _shovel;public MapTarget(HavenGoldenShovel shovel):base(-1,false,TargetFlags.None){_shovel=shovel;}protected override void OnTarget(Mobile from,object target){if(!_shovel.Travel(from,target as TreasureMap))from.SendMessage("Use a decoded, unfinished map in your pack with a safe travel destination.");}}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    }
    public class HavenEndlessBandage : Bandage
    {
        [Constructable] public HavenEndlessBandage(){Name="Mercy's Endless Bandage";Stackable=false;Hue=0x481;LootType=LootType.Blessed;}
        public HavenEndlessBandage(Serial serial):base(serial){}
        public override void Consume(int amount){ }
        public override void OnDoubleClick(Mobile from){if(from.Backpack!=null&&IsChildOf(from.Backpack))base.OnDoubleClick(from);else from.SendMessage("Keep the endless bandage in your pack.");}
        public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Unlimited uses; normal Healing and Veterinary checks apply.");}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    }
}
