using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenBondingPotion : Item
{
    [Constructible]
    public HavenBondingPotion() : base(0xF0E) { Name = "pet bonding potion"; Hue = 0x489; Weight = 1; }
    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack)) { from.SendMessage("Choose your living pet to bond instantly."); from.Target = new BondTarget(this); }
    }
    internal bool ApplyTo(Mobile from, BaseCreature pet)
    {
        if (Deleted || !from.Alive || !IsChildOf(from.Backpack) || pet?.Deleted != false || !pet.Alive || pet.IsDeadPet ||
            !pet.Controlled || pet.ControlMaster != from || pet.Summoned || pet.IsBonded || !pet.IsBondable ||
            pet.Map != from.Map || !from.InRange(pet, 3) || !from.InLOS(pet)) { return false; }
        pet.IsBonded = true;
        pet.Loyalty = BaseCreature.MaxLoyalty;
        from.SendMessage($"{pet.Name} is now bonded to you.");
        Delete(); return true;
    }
    private sealed class BondTarget : Target
    {
        private readonly HavenBondingPotion _potion;
        public BondTarget(HavenBondingPotion potion) : base(3, false, TargetFlags.None) { _potion = potion; }
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCreature pet || !_potion.ApplyTo(from, pet))
            { from.SendMessage("Choose your own living, bondable pet nearby that is not already bonded. The potion was kept."); }
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenPetLeash : Item
{
    [Constructible]
    public HavenPetLeash() : base(0x14F8) { Name = "reusable pet shrinking leash"; Weight = 1; LootType = LootType.Blessed; }
    public override void OnDoubleClick(Mobile from)
    {
        if (!from.Alive || !IsChildOf(from.Backpack)) { return; }
        from.SendMessage("Choose your living pet nearby to shrink. Double-click its token to restore it.");
        FreePetHitchingPost.BeginShrink(from, this);
    }
}

[SerializationGenerator(0)]
public partial class HavenHouseHitchingPost : FreePetHitchingPost
{
    [Constructible]
    public HavenHouseHitchingPost() { Name = "house pet shrinking post"; Movable = true; Weight = 10; }
    internal bool CanUse(Mobile from) => Parent == null && Map == from.Map && BaseHouse.FindHouseAt(this)?.IsCoOwner(from) == true;
    public override void OnDoubleClick(Mobile from)
    {
        if (!CanUse(from)) { from.SendMessage("Place this post in a house you own or co-own. You can lock it down there."); return; }
        base.OnDoubleClick(from);
    }
}
