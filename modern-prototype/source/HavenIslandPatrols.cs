using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
namespace Server.HavenPrototype {
 public partial class HavenIslandEncounters {
  public readonly List<WayPoint> Patrol=new List<WayPoint>();
  DateTime _nextPatrolAttempt;
  internal bool PreparePatrol() {
   if(Patrol.Count>0)return true;
   if(DateTime.UtcNow<_nextPatrolAttempt)return false;
   try{EnsurePatrol();return true;}
   catch(InvalidOperationException){_nextPatrolAttempt=DateTime.UtcNow.AddSeconds(10);return false;}
  }
  internal void EnsurePatrol() {
   if(Patrol.Count!=0)return;
   var corners=new List<Point3D>();
   foreach(var offset in new[]{new Point2D(-4,-4),new Point2D(4,-4),new Point2D(4,4),new Point2D(-4,4)}) {
    Point3D? chosen=null;
    for(int r=0;r<=2&&!chosen.HasValue;r++)for(int dx=-r;dx<=r&&!chosen.HasValue;dx++)for(int dy=-r;dy<=r&&!chosen.HasValue;dy++) {
     int x=X+offset.X+dx,y=Y+offset.Y+dy;var p=new Point3D(x,y,Map.GetAverageZ(x,y));if(Safe(p)&&!corners.Contains(p))chosen=p;
    }
    if(!chosen.HasValue)throw new InvalidOperationException("No safe patrol corner at "+Location);corners.Add(chosen.Value);
   }
   var route=new List<Point3D>();
   for(int i=0;i<corners.Count;i++)route.AddRange(PatrolLeg(corners[i],corners[(i+1)%corners.Count]));
   if(route.Count<4||route.Count>128)throw new InvalidOperationException("Invalid patrol length");
   try{foreach(var p in route){var node=new WayPoint{Movable=false};Patrol.Add(node);node.MoveToWorld(p,Map);}for(int i=0;i<Patrol.Count;i++)Patrol[i].NextPoint=Patrol[(i+1)%Patrol.Count];}
   catch{foreach(var node in Patrol)node.Delete();Patrol.Clear();throw;}
   foreach(var raider in Raiders.Where(r=>!r.Deleted))raider.CurrentWayPoint=Patrol.OrderBy(p=>raider.GetDistanceToSqrt(p)).First();
  }
  List<Point3D> PatrolLeg(Point3D start,Point3D goal) {
   var queue=new Queue<Point3D>();var parent=new Dictionary<Point3D,Point3D>();queue.Enqueue(start);parent[start]=start;
   while(queue.Count>0&&!parent.ContainsKey(goal)) {
    var at=queue.Dequeue();for(int d=0;d<8;d++) {
     int x=at.X,y=at.Y,z;Server.Movement.Movement.Offset((Direction)d,ref x,ref y);
     if(Math.Abs(x-X)>7||Math.Abs(y-Y)>7||!Server.Movement.Movement.CheckMovement(at,Map,at,(Direction)d,out z))continue;
     var next=new Point3D(x,y,z);if(!Safe(next)||parent.ContainsKey(next))continue;parent[next]=at;queue.Enqueue(next);
    }
   }
   if(!parent.ContainsKey(goal))throw new InvalidOperationException("Disconnected patrol at "+Location);
   var points=new List<Point3D>();for(var p=goal;p!=start;p=parent[p])points.Add(p);points.Reverse();return points;
  }
 }
}
