using System.Collections.Generic;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public static class HavenFollowerMovement
 {
  static Mobile Owner(Mobile mobile){var seen=new HashSet<Mobile>();while(mobile!=null&&!mobile.Deleted&&seen.Add(mobile)){if(mobile is PlayerMobile)return mobile;var creature=mobile as BaseCreature;if(creature==null)return null;mobile=creature.Controlled?creature.ControlMaster:creature.Summoned?creature.SummonMaster:null;}return null;}
  public static bool CanPass(Mobile a,Mobile b){if(!HavenPreview.Enabled||a==null||b==null||a==b)return false;var owner=Owner(a);return owner!=null&&owner==Owner(b);}
 }
}
