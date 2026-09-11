using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class SatchelSmoke {
 static void Check(bool value,string name){if(!value)throw new Exception(name);File.AppendAllText("satchel-checks.log","PASS "+name+Environment.NewLine);}
 public static void Initialize(){if(File.Exists("SATCHEL-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(2),Run);}
 static void LedgerChecks(PlayerMobile p) {
 var account=(Account)p.Account;account.Young=true;p.Young=true;for(int i=0;i<p.Skills.Length;i++)p.Skills[i].Base=0;p.Skills.Swords.Base=100;p.Skills.Tactics.Base=100;p.Skills.Anatomy.Base=100;p.Skills.Mining.Base=100;p.Skills.Lumberjacking.Base=49.9;Check(!HavenYoungStatus.CheckProgress(p)&&p.Young,"Young protection retained below 450");p.Skills.Lumberjacking.Base=50;Check(HavenYoungStatus.CheckProgress(p)&&!p.Young&&!account.Young,"Young status clears at 450 including free skills and account flag");Check(HavenYoungStatus.IsRenunciation("I renounce my young player status!")&&!HavenYoungStatus.IsRenunciation("hello"),"plain speech fallback recognizes renunciation only");
 p.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);var c=World.Mobiles.Values.OfType<HavenCompanion>().First(x=>x.BoundOwner==p);c.MoveToWorld(p.Location,p.Map);Check(c.Skills.Magery.Base==105&&c.Skills.Magery.Cap>=120,"companion trained points and caps survive reload");
 var ledger=c.ResourceLedger; if(ledger==null){ledger=new HavenResourceLedger();c.Backpack.DropItem(ledger);}
 int id=Array.IndexOf(HavenResources.Types,typeof(BarbedHides));var raw=new BarbedHides(10);c.Backpack.DropItem(raw);int prior=ledger.Balance(id);Check(ledger.AbsorbCarriedResource(c,raw)&&raw.Deleted&&ledger.Balance(id)==prior+10,"raw companion skins become ledger balance");
 Check(HavenResources.Types.Length==HavenResources.Names.Length,"ledger labels match appended catalog");for(int resourceId=0;resourceId<HavenResources.Types.Length;resourceId++){var item=HavenResources.Create(resourceId,2);Check(item!=null&&item.Amount==2,"ledger withdrawal factory "+HavenResources.Names[resourceId]);item.Delete();}
 var personal=new HavenResourceLedger();p.Backpack.DropItem(personal);Check(ledger.TransferAll(p,personal)&&personal.Balance(id)>=10&&ledger.Balance(id)==0,"companion to player transfer conserves resources");
 if(p.Guild==null)new Server.Guilds.Guild(p,"Pouch test guild","PTG");var shared=HavenGuildResourceLedger.For(p);int before=shared.Balance(id);if(before>0)Check(before>=10,"shared guild balances survive reload");Check(personal.TransferAll(p,shared)&&shared.Balance(id)==before+10,"player to guild transfer conserves resources");Check(!shared.TransferAll(p,shared)&&shared.Balance(id)==before+10,"self transfer cannot erase balances");
 var outsider=new PlayerMobile();Check(!shared.CanUse(outsider),"nonmembers cannot access guild resources");outsider.Delete();personal.Delete();
 }
 static void Run(){try{
 var p=World.Mobiles.Values.OfType<PlayerMobile>().FirstOrDefault(x=>x.Name=="Raw pouch fixture");
 if(p!=null){var saved=(HavenResourceSatchel)p.Backpack.FindItemByType(typeof(HavenResourceSatchel));Check(saved!=null&&saved.Items.OfType<Log>().Single().Amount==50,"raw stacks survive reload");Check(saved.TotalWeight==(int)Math.Ceiling(saved.Items.Sum(x=>x.PileWeight)/10.0),"weight reduction survives reload");LedgerChecks(p);World.Save(false,false);File.AppendAllText("satchel-checks.log","COMPLETE reload\n");Core.Kill(false);return;}
 p=new PlayerMobile{Player=true,Name="Raw pouch fixture",Body=0x190,RawStr=100};p.AddItem(new Backpack());new Account("raw-pouch-fixture",Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);var pouch=new HavenResourceSatchel();p.Backpack.DropItem(pouch);
 var logs=new Log(100);p.Backpack.DropItem(logs);int before=p.TotalWeight;Check(pouch.Collect(p,logs)==1&&logs.Parent==pouch,"collect preserves actual raw stack");Check(pouch.TotalWeight==(int)Math.Ceiling(logs.PileWeight/10.0)&&p.TotalWeight<before,"ninety percent weight reduction reaches owner");logs.Amount=50;Check(pouch.TotalWeight==(int)Math.Ceiling(logs.PileWeight/10.0),"stack changes update reduced weight");int reduced=p.TotalWeight;p.Backpack.DropItem(logs);Check(p.TotalWeight>reduced,"withdrawing restores raw carried weight");pouch.Collect(p,logs);
 var bag=new Bag();var inner=new Bag();bag.DropItem(inner);var gem=new Diamond(2);var sword=new Longsword();inner.DropItem(gem);inner.DropItem(sword);p.Backpack.DropItem(bag);Check(pouch.Collect(p,bag)==1&&gem.Parent==pouch&&sword.Parent==inner,"nested bag collection leaves equipment in place");Check(!pouch.CheckHold(p,new Bag(),false)&&!HavenResourceSatchel.Accepts(sword),"containers and equipment are rejected");pouch.MaxItems=pouch.TotalItems;var feather=new Feather(1);p.Backpack.DropItem(feather);Check(pouch.Collect(p,feather)==0&&feather.Parent==p.Backpack,"full pouch leaves incoming resources untouched");pouch.MaxItems=500;
 var types=new[]{typeof(ValoriteOre),typeof(FrostwoodLog),typeof(OakBoard),typeof(BlueDiamond),typeof(Sand),typeof(Saltpeter),typeof(Granite),typeof(CrystallineBlackrock),typeof(BarkFragment),typeof(BrilliantAmber),typeof(Fish),typeof(BigFish),typeof(Crab),typeof(Lobster),typeof(WhitePearl),typeof(DelicateScales),typeof(BarbedHides),typeof(SpinedLeather),typeof(RedScales),typeof(Feather),typeof(Wool),typeof(RawRibs),typeof(DragonBlood),typeof(Fur),typeof(MessageInABottle),typeof(TreasureMap),typeof(FabledFishingNet)};Check(types.All(HavenResourceSatchel.AcceptsType),"mining lumber fishing skinning and rare materials covered");
 var allFish=typeof(BaseHighseasFish).Assembly.GetTypes().Where(t=>typeof(BaseHighseasFish).IsAssignableFrom(t)&&!t.IsAbstract);Check(allFish.All(HavenResourceSatchel.AcceptsType),"all native High Seas fish crab and lobster variants covered");
 var c=HavenCompanion.Claim(p);Check(c!=null,"companion fixture created");Check(c.Skills.Magery.Cap>=120 && c.Skills.Cap>=c.Skills.Length*1200,"companion 120 caps and total capacity");c.Skills.Magery.Base=105;c.Skills.Magery.Cap=110;c.EnsureProgressionCaps();Check(c.Skills.Magery.Base==105&&c.Skills.Magery.Cap==120,"cap migration preserves trained points");
 c.Backpack.DropItem(pouch);Check(pouch.CanUse(p),"owner can use companion pouch");var leather=new BarbedLeather(10);c.Backpack.DropItem(leather);Check(pouch.Collect(p,leather)==1&&leather.Parent==pouch,"companion leather collection");p.Backpack.DropItem(pouch);leather.Delete();
 World.Save(false,false);File.AppendAllText("satchel-checks.log","COMPLETE fresh\n");Core.Kill(false);
 }catch(Exception e){File.AppendAllText("satchel-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}
