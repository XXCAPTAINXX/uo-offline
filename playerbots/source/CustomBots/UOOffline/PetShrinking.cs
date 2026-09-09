using ModernUO.Serialization;
using Server.Mobiles;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class ShrunkenPet : Item
{
    [SerializableField(0)]
    private BaseCreature _pet;

    [SerializableField(1)]
    private Mobile _owner;

    public override string DefaultName =>
        Pet?.Deleted == false ? $"shrunken pet: {Pet.Name}" : "empty shrunken pet token";

    [Constructible(AccessLevel.Developer)]
    public ShrunkenPet() : base(0x2123)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public ShrunkenPet(BaseCreature pet, Mobile owner) : this()
    {
        Pet = pet;
        Owner = owner;
        Hue = pet?.Hue ?? 0;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("The shrunken pet must be in your backpack.");
            return;
        }

        if (Pet?.Deleted != false)
        {
            from.SendMessage("This pet token is empty.");
            Delete();
            return;
        }

        if (Owner != null && Owner != from)
        {
            from.SendMessage("This shrunken pet belongs to someone else.");
            return;
        }

        if (!from.CheckAlive())
        {
            return;
        }

        if (from.Followers + Pet.ControlSlots > from.FollowersMax)
        {
            from.SendMessage("You have too many followers to restore that pet.");
            return;
        }

        Restore(from, true);
    }

    private void Restore(Mobile from, bool consumeToken)
    {
        var pet = Pet;

        if (pet?.Deleted != false || from == null || from.Deleted)
        {
            return;
        }

        Pet = null;

        pet.SetControlMaster(from);
        pet.ControlTarget = from;
        pet.ControlOrder = OrderType.Follow;
        pet.MoveToWorld(from.Location, from.Map);

        if (Core.SE)
        {
            pet.Loyalty = BaseCreature.MaxLoyalty;
        }

        from.SendMessage($"{pet.Name} has been restored.");

        if (consumeToken)
        {
            Delete();
        }
    }

    public override void OnAfterDelete()
    {
        // A pet must never disappear because the token was accidentally
        // deleted, cleaned up, or removed by a script. Restore it to its owner
        // whenever possible instead of leaving an orphan on Map.Internal.
        var pet = Pet;
        var owner = Owner;

        Pet = null;

        if (pet?.Deleted == false && owner?.Deleted == false &&
            owner.Map != null && owner.Map != Map.Internal)
        {
            pet.SetControlMaster(owner);
            pet.ControlTarget = owner;
            pet.ControlOrder = OrderType.Follow;
            pet.MoveToWorld(owner.Location, owner.Map);
        }

        base.OnAfterDelete();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Pet?.Deleted == false)
        {
            list.Add($"Pet: {Pet.Name}");
            list.Add($"Control slots: {Pet.ControlSlots}");
            list.Add(Pet.IsBonded ? "Bonded" : "Not bonded");
        }

        if (Owner != null)
        {
            list.Add($"Owner: {Owner.Name}");
        }

        list.Add("Double-click to restore the pet");
    }
}

[SerializationGenerator(0)]
public partial class FreePetHitchingPost : Item
{
    public override string DefaultName => "free pet hitching post";

    [Constructible]
    public FreePetHitchingPost() : base(0x14E7)
    {
        Movable = false;
        Hue = 0x59B;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("You are too far away from the hitching post.");
            return;
        }

        from.SendMessage("Target a living pet you own to shrink it for free.");
        from.Target = new ShrinkTarget(this);
    }

    internal static void BeginShrink(Mobile from, Item source) => from.Target = new ShrinkTarget(source);
    private sealed class ShrinkTarget : Target
    {
        private readonly Item _post;

        public ShrinkTarget(Item post) : base(12, false, TargetFlags.None) =>
            _post = post;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_post?.Deleted != false || !from.Alive || from.Map != _post.Map && _post.Parent == null || !from.InRange(_post.GetWorldLocation(), 3) ||
                _post is HavenPetLeash && !(_post.IsChildOf(from.Backpack)) ||
                _post is HavenHouseHitchingPost housePost && !housePost.CanUse(from))
            {
                from.SendMessage("You are too far away from the hitching post.");
                return;
            }

            if (targeted is not BaseCreature pet)
            {
                from.SendMessage("That is not a pet.");
                return;
            }

            if (!pet.Controlled || pet.ControlMaster != from || pet.Map != from.Map || !from.InRange(pet, 3) || !from.InLOS(pet))
            {
                from.SendMessage("You may only shrink a pet that you control.");
                return;
            }

            if (pet.IsDeadPet)
            {
                from.SendMessage("Resurrect that pet before shrinking it.");
                return;
            }

            if (pet.Summoned)
            {
                from.SendMessage("Summoned creatures cannot be shrunk.");
                return;
            }

            if (pet.Body.IsHuman)
            {
                from.SendMessage("That creature cannot be shrunk.");
                return;
            }

            if (pet is PackLlama or PackHorse or Beetle && pet.Backpack?.Items.Count > 0)
            {
                from.SendMessage("Unload the pet's backpack before shrinking it.");
                return;
            }

            if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) &&
                pet.Map == pet.Combatant.Map)
            {
                from.SendMessage("That pet is too busy fighting to shrink.");
                return;
            }

            if (pet is BaseMount { Rider: not null } mount)
            {
                mount.Rider = null;
            }

            var token = new ShrunkenPet(pet, from);

            if (from.Backpack == null || !from.Backpack.TryDropItem(from, token, false))
            {
                token.Delete();
                from.SendMessage("Make room in your backpack before shrinking a pet.");
                return;
            }

            pet.ControlTarget = null;
            pet.ControlOrder = OrderType.Stay;
            pet.Internalize();
            pet.SetControlMaster(null);
            pet.SummonMaster = null;

            from.SendMessage($"{pet.Name} has been safely shrunk for free.");
        }
    }
}
