using System;
using System.Linq;
using Server.Mobiles;
using PartyGroup=Server.Engines.PartySystem.Party;
namespace Server.HavenPrototype
{
 public static class HavenCompanionPresence
 {
  public static void Initialize(){Timer.DelayCall(TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(2),Maintain);}
  public static void Maintain(){if(!HavenPreview.Enabled)return;foreach(var c in World.Mobiles.Values.OfType<HavenCompanion>().ToArray()){var owner=c.BoundOwner;if(owner==null||owner.Deleted||owner.NetState==null)continue;EnsureParty(c,owner);if(!owner.HasGump(typeof(CompanionCombatBarGump)))owner.SendGump(new CompanionCombatBarGump(c));}}
  public static bool EnsureParty(HavenCompanion c,Mobile owner)
  {
   if(!HavenPreview.Enabled||c==null||!c.IsOwner(owner))return false;
   var party=PartyGroup.Get(owner);var current=PartyGroup.Get(c);
   if(party!=null&&party.Contains(c))return true;
   if(party!=null&&party.Count>=PartyGroup.Capacity)return false;
   if(current!=null)current.Remove(c);
   party=PartyGroup.Get(owner);
   if(party==null){party=new PartyGroup(owner);owner.Party=party;}
   party.Add(c);return PartyGroup.Get(c)==party;
  }
 }
}
