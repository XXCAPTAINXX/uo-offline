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
   Check(HavenStoryGifts.ClaimCape(p)&&!HavenStoryGifts.ClaimCape(p),"Cape duplicated");
   p.FollowersMax=10;Check(HavenStoryGifts.ClaimMount(p)&&!HavenStoryGifts.ClaimMount(p),"Mount claim");
   var apple=p.Backpack.FindItemByType(typeof(HavenStoryBondingApple),true) as HavenStoryBondingApple;Check(apple!=null,"Missing apple");horse=apple.Horse;
   Check(!horse.IsBonded&&horse.ControlMaster==p,"Starter horse state");horse.MoveToWorld(p.Location,p.Map);
   other=new Horse();other.MoveToWorld(p.Location,p.Map);Check(!other.OnDragDrop(p,apple)&&!apple.Deleted,"Other horse consumed quest food");
   Check(horse.OnDragDrop(p,apple)&&horse.IsBonded&&apple.Deleted,"Instant bonding failed");
   log("PASS one-time starter cape and controlled low-level horse; apple rejected by other horse; exact owner/horse bonds immediately");
   var ledger=HavenBeaconQuest.Node(0);var stone=HavenBeaconQuest.Node(1);Check(ledger!=null&&stone!=null,"Clues missing");
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
   log("PASS clues and glyph order; one-time Marks and fountain deed;60234 bandages instantly enhanced preserving existing7; repeated conversion idempotent; all journal pages and mute");
  }finally{if(p.QuestArrow!=null)p.QuestArrow.Stop();if(fountain!=null)fountain.Delete();if(other!=null)other.Delete();if(horse!=null)horse.Delete();if(c!=null)c.Delete();p.Delete();}
 }
}
