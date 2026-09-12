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
  public HavenCoveEncounter(){Name="Blackwake cove expedition";Visible=true;ItemID=0x1F14;Hue=0x972;}
  public HavenCoveEncounter(Serial serial):base(serial){}
  public static HavenCoveEncounter BuildTest(){if(!File.Exists("ISLAND-TEST-ONLY"))throw new InvalidOperationException("Cove expedition requires isolated testing.");if(World.Items.Values.OfType<HavenCoveEncounter>().Any(x=>!x.Deleted))throw new InvalidOperationException("Cove expedition already exists.");var camp=new HavenCoveEncounter();camp.MoveToWorld(new Point3D(4214,2922,0),Map.Trammel);if(!camp.ValidEncounterSite()){camp.Delete();throw new InvalidOperationException("Cove battlefield obstructed");}return camp;}
  protected override bool IslandRewards {get{return true;}}
  protected override bool ValidEncounterSite(){int open=0;for(int x=X-7;x<=X+7;x++)for(int y=Y-7;y<=Y+7;y++){var p=new Point3D(x,y,Map.GetAverageZ(x,y));if(SafeSite(Map,p))open++;}return SafeSite(Map,Location)&&open>=100;}
  protected override bool SpawnPoint(out Point3D point){for(int i=0;i<120;i++){int x=X+Utility.RandomMinMax(-7,7),y=Y+Utility.RandomMinMax(-7,7);point=new Point3D(x,y,Map.GetAverageZ(x,y));if(SafeSite(Map,point)&&Map.CanSpawnMobile(point))return true;}point=Point3D.Zero;return false;}
  protected override HavenMiniEnemy CreateEnemy(int theme,int stage){return new HavenCoveEnemy(this,stage);}
  public override void OnDoubleClick(Mobile from){Show(from);}
  public override void Show(Mobile from){if(HavenMarks.CanUse(from)){from.CloseGump(typeof(HavenCoveGump));from.SendGump(new HavenCoveGump(this,from));}}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenCoveEnemy:HavenMiniEnemy {
  private HavenCoveEncounter _cove;
  public HavenCoveEnemy(HavenCoveEncounter camp,int stage):base(camp,1,stage){_cove=camp;Body=0x190;BaseSoundID=0x45A;Name=stage==3?"Admiral Blackwake":stage==0?"a Blackwake deckhand":stage==1?"a Blackwake boarding guard":"a Blackwake quartermaster";Hue=0;AddItem(new FancyShirt{Hue=0x455});AddItem(new ShortPants{Hue=0x972});AddItem(new Boots());AddItem(new Bandana{Hue=0x21});AddItem(new Scimitar());SetSkill(SkillName.Swords,stage==3?115:80+stage*10);if(stage==3){SetHits(3600);SetStr(400);SetDamage(18,26);SetResistance(ResistanceType.Physical,55);}}
  public HavenCoveEnemy(Serial serial):base(serial){}
  public override int Damage(int amount,Mobile from,bool informMount,bool checkDisrupt){if(from!=null&&(_cove==null||_cove.Deleted||from.Map!=_cove.Map||!from.InRange(_cove,12)||BaseHouse.FindHouseAt(from.Location,from.Map,20)!=null))return 0;return base.Damage(amount,from,informMount,checkDisrupt);}
  public override bool CanBeHarmful(IDamageable target,bool message,bool ignoreOurBlessedness){var mob=target as Mobile;if(_cove==null||_cove.Deleted||mob==null||mob.Map!=_cove.Map||!mob.InRange(_cove,12)||BaseHouse.FindHouseAt(mob.Location,mob.Map,20)!=null)return false;return base.CanBeHarmful(target,message,ignoreOurBlessedness);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_cove);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_cove=r.ReadItem() as HavenCoveEncounter;}
 }
 public class HavenCoveGump:HavenMenuGump {
  readonly HavenCoveEncounter _camp;
  public HavenCoveGump(HavenCoveEncounter camp,Mobile p):base(60,60){_camp=camp;AddBackground(0,0,610,335,3000);AddLabel(24,22,0,"BLACKWAKE COVE | Pirate expedition");AddHtml(24,64,560,85,"Defeat three waves of five pirates, then Admiral Blackwake. Damage from your pets and companion counts toward your participation. Leave the battlefield to abandon the encounter.",false,false);AddLabel(24,151,0,camp.Active?"Wave "+(camp.Stage+1)+" | Enemies: "+camp.Remaining:"Ready in "+Math.Max(0,(int)Math.Ceiling((camp.Cooldown-DateTime.UtcNow).TotalSeconds))+" seconds");AddHtml(24,183,560,60,"Each participant: 10,000 gold, 20 Marks, resources and scrolls, plus cannonballs, powder charges and fuse cord. Boss maps and weapon/shield sets are chance drops.",false,false);FlatButton(24,263,180,1,"Start expedition");FlatButton(215,263,225,2,"Collect pending rewards");FlatButton(450,263,130,0,"Close");}
  public override void OnResponse(NetState s,RelayInfo i){if(_camp.Deleted||!HavenMarks.CanUse(s.Mobile)||i.ButtonID==0)return;if(i.ButtonID==1)_camp.Begin(s.Mobile,1);if(i.ButtonID==2)HavenMiniPrize.Collect(s.Mobile);_camp.Show(s.Mobile);}
 }
}
