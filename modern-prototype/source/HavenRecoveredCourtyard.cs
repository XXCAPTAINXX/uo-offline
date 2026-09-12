using Server;
namespace Server.HavenPrototype {
 public partial class HavenRecoveredHeadquarters {
  internal void FurnishRecoveredCourtyard() {
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
            Decorate(0xE77,"Stable feed barrel",-6,12); Decorate(0x14F8,"Leads and spare halters",-5,10);
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
  }
 }
}

