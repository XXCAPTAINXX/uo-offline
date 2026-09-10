using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

// Temporary combat effects deliberately expire on restart.
[SerializationGenerator(0)]
public partial class HavenSignatureField : BaseAddon
{
    private BaseCreature _pet;
    private Mobile _owner;
    private bool _grove;
    private int _tier,_ticks;
    private Timer _timer;
    public override BaseAddonDeed Deed=>null;
    [Constructible] public HavenSignatureField():this(null,null,false,0) { }
    internal HavenSignatureField(BaseCreature pet,Mobile owner,bool grove,int tier)
    {
        _pet=pet;_owner=owner;_grove=grove;_tier=tier;
        Name=grove ? "Sanctuary Grove" : "Cinderwake";Movable=false;
        var radius=grove ? 1 : tier>=2 ? 2 : 1;
        for(var x=-radius;x<=radius;x++)
        {
            for(var y=-radius;y<=radius;y++)
            { AddComponent(new AddonComponent(grove ? 0xC85 : 0x122A) { Hue=grove ? 0x59B : 0x489,Name=Name },x,y,0); }
        }
    }
    [AfterDeserialization] private void Expire() => Delete();
    internal void Start() { _timer?.Stop();_timer=Timer.DelayCall(TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(2),Tick); }
    internal void Tick()
    {
        if(Deleted) { return; }
        if(!HavenPetSignatures.Active(_pet) || _pet.ControlMaster!=_owner || _pet.Map!=Map || !_pet.InRange(this,12)) { Delete();return; }
        if(_grove)
        {
            using var patients=PooledRefList<Mobile>.Create();
            if(_owner.InRange(this,3)) { patients.Add(_owner); }
            foreach(var pet in Map.GetMobilesInRange<BaseCreature>(Location,3))
            { if(pet.Controlled && pet.ControlMaster==_owner && pet.Alive && !pet.IsDeadPet) { patients.Add(pet); } }
            foreach(var patient in patients)
            {
                if(patient.Deleted || Math.Abs(patient.Z-Z)>16 || !_pet.InLOS(patient)) { continue; }
                if(patient.Poisoned && _tier>=2 && patient.Poison.Level<=(_tier==3 ? 2 : 1)) { patient.CurePoison(_pet); }
                if(!patient.Poisoned && !MortalStrike.IsWounded(patient)) { patient.Heal(4+_tier*2,_pet); }
            }
        }
        else
        {
            using var enemies=PooledRefList<BaseCreature>.Create();
            foreach(var enemy in Map.GetMobilesInRange<BaseCreature>(Location,_tier>=2 ? 2 : 1)) { enemies.Add(enemy); }
            foreach(var enemy in enemies)
            {
                if(!HavenPetSignatures.Enemy(_pet,enemy,true) || Math.Abs(enemy.Z-Z)>16) { continue; }
                _pet.DoHarmful(enemy);HavenPetSignatures.Damage(_pet,enemy,8+_tier*4,ResistanceType.Fire);
            }
        }
        if(++_ticks>=(_grove ? 4 : 3)) { Delete(); }
    }
    public override void OnAfterDelete()
    {
        _timer?.Stop();_timer=null;
        var state=HavenPetSignatures.Find(_pet);if(state?.Field==this) { state.Field=null; }
        _pet=null;_owner=null;base.OnAfterDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenPetHex : Item
{
    private BaseCreature _pet,_target;
    private Mobile _owner;
    private int _kind;
    private DateTime _expires;
    private Timer _timer;
    private ResistanceMod _resistance;
    private StatMod _stat;
    [Constructible] public HavenPetHex():base(1) { Name="temporary pet effect";Visible=false;Movable=false;Weight=0; }
    public override bool IsVirtualItem=>true;
    [AfterDeserialization] private void Expire() => Delete();
    internal static bool Apply(BaseCreature pet,BaseCreature target,int kind,int power,int seconds)
    {
        if(!HavenPetSignatures.Enemy(pet,target)) { return false; }
        if(target.Backpack==null) { target.AddItem(new Backpack()); }
        HavenPetHex previous=null;
        foreach(var existing in target.Backpack.FindItemsByType<HavenPetHex>())
        { if(existing._kind==kind) { previous=existing;break; } }
        if(previous!=null)
        { if(Core.Now<previous._expires) { return false; } previous.Delete(); }
        var record=new HavenPetHex { _pet=pet,_target=target,_owner=pet.ControlMaster,_kind=kind,_expires=Core.Now+TimeSpan.FromSeconds(seconds) };
        target.Backpack.DropItem(record);
        if(kind==0)
        { record._resistance=new ResistanceMod(ResistanceType.Physical,"Haven Moon Hunt",-power);target.AddResistanceMod(record._resistance); }
        else
        { record._stat=new StatMod(StatType.Dex,"Haven Winter Grasp",-power,TimeSpan.FromSeconds(seconds));target.AddStatMod(record._stat); }
        record._timer=Timer.DelayCall(TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(1),record.Tick);return true;
    }
    internal void Tick()
    {
        if(Core.Now>=_expires || !HavenPetSignatures.Active(_pet) || _pet.ControlMaster!=_owner || _target?.Deleted!=false ||
            !_target.Alive || _target.Controlled || _target.Map!=_pet.Map || !_target.InRange(_pet,18)) { Delete(); }
    }
    public override void OnDelete()
    {
        _timer?.Stop();_timer=null;
        if(_target!=null)
        {
            if(_resistance!=null) { _target.RemoveResistanceMod(_resistance); }
            if(_stat!=null && _target.GetStatMod(_stat.Name)==_stat) { _target.RemoveStatMod(_stat.Name); }
        }
        _resistance=null;_stat=null;_target=null;_pet=null;_owner=null;base.OnDelete();
    }
}
