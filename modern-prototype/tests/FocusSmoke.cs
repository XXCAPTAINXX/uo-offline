using System;
using System.IO;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Spellweaving;
using Server.HavenPrototype;
public static class FocusSmoke {
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText("focus-checks.log","PASS "+label+"\n");}
 public static void Initialize(){if(File.Exists("FOCUS-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(4),Run);}
 static void Run(){try{
  var saved=World.Mobiles.Values.OfType<HavenCompanion>().FirstOrDefault(x=>x.BoundOwner!=null&&x.BoundOwner.Name=="Focus fixture");
  if(saved!=null){Check(ArcanistSpell.GetFocusLevel(saved)==6&&saved.Backpack.FindItemsByType(typeof(HavenCompanionArcaneFocus),true).Length==1,"reload retains one effective strength6 focus");File.AppendAllText("focus-checks.log","RELOAD COMPLETE\n");Core.Kill(false);return;}
  var p=new PlayerMobile{Player=true,Name="Focus fixture",Body=0x190,RawStr=200};p.AddItem(new Backpack());new Account("focus-fixture",Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
  var c=HavenCompanion.Claim(p);Check(c.SetRole(p,CompanionRole.Caster),"caster role selected");var focus=ArcanistSpell.FindArcaneFocus(c);
  Check(focus is HavenCompanionArcaneFocus&&ArcanistSpell.GetFocusLevel(c)==6,"actual native focus lookup sees strength6 gem");Check(!focus.Movable&&focus.Nontransferable,"gem cannot be transferred");
  c.EnsureArcaneFocus();Check(ArcanistSpell.FindArcaneFocus(c)==focus&&c.Backpack.FindItemsByType(typeof(ArcaneFocus),true).Length==1,"maintenance does not duplicate gems");focus.CreationTime=DateTime.UtcNow.AddHours(-1);c.EnsureArcaneFocus();Check(focus.CreationTime>DateTime.UtcNow.AddMinutes(-1),"focus renews before expiry");
  Check(c.SetRole(p,CompanionRole.Warrior)&&focus.Deleted,"leaving caster removes supplied focus");Check(c.SetRole(p,CompanionRole.Caster)&&ArcanistSpell.GetFocusLevel(c)==6,"returning to caster restores focus");
  World.Save();File.AppendAllText("focus-checks.log","FRESH COMPLETE\n");Core.Kill(false);
 }catch(Exception e){File.AppendAllText("focus-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}
