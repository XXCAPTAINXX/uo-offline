using System;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public interface IHavenStarterGear {int Kind{get;} HavenStarterProgress Progress{get;}}
 public class HavenStarterProgress {
  public Mobile Owner;public int Experience;public int Tier;
  public HavenStarterProgress(){}
  public HavenStarterProgress(GenericReader r){Owner=r.ReadMobile();Experience=r.ReadInt();Tier=r.ReadInt();}
  public void Write(GenericWriter w){w.Write(Owner);w.Write(Experience);w.Write(Tier);}
 }
 public static class HavenStarterGear {
  public static void Initialize(){CommandSystem.Register("startergear",AccessLevel.Player,e=>{if(HavenPreview.Enabled)e.Mobile.SendGump(new HavenStarterGearGump(e.Mobile));});EventSink.CreatureDeath+=e=>{var c=e.Creature as BaseCreature;var p=e.Killer==null?null:e.Killer.GetDamageMaster(e.Creature)??e.Killer;if(!Eligible(c,p))return;int xp=Math.Max(1,Math.Min(20,c.HitsMax/100));foreach(var item in p.Items.ToArray()){var gear=item as IHavenStarterGear;if(gear!=null)Gain(gear,p,xp);}};EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(3),Ensure);};}
  static void EnsureFlowers(Item stone){if(World.Items.Values.OfType<HavenPlazaPlanter>().Any(x=>!x.Deleted&&x.Stone==stone))return;foreach(var offset in new[]{new Point2D(-1,0),new Point2D(0,-1),new Point2D(-1,-1)}){var point=new Point3D(stone.X+offset.X,stone.Y+offset.Y,stone.Z);if(stone.Map.CanFit(point,16,false,true)){new HavenPlazaPlanter(stone).MoveToWorld(point,stone.Map);return;}}}
  public static void Ensure(){var existing=World.Items.Values.OfType<HavenStarterGearStone>().FirstOrDefault(x=>!x.Deleted);if(existing!=null){existing.ItemID=0xED4;existing.Hue=0;EnsureFlowers(existing);return;}Point3D p;if(HavenPreview.FindLanding(new HavenPreview.Destination("Evolving starter gear",Map.Trammel,3500,2572,14),out p)){var stone=new HavenStarterGearStone();stone.MoveToWorld(p,Map.Trammel);EnsureFlowers(stone);}}
  public static bool Eligible(BaseCreature c,Mobile p){return HavenPreview.Enabled && c!=null && p is PlayerMobile && p.Alive && p.Map==c.Map && p.InRange(c,18) && !c.Controlled && !c.Summoned && !c.IsBonded && !c.NoKillAwards && !(c is BaseVendor) && c.Owners.Count==0 && c.HitsMax>=100 && c.Karma<0;}
  public static int Level(IHavenStarterGear g){if(g.Kind==4)return 1;if(g.Kind==6)return Math.Min(20,1+(int)Math.Sqrt(Math.Max(0,g.Progress.Experience)/25.0));if(g.Kind>=7)return Math.Min(20,1+g.Progress.Experience/100);int xp=g.Progress.Experience,l=1;while(l<20 && xp>=20+l*10){xp-=20+l*10;l++;}return l;}
  public static int Limit(IHavenStarterGear g){return g.Kind==6?9025:g.Kind>=7?1900:2280;}
  public static void Gain(IHavenStarterGear g,Mobile owner,int amount){var item=(Item)g;if(!HavenPreview.Enabled || g.Kind==4 || amount<=0 || item.Deleted || g.Progress.Owner!=owner || item.Parent!=owner)return;int before=Level(g);g.Progress.Experience=Math.Min(Limit(g),g.Progress.Experience+Math.Min(amount,Limit(g)));Apply(g);item.InvalidateProperties();if(Level(g)>before)owner.SendMessage(item.Name+" reached level "+Level(g)+".");}
  public static void Cast(Mobile p){var book=p.FindItemOnLayer(Layer.OneHanded) as HavenApprenticeGrimoire;if(book!=null)Gain(book,p,1);}
  public static void Hit(IHavenStarterGear g,Mobile attacker,Mobile target){if(Eligible(target as BaseCreature,attacker))Gain(g,attacker,1);}
  public static void Apply(IHavenStarterGear g){int l=Level(g),t=g.Progress.Tier;var item=(Item)g;var w=item as BaseWeapon;var clothing=item as BaseClothing;var jewel=item as BaseJewel;var book=item as Spellbook;var a=w!=null?w.Attributes:clothing!=null?clothing.Attributes:jewel!=null?jewel.Attributes:book.Attributes;
   if(w!=null){w.WeaponAttributes.HitLeechMana=Math.Max(w.WeaponAttributes.HitLeechMana,Math.Max(30,20+(l-1)*80/19));a.WeaponDamage=l;a.AttackChance=l/2;a.BonusStam=Math.Min(8,l/3);a.RegenStam=l>=10?1:0;a.RegenHits=l>=15?1:0;}
   else if(g.Kind==4){a.Luck=50+t*25;a.RegenHits=a.RegenMana=1+t/2;a.LowerManaCost=3+t;a.BonusHits=t>=3?3:0;a.BonusMana=t>=2?4:0;}
   else if(g.Kind==5){a.Luck=25+Math.Min(l,10)*10;a.BonusMana=a.LowerManaCost=Math.Min(10,l/2);a.RegenMana=1+l/3;a.CastRecovery=l>=10?1:0;a.CastSpeed=l>=20?1:0;}
   else if(g.Kind==6){a.Luck=l*25;a.RegenHits=a.RegenStam=a.RegenMana=1+l/5;a.LowerManaCost=a.DefendChance=l/2;a.BonusStr=a.BonusDex=a.BonusInt=1+l/4;}
   else if(g.Kind==7){a.BonusStr=a.BonusDex=a.BonusInt=1+l/5;a.RegenHits=a.RegenStam=a.RegenMana=1;a.Luck=(l-1)*5;}
   else {a.LowerRegCost=100;a.Luck=200+(l-1)*5;a.BonusStr=a.BonusDex=a.BonusInt=l/5;}
  }
  public static void Properties(IHavenStarterGear g,ObjectPropertyList list){list.Add(g.Kind==4?"Robe upgrade tier: "+g.Progress.Tier+"/4":"Equipment level: "+Level(g)+"/20 | XP: "+g.Progress.Experience+"/"+Limit(g));if(g.Progress.Owner!=null)list.Add("Bound to: "+g.Progress.Owner.Name);}
  public static bool CanUse(Mobile p){return HavenMarks.CanUse(p) && World.Items.Values.OfType<HavenStarterGearStone>().Any(s=>!s.Deleted && s.Map==p.Map && p.InRange(s,3) && p.InLOS(s));}
  public static bool Claim(Mobile p){if(!CanUse(p) || p.Backpack==null)return false;var account=(Account)p.Account;string key="Haven.EvolvingStarter:"+p.Serial.Value;if(account.GetTag(key)!=null)return false;var items=new Item[]{new HavenApprenticeBlade(),new HavenApprenticeFencer(),new HavenApprenticeMace(),new HavenApprenticeBow(),new HavenAdventurersRobe(),new HavenApprenticeGrimoire(),new HavenEvolvingCape(),new HavenEvolvingSash(),new HavenFortuneEarrings()};int count=0,weight=0;foreach(var item in items){if(!p.Backpack.CheckHold(p,item,false,true,count,weight)){foreach(var unused in items)unused.Delete();return false;}count+=item.TotalItems+1;weight+=item.TotalWeight+item.PileWeight;}foreach(var item in items){((IHavenStarterGear)item).Progress.Owner=p;p.Backpack.DropItem(item);}account.SetTag(key,"claimed");return true;}
  public static HavenAdventurersRobe Robe(Mobile p){return p.Items.OfType<HavenAdventurersRobe>().FirstOrDefault()??(p.Backpack==null?null:p.Backpack.FindItemByType(typeof(HavenAdventurersRobe),true) as HavenAdventurersRobe);}
  public static bool Upgrade(Mobile p,int tier){if(!CanUse(p))return false;var robe=Robe(p);if(robe==null || robe.Progress.Owner!=p || robe.Progress.Tier!=tier || tier<0 || tier>=4)return false;int cost=tier+1;if(!HavenMarks.Spend(p,cost) && !HavenWallet.PayGold(p,cost*5000))return false;robe.Progress.Tier++;Apply(robe);robe.InvalidateProperties();return true;}
 }
 public class HavenStarterGearStone:Item {
  public HavenStarterGearStone():base(0xED4){Name="Evolving starter gear and upgrades";Hue=0;Movable=false;}
  public HavenStarterGearStone(Serial s):base(s){}
  public override void OnDoubleClick(Mobile from){if(HavenStarterGear.CanUse(from))from.SendGump(new HavenStarterGearGump(from));}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenStarterGearGump:HavenMenuGump {
  private readonly int _tier;
  public HavenStarterGearGump(Mobile p):base(40,40){var robe=HavenStarterGear.Robe(p);_tier=robe==null?-1:robe.Progress.Tier;AddBackground(0,0,570,420,0x13BE);AddLabel(20,18,1152,"Haven evolving starter equipment");AddHtml(20,55,525,240,"<BASEFONT COLOR=#FFFFFF>One free set per character, including existing preview characters.<BR><BR>Four weapons: sword, fencing, mace and bow. Start with 30% mana leech; reach 100% at level 20.<BR>Grimoire: full Magery book with growing mana bonuses.<BR>Cape: growing Luck, regeneration and defenses.<BR>Sash: stats and regeneration.<BR>Fortune Earrings: 100% lower reagent cost and 200 starting Luck.<BR>Robe: four paid upgrade tiers.<BR><BR>Wear equipment to earn XP from credited hostile monster kills. Weapons also gain XP on hits; the grimoire gains XP from successful spell sequences. Bound equipment cannot be worn by another character.</BASEFONT>",false,true);Button(20,315,1,"Claim free evolving set");Button(300,315,3,"Wallet / tithing");AddLabel(20,350,1152,_tier<0?"Carry your starter robe to upgrade it.":_tier>=4?"Robe fully upgraded.":"Next robe tier: "+(_tier+1)+" Marks or "+((_tier+1)*5000)+" wallet/bank gold");if(_tier>=0&&_tier<4)Button(20,382,2,"Upgrade robe");Button(430,382,0,"Close");}
  private void Button(int x,int y,int id,string label){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);AddLabel(x+34,y,1152,label);}
  public override void OnResponse(NetState state,RelayInfo info){if(info.ButtonID==0)return;var p=state.Mobile;if(!HavenStarterGear.CanUse(p)){p.SendMessage("Stand beside the evolving gear stone in Haven.");return;}if(info.ButtonID==3){HavenWallet.Open(p);return;}if(info.ButtonID==1)p.SendMessage(HavenStarterGear.Claim(p)?"Your evolving equipment is in your pack.":"Already claimed, or your pack needs more room.");else if(info.ButtonID==2)p.SendMessage(HavenStarterGear.Upgrade(p,_tier)?"Your robe has been upgraded.":"Upgrade unavailable: check robe, funds and current tier.");p.SendGump(new HavenStarterGearGump(p));}
 }
}

