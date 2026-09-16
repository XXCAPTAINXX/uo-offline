using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenTamedPetClaim : ShrunkenPet
{
    [Constructible] public HavenTamedPetClaim() { ItemID=0x14F0;Name="Companion-tamed pet claim"; }
    public HavenTamedPetClaim(BaseCreature pet,Mobile owner) : base(pet,owner)
    { ItemID=0x14F0;Name=$"Pet claim: {pet.Name}"; }
    public override void OnDoubleClick(Mobile from)
    {
        var pet=Pet;base.OnDoubleClick(from);
        if(pet?.Deleted==false && Pet==null && pet.ControlMaster==from)
        {
            if(!pet.Owners.Contains(from)) { pet.Owners.Add(from); }
            pet.BondingBegin=Core.Now-pet.BondingDelay-TimeSpan.FromSeconds(1);
            from.SendMessage("This is the original animal. Feed suitable food to bond once you meet its taming requirement.");
        }
    }
    internal static HavenTamedPetClaim Store(BaseCreature pet,Mobile owner,Container pack)
    {
        if(pet?.Deleted!=false || owner?.Deleted!=false || pack?.Deleted!=false || pet.IsDeadPet || pet.Summoned || !(pet.ControlMaster==owner || pet.ControlMaster is HavenCompanion helper && helper.BoundOwner==owner)) { return null; }
        var claim=new HavenTamedPetClaim(pet,owner);
        if(!pack.TryDropItem(owner,claim,false)) { claim.Pet=null;claim.Delete();return null; }
        if(pet is BaseMount mount) { mount.Rider=null; }
        pet.Combatant=null;pet.ControlTarget=null;pet.ControlOrder=OrderType.Stay;
        pet.Internalize();pet.SetControlMaster(null);pet.SummonMaster=null;
        return claim;
    }
}

public partial class HavenCompanion
{
    private DateTime _nextTamingAttempt;
    internal bool ContinuingAssistedTame(BaseCreature animal) => !Deleted && !IsDeadPet && _tamingTarget==animal &&
        BoundOwner?.Deleted==false && BoundOwner.Alive && BoundOwner.Map==Map && InRange(BoundOwner,18) && Role==HavenCompanionRole.Bard;
    internal bool FinishAssistedTame(BaseCreature animal)
    {
        if(!ContinuingAssistedTame(animal) || animal.ControlMaster!=this || Backpack==null) { return false; }
        var claim=HavenTamedPetClaim.Store(animal,BoundOwner,Backpack);
        if(claim==null)
        {
            Backpack.DropItem(new HavenCompanionAssignedPet { Companion=this,Owner=BoundOwner,Pet=animal,AutoMount=false });
            StopTamingAssist();ControlTarget=BoundOwner;ControlOrder=OrderType.Follow;
            BoundOwner.SendMessage("The tame succeeded, but my pack is full. The pet will follow me; make room and reclaim it with [companionpets.");
            return true;
        }
        StopTamingAssist();ControlTarget=BoundOwner;ControlOrder=OrderType.Follow;
        BoundOwner.SendMessage($"I tamed {animal.Name}. Its claim ticket is in my pack, with the animal's original stats and rarity.");
        return true;
    }
    private void TryAssistedTaming()
    {
        var animal=_tamingTarget;
        if(animal==null || !animal.BardPacified || Core.Now<_nextTamingAttempt || !InRange(animal,3) || !InLOS(animal) ||
            Server.SkillHandlers.AnimalTaming.IsBeingTamed(animal)) { return; }
        _nextTamingAttempt=Core.Now+TimeSpan.FromSeconds(10);
        Server.SkillHandlers.AnimalTaming.OnUse(this);
        Target?.Invoke(this,animal);
    }
}

