using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Accounting;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Spellweaving;
using Server.HavenPrototype;
public static class RoleCombatRestoreSmoke {
 static PlayerMobile owner;static HavenCompanion c;static int failures;
 static void Check(bool ok,string name){File.AppendAllText("role-combat-restore-checks.log",(ok?"PASS ":"FAIL ")+name+"\n");if(!ok)failures++;}
 public static void Initialize(){if(File.Exists("ROLE-COMBAT-RESTORE-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(8),Run);}
 static void Run(){try{owner=new PlayerMobile{Player=true,Body=0x190,RawStr=250};owner.AddItem(new Backpack());new Account("combat-restore-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;HavenMiniChamp.Ensure();var camp=HavenMiniChamp.Find();if(camp==null)throw new Exception("Missing verified combat clearing");owner.MoveToWorld(camp.Location,camp.Map);c=HavenCompanion.Claim(owner);var stranger=new PlayerMobile();RoleSmoke.Run(c,owner,stranger,(name,action)=>{try{action();Check(true,name);}catch(Exception e){Check(false,name+": "+e.Message);}},StartCaster);}catch(Exception e){End(e);}}
 static void StartCaster(){try{owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);c.MoveToWorld(owner.Location,owner.Map);Check(c.SetRole(owner,CompanionRole.Caster),"restored caster selected");c.Skills.Spellweaving.Base=125;c.Skills.Necromancy.Base=125;c.Hits=c.HitsMax;c.Mana=c.ManaMax;owner.Hits=owner.HitsMax;Check(c.MaintainSpellweaver(),"Wraith Form cast begins");Timer.DelayCall(TimeSpan.FromSeconds(6),Life);}catch(Exception e){End(e);}}
 static void Life(){try{Check(TransformationSpellHelper.UnderTransformation(c,typeof(WraithFormSpell)),"native Wraith Form active");c.Mana=c.ManaMax;Check(c.MaintainSpellweaver(),"owner Gift of Life cast begins");Timer.DelayCall(TimeSpan.FromSeconds(6),Renewal);}catch(Exception e){End(e);}}
 static void Renewal(){try{var table=(IDictionary)typeof(GiftOfLifeSpell).GetField("m_Table",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);Check(table.Contains(owner),"native Gift of Life protects bound owner");var stranger=new PlayerMobile{Player=true,Body=0x190};stranger.MoveToWorld(owner.Location,owner.Map);new GiftOfLifeSpell(c,null).Target(stranger);Check(!table.Contains(stranger),"owner exception does not allow unrelated players");stranger.Delete();owner.Hits=30;c.Mana=c.ManaMax;Check(c.MaintainSpellweaver(),"owner Gift of Renewal cast begins");Timer.DelayCall(TimeSpan.FromSeconds(6),Finish);}catch(Exception e){End(e);}}
 static void Finish(){try{Check(GiftOfRenewalSpell.IsUnderEffects(owner),"native renewal buff reaches owner");var ai=c.AIObject as HavenCompanionMageAI;var foe=new Ogre{CantWalk=true};var harmless=new Sheep{CantWalk=true};foe.MoveToWorld(owner.Location,owner.Map);harmless.MoveToWorld(owner.Location,owner.Map);c.Mana=c.ManaMax;foe.Hits=foe.HitsMax;var spell=ai.ChooseSpell(foe);Check(spell is ThunderstormSpell,"caster selects restored Thunderstorm");var targets=spell.AcquireIndirectTargets(c.Location,9).ToArray();Check(targets.Contains(foe)&&!targets.Contains(harmless),"Thunderstorm excludes harmless tameables");foe.Hits=1;Check(ai.ChooseSpell(foe) is WordOfDeathSpell,"Spellweaving finisher remains available");foe.Delete();harmless.Delete();Check(c.SetRole(owner,CompanionRole.Warrior),"caster can change back to warrior");c.MaintainSpellweaver();Check(!TransformationSpellHelper.UnderTransformation(c,typeof(WraithFormSpell)),"role change removes mana form");End(null);}catch(Exception e){End(e);}}
 static void End(Exception e){if(e!=null)Check(false,e.ToString());File.AppendAllText("role-combat-restore-checks.log","COMPLETE failures="+failures+"\n");Core.Kill(false);}
}
