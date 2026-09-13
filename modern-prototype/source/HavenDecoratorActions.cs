using System;
using Server.Items;
using Server.Multis;
namespace Server.HavenPrototype
{
    public static class HavenDecoratorActions
    {
        public static bool Apply(Mobile from, Item item, DecorateCommand command)
        {
            if (HavenIslandDecorating.Apply(from,item,command)) return true;
            if (command < DecorateCommand.North || command > DecorateCommand.Release) return false;
            var house = from == null ? null : BaseHouse.FindHouseAt(from);
            if (from == null || !from.Alive || house == null || !house.IsCoOwner(from) || item == null || item.Deleted || item.Parent != null || item.Map != from.Map || !house.IsInside(item))
            { from?.SendMessage("Target furniture placed inside your house."); return true; }
            if (command == DecorateCommand.LockDown) { house.LockDown(from,item); return true; }
            if (command == DecorateCommand.Secure) { house.AddSecure(from,item); return true; }
            if (command == DecorateCommand.Release) { house.Release(from,item); return true; }
            if ((!house.IsLockedDown(item) && !house.IsSecure(item)) || item is AddonComponent || item is AddonContainerComponent || item is BaseAddonContainer || item is BaseDoor)
            { from.SendMessage("Lock down or secure an individual piece of furniture before moving it."); return true; }
            int dx = command == DecorateCommand.East ? 1 : command == DecorateCommand.West ? -1 : 0;
            int dy = command == DecorateCommand.South ? 1 : command == DecorateCommand.North ? -1 : 0;
            var point = new Point3D(item.X+dx,item.Y+dy,item.Z);
            int height = Math.Max(1,item.ItemData.Height);
            bool floor = false;
            foreach (var tile in item.Map.Tiles.GetStaticTiles(point.X,point.Y,true))
            {
                var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
                if (data.Surface && !data.Impassable && tile.Z <= point.Z && tile.Z >= point.Z-15) { floor=true;break; }
            }
            if (!house.IsInside(point,height) || !floor || !item.Map.CanFit(point.X,point.Y,point.Z,height,true,true,false))
            { from.SendMessage("That space is blocked, outside your house, or has no supporting floor."); return true; }
            item.Location=point;
            return true;
        }
    }
}

