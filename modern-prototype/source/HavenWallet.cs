using System;
using System.Linq;
using Server.Commands;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenWallet:Item {
  private long _legacyGold;
  public long Balance{get{return Owner==null?0:Banker.GetBalance(Owner);}} public Mobile Owner{get;private set;}
  public bool MergeLegacy(){if(_legacyGold<=0)return true;if(Owner==null||Owner.Deleted)return false;while(_legacyGold>0){int amount=(int)Math.Min(_legacyGold,1000000);if(!Banker.Deposit(Owner,amount))return false;_legacyGold-=amount;}InvalidateProperties();return true;}
  public HavenWallet(Mobile owner):base(0xEEF){Owner=owner;Name="adventurer's wallet";Hue=0x8A5;Weight=1;LootType=LootType.Blessed;}
  public HavenWallet(Serial serial):base(serial){}
  public static void Open(Mobile p){if(!HavenMarks.CanUse(p)||p.Backpack==null)return;var w=Find(p);if(w==null){w=new HavenWallet(p);if(!p.Backpack.TryDropItem(p,w,false)){w.Delete();p.SendMessage("Make room for your wallet.");return;}}w.OnDoubleClick(p);}
  public static void Initialize(){CommandSystem.Register("wallet",AccessLevel.Player,e=>Open(e.Mobile));CommandSystem.Register("tithe",AccessLevel.Player,e=>{int amount;var w=Find(e.Mobile);if(w==null || !int.TryParse(e.ArgString,out amount) || !w.Tithe(e.Mobile,amount))e.Mobile.SendMessage("Use [wallet, deposit gold, then choose Tithe or use [tithe 1000.");});}
  public static HavenWallet Find(Mobile p){return p==null||p.Backpack==null?null:p.Backpack.FindItemsByType(typeof(HavenWallet),true).Cast<HavenWallet>().FirstOrDefault(x=>x.Owner==p);}
  public bool CanUse(Mobile p){return HavenMarks.CanUse(p) && Owner==p && !Deleted && p.Backpack!=null && IsChildOf(p.Backpack) && HavenResources.Accessible(p,this);}
  public long DepositPack(Mobile p){if(!CanUse(p)||!MergeLegacy())return 0;long total=0;foreach(var item in p.Backpack.FindItemsByType(typeof(Item),true)){int amount=item is Gold?item.Amount:item is BankCheck?((BankCheck)item).Worth:0;if(amount<=0 || !HavenResources.Accessible(p,item))continue;if(!Banker.Deposit(p,amount))continue;total+=amount;item.Delete();}InvalidateProperties();return total;}
  // Native Banker.Withdraw cannot combine a partial account balance with physical bank gold safely.
  public static bool SpendBank(Mobile p,int amount){if(p==null||amount<=0||Banker.GetBalance(p)<amount)return false;int accountPart=0;if(AccountGold.Enabled&&p.Account!=null){int platinum;double gold;p.Account.GetGoldBalance(out platinum,out gold);accountPart=(int)Math.Min(amount,Math.Max(0,gold));if(accountPart>0&&!p.Account.WithdrawGold(accountPart))return false;}int remainder=amount-accountPart;if(remainder>0&&!Banker.Withdraw(p,remainder)){if(accountPart>0)p.Account.DepositGold(accountPart);return false;}return true;}
  public bool Spend(Mobile p,long amount){if(!CanUse(p)||!MergeLegacy()||amount<=0||amount>int.MaxValue)return false;bool ok=SpendBank(p,(int)amount);InvalidateProperties();return ok;}
  public bool Withdraw(Mobile p,int amount){if(!CanUse(p)||!MergeLegacy()||amount<1||amount>60000||Balance<amount)return false;var gold=new Gold(amount);if(!p.Backpack.CheckHold(p,gold,false)){gold.Delete();return false;}if(!SpendBank(p,amount)){gold.Delete();return false;}p.Backpack.DropItem(gold);InvalidateProperties();return true;}
  public bool Tithe(Mobile p,int requested){if(!CanUse(p)||requested<1)return false;int amount=Math.Min(requested,Math.Max(0,100000-p.TithingPoints));if(amount==0||!Spend(p,amount))return false;p.TithingPoints+=amount;p.PlaySound(0x243);p.SendMessage("Tithed "+amount+" gold. Tithing points: "+p.TithingPoints+".");return true;}
  public static bool PayGold(Mobile p,int amount){if(!HavenMarks.CanUse(p)||amount<=0)return false;var w=Find(p);if(w!=null&&w.CanUse(p)&&!w.MergeLegacy())return false;return SpendBank(p,amount);}
  public override void OnDoubleClick(Mobile p){if(CanUse(p)){if(!MergeLegacy())p.SendMessage("Some old wallet gold is awaiting bank space; it remains safely stored. Make bank space and reopen your wallet.");long deposited=DepositPack(p);if(deposited>0)p.SendMessage("Deposited "+deposited.ToString("N0")+" gold from pack gold and checks into your bank.");p.SendGump(new HavenWalletGump(this,p));}}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Bank gold: "+Balance.ToString("N0"));list.Add("Double-click: collect pack gold/checks, then open bank and tithing");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Owner);w.Write(_legacyGold);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Owner=r.ReadMobile();_legacyGold=r.ReadLong();Timer.DelayCall(TimeSpan.Zero,()=>{if(!Deleted)MergeLegacy();});}
 }
 public class HavenWalletGump:HavenMenuGump {
  private readonly HavenWallet _wallet;
  public HavenWalletGump(HavenWallet w,Mobile p):base(50,50){
   _wallet=w; AddBackground(0,0,460,331,0x13BE);
   Text(24,22,400,24,"<B>ADVENTURER'S WALLET</B>");
   Text(24,56,260,22,"Bank gold: "+w.Balance.ToString("N0"));
   Text(295,56,140,22,"Marks: "+HavenMarks.Balance(p).ToString("N0"));
   Text(24,82,410,22,"Tithing: "+p.TithingPoints.ToString("N0")+" / 100,000");
   Button(24,116,1,"Deposit all pack gold / checks",360);
   Text(24,158,85,22,"Amount"); AddBackground(110,151,145,32,0xBB8); AddTextEntry(119,157,127,22,0,1,"1000");
   Text(269,158,165,22,"Gold per action");
   Button(24,201,2,"Withdraw to pack",180); Button(242,201,3,"Tithe gold",170);
   Text(24,242,410,42,"Uses your bank gold directly. Withdraw up to 60,000.<BR>Tithing costs 1 gold per point added.");
   Button(340,290,0,"Close",70);
  }
  private void Text(int x,int y,int width,int height,string text){AddHtml(x,y,width,height,"<BASEFONT COLOR=#F2F2F2>"+text+"</BASEFONT>",false,false);}
  private void Button(int x,int y,int id,string text,int width){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);Text(x+34,y+1,width,24,text);}
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||!_wallet.CanUse(p))return;if(info.ButtonID==1)p.SendMessage("Deposited "+_wallet.DepositPack(p)+" gold.");else{int amount;var entry=info.GetTextEntry(1);bool ok=entry!=null&&int.TryParse(entry.Text,out amount);if(!ok){p.SendMessage("Enter a positive whole number.");return;}int.TryParse(entry.Text,out amount);ok=info.ButtonID==2?_wallet.Withdraw(p,amount):info.ButtonID==3?_wallet.Tithe(p,amount):false;p.SendMessage(ok?"Done.":"Could not complete: check amount, funds and backpack capacity.");}p.SendGump(new HavenWalletGump(_wallet,p));}
 }
}

