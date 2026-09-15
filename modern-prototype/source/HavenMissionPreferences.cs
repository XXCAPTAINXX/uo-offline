using System;
using Server.Accounting;
namespace Server.HavenPrototype
{
 public static class HavenMissionPreferences
 {
  static string Key(HavenCompanion c,string name){return "Haven.MissionPreference:"+c.Serial.Value+":"+name;}
  public static int Get(HavenCompanion c,string name,int fallback){var account=c==null||c.BoundOwner==null?null:c.BoundOwner.Account as Account;int n;return account!=null&&int.TryParse(account.GetTag(Key(c,name)),out n)?n:fallback;}
  static void Set(HavenCompanion c,string name,int value){var account=c==null||c.BoundOwner==null?null:c.BoundOwner.Account as Account;if(account!=null)account.SetTag(Key(c,name),value.ToString());}
  public static void Remember(HavenCompanion c,int tab,int selection,int minutes){if(tab<0||tab>1)return;Set(c,"Tab",tab);Set(c,"Selection",selection);Set(c,"Minutes",minutes);}
  public static void Started(HavenCompanion c,CompanionMission kind,int minutes){Set(c,"LastKind",(int)kind);Set(c,"LastMinutes",minutes);}
  public static bool Repeat(HavenCompanion c,Mobile owner){if(c==null||!c.IsOwner(owner))return false;int kind=Get(c,"LastKind",-1);if(kind<0||kind>(int)CompanionMission.DoomRecon){owner.SendMessage("Complete or start a mission first to save a repeat route.");return false;}return c.StartMission(owner,Get(c,"LastMinutes",5),(CompanionMission)kind);}
 }
 public partial class HavenCompanion
 {
  public string PendingMissionSummary {get{return "Pending: "+_pendingGold.ToString("N0")+" gold, "+PendingResources.ToString("N0")+" materials, "+PendingPetTickets+" tickets, "+HavenDoomMissionLoot.Pending(BoundOwner)+" Doom items";}}
  public void CollectMissionRewards(Mobile owner){if(!IsOwner(owner))return;DeliverRewards();DeliverPetTickets();owner.SendMessage(PendingMissionSummary);}
 }
}
