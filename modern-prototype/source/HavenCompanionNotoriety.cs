using Server;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        public override bool IsBeneficialCriminal(Mobile target)
        {
            // BaseCreature passes a pet's criminal action back to its master.
            // Automatic owner healing must not renew that master's existing flag.
            if (target != null && target == BoundOwner && ControlMaster == target)
                return false;

            var pet = target as BaseCreature;
            if (BoundOwner != null && ControlMaster == BoundOwner && pet != null &&
                (pet.ControlMaster == BoundOwner || pet.ControlMaster == this)) return false;
            return base.IsBeneficialCriminal(target);
        }
    }
}
