using System;
using System.Linq;
using System.Collections.Generic;
using Server.Accounting;
using Server.Multis;
namespace Server.HavenPrototype
{
 public static class HavenFreshStartHandoff
 {
  public const string Tag="Haven.FreshStart.Houses";
  public const string OwnerTag="Haven.FreshStart.ArchivedOwner";
  public static void Initialize(){EventSink.Login+=e=>Claim(e.Mobile);}
  public static void Transfer(BaseHouse house,Mobile oldOwner,Mobile newOwner)
  {
   house.Owner=newOwner;
   foreach(var item in house.LockDowns.Keys.ToArray())if(house.LockDowns[item]==oldOwner)house.LockDowns[item]=newOwner;
   foreach(var secure in house.Secures)if(secure.Owner==oldOwner)secure.Owner=newOwner;
   foreach(var item in house.Addons.Keys.ToArray())if(house.Addons[item]==oldOwner)house.Addons[item]=newOwner;
   house.OnTransfer();house.ChangeLocks(newOwner);
  }
  public static int Claim(Mobile p)
  {
   var a=p==null?null:p.Account as Account;if(a==null||p.Deleted||!p.Player)return 0;
   string value=a.GetTag(Tag);int oldId;if(String.IsNullOrEmpty(value)||!Int32.TryParse(a.GetTag(OwnerTag),out oldId)||p.Serial.Value==oldId)return 0;
   var old=World.FindMobile((Serial)oldId);if(old==null||old.Deleted)return 0;int count=0;var pending=new List<string>();
   foreach(var part in value.Split(',')){int id;if(!Int32.TryParse(part,out id)){pending.Add(part);continue;}var h=World.FindItem((Serial)id) as BaseHouse;if(h!=null&&!h.Deleted&&h.Owner==p)continue;if(h==null||h.Deleted||h.Owner!=old){pending.Add(part);continue;}Transfer(h,old,p);count++;}
   if(pending.Count==0){a.RemoveTag(Tag);a.RemoveTag(OwnerTag);}else{a.SetTag(Tag,String.Join(",",pending));p.SendMessage("Some homes could not be transferred yet; their recovery record has been kept.");}if(count>0)p.SendMessage("Your "+count+" furnished homes are ready. Their stored belongings were archived for your fresh start.");return count;
  }
 }
}
