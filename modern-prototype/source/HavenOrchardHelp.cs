using System;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Shadowguard;
using Server.Network;
using VirtueType = Server.Engines.Shadowguard.VirtueType;
namespace Server.HavenPrototype
{
 public static class HavenOrchardHelp
 {
  public const int MatchHue = 53;
  private sealed class TreeHighlight
  {
   public ShadowguardApple Apple;
   public int TrunkHue;
   public int FoliageHue;
  }
  private static readonly Dictionary<ShadowguardCypress,TreeHighlight> Highlights = new Dictionary<ShadowguardCypress,TreeHighlight>();
  public static void Initialize()
  {
   EventSink.ServerStarted += () =>
   {
    // Native Orchard trees are uncolored. Clear saved temporary highlights after a restart.
    foreach(var tree in World.Items.Values.OfType<ShadowguardCypress>())
    {
     if(tree.Hue==MatchHue)tree.Hue=0;
     if(tree.Foilage!=null&&tree.Foilage.Hue==MatchHue)tree.Foilage.Hue=0;
    }
    Timer.DelayCall(TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(1),UpdateHighlights);
   };
  }
  private static void UpdateHighlights()
  {
   foreach(var entry in Highlights.ToArray())
   {
    var tree=entry.Key;var mark=entry.Value;
    if(!tree.Deleted&&!mark.Apple.Deleted&&tree.Encounter!=null&&tree.Encounter.Apple==mark.Apple)continue;
    if(!tree.Deleted)
    {
     tree.Hue=mark.TrunkHue;
     if(tree.Foilage!=null&&!tree.Foilage.Deleted)tree.Foilage.Hue=mark.FoliageHue;
    }
    Highlights.Remove(tree);
   }
  }
  internal static void Highlight(ShadowguardCypress tree,ShadowguardApple apple)
  {
   UpdateHighlights();
   if(!Highlights.ContainsKey(tree))Highlights[tree]=new TreeHighlight{Apple=apple,TrunkHue=tree.Hue,FoliageHue=tree.Foilage==null?0:tree.Foilage.Hue};
   tree.Hue=MatchHue;
   if(tree.Foilage!=null&&!tree.Foilage.Deleted)tree.Foilage.Hue=MatchHue;
  }
  public static string Label(VirtueType virtue){return virtue==VirtueType.Sacrafice?"Sacrifice":virtue.ToString();}
  public static VirtueType Opposite(VirtueType virtue){return (VirtueType)(((int)virtue+8)%16);}
  public static void TreeProperties(ShadowguardCypress tree,ObjectPropertyList list){if(!HavenPreview.Enabled||tree==null)return;list.Add(Label(tree.VirtueType)+" tree");list.Add("Pair "+((int)tree.VirtueType%8+1)+": "+Label(tree.VirtueType)+" / "+Label(Opposite(tree.VirtueType)));}
  public static void AppleProperties(ShadowguardApple apple,ObjectPropertyList list){if(HavenPreview.Enabled&&apple.Tree!=null)list.Add("Use on: "+Label(Opposite(apple.Tree.VirtueType))+" tree (pair "+((int)apple.Tree.VirtueType%8+1)+")");}
  public static void Help(Mobile owner,HavenCompanion companion)
  {
   if(!HavenPreview.Enabled||companion==null||!companion.IsOwner(owner))return;
   if(companion.ArmoryHelpActive){companion.RequestArmoryHelp(owner);return;}
   if(!companion.CanOpenPack(owner))return;
   if(companion.RequestArmoryHelp(owner))return;
   if(HavenFountainHelp.TryHelp(owner,companion))return;
   var encounter=World.Items.Values.OfType<ShadowguardCypress>().Where(t=>!t.Deleted&&t.Map==owner.Map).Select(t=>t.Encounter).FirstOrDefault(e=>e!=null&&e.HasBegun&&!e.Completed&&e.Instance!=null&&e.Participants.Contains(owner as PlayerMobile)&&e.Region!=null&&e.Region.Contains(owner.Location));
   if(encounter==null||encounter.Trees==null){owner.SendMessage("Puzzle help supports Shadowguard's Orchard, Fountain and Armory. Enter the room with your companion first.");return;}
   var apple=encounter.Apple;
   if(apple==null||apple.Deleted){var tree=encounter.Trees.Where(t=>t!=null&&!t.Deleted&&owner.InRange(t,3)&&owner.InLOS(t)).OrderBy(t=>owner.GetDistanceToSqrt(t.Location)).FirstOrDefault();if(tree==null){companion.SayTo(owner,"Stand within three tiles of a tree, then ask me again. I'll help you pick and match its apple.");return;}tree.OnDoubleClick(owner);apple=encounter.Apple;}
   if(apple==null||apple.Deleted||!apple.IsChildOf(owner.Backpack)){owner.SendMessage("An Orchard apple is already in use. Its holder needs to finish that pair first.");return;}
   if(apple.Tree==null||apple.Tree.Deleted){owner.SendMessage("That apple no longer has a source tree.");return;}
   if(apple._Thrown){owner.SendMessage("That apple is already being thrown.");return;}
   var match=encounter.Trees.FirstOrDefault(t=>t!=null&&!t.Deleted&&t!=apple.Tree&&t.IsOppositeVirtue(apple.Tree.VirtueType));
   if(match==null){owner.SendMessage("That apple's matching tree is no longer available.");return;}
   Highlight(match,apple);
   match.PublicOverheadMessage(MessageType.Regular,MatchHue,false,"MATCH: "+Label(match.VirtueType));
   if(!owner.InRange(match,10)||!owner.InLOS(match)){companion.SayTo(owner,"Your apple matches "+Label(match.VirtueType)+". Move closer to the marked tree and ask me again.");owner.SendMessage("Matching tree: "+match.X+", "+match.Y+" ("+(int)Math.Ceiling(owner.GetDistanceToSqrt(match.Location))+" tiles away).");return;}
   companion.SayTo(owner,"That's the matching tree. Let's use your apple!");var previousTarget=owner.Target;apple.OnDoubleClick(owner);if(owner.Target!=null&&owner.Target!=previousTarget)owner.Target.Invoke(owner,match);
  }
 }
}
