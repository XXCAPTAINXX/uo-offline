using System;
using System.Linq;
using Server.Items;
namespace Server.HavenPrototype
{
 public sealed class HavenMissionSpoils:Container
 {
  public HavenCompanion Companion;public Mobile Owner;public bool Ready;public int ExtraGold;
  public HavenMissionSpoils(HavenCompanion c,int minutes):base(0xE76){Companion=c;Owner=c.BoundOwner;Visible=false;Movable=false;Name="Protected mission spoils";Internalize();int units=TrainingMinutes(minutes);ExtraGold=minutes*Utility.RandomMinMax(200,300)*HavenRegionalMissions.Bonus(minutes)/100-minutes*100;for(int i=0;i<GearCount(minutes);i++)DropItem(HavenOldWarden.Gear(0));}
  public HavenMissionSpoils(Serial serial):base(serial){}
  public static int TrainingMinutes(int minutes){return Math.Max(0,Math.Min(60,minutes))*HavenRegionalMissions.Bonus(minutes)/100;}
  public static int Experience(int minutes){return Math.Max(0,Math.Min(60,minutes))*10*HavenRegionalMissions.Bonus(minutes)/100;}
  public static int GearCount(int minutes){return Math.Max(1,TrainingMinutes(minutes)/5);}
  public static HavenMissionSpoils[] For(HavenCompanion c){return World.Items.Values.OfType<HavenMissionSpoils>().Where(x=>!x.Deleted&&x.Companion==c).ToArray();}
  public static int Pending(HavenCompanion c){return For(c).Where(x=>x.Ready).Sum(x=>x.Items.Count);}
  public static bool CanPrepare(HavenCompanion c,CompanionMission kind,int minutes){return kind!=CompanionMission.Supply||(!For(c).Any(x=>!x.Ready)&&Pending(c)+GearCount(minutes)<=200);}
  public static void Prepare(HavenCompanion c,CompanionMission kind,int minutes){if(kind==CompanionMission.Supply)new HavenMissionSpoils(c,minutes);}
  public static int Complete(HavenCompanion c,out int count){int gold=0;count=0;foreach(var x in For(c).Where(x=>!x.Ready)){x.Ready=true;gold+=x.ExtraGold;x.ExtraGold=0;count+=x.Items.Count;}return gold;}
  public static void Cancel(HavenCompanion c){foreach(var x in For(c).Where(x=>!x.Ready))x.Delete();}
  public static int Deliver(HavenCompanion c){if(c==null||c.Deleted||c.Backpack==null)return 0;int count=0;foreach(var parcel in For(c).Where(x=>x.Ready)){foreach(var item in parcel.Items.ToArray())if(c.PlaceMissionReward(item))count++;if(parcel.Items.Count==0)parcel.Delete();}return count;}
  public static void Recover(HavenCompanion c){foreach(var parcel in For(c))RecoverParcel(parcel);}
  static void RecoverParcel(HavenMissionSpoils parcel){if(parcel.Ready&&parcel.Owner!=null&&!parcel.Owner.Deleted){foreach(var item in parcel.Items.ToArray())parcel.Owner.BankBox.DropItem(item);parcel.Owner.SendMessage("Recovered mission equipment was placed in your bank.");}parcel.Delete();}
  public static void Initialize(){Timer.DelayCall(TimeSpan.FromMinutes(1),TimeSpan.FromMinutes(1),()=>{foreach(var parcel in World.Items.Values.OfType<HavenMissionSpoils>().Where(x=>x.Companion==null||x.Companion.Deleted).ToArray())RecoverParcel(parcel);});}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(1);w.Write(Companion);w.Write(Ready);w.Write(ExtraGold);w.Write(Owner);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);int version=r.ReadInt();Companion=r.ReadMobile() as HavenCompanion;Ready=r.ReadBool();ExtraGold=r.ReadInt();Owner=version>=1?r.ReadMobile():Companion==null?null:Companion.BoundOwner;}
 }
}
