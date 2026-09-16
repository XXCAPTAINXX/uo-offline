using System;
using System.IO;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.HavenPrototype;
public static class OvercapBenefitsSmoke {
 public static void Initialize(){if(File.Exists("OVERCAP-TEST-ONLY"))EventSink.ServerStarted+=Run;}
 static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Skill(Mobile m,SkillName name,double value){m.Skills[name].Cap=200;m.Skills[name].Base=value;m.Skills[name].SetLockNoRelay(SkillLock.Locked);}
 static void Run(){var bard=new Orc();var target=new Rabbit();var ordinary=new Orc();var companion=new HavenCompanion();var lute=new Lute();
 try {
  HavenPetRarity.Attach(bard,3);bard.Backpack.DropItem(lute);
  bard.MoveToWorld(new Point3D(4210,2928,Map.Trammel.GetAverageZ(4210,2928)),Map.Trammel);
  target.MoveToWorld(new Point3D(4211,2928,Map.Trammel.GetAverageZ(4211,2928)),Map.Trammel);
  Skill(bard,SkillName.Musicianship,120);Skill(bard,SkillName.Peacemaking,120);
  Peacemaking.OnPickedInstrument(bard,lute);bard.Target.Invoke(bard,target);
  Require(target.BardPacified,"Native 120 peace failed: LOS="+bard.InLOS(target)+" harmful="+bard.CanBeHarmful(target,false)+" uncalmable="+target.Uncalmable+" instrument="+lute.IsChildOf(bard.Backpack));double baseline=(target.BardEndTime-DateTime.UtcNow).TotalSeconds;
  target.BardPacified=false;Skill(bard,SkillName.Peacemaking,150);
  Peacemaking.OnPickedInstrument(bard,lute);bard.Target.Invoke(bard,target);
  double improved=(target.BardEndTime-DateTime.UtcNow).TotalSeconds;
  Require(target.BardPacified&&Math.Abs(improved-baseline*1.3)<0.25,"Native peace duration did not gain 30%");
  Skill(bard,SkillName.Discordance,150);Discordance.OnPickedInstrument(bard,lute);bard.Target.Invoke(bard,target);
  int effect=0;Require(Discordance.GetEffect(target,ref effect)&&effect==34,"Native discord did not apply 34%: "+effect);
  Skill(ordinary,SkillName.Peacemaking,150);Skill(ordinary,SkillName.Discordance,150);Skill(ordinary,SkillName.Musicianship,150);
  Require(HavenOvercapBenefits.PeaceSeconds(ordinary,100)==100&&HavenOvercapBenefits.DiscordEffect(ordinary,-28)==-28&&HavenOvercapBenefits.DiscordMusic(ordinary,120)==120,"Ordinary NPC changed");
  Skill(bard,SkillName.Musicianship,150);Require(HavenOvercapBenefits.DiscordMusic(bard,120)==150,"Overcap music discarded");
  Skill(companion,SkillName.Peacemaking,125);Require(HavenOvercapBenefits.PeaceSeconds(companion,100)==105,"Companion overcap missing");
  Skill(bard,SkillName.Peacemaking,120);Skill(bard,SkillName.Discordance,120);Require(HavenOvercapBenefits.PeaceSeconds(bard,100)==100&&HavenOvercapBenefits.DiscordEffect(bard,-28)==-28,"Under-cap behavior changed");
  Skill(bard,SkillName.Peacemaking,200);Skill(bard,SkillName.Discordance,200);Require(HavenOvercapBenefits.PeaceSeconds(bard,100)==130&&HavenOvercapBenefits.DiscordEffect(bard,-28)==-34,"Bonus limit failed");
  File.WriteAllText("overcap-benefits-result.txt","PASS native peace duration +30%; native Discordance 34%; actual music; companion +5%; ordinary NPC/120 unchanged; 150 bonus ceiling.");
 }catch(Exception e){File.WriteAllText("overcap-benefits-result.txt","FAIL "+e);}
 finally{bard.Delete();target.Delete();ordinary.Delete();companion.Delete();lute.Delete();Core.Kill(false);}
 }
}
