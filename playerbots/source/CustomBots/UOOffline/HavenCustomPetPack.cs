using Server.Commands;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.UOOffline;

public static class HavenCustomPetPack
{
    internal static bool CanOpen(Mobile owner, BaseCreature pet) => owner?.Deleted == false && owner.Alive &&
        pet?.Deleted == false && pet.Alive && !pet.IsDeadPet && pet.Controlled && !pet.Summoned &&
        pet.ControlMaster == owner && HavenTamingMissions.IsCustomPet(pet) && pet.Map == owner.Map &&
        owner.InRange(pet, 3) && owner.InLOS(pet);
    public static void Initialize() => CommandSystem.Register("PetPack", AccessLevel.Player, e =>
    { e.Mobile.SendMessage("Select your custom pet to open its pack. Dismount first."); e.Mobile.Target = new PackTarget(); });
    internal static void OpenPet(Mobile owner, BaseCreature pet)
    {
        if (!CanOpen(owner, pet)) { owner.SendMessage("Bring your own living custom pet within three steps and dismount first."); return; }
        if (pet.Backpack == null) { pet.AddItem(new Backpack()); }
        Open(pet.Backpack, owner);
    }
    public static void Open(Container container, Mobile owner)
    {
        if (container?.Deleted != false || container.RootParent is not BaseCreature pet || !CanOpen(owner, pet) ||
            (container != pet.Backpack && !container.IsChildOf(pet.Backpack))) { return; }
        if (container is TrappableContainer trapped && trapped.ExecuteTrap(owner)) { return; }
        container.DisplayTo(owner);
    }
    public sealed class Entry : ContextMenuEntry
    {
        public Entry() : base(6145, 3) { }
        public override void OnClick(Mobile from, IEntity target) { if (target is BaseCreature pet) { OpenPet(from, pet); } }
    }
    private sealed class PackTarget : Target
    {
        public PackTarget() : base(3, false, TargetFlags.None) { }
        protected override void OnTarget(Mobile from, object targeted) { if (targeted is BaseCreature pet) { OpenPet(from, pet); } }
    }
}
