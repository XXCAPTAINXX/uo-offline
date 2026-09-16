using System;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;
namespace Server.HavenPrototype
{
    public static class HavenIslandDecorating
    {
        static readonly Dictionary<int,FlipableAttribute> Facings = new Dictionary<int,FlipableAttribute>();
        static bool _facingsReady;
        internal static bool Managed(Item item)
        {
            return World.Items.Values.OfType<HavenIslandFoundation>().Any(x=>x.Fixtures.Contains(item)) ||
                World.Items.Values.OfType<HavenIslandCommons>().Any(x=>x.Fixtures.Contains(item)) ||
                World.Items.Values.OfType<HavenRecoveredHeadquarters>().Any(x=>x.CompanyFixtures.Contains(item)) ||
                World.Items.Values.OfType<HavenCoveApproach>().Any(x=>x.Fixtures.Contains(item));
        }
        internal static bool Owner(Mobile from)
        {
            return from!=null && World.Items.Values.OfType<HavenRecoveredHeadquarters>().Any(h=>!h.Deleted && h.Owner!=null && (h.Owner==from || (from.Account!=null && h.Owner.Account==from.Account)));
        }
        static bool Island(Point3D p) { return p.X>=3936 && p.X<=4319 && p.Y>=2780 && p.Y<=3079; }
        static int Floor(Item item)
        {
            int z=item.Map.GetAverageZ(item.X,item.Y);
            foreach(var tile in item.Map.Tiles.GetStaticTiles(item.X,item.Y,true))
            {var data=TileData.ItemTable[tile.ID&TileData.MaxItemValue];if(data.Surface&&!data.Impassable&&tile.Z<=item.Z&&tile.Z>z)z=tile.Z;}
            return z;
        }
        static void Turn(Item item,Mobile from)
        {
            var declared=item.GetType().GetCustomAttributes(typeof(FlipableAttribute),false).Cast<FlipableAttribute>().FirstOrDefault();
            if(declared!=null){declared.Flip(item);return;}
            if(!_facingsReady)
            {
                foreach(var type in typeof(WoodenChest).Assembly.GetTypes().Where(t=>typeof(Item).IsAssignableFrom(t)).OrderBy(t=>t.FullName))
                    foreach(FlipableAttribute attr in type.GetCustomAttributes(typeof(FlipableAttribute),false))
                        if(attr.ItemIDs!=null && attr.ItemIDs.Length>1)foreach(int id in attr.ItemIDs)if(!Facings.ContainsKey(id))Facings[id]=attr;
                _facingsReady=true;
            }
            FlipableAttribute facing;
            if(item is Static && Facings.TryGetValue(item.ItemID,out facing))facing.Flip(item);
            else from.SendMessage("This decoration has no alternate facing in the game art.");
        }
        public static bool Apply(Mobile from,Item item,DecorateCommand command)
        {
            if(command==DecorateCommand.None || command==DecorateCommand.GetHue || item==null || item.Deleted || item.Parent!=null || !item.Visible || !Managed(item))return false;
            if(from==null || !from.Alive || !Owner(from) || from.Map!=Map.Trammel || item.Map!=from.Map || !Island(item.Location) || !from.InRange(item,24))
            {from?.SendMessage("Only the island owner can rearrange these fixtures, from within 24 tiles.");return true;}
            if(command==DecorateCommand.LockDown || command==DecorateCommand.Secure || command==DecorateCommand.Release)
            {
                if(BaseHouse.FindHouseAt(item)!=null)return false;
                from.SendMessage("Island fixtures already stay in place. Use the direction buttons to arrange them.");return true;
            }
            if(item is BaseAddon || item is AddonComponent || item is BaseAddonContainer || item is AddonContainerComponent || item is BaseDoor || item is HavenRecoveredLadder || item is HavenCoveBoard || item is HavenServiceStone)
            {from.SendMessage("Use the decorator on individual furnishings and scenery here.");return true;}
            if(command==DecorateCommand.Turn){Turn(item,from);return true;}
            int dx=command==DecorateCommand.East?1:command==DecorateCommand.West?-1:0;
            int dy=command==DecorateCommand.South?1:command==DecorateCommand.North?-1:0;
            int dz=command==DecorateCommand.Up?1:command==DecorateCommand.Down?-1:0;
            if(dx==0&&dy==0&&dz==0)return false;
            var point=new Point3D(item.X+dx,item.Y+dy,item.Z+dz);
            var house=BaseHouse.FindHouseAt(item); var destinationHouse=BaseHouse.FindHouseAt(point,item.Map,Math.Max(1,item.ItemData.Height));
            if(!Island(point) || house!=destinationHouse || (dz!=0 && (point.Z<Floor(item)||point.Z>Floor(item)+15)) ||
                (dz==0 && !item.Map.CanFit(point.X,point.Y,point.Z,Math.Max(1,item.ItemData.Height),true,true,false)))
            {from.SendMessage("That space is blocked, beyond the decorating area, or outside this floor's height range.");return true;}
            item.Location=point;item.InvalidateProperties();
            return true;
        }
    }
}
