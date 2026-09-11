using System;
using Server;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype {

public partial class HavenCompanionAssignedPet : Item
{
    public HavenCompanion Companion;
    public Mobile Owner;
    public BaseCreature Pet;
    public bool AutoMount=true;
    public bool Parked;

    [Constructable] public HavenCompanionAssignedPet() : base(1) { Visible=false;Movable=false;Weight=0;Name="Companion pet assignment"; }
    public HavenCompanionAssignedPet(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Companion);w.Write(Owner);w.Write(Pet);w.Write(AutoMount);w.Write(Parked);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Companion=r.ReadMobile() as HavenCompanion;Owner=r.ReadMobile();Pet=r.ReadMobile() as BaseCreature;AutoMount=r.ReadBool();Parked=r.ReadBool();}
    internal void Tick()
    {
        var companion=Companion;var pet=Pet;
        if(companion?.Deleted!=false) { Delete();return; }
        if(pet?.Deleted!=false || pet.ControlMaster!=companion) { Pet=null;Delete();return; }
        if(companion.Map==null || companion.Map==Map.Internal || companion.OnMission) { return; }
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
        if(Pet is BaseMount && ((BaseMount)Pet).Rider!=null) { return; }
        Pet.Combatant=null;Pet.ControlOrder=OrderType.Stay;Pet.Internalize();Parked=true;
    }
    internal bool ClaimBack(Mobile owner)
    {
        if(Deleted || owner==null || owner!=Owner || Companion?.Deleted!=false || Companion.BoundOwner!=owner || Pet?.ControlMaster!=Companion || Pet.IsDeadPet ||
            owner.Map!=Companion.Map || !owner.InRange(Companion,12) || !owner.Alive) { return false; }
        var claim=HavenPetTicket.Store(Pet,owner,Companion.Backpack);
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
            (pet is BaseMount && ((BaseMount)pet).Rider!=null) || Followers+pet.ControlSlots>FollowersMax ||
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

}
