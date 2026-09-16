using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class BeaconQuestSmoke
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,Body=0x190,RawStr=100};p.AddItem(new Backpack());var a=new Account("beacon-test-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));a[0]=p;
  HavenCompanion c=null;HavenStoryHorse horse=null;HavenExpeditionFountain fountain=null;Horse other=null;
  try
  {
   HavenArrivalStory.PlaceNewCharacter(p);var b=HavenArrivalStory.EnsureBeacon();p.MoveToWorld(b.Location,b.Map);c=HavenCompanion.Claim(p);c.MoveToWorld(p.Location,p.Map);
   Check(!HavenBeaconQuest.Accept(p),"Skipped arrival meeting");Check(HavenArrivalStory.Meet(p)!=null,"Meet failed");
   Check(HavenBeaconQuest.Accept(p)&&!HavenBeaconQuest.Accept(p),"Accept/repeat");
   Check(HavenStoryGifts.Claimed(p,"cape")&&!HavenStoryGifts.ClaimCape(p),"Automatic cape missing or duplicated");
   p.FollowersMax=10;Check(HavenStoryGifts.Claimed(p,"mount")&&!HavenStoryGifts.ClaimMount(p),"Automatic mount missing or duplicated");
   var apple=p.Backpack.FindItemByType(typeof(HavenStoryBondingApple),true) as HavenStoryBondingApple;Check(apple!=null,"Missing apple");horse=apple.Horse;
   Check(!horse.IsBonded&&horse.ControlMaster==p,"Starter horse state");
   var def=PetTrainingHelper.GetTrainingDefinition(horse);Check(def!=null&&def.Class!=Class.Untrainable&&def.ControlSlotsMax==5&&horse.ControlSlotsMax==5,"Horse training limits");foreach(var ability in PetTrainingHelper.MagicalAbilities)Check((def.MagicalAbilities&ability)==ability,"Horse missing magic "+ability);foreach(var definition in PetTrainingHelper.Definitions){foreach(var ability in definition.SpecialAbilities??new SpecialAbility[0])Check(def.SpecialAbilities.Contains(ability),"Horse special restriction");foreach(var ability in definition.WeaponAbilities??new WeaponAbility[0])Check(def.WeaponAbilities.Contains(ability),"Horse weapon restriction");foreach(var ability in definition.AreaEffects??new AreaEffect[0])Check(def.AreaEffects.Contains(ability),"Horse area restriction");}log("PASS starter horse full native training catalog and 1-to-5 follower-slot progression");horse.MoveToWorld(p.Location,p.Map);
   other=new Horse();other.MoveToWorld(p.Location,p.Map);Check(!other.OnDragDrop(p,apple)&&!apple.Deleted,"Other horse consumed quest food");
   Check(horse.OnDragDrop(p,apple)&&horse.IsBonded&&apple.Deleted,"Instant bonding failed");
   log("PASS one-time starter cape and controlled low-level horse; apple rejected by other horse; exact owner/horse bonds immediately");
   var ledger=HavenBeaconQuest.Node(0);var stone=HavenBeaconQuest.Node(1);Check(ledger!=null&&stone!=null,"Clues missing");var stoneSerial=stone.Serial;stone.PlacementRevision=0;stone.MoveToWorld(new Point3D(3461,2595,stone.Z),Map.Trammel);Check(HavenBeaconQuest.EnsureNodes()&&HavenBeaconQuest.Node(1).Serial==stoneSerial&&stone.PlacementRevision==1&&stone.InRange(new Point3D(3473,2603,stone.Z),3),"Waystone migration");var stonePlace=stone.Location;HavenBeaconQuest.EnsureNodes();Check(stone.Location==stonePlace,"Repeated waystone move");for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)foreach(var tile in stone.Map.Tiles.GetStaticTiles(stone.X+dx,stone.Y+dy,true))Check((TileData.ItemTable[tile.ID].Flags&(TileFlag.Wall|TileFlag.Roof))==0,"Wall/roof beside new waystone");log("PASS existing waystone relocated once, same serial, no walls/roofs within2 tiles: "+stone.Location);
   Check(!HavenBeaconQuest.Inspect(p,stone),"Out of order clue");c.MoveToWorld(new Point3D(1400,1700,0),Map.Felucca);Check(!HavenBeaconQuest.Inspect(p,ledger),"Missing Jenna accepted clue");p.MoveToWorld(ledger.Location,ledger.Map);c.MoveToWorld(p.Location,p.Map);Check(HavenBeaconQuest.Inspect(p,ledger),"Ledger");
   var golem=HavenBeaconQuest.Target(p) as HavenTrainingGolem;Check(golem!=null,"Station golem missing");p.MoveToWorld(golem.Location,golem.Map);c.MoveToWorld(p.Location,p.Map);horse.MoveToWorld(p.Location,p.Map);
   int damage=HavenBeaconQuest.Damage(p);golem.Damage(10,p);Check(HavenBeaconQuest.Damage(p)==damage+10,"Native damage not counted");
   golem.Damage(20,horse);Check(HavenBeaconQuest.Damage(p)==30,"Pet damage not counted");golem.Damage(20,c);Check(HavenBeaconQuest.Phase(p)==3,"Companion damage not counted");
   Check(golem.CanBeHarmful(c,false,false),"Golem not retaliating");Check(!golem.CanBeHarmful(other,false,false),"Golem attacks bystander");
   c.Hits=c.HitsMax;int before=c.Hits;AOS.Damage(c,golem,1000,100,0,0,0,0);Check(before-c.Hits>=0&&before-c.Hits<=1,"Damage exceeded1");
   c.Hits=1;AOS.Damage(c,golem,1000,100,0,0,0,0);Check(c.Alive&&c.Hits==1,"Lethal training damage");c.Hits=c.HitsMax;
   horse.Skills[SkillName.Wrestling].Base=60;horse.Skills[SkillName.Parry].Base=20;
   Check(HavenTrainingGolem.PracticePet(horse),"Pet parry eligibility");for(int i=0;i<100;i++)((BaseWeapon)golem.Weapon).AbsorbDamageAOS(golem,horse,1);
   log("PASS native damage credits player/pet/Jenna; retaliation excludes bystander; native1000 damage clamps to1 and never kills; low-Wrestling pet parry path runs");
   p.MoveToWorld(stone.Location,stone.Map);c.MoveToWorld(p.Location,p.Map);Check(HavenBeaconQuest.Inspect(p,stone),"Waystone");p.MoveToWorld(b.Location,b.Map);c.MoveToWorld(p.Location,p.Map);
   Check(!HavenBeaconQuest.ChooseGlyph(p,2)&&HavenBeaconQuest.Glyph(p)==0,"Wrong symbol");Check(HavenBeaconQuest.ChooseGlyph(p,0)&&HavenBeaconQuest.ChooseGlyph(p,1),"Symbols");
   int marks=HavenMarks.Balance(p);HavenMarks.Award(p,HavenMarks.Maximum-marks);Check(!HavenBeaconQuest.ChooseGlyph(p,2)&&HavenBeaconQuest.Phase(p)==4,"Mark cap accepted payout");HavenMarks.Spend(p,HavenMarks.Maximum-marks);
   Check(HavenBeaconQuest.ChooseGlyph(p,2)&&HavenBeaconQuest.Phase(p)==5&&HavenMarks.Balance(p)==marks+20,"Reward");Check(!HavenBeaconQuest.ChooseGlyph(p,2)&&HavenMarks.Balance(p)==marks+20,"Duplicate reward");
   Check(HavenStoryGifts.ClaimFountain(p)&&!HavenStoryGifts.ClaimFountain(p),"Fountain reward duplication");
   fountain=new HavenExpeditionFountain();fountain.DropItem(new Bandage(60000));fountain.DropItem(new Bandage(234));fountain.DropItem(new EnhancedBandage(7));fountain.ConvertBandages();
   Check(fountain.Items.OfType<EnhancedBandage>().Sum(x=>x.Amount)==60241&&!fountain.Items.Any(x=>x.GetType()==typeof(Bandage)),"Fountain quantity");fountain.ConvertBandages();Check(fountain.Items.OfType<EnhancedBandage>().Sum(x=>x.Amount)==60241,"Repeated enhancement duplication");
   for(int phase=0;phase<=5;phase++){a.SetTag("Haven.BeaconQuest:"+p.Serial.Value+":phase",phase.ToString());Check(new HavenBeaconQuestGump(p).Entries.Count>0,"Journal phase");}
   HavenBeaconQuest.ToggleVoice(p);Check(!HavenBeaconQuest.VoiceEnabled(p),"Mute");HavenBeaconQuest.Speak(p,0);
   Check(!HavenRefugeQuest.Continue(p),"Refuge remote recovery stop accepted");
   HavenRecovery.Ensure();var steward=HavenRecovery.Steward();Check(steward!=null,"Recovery steward missing");
   p.MoveToWorld(steward.Location,steward.Map);c.MoveToWorld(p.Location,p.Map);
   Check(HavenRefugeQuest.Continue(p)&&HavenRefugeQuest.Stage(p)==1,"Recovery introduction");
   HavenStarterHub.Ensure();var shop=World.Items.Values.OfType<HavenServiceStone>().First(s=>!s.Deleted&&s.Service==1&&s.Map==Map.Trammel);
   p.MoveToWorld(shop.Location,shop.Map);c.MoveToWorld(p.Location,p.Map);Check(HavenRefugeQuest.Continue(p)&&HavenRefugeQuest.Stage(p)==2,"Shop visit without purchase");
   var camp=HavenRefugeQuest.Camp();Check(camp!=null,"Haven mini-champ missing");
   HavenRefugeQuest.CompletedExpedition(p,camp);Check(HavenRefugeQuest.Stage(p)==2,"Premature expedition credit");
   p.MoveToWorld(camp.Location,camp.Map);c.MoveToWorld(p.Location,p.Map);Check(HavenRefugeQuest.Continue(p)&&HavenRefugeQuest.Stage(p)==3,"Camp scouting");
   Check(!HavenRefugeQuest.Continue(p),"Clicked through victory requirement");
   var wrongCamp=HavenRefugeQuest.Camp(); var islandCamp=new HavenCoveEncounter(); try{HavenRefugeQuest.CompletedExpedition(p,islandCamp);Check(HavenRefugeQuest.Stage(p)==3,"Island encounter counted before island arrival");}finally{islandCamp.Delete();} int originalMarks=HavenMarks.Balance(p);HavenRefugeQuest.CompletedExpedition(p,camp);HavenRefugeQuest.CompletedExpedition(p,camp);
   Check(HavenRefugeQuest.Stage(p)==4&&HavenMarks.Balance(p)==originalMarks,"Chapter completion duplicates expedition currency");
   for(int step=0;step<5;step++){a.SetTag("Haven.RefugeQuest:"+p.Serial.Value,step.ToString());Check(new HavenRefugeQuestGump(p).Entries.Count>0,"Refuge page");}
   log("PASS chapter two physical visits, no purchase required, premature win rejected, victory required, duplicate completion safe, all pages");
   PersonalCampSmoke.Run(log);
   log("PASS clues and glyph order; one-time Marks and fountain deed;60234 bandages instantly enhanced preserving existing7; repeated conversion idempotent; all journal pages and mute");
  }finally{if(p.QuestArrow!=null)p.QuestArrow.Stop();if(fountain!=null)fountain.Delete();if(other!=null)other.Delete();if(horse!=null)horse.Delete();if(c!=null)c.Delete();p.Delete();}
 }
}
