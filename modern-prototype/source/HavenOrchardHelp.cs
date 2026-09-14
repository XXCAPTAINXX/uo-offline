using System;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Shadowguard;
using Server.Network;
using VirtueType = Server.Engines.Shadowguard.VirtueType;
namespace Server.HavenPrototype
{
 public static class HavenOrchardHelp
 {
  public static string Label(VirtueType virtue){return virtue==VirtueType.Sacrafice?"Sacrifice":virtue.ToString();}
  public static VirtueType Opposite(VirtueType virtue){return (VirtueType)(((int)virtue+8)%16);}
  public static void TreeProperties(ShadowguardCypress tree,ObjectPropertyList list){if(!HavenPreview.Enabled||tree==null)return;list.Add(Label(tree.VirtueType)+" tree");list.Add("Pair "+((int)tree.VirtueType%8+1)+": "+Label(tree.VirtueType)+" / "+Label(Opposite(tree.VirtueType)));}
  public static void AppleProperties(ShadowguardApple apple,ObjectPropertyList list){if(HavenPreview.Enabled&&apple.Tree!=null)list.Add("Use on: "+Label(Opposite(apple.Tree.VirtueType))+" tree (pair "+((int)apple.Tree.VirtueType%8+1)+")");}
  public static void Help(Mobile owner,HavenCompanion companion)
  {
   if(!HavenPreview.Enabled||companion==null||!companion.CanOpenPack(owner))return;
   if(HavenFountainHelp.TryHelp(owner,companion))return;
   var encounter=World.Items.Values.OfType<ShadowguardCypress>().Where(t=>!t.Deleted&&t.Map==owner.Map).Select(t=>t.Encounter).FirstOrDefault(e=>e!=null&&e.HasBegun&&!e.Completed&&e.Instance!=null&&e.Participants.Contains(owner as PlayerMobile)&&e.Region!=null&&e.Region.Contains(owner.Location));
   if(encounter==null||encounter.Trees==null){owner.SendMessage("Puzzle help supports Shadowguard's Orchard and Fountain. Enter the room with your companion first.");return;}
   var apple=encounter.Apple;
   if(apple==null||apple.Deleted){var tree=encounter.Trees.Where(t=>t!=null&&!t.Deleted&&owner.InRange(t,3)&&owner.InLOS(t)).OrderBy(t=>owner.GetDistanceToSqrt(t.Location)).FirstOrDefault();if(tree==null){companion.SayTo(owner,"Stand within three tiles of a tree, then ask me again. I'll help you pick and match its apple.");return;}tree.OnDoubleClick(owner);apple=encounter.Apple;}
   if(apple==null||apple.Deleted||!apple.IsChildOf(owner.Backpack)){owner.SendMessage("An Orchard apple is already in use. Its holder needs to finish that pair first.");return;}
   if(apple.Tree==null||apple.Tree.Deleted){owner.SendMessage("That apple no longer has a source tree.");return;}
   if(apple._Thrown){owner.SendMessage("That apple is already being thrown.");return;}
   var match=encounter.Trees.FirstOrDefault(t=>t!=null&&!t.Deleted&&t!=apple.Tree&&t.IsOppositeVirtue(apple.Tree.VirtueType));
   if(match==null){owner.SendMessage("That apple's matching tree is no longer available.");return;}
   match.PublicOverheadMessage(MessageType.Regular,53,false,"MATCH: "+Label(match.VirtueType));
   if(!owner.InRange(match,10)||!owner.InLOS(match)){companion.SayTo(owner,"Your apple matches "+Label(match.VirtueType)+". Move closer to the marked tree and ask me again.");owner.SendMessage("Matching tree: "+match.X+", "+match.Y+" ("+(int)Math.Ceiling(owner.GetDistanceToSqrt(match.Location))+" tiles away).");return;}
   companion.SayTo(owner,"That's the matching tree. Let's use your apple!");var previousTarget=owner.Target;apple.OnDoubleClick(owner);if(owner.Target!=null&&owner.Target!=previousTarget)owner.Target.Invoke(owner,match);
  }
 }
}
