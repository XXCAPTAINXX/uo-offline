using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class ArrivalStorySmoke
{
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,Body=0x190,RawStr=100};p.AddItem(new Backpack());var account=new Account("arrival-test-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));account[0]=p;
  HavenCompanion companion=null;
  try
  {
   if(!HavenArrivalStory.PlaceNewCharacter(p)||p.Map!=Map.Trammel||!p.InRange(HavenArrivalStory.Arrival.Point,3)||HavenArrivalStory.Stage(p)!=1)throw new Exception("New arrival placement");
   var at=p.Location;if(HavenArrivalStory.PlaceNewCharacter(p)||p.Location!=at)throw new Exception("Repeated placement");
   var beacon=HavenArrivalStory.EnsureBeacon();if(beacon==null||HavenArrivalStory.EnsureBeacon()!=beacon)throw new Exception("Beacon duplicated/missing");
   companion=HavenCompanion.Claim(p);companion.MoveToWorld(new Point3D(1400,1700,0),Map.Felucca);if(HavenArrivalStory.Meet(p)!=null||HavenArrivalStory.Stage(p)!=1)throw new Exception("Absent companion completed meeting");companion.MoveToWorld(p.Location,p.Map);
   companion=HavenArrivalStory.Meet(p);if(companion==null||!companion.Female||companion.Body!=0x191||companion.Name!="Jenna Ashford"||HavenArrivalStory.Stage(p)!=2)throw new Exception("Jenna introduction");
   int count=World.Mobiles.Values.OfType<HavenCompanion>().Count(c=>!c.Deleted&&c.IsOwner(p));double skills=companion.Skills.Total;
   if(HavenArrivalStory.Meet(p)!=null||World.Mobiles.Values.OfType<HavenCompanion>().Count(c=>!c.Deleted&&c.IsOwner(p))!=count)throw new Exception("Repeated recruitment");
   p.MoveToWorld(new Point3D(1400,1700,0),Map.Felucca);at=p.Location;HavenArrivalStory.Show(p);if(p.Location!=at||p.Map!=Map.Felucca||companion.Skills.Total!=skills)throw new Exception("Replay changed progress/location");
   var old=new PlayerMobile{Player=true,Body=0x190};account[1]=old;old.MoveToWorld(at,Map.Felucca);try{if(HavenArrivalStory.PlaceNewCharacter(old)||HavenArrivalStory.Meet(old)!=null||HavenArrivalStory.Stage(old)!=0)throw new Exception("Existing character enrolled");if(new HavenArrivalStoryGump(old,0).Entries.Count==0||new HavenArrivalStoryGump(old,1).Entries.Count==0)throw new Exception("Replay pages");}finally{old.Delete();}
   log("PASS first-character arrival at valid Haven landing; no repeated movement; beacon idempotence; female Jenna recruitment once; existing-character replay preserves location/skills and cannot recruit/reset");
  }finally{foreach(var horse in World.Mobiles.Values.OfType<HavenStoryHorse>().Where(h=>h.GiftOwner==p).ToArray())horse.Delete();if(companion!=null)companion.Delete();p.Delete();}
 }
}
