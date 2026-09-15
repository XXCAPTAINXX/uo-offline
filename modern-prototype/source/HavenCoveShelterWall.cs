using System;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;

namespace Server.HavenPrototype
{
 public static class HavenCoveShelterWall
 {
  const string Marker="Blackwake shelter stone wall integrated";
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(5),()=>{try{HavenCoveBackdrop.Apply();Apply();}catch(Exception e){Console.WriteLine("Cove shelter: "+e.Message);}});};}
  public static void Apply()
  {
   var approach=World.Items.Values.OfType<HavenCoveApproach>().FirstOrDefault(x=>!x.Deleted);
   var foundation=World.Items.Values.OfType<HavenIslandFoundation>().FirstOrDefault(x=>!x.Deleted);
   if(approach==null||foundation==null||approach.Fixtures.Any(x=>!x.Deleted&&x.Name==Marker))return;
   var roof=foundation.Fixtures.Where(x=>!x.Deleted&&x.Name=="R.E.C. notice shelter awning").ToArray();
   if(roof.Length!=20)throw new Exception("Unexpected shelter roof");
   int left=roof.Min(x=>x.X),right=roof.Max(x=>x.X),back=roof.Max(x=>x.Y);
   if(right-left!=4)throw new Exception("Unexpected shelter width");
   var walls=approach.Fixtures.Where(x=>!x.Deleted&&x.Name=="Blackwake noticeboard stone backdrop").OrderBy(x=>x.X).ToArray();
   var lamps=approach.Fixtures.Where(x=>!x.Deleted&&x.Name=="Blackwake cove entrance lamp").OrderBy(x=>x.X).ToArray();
   var flags=approach.Fixtures.Where(x=>!x.Deleted&&x.Name=="Blackwake expedition banner").OrderBy(x=>x.X).ToArray();
   var board=approach.Fixtures.OfType<HavenCoveBoard>().Single(x=>!x.Deleted);
   var posts=foundation.Fixtures.Where(x=>!x.Deleted&&x.Name=="R.E.C. notice shelter post"&&x.Y==back).ToArray();
   if(walls.Length!=5||lamps.Length!=2||flags.Length!=2||posts.Length!=2)throw new Exception("Unexpected display fixtures");
   var moves=new Dictionary<Item,Point3D>();
   for(int i=0;i<5;i++)moves[walls[i]]=new Point3D(left+i,back,0);
   moves[board]=new Point3D(left+2,back+1,0);
   for(int i=0;i<2;i++){int x=i==0?left:right;moves[flags[i]]=new Point3D(x,back+1,5);moves[lamps[i]]=new Point3D(x,back+2,0);}
   var original=moves.Keys.Concat(posts).ToDictionary(x=>x,x=>x.Location);
   try {
    foreach(var item in original.Keys)item.Internalize();
    foreach(var move in moves){var ground=new Point3D(move.Value.X,move.Value.Y,0);if(BaseHouse.FindHouseAt(ground,foundation.Map,20)!=null||!foundation.Map.CanFit(ground,20,true,true))throw new Exception("Occupied wall site "+ground);move.Key.MoveToWorld(move.Value,foundation.Map);}
    var junction=new Point3D(4214,2929,0);
    foreach(var target in new[]{new Point3D(left+2,back+2,0),new Point3D(left+2,back-1,0),new Point3D(4215,2935,0)})
     if(!new MovementPath(junction,target,foundation.Map).Success)throw new Exception("Shelter or board route blocked");
    var marker=new Static(1){Name=Marker,Visible=false,Movable=false};marker.MoveToWorld(approach.Location,approach.Map);approach.Fixtures.Add(marker);
    foreach(var post in posts){foundation.Fixtures.Remove(post);post.Delete();}
    Console.WriteLine("Cove shelter: stone back wall integrated; shelter, board and junction routes pass");
   }catch{foreach(var old in original)old.Key.MoveToWorld(old.Value,foundation.Map);throw;}
  }
 }
}
