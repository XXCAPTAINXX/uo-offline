using System;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.HavenPrototype;
public static class TravelMenuSmoke {
 public static void Run(Action<string> log){
  var all=Enumerable.Range(0,5).SelectMany(c=>PreviewGump.Stops(c)).ToArray();
  var indices=all.Where(s=>s.Index>=0).Select(s=>s.Index).ToArray();
  if(indices.Distinct().Count()!=HavenPreview.Destinations.Length||indices.Length!=HavenPreview.Destinations.Length)throw new Exception("Missing or duplicated fixed destination");
  var probe=new PlayerMobile();probe.Player=true;probe.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
  try {
   System.IO.File.WriteAllText("HAVEN-INTERACTIVE-PREVIEW","1");
   foreach(var stop in all.Where(s=>s.Source!=null)){
    bool moved=stop.Travel(probe);
    if(moved&&probe.Map!=stop.Source.Map)throw new Exception("Wrong destination facet");
    log((moved?"PASS":"BLOCKED")+" approach: "+stop.Name+" | "+stop.Map+" | "+stop.Detail);
   }
   for(int c=0;c<5;c++) {new PreviewGump(0,c);new PreviewGump(999,c);}
   var stale=new HavenMiniChamp();stale.MoveToWorld(probe.Location,probe.Map);var old=new HavenTravelStop{Name="Deleted",Source=stale};stale.Delete();if(old.Travel(probe))throw new Exception("Deleted target accepted");
   log("PASS all 38 fixed destinations exactly once; all category/page constructors; deleted target rejected");
  }finally{probe.Delete();}
 }
}

