using System;
using Server;
using Server.Items;
using Server.Spells;
using Server.HavenPrototype;
public static class SmokeTeleportSmoke
{
 public static void Run(Mobile p,Action<bool,string> check)
 {
  p=new Server.Mobiles.PlayerMobile {Player=true,Body=0x190};p.AddItem(new Backpack());p.MoveToWorld(new Point3D(3474,2602,10),Map.Trammel);
  var bomb=new SmokeBomb {Amount=3};p.Backpack.DropItem(bomb);
  var start=p.Location;var map=p.Map;
  HavenSmokeTeleport.Begin(p,bomb);check(p.Target!=null,"smoke bomb opens ground target without skill requirement");
  p.Target.Invoke(p,new Server.Targeting.LandTarget(new Point3D(start.X+30,start.Y,start.Z),map));check(bomb.Amount==3&&p.Location==start,"out-of-range smoke target preserves item and position");
  Point3D destination=start;
  for(int x=-4;x<=4&&destination==start;x++)for(int y=-4;y<=4;y++){
   var candidate=new Point3D(start.X+x,start.Y+y,map.GetAverageZ(start.X+x,start.Y+y));
   if(candidate!=start&&p.InLOS(candidate)&&map.CanSpawnMobile(candidate.X,candidate.Y,candidate.Z)&&!SpellHelper.CheckMulti(candidate,map)&&Region.Find(candidate,map).GetRegion(typeof(Server.Regions.HouseRegion))==null){destination=candidate;break;}
  }
  check(destination!=start,"smoke test has a walkable visible destination");
  HavenSmokeTeleport.Begin(p,bomb);p.Target.Invoke(p,new Server.Targeting.LandTarget(destination,map));
  check(p.Location==destination&&bomb.Amount==2,"successful smoke teleport consumes exactly one bomb");
  HavenSmokeTeleport.Begin(p,bomb);check(!p.CanBeginAction(typeof(HavenSmokeTeleport)),"smoke teleport cooldown enforced");
  p.EndAction(typeof(HavenSmokeTeleport));bomb.Delete();p.Delete();
 }
}
