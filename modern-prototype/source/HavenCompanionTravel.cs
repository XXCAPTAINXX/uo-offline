using System.Linq;
using Server.Mobiles;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        public static void TravelWithOwner(Mobile owner, Point3D destination, Map map)
        {
            if (!HavenPreview.Enabled || owner == null || owner.Deleted || !owner.Alive || map == null || map == Map.Internal) return;
            foreach (var companion in World.Mobiles.Values.OfType<HavenCompanion>().Where(c =>
                !c.Deleted && c.BoundOwner == owner && c.ControlMaster == owner && c.Alive && !c.IsDeadPet &&
                !c.IsStabled && !c.OnMission).ToArray())
            {
                companion.StopTamingAssist();
                var spell = companion.Spell as Server.Spells.Spell;
                if (spell != null) spell.Disturb(Server.Spells.DisturbType.NewCast);
                if (companion.Target != null) companion.Target.Cancel(companion, TargetCancelType.Canceled);
                companion.Combatant = null; companion.FocusMob = null; companion.Warmode = false;
                companion.MoveToWorld(destination, map);
                companion.ControlTarget = owner;
                companion.ControlOrder = OrderType.Follow;
                companion.ClearMissionReturn();
                if (companion.AIObject != null) {
                    companion.AIObject.OnTeleported();
                    companion.AIObject.NextMove = Core.TickCount;
                    companion.AIObject.Activate();
                }
            }
        }
    }
}
