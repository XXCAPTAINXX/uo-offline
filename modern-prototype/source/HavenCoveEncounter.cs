using System;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenCoveEncounter:HavenMiniChamp {
  public static readonly string[] CoveThemes = { "Stormsail raiders", "Blackwake boarding party", "The drowned fleet", "Challenge: all three crews" };
  public HavenCoveEncounter(){Name="Blackwake cove expedition";Visible=true;ItemID=0x1F14;Hue=0x972;}
  public HavenCoveEncounter(Serial serial):base(serial){}
  public static HavenCoveEncounter BuildTest(){if(!HavenIslandInstall.CanBuild)throw new InvalidOperationException("Cove expedition requires isolated testing.");if(World.Items.Values.OfType<HavenCoveEncounter>().Any(x=>!x.Deleted))throw new InvalidOperationException("Cove expedition already exists.");var camp=new HavenCoveEncounter();camp.MoveToWorld(new Point3D(4214,2922,0),Map.Trammel);if(!camp.ValidEncounterSite()){camp.Delete();throw new InvalidOperationException("Cove battlefield obstructed");}return camp;}
  protected override bool IslandRewards {get{return true;}}
  protected override bool ValidEncounterSite(){int open=0;for(int x=X-7;x<=X+7;x++)for(int y=Y-7;y<=Y+7;y++){var p=new Point3D(x,y,Map.GetAverageZ(x,y));if(SafeSite(Map,p))open++;}return SafeSite(Map,Location)&&open>=100;}
  protected override bool SpawnPoint(out Point3D point){for(int i=0;i<120;i++){int x=X+Utility.RandomMinMax(-7,7),y=Y+Utility.RandomMinMax(-7,7);point=new Point3D(x,y,Map.GetAverageZ(x,y));if(SafeSite(Map,point)&&Map.CanSpawnMobile(point))return true;}point=Point3D.Zero;return false;}
  protected override HavenMiniEnemy CreateEnemy(int theme,int stage){return new HavenCoveEnemy(this,theme,stage);}
  public override void OnDoubleClick(Mobile from){Show(from);}
  public override void Show(Mobile from){if(HavenMarks.CanUse(from)){from.CloseGump(typeof(HavenCoveGump));from.SendGump(new HavenCoveGump(this,from));}}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenCoveEnemy:HavenMiniEnemy {
  private HavenCoveEncounter _cove;
  public override bool AlwaysMurderer { get { return true; } }
  public HavenCoveEnemy(HavenCoveEncounter camp,int stage):this(camp,1,stage){}
  public HavenCoveEnemy(HavenCoveEncounter camp,int theme,int stage):base(camp,theme,stage){
   _cove=camp;RangeHome=8;
   bool boss=stage==3;
   if(theme==2){
    Name=boss?"Captain of the Drowned Fleet":stage==0?"a drowned deckhand":stage==1?"a drowned boarding guard":"a drowned quartermaster";
    Body=boss?24:50;Hue=0x482;BaseSoundID=0x48D;
   }else{
    Body=0x190;BaseSoundID=0x45A;Hue=0;
    Name=theme==0?(boss?"Captain Stormsail":stage==0?"a Stormsail lookout":stage==1?"a Stormsail duelist":"a Stormsail first mate"):(boss?"Admiral Blackwake":stage==0?"a Blackwake deckhand":stage==1?"a Blackwake boarding guard":"a Blackwake quartermaster");
    AddItem(new FancyShirt{Hue=theme==0?0x53D:0x455});AddItem(new ShortPants{Hue=0x972});AddItem(new Boots());AddItem(new Bandana{Hue=theme==0?0x53D:0x21});
    if(theme==0){AddItem(new Kryss());SetDex(boss?150:100+stage*10);SetSkill(SkillName.Fencing,boss?115:80+stage*10);}
    else {AddItem(new Scimitar());SetSkill(SkillName.Swords,boss?115:80+stage*10);}
   }
   if(boss){SetHits(3600);SetStr(400);SetDamage(18,26);SetResistance(ResistanceType.Physical,55);}
  }
  public HavenCoveEnemy(Serial serial):base(serial){}
  protected override bool OnMove(Direction direction){
   if(_cove!=null&&!_cove.Deleted){
    int x=X,y=Y;Server.Movement.Movement.Offset(direction,ref x,ref y);
    if(Map!=_cove.Map||Math.Max(Math.Abs(x-_cove.X),Math.Abs(y-_cove.Y))>11)return false;
   }
   return base.OnMove(direction);
  }
  public override void OnThink(){
   if(_cove!=null&&!_cove.Deleted&&(Map!=_cove.Map||!InRange(_cove,11))){Combatant=null;MoveToWorld(_cove.Location,_cove.Map);}
   base.OnThink();
  }
  public override int Damage(int amount,Mobile from,bool informMount,bool checkDisrupt){if(from!=null&&(_cove==null||_cove.Deleted||from.Map!=_cove.Map||!from.InRange(_cove,12)||BaseHouse.FindHouseAt(from.Location,from.Map,20)!=null))return 0;return base.Damage(amount,from,informMount,checkDisrupt);}
  public override bool CanBeHarmful(IDamageable target,bool message,bool ignoreOurBlessedness){var mob=target as Mobile;if(_cove==null||_cove.Deleted||mob==null||mob.Map!=_cove.Map||!mob.InRange(_cove,12)||BaseHouse.FindHouseAt(mob.Location,mob.Map,20)!=null)return false;return base.CanBeHarmful(target,message,ignoreOurBlessedness);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_cove);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_cove=r.ReadItem() as HavenCoveEncounter;RangeHome=8;}
 }
 public class HavenCoveGump:HavenMenuGump {
  readonly HavenCoveEncounter _camp;
  public HavenCoveGump(HavenCoveEncounter camp,Mobile p):base(60,60){
   _camp=camp;AddBackground(0,0,650,490,3000);
   AddLabel(24,22,0,"BLACKWAKE COVE | Choose an expedition");
   AddHtml(24,58,600,50,"Normal: three waves of five enemies, then the crew's champion. Pet and companion damage counts toward your participation.",false,false);
   for(int n=0;n<4;n++)FlatButton(24,120+n*38,290,10+n,HavenCoveEncounter.CoveThemes[n]);
   AddHtml(330,120,290,135,"Stormsail: fast blade fighters.<BR>Blackwake: a boarding crew led by Admiral Blackwake.<BR>Drowned fleet: undead sailors and their spellcasting captain.",false,false);
   AddHtml(24,282,600,55,"Challenge: three waves of 15 mixed enemies, then all three champions together. Four reward sets, 80 Marks and an Astral Shard. Weapons and boss maps remain chance drops.",false,false);
   AddLabel(24,345,0,camp.Active?(camp.Challenge?"Challenge | ":"Expedition | ")+(camp.Stage==3?"Champions":"Wave "+(camp.Stage+1)+" / 3")+" | Enemies: "+camp.Remaining:camp.Cooldown<=DateTime.UtcNow?"Ready to begin":"Ready in "+(int)Math.Ceiling((camp.Cooldown-DateTime.UtcNow).TotalSeconds)+" seconds");
   AddHtml(24,374,600,42,"Normal completion: 10,000 gold, 20 Marks, resources, scrolls and ship supplies. Abandons after two minutes without a living player within 32 tiles.",false,false);
   FlatButton(24,433,140,3,"Travel to camp");FlatButton(174,433,195,2,"Collect pending rewards");FlatButton(379,433,130,4,"Refresh status");FlatButton(519,433,100,0,"Close");
  }
  public override void OnResponse(NetState s,RelayInfo i){
   if(_camp.Deleted||!HavenMarks.CanUse(s.Mobile)||i.ButtonID==0)return;
   if(i.ButtonID>=10&&i.ButtonID<=13)_camp.Begin(s.Mobile,i.ButtonID-10);
   if(i.ButtonID==2)HavenMiniPrize.Collect(s.Mobile);
   if(i.ButtonID==3)_camp.Travel(s.Mobile);
   _camp.Show(s.Mobile);
  }
 }
}


