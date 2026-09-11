using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenWardenPost:Item {
  public HavenOldWarden Warden;public DateTime NextSpawn;
  public HavenWardenPost():base(0xBD2){Name="Old Haven Warden - encounter marker";Movable=false;}
  public HavenWardenPost(Serial s):base(s){}
  public static HavenPreview.Destination Arrival{get{return new HavenPreview.Destination("Old Haven Warden",Map.Trammel,3698,2595,Map.Trammel.GetAverageZ(3698,2595));}}
  public static HavenWardenPost Find(){return World.Items.Values.OfType<HavenWardenPost>().FirstOrDefault(x=>!x.Deleted);}
  public static void Initialize(){CommandSystem.Register("warden",AccessLevel.Player,e=>{var post=Find();if(post!=null)post.Show(e.Mobile);});EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(3),TimeSpan.FromSeconds(5),Ensure);};}
  public static void Ensure(){if(!HavenPreview.Enabled)return;var post=Find();if(post==null){Point3D landing;if(!HavenPreview.FindLanding(Arrival,out landing))return;post=new HavenWardenPost();post.MoveToWorld(landing,Arrival.Map);}post.Tick();}
  public void Tick(){if(Deleted||Warden!=null&&!Warden.Deleted||DateTime.UtcNow<NextSpawn)return;Point3D landing;var spot=new HavenPreview.Destination("Warden arena",Map,Location.X+5,Location.Y,Map.GetAverageZ(Location.X+5,Location.Y));if(!HavenPreview.FindLanding(spot,out landing))return;Warden=new HavenOldWarden(this);Warden.MoveToWorld(landing,Map);Warden.Home=landing;}
  public void Defeated(HavenOldWarden boss){if(Warden!=boss)return;Warden=null;NextSpawn=DateTime.UtcNow.AddSeconds(Utility.RandomMinMax(120,180));}
  public void Show(Mobile p){if(HavenMarks.CanUse(p))p.SendGump(new HavenWardenGump(this));}
  public override void OnDoubleClick(Mobile p){if(p.Map==Map&&p.InRange(this,3)&&p.InLOS(this))Show(p);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Warden);w.Write(NextSpawn);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Warden=r.ReadMobile() as HavenOldWarden;NextSpawn=r.ReadDateTime();}
 }
 public class HavenOldWarden:BaseCreature {
  HavenWardenPost _post;readonly HashSet<Mobile> _participants=new HashSet<Mobile>();bool _paid;
  public HavenOldWarden(HavenWardenPost post):base(AIType.AI_Melee,FightMode.Closest,12,1,0.2,0.4){_post=post;Name="the Old Haven warden";Body=0x3CA;Hue=0x455;BaseSoundID=0x107;RangeHome=6;SetStr(145,175);SetDex(70,90);SetInt(55,75);SetHits(425,525);SetDamage(7,13);SetDamageType(ResistanceType.Physical,70);SetDamageType(ResistanceType.Cold,30);SetResistance(ResistanceType.Physical,25,35);SetResistance(ResistanceType.Fire,15,25);SetResistance(ResistanceType.Cold,30,40);SetResistance(ResistanceType.Poison,15,25);SetResistance(ResistanceType.Energy,15,25);SetSkill(SkillName.MagicResist,55,70);SetSkill(SkillName.Tactics,65,75);SetSkill(SkillName.Wrestling,65,75);Fame=3500;Karma=-3500;VirtualArmor=28;Tamable=false;}
  public HavenOldWarden(Serial s):base(s){}
  public override bool AlwaysAttackable{get{return true;}}public override bool BleedImmune{get{return true;}}
  public override void OnDamage(int amount,Mobile from,bool willKill){if(amount>0&&from!=null){var owner=from.GetDamageMaster(this)??from;if(owner is PlayerMobile&&!owner.Deleted)_participants.Add(owner);}base.OnDamage(amount,from,willKill);}
  public static Item Gear(int luck){var item=Loot.RandomArmorOrShieldOrWeaponOrJewelry();BaseRunicTool.ApplyAttributesTo(item,false,luck,Utility.RandomMinMax(3,5),50,85);return item;}
  public override void GenerateLoot(){AddLoot(LootPack.Rich);if(m_Spawning)return;PackGold(1500,2500);if(Utility.RandomDouble()<0.20)PackItem(new HavenSetRing(Utility.Random(9)));PackItem(Gear(m_KillersLuck));PackItem(Gear(m_KillersLuck));if(Utility.RandomDouble()<0.15){var factories=new Func<Item>[]{()=>new BraceletOfTheVanguard(),()=>new BraceletOfArcaneFocus(),()=>new BraceletOfTheWind(),()=>new BraceletOfTheBeastmaster(),()=>new BraceletOfTheVirtuoso(),()=>new BraceletOfTheArtisan(),()=>new BraceletOfFortune(),()=>new BraceletOfTheGuardian(),()=>new BraceletOfTheNight()};PackItem(factories[Utility.Random(factories.Length)]());}}
  public override void OnThink(){base.OnThink();if(_post!=null&&!_post.Deleted&&!InRange(Home,18)){Combatant=null;MoveToWorld(Home,_post.Map);}}
  public override void OnDeath(Container corpse){base.OnDeath(corpse);if(!_paid){_paid=true;foreach(var p in _participants.Where(x=>x!=null&&!x.Deleted)){int marks=Utility.RandomMinMax(3,6);HavenMarks.Award(p,marks);p.SendMessage("The Warden fell: "+marks+" Haven Marks credited. Gold and equipment are on the corpse.");}}if(_post!=null&&!_post.Deleted)_post.Defeated(this);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_post);w.Write(_paid);w.Write(_participants.Count);foreach(var p in _participants)w.Write(p);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_post=r.ReadItem() as HavenWardenPost;_paid=r.ReadBool();int n=r.ReadInt();for(int i=0;i<n;i++){var p=r.ReadMobile();if(p!=null)_participants.Add(p);}}
 }
 public class HavenWardenGump:HavenMenuGump {
  readonly HavenWardenPost _post;
  public HavenWardenGump(HavenWardenPost post):base(50,50){_post=post;AddBackground(0,0,480,260,0xA28);AddLabel(24,20,0,"Old Haven Warden");AddHtml(24,62,430,118,"<BASEFONT COLOR=#342B23>Repeatable starter boss. Returns 2-3 minutes after defeat.<BR>Each damage participant earns 3-6 Marks; companion and pet damage counts for you.<BR>Corpse loot: 1,500-2,500 gold, two enhanced equipment pieces, plus 15% bracelet and 20% matching-ring chances.</BASEFONT>",false,false);AddLabel(24,181,0,post.Warden!=null&&!post.Warden.Deleted?"The Warden is present.":"Respawn in "+Math.Max(0,(int)(post.NextSpawn-DateTime.UtcNow).TotalSeconds)+" seconds");AddButton(24,216,0xFA5,0xFA7,1,GumpButtonType.Reply,0);AddLabel(58,216,0,"Travel to marker");AddButton(365,216,0xFA5,0xFA7,0,GumpButtonType.Reply,0);AddLabel(399,216,0,"Close");}
  public override void OnResponse(NetState state,RelayInfo info){if(info.ButtonID!=1||_post.Deleted)return;var p=state.Mobile;Point3D landing;if(!HavenPreview.CanTravel(p)||!HavenPreview.FindLanding(new HavenPreview.Destination("Warden",_post.Map,_post.X,_post.Y,_post.Z),out landing)){p.SendMessage("Travel unavailable; leave combat and clear criminal status.");return;}BaseCreature.TeleportPets(p,landing,_post.Map);p.MoveToWorld(landing,_post.Map);}
 }
}
