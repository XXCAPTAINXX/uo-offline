using System;
using System.Linq;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenWallet:Item {
  public long Balance{get;private set;} public Mobile Owner{get;private set;}
  public HavenWallet(Mobile owner):base(0xEEF){Owner=owner;Name="adventurer's wallet";Hue=0x8A5;Weight=1;LootType=LootType.Blessed;}
  public HavenWallet(Serial serial):base(serial){}
  public static void Open(Mobile p){if(!HavenMarks.CanUse(p)||p.Backpack==null)return;var w=Find(p);if(w==null){w=new HavenWallet(p);if(!p.Backpack.TryDropItem(p,w,false)){w.Delete();p.SendMessage("Make room for your wallet.");return;}}w.OnDoubleClick(p);}
  public static void Initialize(){CommandSystem.Register("wallet",AccessLevel.Player,e=>Open(e.Mobile));CommandSystem.Register("tithe",AccessLevel.Player,e=>{int amount;var w=Find(e.Mobile);if(w==null || !int.TryParse(e.ArgString,out amount) || !w.Tithe(e.Mobile,amount))e.Mobile.SendMessage("Use [wallet, deposit gold, then choose Tithe or use [tithe 1000.");});}
  public static HavenWallet Find(Mobile p){return p==null||p.Backpack==null?null:p.Backpack.FindItemsByType(typeof(HavenWallet),true).Cast<HavenWallet>().FirstOrDefault(x=>x.Owner==p);}
  public bool CanUse(Mobile p){return HavenMarks.CanUse(p) && Owner==p && !Deleted && p.Backpack!=null && IsChildOf(p.Backpack) && HavenResources.Accessible(p,this);}
  public long DepositPack(Mobile p){if(!CanUse(p))return 0;long total=0;foreach(var item in p.Backpack.FindItemsByType(typeof(Item),true)){long amount=item is Gold?item.Amount:item is BankCheck?((BankCheck)item).Worth:0;if(amount<=0 || !HavenResources.Accessible(p,item) || amount>long.MaxValue-Balance)continue;Balance+=amount;total+=amount;item.Delete();}InvalidateProperties();return total;}
  public bool Spend(Mobile p,long amount){if(!CanUse(p)||amount<=0||Balance<amount)return false;Balance-=amount;InvalidateProperties();return true;}
  public bool Withdraw(Mobile p,int amount){if(!CanUse(p)||amount<1||amount>60000||Balance<amount)return false;var gold=new Gold(amount);if(!p.Backpack.TryDropItem(p,gold,false)){gold.Delete();return false;}Balance-=amount;InvalidateProperties();return true;}
  public bool BankDeposit(Mobile p,int amount){if(!CanUse(p)||amount<1||amount>60000||Balance<amount || !Banker.Deposit(p,amount))return false;Balance-=amount;InvalidateProperties();return true;}
  public bool BankWithdraw(Mobile p,int amount){if(!CanUse(p)||amount<1||amount>60000||amount>long.MaxValue-Balance || !Banker.Withdraw(p,amount))return false;Balance+=amount;InvalidateProperties();return true;}
  public bool Tithe(Mobile p,int requested){if(!CanUse(p)||requested<1)return false;int amount=Math.Min(requested,Math.Max(0,100000-p.TithingPoints));if(amount==0||!Spend(p,amount))return false;p.TithingPoints+=amount;p.PlaySound(0x243);p.SendMessage("Tithed "+amount+" gold. Tithing points: "+p.TithingPoints+".");return true;}
  public static bool PayGold(Mobile p,int amount){if(!HavenMarks.CanUse(p)||amount<=0)return false;var w=Find(p);long usable=w!=null&&w.CanUse(p)?Math.Min(w.Balance,amount):0;int bank=amount-(int)usable;if(bank>0&&!Banker.Withdraw(p,bank))return false;if(usable>0)w.Spend(p,usable);return true;}
  public override void OnDoubleClick(Mobile p){if(CanUse(p))p.SendGump(new HavenWalletGump(this,p));}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Stored gold: "+Balance.ToString("N0"));list.Add("Double-click: deposit, withdraw, bank transfers and tithing");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Owner);w.Write(Balance);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Owner=r.ReadMobile();Balance=r.ReadLong();}
 }
 public class HavenWalletGump:Gump {
  private readonly HavenWallet _wallet;
  public HavenWalletGump(HavenWallet w,Mobile p):base(50,50){_wallet=w;AddBackground(0,0,540,380,0x13BE);AddLabel(20,18,1152,"Adventurer's wallet");AddLabel(20,55,1152,"Wallet gold: "+w.Balance.ToString("N0")+"    Haven Marks: "+HavenMarks.Balance(p));AddLabel(20,82,1152,"Chivalry tithing points: "+p.TithingPoints.ToString("N0")+" / 100,000");AddLabel(20,125,1152,"Amount:");AddBackground(110,120,145,30,0xA28);AddTextEntry(118,124,125,22,0,1,"1000");Button(20,173,1,"Deposit pack gold / checks");Button(20,212,2,"Withdraw gold");Button(280,212,3,"Tithe gold (1:1)");Button(20,251,4,"Wallet to bank");Button(280,251,5,"Bank to wallet");AddHtml(20,295,490,45,"<BASEFONT COLOR=#FFFFFF>Transfers and coin withdrawals: 1-60,000. Tithing charges only the points added. Keep this wallet in your pack.</BASEFONT>",false,false);Button(420,344,0,"Close");}
  private void Button(int x,int y,int id,string text){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);AddLabel(x+34,y,1152,text);}
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||!_wallet.CanUse(p))return;if(info.ButtonID==1)p.SendMessage("Deposited "+_wallet.DepositPack(p)+" gold.");else{int amount;var entry=info.GetTextEntry(1);bool ok=entry!=null&&int.TryParse(entry.Text,out amount);if(!ok){p.SendMessage("Enter a positive whole number.");return;}int.TryParse(entry.Text,out amount);ok=info.ButtonID==2?_wallet.Withdraw(p,amount):info.ButtonID==3?_wallet.Tithe(p,amount):info.ButtonID==4?_wallet.BankDeposit(p,amount):info.ButtonID==5&&_wallet.BankWithdraw(p,amount);p.SendMessage(ok?"Done.":"Could not complete: check amount, funds and backpack capacity.");}p.SendGump(new HavenWalletGump(_wallet,p));}
 }
}