[SerializationGenerator(0)]
public partial class HavenCompanionAssignedPet : Item
{
    [SerializableField(0)] private HavenCompanion _companion;
    [SerializableField(1)] private Mobile _owner;
    [SerializableField(2)] private BaseCreature _pet;
    [SerializableField(3)] private bool _autoMount=true;
    [SerializableField(4)] private bool _parked;
    public override bool IsVirtualItem => true;
    [Constructible] public HavenCompanionAssignedPet() : base(1) { Visible=false;Movable=false;Weight=0;Name="Companion pet assignment"; }
    internal void Tick()
    {
        var companion=Companion;var pet=Pet;
        if(companion?.Deleted!=false) { Delete();return; }
        if(pet?.Deleted!=false || pet.ControlMaster!=companion) { Pet=null;Delete();return; }
        if(companion.Map==null || companion.Map==Map.Internal || companion.Expedition!=null) { return; }
        if(Parked) { pet.MoveToWorld(companion.Location,companion.Map);Parked=false; }
        pet.Loyalty=BaseCreature.MaxLoyalty;
        if(pet is BaseMount mount && mount.Rider==companion)
        { if(AutoMount && !companion.IsDeadPet) { return; } mount.Rider=null; }
        if(pet.IsDeadPet || companion.IsDeadPet) { pet.ControlTarget=Owner;pet.ControlOrder=OrderType.Follow;return; }
        if(pet.Map!=companion.Map || !pet.InRange(companion,18))
        { pet.MoveToWorld(companion.Location,companion.Map); }
        if(AutoMount && pet is BaseMount ride && ride.Rider==null && companion.Mount==null && pet.InRange(companion,3))
        { ride.Rider=companion;return; }
        var enemy=companion.Combatant as Mobile;
        if(!companion.TamingAssistActive && enemy?.Deleted==false && enemy.Alive && companion.CanBeHarmful(enemy,false))
        { pet.ControlTarget=enemy;if(pet.ControlOrder!=OrderType.Attack) { pet.ControlOrder=OrderType.Attack; }pet.Combatant=enemy; }
        else
        {
            pet.Combatant=null;pet.ControlTarget=companion;
            var order=companion.ControlOrder==OrderType.Stay ? OrderType.Stay : OrderType.Follow;
            if(pet.ControlOrder!=order) { pet.ControlOrder=order; }
        }
    }
    internal void Park()
    {
        if(Pet?.Deleted!=false || Pet.ControlMaster!=Companion) { return; }
        if(Pet is BaseMount { Rider: not null }) { return; }
        Pet.Combatant=null;Pet.ControlOrder=OrderType.Stay;Pet.Internalize();Parked=true;
    }
    internal bool ClaimBack(Mobile owner)
    {
        if(Deleted || owner==null || owner!=Owner || Companion?.Deleted!=false || Companion.BoundOwner!=owner || Pet?.ControlMaster!=Companion || Pet.IsDeadPet ||
            owner.Map!=Companion.Map || !owner.InRange(Companion,12) || !owner.Alive) { return false; }
        var claim=HavenTamedPetClaim.Store(Pet,owner,Companion.Backpack);
        if(claim==null) { return false; }
        Pet=null;Delete();owner.SendMessage("Your pet's claim is in the companion pack.");return true;
    }
    public override void OnDelete()
    {
        var pet=Pet;var owner=Owner;var companion=Companion;Pet=null;Owner=null;Companion=null;
        if(pet?.Deleted==false && pet.ControlMaster==companion && owner?.Deleted==false)
        {
            if(pet is BaseMount mount && mount.Rider==companion) { mount.Rider=null; }
            pet.SetControlMaster(owner);pet.ControlTarget=owner;pet.ControlOrder=OrderType.Follow;
            if(owner.Map!=null && owner.Map!=Map.Internal) { pet.MoveToWorld(owner.Location,owner.Map); }
        }
        base.OnDelete();
    }
}
public partial class HavenCompanion
{
    internal bool AcceptAssignedPet(Mobile owner,BaseCreature pet)
    {
        if(Deleted || IsDeadPet || BoundOwner!=owner || owner?.Alive!=true || Backpack==null || pet?.Deleted!=false || pet==this || pet is HavenCompanion ||
            pet.ControlMaster!=owner || pet.IsDeadPet || pet.Summoned || pet.Body.IsHuman || owner.Map!=Map || pet.Map!=Map ||
            !InRange(owner,3) || !InRange(pet,3) || !InLOS(pet) || pet.Combatant!=null || Combatant!=null ||
            pet is BaseMount { Rider: not null } || Followers+pet.ControlSlots>FollowersMax ||
            Skills.AnimalTaming.Value<pet.MinTameSkill || Skills.AnimalLore.Value<pet.MinTameSkill) { return false; }
        if(!pet.SetControlMaster(this)) { return false; }
        if(!pet.Owners.Contains(this)) { pet.Owners.Add(this); }
        var record=new HavenCompanionAssignedPet { Companion=this,Owner=owner,Pet=pet };
        Backpack.DropItem(record);record.Tick();
        owner.SendMessage("I will use this pet in combat, or ride it if it is a mount. Use [companionpets to manage or reclaim it.");return true;
    }
    internal void ThinkAssignedPets()
    {
        if(Backpack==null) { return; }
        for(var i=Backpack.Items.Count-1;i>=0;i--)
        { if(Backpack.Items[i] is HavenCompanionAssignedPet record) { record.Tick(); } }
    }
    internal void ParkAssignedPets()
    {
        if(Backpack==null) { return; }
        foreach(var item in Backpack.Items) { if(item is HavenCompanionAssignedPet record) { record.Park(); } }
    }
}
