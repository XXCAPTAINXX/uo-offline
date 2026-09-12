using System;
using System.Linq;
using System.IO;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class RestoredItemsSmoke {
 public static void Run(Mobile p,HavenCompanion c,Action<bool,string> check){
  p.Combatant=null;p.Aggressors.Clear();p.Aggressed.Clear();p.Criminal=false;c.Combatant=null;
  p.MoveToWorld(new Point3D(3500,2580,14),Map.Trammel);c.MoveToWorld(p.Location,p.Map);
  check(HavenMenuText.Encode("Haven's <pet>")=="Haven's &lt;pet&gt;","menu apostrophes render while markup stays escaped");
  var bandage=new HavenEndlessBandage();p.Backpack.DropItem(bandage);p.Hits=Math.Max(1,p.HitsMax-10);Bandage.BandageTargetRequest(bandage,p,p);check(BandageContext.GetContext(p)!=null&&!bandage.Deleted&&bandage.Amount==1,"native healing starts without consuming endless bandage");BandageContext.GetContext(p)?.StopHeal();bandage.Delete();p.Hits=p.HitsMax;
  var pouch=new HavenRunePouch();p.Backpack.DropItem(pouch);var blank=new RecallRune();p.Backpack.DropItem(blank);check(pouch.Store(p,blank)&&blank.Deleted&&pouch.Runes==1,"rune pouch stores ordinary blank");var named=new RecallRune{Name="keep me"};p.Backpack.DropItem(named);check(!pouch.Store(p,named)&&!named.Deleted,"rune pouch preserves personalized rune");named.Delete();
  check(!pouch.MarkRune(p,()=>false)&&pouch.Runes==1,"failed Mark preserves stored rune");
  p.Mana=p.ManaMax;var scroll=new MarkScroll();p.Backpack.DropItem(scroll);var spell=new Server.Spells.Sixth.MarkSpell(p,scroll);p.Spell=spell;spell.State=Server.Spells.SpellState.Sequencing;spell.OnCast();p.Target.Invoke(p,pouch);
  check(scroll.Deleted&&pouch.Runes==0&&p.Backpack.FindItemsByType(typeof(RecallRune),true).Cast<RecallRune>().Any(r=>r.Marked&&r.TargetMap==p.Map&&r.Target==p.Location),"real Mark spell targets pouch and produces marked rune");pouch.Delete();
  var map=new TreasureMap(1,Map.Trammel){Decoder=p,ChestLocation=new Point2D(3500,2580)};var shovel=new HavenGoldenShovel();p.Backpack.DropItem(map);p.Backpack.DropItem(shovel);check(shovel.Travel(p,map)&&!map.Completed&&!map.Deleted&&p.InRange(new Point3D(3500,2580,14),2)&&c.Map==p.Map&&c.Location==p.Location,"shovel lands beside decoded map with companion and preserves map");map.Completed=true;check(!shovel.Travel(p,map),"completed treasure map cannot trigger shovel travel");map.Delete();shovel.Delete();
  var steed=new HavenTideSteed();steed.SetControlMaster(p);steed.MoveToWorld(p.Location,p.Map);p.Skills.Fishing.Base=80;bool swim=p.CanSwim;double fishing=p.Skills.Fishing.Value;steed.Rider=p;File.AppendAllText("inventory-checks.log","Sea horse: rider="+(steed.Rider==p)+" swim="+p.CanSwim+" fishing="+fishing+" -> "+p.Skills.Fishing.Value+"\n");check(p.CanSwim&&Math.Abs(p.Skills.Fishing.Value-fishing-10)<0.001,"sea horse mounting grants swimming and ten fishing");var cargo=new Gold(25);steed.Backpack.DropItem(cargo);check(HavenTideCargoGump.Withdraw(steed,p,cargo)&&cargo.IsChildOf(p.Backpack),"mounted sea horse cargo can be withdrawn by owner");cargo.Delete();
  var visitor=new PlayerMobile{Player=true,Body=0x190};visitor.AddItem(new Backpack());visitor.MoveToWorld(p.Location,p.Map);var privateCargo=new Gold(20);steed.Backpack.DropItem(privateCargo);check(!HavenTideCargoGump.Withdraw(steed,visitor,privateCargo)&&privateCargo.Parent==steed.Backpack,"sea horse cargo rejects another player");visitor.Delete();
  steed.Rider=null;check(p.CanSwim==swim&&p.Skills.Fishing.Value==fishing,"sea horse dismount restores swimming and fishing");steed.Delete();
  var home=HavenStarterHome.Find(p)??HavenStarterHome.Claim(p);check(home!=null,"house exists for cartography access tests");var site=new Point3D(home.X+5,home.Y+5,home.Z+7);p.MoveToWorld(site,home.Map);
  var chest=new HavenMapStorageChest();chest.MoveToWorld(site,home.Map);var library=new HavenHouseMapLibrary();library.MoveToWorld(site,home.Map);
  check(chest.CanUse(p)&&HavenHouseMapLibrary.HouseAccess(p,library),"owner can access placed cartography storage and library");
  var nested=new Bag();p.Backpack.DropItem(nested);var chart=new TreasureMap(1,Map.Trammel);nested.DropItem(chart);var message=new SOS(Map.Trammel);nested.DropItem(message);var keep=new Gold(9);nested.DropItem(keep);
  check(chest.Collect(p,nested)==2&&chart.Parent==chest&&message.Parent==chest&&keep.Parent==nested,"map storage collects charts and SOS from nested bag without other items");
  check(chest.Withdraw(p,chart)&&chart.IsChildOf(p.Backpack),"map storage withdraws original chart");
  var stranger=new PlayerMobile{Player=true,Body=0x190};stranger.AddItem(new Backpack());stranger.MoveToWorld(site,home.Map);check(!chest.CanUse(stranger)&&!chest.Withdraw(stranger,message)&&!HavenHouseMapLibrary.HouseAccess(stranger,library),"cartography services reject stranger");stranger.Delete();
  check(HavenHouseMapLibraryGump.Catalog().Any(d=>d.Map==Map.Malas)&&HavenHouseMapLibraryGump.Catalog().Any(d=>d.Map==Map.TerMur),"library retains original expanded facet catalog");
  chest.Delete();library.Delete();chart.Delete();nested.Delete();p.MoveToWorld(new Point3D(3500,2580,14),Map.Trammel);c.MoveToWorld(p.Location,p.Map);
  for(int i=0;i<4;i++){var item=HavenWorldDiscoveries.UtilityItem(i);check(item!=null&&!item.Deleted,"original discovery utility factory "+i);item.Delete();}
  var enemy=new Horse();enemy.MoveToWorld(p.Location,p.Map);check(HavenWorldDiscoveries.Award(enemy,p,0.001),"original utility discovery interval awards restored item");check(!HavenWorldDiscoveries.Award(enemy,p,0.001),"utility discovery cannot duplicate same kill");enemy.Delete();
  c.Skills.AnimalTaming.Base=120;c.Skills.AnimalLore.Base=120;
  for(int i=0;i<10;i++){var t=new HavenPetTicket(p,0);p.Backpack.DropItem(t);check(HavenPetExchange.Exchange(p,t)&&t.Deleted,"unused mission ticket exchanges once");check(!HavenPetExchange.Exchange(p,t),"spent ticket cannot duplicate credits");}
  check(HavenPetExchange.Balance(p)==10,"pet credits accumulate separately from Marks");
  check(HavenPetExchange.Redeem(p,6,1)&&HavenPetExchange.Balance(p)==0,"credits redeem qualified rare pet reward");var reward=p.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Cast<HavenPetTicket>().First(t=>t.Kind==6);check(reward.Rarity>=1,"credit redemption guarantees minimum rarity");reward.Delete();
  var ordinary=new Horse();ordinary.SetControlMaster(p);ordinary.MoveToWorld(p.Location,p.Map);var stored=HavenPetTicket.Store(ordinary,p,p.Backpack);check(stored!=null&&!HavenPetExchange.Eligible(p,stored)&&!HavenPetExchange.Exchange(p,stored),"claimed stored pets excluded from exchange");stored.Delete();
  foreach(int group in Enumerable.Range(0,5))check(HavenSupplyShops.Catalogs[2].Any(e=>e.Group==group),"reward category has catalog entries "+group);
  var screen=new HavenSupplyShopGump(p,2,0,group:4);check(screen.Entries.OfType<Server.Gumps.GumpButton>().Any(b=>b.ButtonID==1),"grouped shop includes purchase action");screen.OnServerClose(null);
  for(int category=0;category<5;category++){var training=new HavenPetTrainingGump(c,category);check(training.Entries.Count>0,"training category renders "+category);}
 }
}
