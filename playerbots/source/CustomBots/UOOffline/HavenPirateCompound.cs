using System;
using System.Collections.Generic;
using System.Linq;
using Server.CustomBots;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.UOOffline;

public partial class HavenPirateHeadquarters
{
    internal bool HasCompound => CompanyFixtures.Any(i => !i.Deleted && i.Name == "R.E.C. courtyard compound layout v2");
    internal void RebuildCompound()
    {
        if (Deleted || HasCompound || CompanyFixtures.Count == 0) { return; }
        if (Customizer != null) { throw new InvalidOperationException("Finish the active house customization before rebuilding."); }
        var previous = CurrentState; var design = DesignState; var backup = BackupState;
        var locations = new Dictionary<Item,Point3D>(); var additions = new List<Item>();
        var people = new List<Mobile>();
        foreach (var person in Map.GetMobilesInRange<Mobile>(Location,22)) { if (IsInside(person)) { people.Add(person); } }
        foreach (var item in Map.GetItemsInRange<Item>(Location,22))
        { if (item.Parent == null && item != this && item != Sign && item is not AddonComponent && item is not BaseDoor && IsInside(item)) { locations.TryAdd(item,item.Location); } }
        foreach (var item in CompanyFixtures) { if (!item.Deleted) { locations.TryAdd(item,item.Location); } }
        var lamps = 0; var beds = 0; var troughs = 0;
        Point3D Position(Item item)
        {
            if (item == MasterStorage) { return new(1,6,7); }
            if (item is GuildProfessionChest chest)
            { return chest.Role switch {
                GuildStorageRole.Smithing => new(12,-10,7), GuildStorageRole.Tailoring => new(12,-5,7),
                GuildStorageRole.Carpentry => new(6,-10,7), GuildStorageRole.Tinkering => new(6,-5,7),
                GuildStorageRole.Resources => new(5,2,7), GuildStorageRole.Unsorted => new(2,6,7), GuildStorageRole.Overflow => new(3,6,7),
                GuildStorageRole.Pantry => new(-11,-11,7), GuildStorageRole.Taming => new(-12,10,7),
                GuildStorageRole.Alchemy => new(12,-10,27), GuildStorageRole.Scribing => new(6,-10,27),
                GuildStorageRole.Armory => new(-12,-11,27), GuildStorageRole.Treasury => new(-12,-11,47), _ => new(2,8,7) }; }
            if (item is HavenPirateStair) { return new(-1,0,item.Z-Z); }
            if (item is HavenPirateCharter) { return new(-5,-5,27); }
            if (item is HavenSmallSoulForge) { return new(11,-12,7); }
            if (item is AnvilSouthAddon) { return new(10,-9,7); }
            if (item is LoomSouthAddon) { return new(11,-7,7); }
            if (item is SpinningWheelSouthAddon) { return new(13,-3,7); }
            if (item is HavenRepairBench) { return new(7,1,7); }
            if (item is StoneOvenSouthAddon) { return new(-12,-13,7); }
            if (item is FlourMillSouthAddon) { return new(-12,-7,7); }
            if (item is WaterTroughEastAddon) { return troughs++ == 0 ? new(-12,-4,7) : new(-12,12,7); }
            if (item is HavenHouseHitchingPost) { return new(-6,11,7); }
            if (item is AlchemistTableSouthAddon) { return new(10,-7,27); }
            if (item is MediumStoneTableSouthAddon) { return new(-8,-7,27); }
            if (item is LargeBedSouthAddon) { return new(-12,-7,47); }
            if (item is SmallBedSouthAddon) { return beds++ == 0 ? new(-12,7,27) : new(-6,7,27); }
            if (item.Name is "A shaded ship's lantern" or "R.E.C. watch lantern")
            { var p = new[] {new Point2D(-13,-2),new Point2D(3,3),new Point2D(-13,14),new Point2D(14,14),new Point2D(-2,12),new Point2D(2,12),new Point2D(-13,-12),new Point2D(13,-12),new Point2D(-13,3),new Point2D(4,6),new Point2D(14,7)}[lamps++ % 11]; return new(p.X,p.Y,7); }
            return item.Name switch {
                "Charcoal and dry firewood" => new(13,-12,7), "Smith's workbench tools" => new(13,-9,7),
                "Bolts of sailcloth" => new(13,-7,7), "Rigging rope" => new(13,-2,7),
                "Shipwright's workbench" => new(6,-7,7), "Tinker's workbench" => new(6,-3,7),
                "Freshwater cask" => new(-5,-12,7), "The galley's stores" => new(-5,-11,7), "Pet bedding" => new(-6,6,7),
                "Charts and sailing accounts" => new(6,-7,27), "Cartographer's desk" => new(6,-5,27),
                "Quartermaster's chair" => new(-9,-8,27), "Company officer's chair" => new(-6,-8,27),
                "R.E.C. trade routes" => new(-8,-7,33), "The company export ledger" => new(6,-5,33),
                "The captain's desk" => new(-6,-7,47), "An unfinished voyage chart" => new(-6,-7,53),
                "The captain's chair" => new(-6,-6,47), "Captain's sea chest" => new(-5,-11,47),
                "Lookout's spyglass" => new(-12,1,47), "Harbor watch spyglass" => new(-3,1,47),
                "R.E.C. - Rare Export Company" => new(-3,1,27), "Rare cargo. Fair shares. Safe harbor." => new(3,1,27),
                _ => new(2,9,7)
            };
        }
        void Decorate(int id,string name,int x,int y,int z=7)
        {
            var item = new Static(id) { Name=name }; additions.Add(item);
            Place(item,x,y,z-1);
        }
        try
        {
            BuildPirateLayout(); HousePackets.CreateHouseDesignStateDetailed(Serial,LastRevision,Components);
            foreach (var item in CompanyFixtures)
            { if (item.Deleted) { continue; } var p = Position(item); item.MoveToWorld(new Point3D(X+p.X,Y+p.Y,Z+p.Z),Map); }
            // Unrelated placed possessions remain owned and secured, gathered into the open receiving yard.
            var n=0;
            foreach (var pair in locations)
            { if (!CompanyFixtures.Contains(pair.Key) && pair.Key is not TrashBarrel) { pair.Key.MoveToWorld(new Point3D(X+1+n%3,Y+9+n/3%4,Z+7),Map); n++; } }
            // Life in the courtyard: planted corners, a working dockside yard, not rows of props.
            foreach (var p in new[] {(7,10),(8,12),(11,12),(13,11),(12,8)})
            { Decorate(0xC8F,"Salt-tolerant island planting",p.Item1,p.Item2); }
            Decorate(0xC95,"A wind-bent island palm",12,12);
            Decorate(0xC83,"Flowers beside the crew path",6,12);
            Decorate(0xC87,"Flowers beside the crew path",13,8);
            Decorate(0xB90,"Crew mess table",7,8);
            Decorate(0x9D7,"Fresh provisions for the watch",7,8,13);
            Decorate(0xB2D,"A chair in the sea breeze",6,9); Decorate(0xB2D,"A chair in the sea breeze",8,9);
            Decorate(0xE77,"A barrel of grog",4,11); Decorate(0x14F8,"Spare rigging for the next voyage",4,12);
            Decorate(0xE3F,"Export cargo awaiting shipment",2,5); Decorate(0x14F8,"Coiled dock rope",3,5);
            Decorate(0xDCA,"Nets hung beside the sail loft",13,1,16);
            Decorate(0x1BDD,"Shipwright's seasoned timber",8,-11); Decorate(0x1036,"Sailmaker's spare wheel",8,-9);
            Decorate(0xF36,"Fresh straw in the barn",-11,6); Decorate(0xF36,"Fresh straw in the barn",-11,8);
            Decorate(0xE77,"Stable feed barrel",-5,12); Decorate(0x14F8,"Leads and spare halters",-5,10);
            Decorate(0xA9A,"R.E.C. navigators' library",12,-5,27);
            Decorate(0x1047,"The captain's private log",-6,-7,53);
            Decorate(0x14F5,"A brass spyglass",-5,-9,47);
            Decorate(0xE3F,"The company's recovered treasure",-11,-11,47);
            Decorate(0x14F7,"Anchor salvaged from the Blackwake",-2,7);
            Decorate(0x14F3,"A model of the company flagship",-6,-7,53);
            Decorate(0x1854,"The captain's skull candle",-6,-8,53);
            Decorate(0xFFB,"The quartermaster's grog mug",7,8,13);
            foreach (var p in new[] {new Point3D(-10,2,47),new Point3D(10,14,7)})
            { Decorate(0xE91,"Salvaged ship's cannon",p.X,p.Y,p.Z); Decorate(0xE92,"Salvaged ship's cannon",p.X,p.Y-1,p.Z); Decorate(0xE93,"Salvaged ship's cannon",p.X,p.Y-2,p.Z); Decorate(0xE74,"A rack of cannonballs",p.X+1,p.Y-1,p.Z); }
            Decorate(0x1F14,"R.E.C. courtyard compound layout v2",0,14); additions[^1].Visible=false;
            foreach (var person in people) { person.MoveToWorld(new Point3D(X,Y+11,Z+7),Map); }
            Sign.Name="R.E.C. - Rare Export Company | Pirate compound";
            Delta(ItemDelta.Update); this.MarkDirty();
        }
        catch
        {
            CurrentState=previous; DesignState=design; BackupState=backup;
            foreach (var item in additions) { CompanyFixtures.Remove(item); LockDowns.Remove(item); item.Delete(); }
            foreach (var pair in locations) { if (!pair.Key.Deleted) { pair.Key.MoveToWorld(pair.Value,Map); } }
            throw;
        }
    }
}
