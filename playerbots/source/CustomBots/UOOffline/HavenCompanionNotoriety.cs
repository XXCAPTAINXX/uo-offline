using Server.Mobiles;

namespace Server.UOOffline;

public partial class HavenCompanion
{
    public override bool IsBeneficialCriminal(Mobile target)
    {
        // Native criminal aid propagates from a controlled creature to its master.
        // Supporting that same owner must not repeatedly renew the owner's flag.
        if (Controlled && BoundOwner is { Deleted: false } owner && ControlMaster == owner)
        {
            if (target == owner) { return false; }
            if (target is BaseCreature { Controlled: true } pet)
            {
                if (pet.ControlMaster == owner) { return false; }
                if (pet.ControlMaster == this && Backpack != null)
                {
                    foreach (var item in Backpack.Items)
                    {
                        if (item is HavenCompanionAssignedPet { Deleted: false } assignment &&
                            assignment.Companion == this && assignment.Owner == owner && assignment.Pet == pet)
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return base.IsBeneficialCriminal(target);
    }
}
