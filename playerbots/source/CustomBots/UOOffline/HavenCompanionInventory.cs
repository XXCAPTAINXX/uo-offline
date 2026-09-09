using Server.Items;
namespace Server.UOOffline;
public static class HavenCompanionInventory
{
    public static void Open(Container container, Mobile owner)
    {
        if (container?.Deleted != false || owner?.Deleted != false || !owner.Alive ||
            container.RootParent is not HavenCompanion companion || companion.Deleted || companion.BoundOwner != owner ||
            owner.Map != companion.Map || !owner.InRange(companion, 3) || !owner.InLOS(companion) ||
            (container != companion.Backpack && !container.IsChildOf(companion.Backpack))) { return; }
        if (container is TrappableContainer trapped && trapped.ExecuteTrap(owner)) { return; }
        container.DisplayTo(owner);
    }
}
