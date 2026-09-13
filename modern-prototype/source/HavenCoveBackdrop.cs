using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Multis;
namespace Server.HavenPrototype {
 public static class HavenCoveBackdrop {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(3),()=>{try{Apply();}catch(Exception e){Console.WriteLine("Cove backdrop: "+e.Message);}});};}
  public static void Apply(){
   var approach=World.Items.Values.OfType<HavenCoveApproach>().FirstOrDefault(a=>!a.Deleted);
   if(approach==null||approach.Fixtures.Any(i=>!i.Deleted&&i.Name=="Blackwake clearing display complete"))return;
   var board=approach.Fixtures.OfType<HavenCoveBoard>().FirstOrDefault(b=>!b.Deleted);
   if(board==null||board.Map!=Map.Trammel||board.Location!=new Point3D(4211,2930,0))return;
   var oldWalls=approach.Fixtures.Where(i=>!i.Deleted&&i.Name=="Blackwake noticeboard stone backdrop").ToArray();
   var lamps=approach.Fixtures.Where(i=>!i.Deleted&&i.Name=="Blackwake cove entrance lamp").OrderBy(i=>i.X).ToArray();
   var flags=approach.Fixtures.Where(i=>!i.Deleted&&i.Name=="Blackwake expedition banner").OrderBy(i=>i.X).ToArray();
   if(lamps.Length!=2||flags.Length!=2)throw new Exception("Expected two lamps and banners");
   var moves=new Dictionary<Item,Point3D>{{board,new Point3D(4207,2926,0)},{lamps[0],new Point3D(4205,2927,0)},{lamps[1],new Point3D(4209,2927,0)},{flags[0],new Point3D(4205,2926,5)},{flags[1],new Point3D(4209,2926,5)}};
   var original=moves.Keys.Concat(oldWalls).ToDictionary(i=>i,i=>i.Location);
   var added=new List<Item>();
   try{
    foreach(var wall in oldWalls)wall.Internalize();
    foreach(var move in moves){
     var ground=new Point3D(move.Value.X,move.Value.Y,0);
     if(BaseHouse.FindHouseAt(ground,board.Map,20)!=null||!board.Map.CanFit(ground,20,true,true))throw new Exception("Display position occupied: "+ground);
     move.Key.MoveToWorld(move.Value,Map.Trammel);
    }
    for(int x=4205;x<=4209;x++){
     var spot=new Point3D(x,2925,0);
     if(BaseHouse.FindHouseAt(spot,Map.Trammel,20)!=null||!Map.Trammel.CanFit(spot,20,true,true))throw new Exception("Backdrop position occupied: "+spot);
     var wall=new Static(0x21){Name="Blackwake noticeboard stone backdrop",Movable=false};added.Add(wall);wall.MoveToWorld(spot,Map.Trammel);
    }
    var to=new Point3D(4214,2929,0);
    if(!new MovementPath(new Point3D(4215,2935,0),to,Map.Trammel).Success||!new MovementPath(new Point3D(4210,2928,0),to,Map.Trammel).Success||!new MovementPath(new Point3D(4210,2928,0),new Point3D(4207,2927,0),Map.Trammel).Success)throw new Exception("Display blocked access");
    var marker=new Static(1){Name="Blackwake clearing display complete",Visible=false,Movable=false};added.Add(marker);marker.MoveToWorld(approach.Location,Map.Trammel);
    approach.Fixtures.AddRange(added);
    foreach(var wall in oldWalls){approach.Fixtures.Remove(wall);wall.Delete();}
    Console.WriteLine("Cove backdrop: display relocated off junction; three walking routes pass");
   }catch{foreach(var i in added)i.Delete();foreach(var pair in original)pair.Key.MoveToWorld(pair.Value,Map.Trammel);throw;}
  }
 }
}

