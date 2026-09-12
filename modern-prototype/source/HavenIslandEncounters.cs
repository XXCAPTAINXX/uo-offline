using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
namespace Server.HavenPrototype {
 // World encounter prototype; installation is deliberately isolated until island release.
 public class HavenIslandEncounters:Item {
  public readonly List<HavenIslandRaider> Raiders=new List<HavenIslandRaider>();
  public int Theme;public DateTime NextWave;private Timer _timer;
  public HavenIslandEncounters(int theme):base(1){Theme=theme;Visible=false;Movable=false;Name="Island encounter habitat";}
  public HavenIslandEncounters(Serial serial):base(serial){}
  public static HavenIslandEncounters[] BuildTest(){
   if(!File.Exists("ISLAND-TEST-ONLY"))throw new InvalidOperationException("Island encounters require isolated testing.");
   if(World.Items.Values.OfType<HavenIslandEncounters>().Any(x=>!x.Deleted))throw new InvalidOperationException("Island encounters already installed.");
   var sites=new[]{new Point3D(4172,2910,0),new Point3D(4240,2845,0),new Point3D(4220,2930,0)};var result=new List<HavenIslandEncounters>();
   try{for(int i=0;i<sites.Length;i++){var site=new HavenIslandEncounters(i);result.Add(site);site.MoveToWorld(sites[i],Map.Trammel);site.Tick();if(site.Raiders.Count!=3)throw new InvalidOperationException("Encounter site lacks safe ground: "+sites[i]);site.Start();}return result.ToArray();}
   catch{foreach(var site in result)site.Delete();throw;}
  }
  void Start(){_timer?.Stop();_timer=Timer.DelayCall(TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(2),Tick);}
  public bool Safe(Point3D p){return Map!=null&&Map!=Map.Internal&&Math.Abs(p.X-X)<=8&&Math.Abs(p.Y-Y)<=8&&Map.CanSpawnMobile(p)&&BaseHouse.FindHouseAt(p,Map,20)==null;}
  internal void Tick(){
   if(Deleted)return;
   foreach(var pet in Raiders.ToArray())if(pet.Deleted||pet.Controlled||pet.Owners.Count>0)Raiders.Remove(pet);
   if(Raiders.Count>0||DateTime.UtcNow<NextWave)return;
   var floor=new List<Point3D>();for(int x=X-6;x<=X+6;x++)for(int y=Y-6;y<=Y+6;y++){var p=new Point3D(x,y,Map.GetAverageZ(x,y));if(Safe(p))floor.Add(p);}
   if(floor.Count<3)return;
   for(int i=0;i<3;i++){int pick=Utility.Random(floor.Count);var point=floor[pick];floor.RemoveAt(pick);var mob=new HavenIslandRaider(this,i==0&&Utility.RandomDouble()<0.15);Raiders.Add(mob);mob.MoveToWorld(point,Map);mob.Home=point;mob.RangeHome=3;}
  }
  internal void Defeated(HavenIslandRaider mob){Raiders.Remove(mob);if(Raiders.Count==0)NextWave=DateTime.UtcNow.AddMinutes(2);}
  public override void OnDelete(){_timer?.Stop();foreach(var mob in Raiders.ToArray())if(!mob.Deleted&&!mob.Controlled&&mob.Owners.Count==0)mob.Delete();Raiders.Clear();base.OnDelete();}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Theme);w.Write(NextWave);w.Write(Raiders.Count);foreach(var mob in Raiders)w.Write(mob);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Theme=r.ReadInt();NextWave=r.ReadDateTime();int count=r.ReadInt();if(count<0||count>3)throw new InvalidOperationException("Invalid island patrol size");for(int i=0;i<count;i++){var mob=r.ReadMobile() as HavenIslandRaider;if(mob!=null)Raiders.Add(mob);}Timer.DelayCall(TimeSpan.Zero,Start);}
 }
 public class HavenIslandRaider:Brigand {
  public HavenIslandEncounters Encounter;public bool Captain;
  public HavenIslandRaider(HavenIslandEncounters encounter,bool captain){Encounter=encounter;Captain=captain;Name=captain?"Captain Blackwake":encounter.Theme==0?"a corsair lookout":encounter.Theme==1?"a relic poacher":"a cove smuggler";Title=null;SetHits(captain?1800:320);SetStr(captain?300:150);SetDamage(captain?16:8,captain?24:14);SetSkill(SkillName.Swords,captain?110:85);SetSkill(SkillName.Tactics,captain?110:85);SetSkill(SkillName.MagicResist,captain?100:70);Tamable=false;}
  public HavenIslandRaider(Serial serial):base(serial){}
  public override bool CanBeHarmful(IDamageable target,bool message,bool ignoreOurBlessedness){var mob=target as Mobile;if(Encounter==null||Encounter.Deleted||mob==null||mob.Map!=Encounter.Map||!mob.InRange(Encounter,8)||BaseHouse.FindHouseAt(mob.Location,mob.Map,20)!=null)return false;return base.CanBeHarmful(target,message,ignoreOurBlessedness);}
  public override void OnThink(){base.OnThink();if(Encounter==null||Encounter.Deleted||Controlled)return;if(Map!=Encounter.Map||!InRange(Encounter,8)||BaseHouse.FindHouseAt(Location,Map,20)!=null){Combatant=null;if(Encounter.Safe(Home))MoveToWorld(Home,Encounter.Map);}}
  public override void OnDeath(Container corpse){base.OnDeath(corpse);if(corpse!=null){corpse.DropItem(new Gold(Captain?Utility.RandomMinMax(1500,2500):Utility.RandomMinMax(150,300)));if(Captain)HavenMiniBossLoot.Drop(corpse,1);else if(Utility.RandomDouble()<.05)corpse.DropItem(new TreasureMap(Utility.RandomMinMax(1,3),Map.Trammel));if(Utility.RandomDouble()<.35)corpse.DropItem(new Cannonball(Captain?25:5));}Encounter?.Defeated(this);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Encounter);w.Write(Captain);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Encounter=r.ReadItem() as HavenIslandEncounters;Captain=r.ReadBool();}
 }
}
