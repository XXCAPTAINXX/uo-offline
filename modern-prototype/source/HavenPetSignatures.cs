using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.HavenPrototype
{

public static class HavenPetSignatures
{
    internal static int Clamp(int value,int min,int max){return Math.Max(min,Math.Min(max,value));}
    internal static int Kind(BaseCreature pet) { if(pet==null)return 0;switch(pet.GetType().Name){case "HavenEmberwing":return 1;case "HavenMoonfang":return 2;case "HavenFrostmane":return 3;case "HavenVerdantLlama":return 4;case "HavenStormscale":return 5;case "HavenStormhorn":return 6;case "HavenSnowBear":return 7;case "HavenAncientHellhound":return 8;case "VampiricSteed":return 9;case "HavenChelonian":return 10;default:return 0;}}
    internal static int Tier(BaseCreature pet){return HavenPetDefenses.Tier(pet);}
    public static string Describe(BaseCreature pet){switch(Kind(pet)){
case 1:return "Cinderwake — ranged fire patch, 6 seconds; burns engaged enemies standing in it. Epic+ increases its radius. Cooldown 18s.";
case 2:return "Moon Hunt — marks prey for 8s, lowering physical resistance by 5–14. Extra physical damage below one-third health. Marks do not stack. Cooldown 12s.";
case 3:return "Winter's Grasp — cold strike and 6s attack-speed chill, reducing Dexterity and stamina. Epic/Legendary also chill 1/2 engaged enemies. Cooldown 14s.";
case 4:return "Sanctuary Grove — heals owner and owned pets within 3 tiles over 8s, without needing melee. Epic cures up to regular poison; Legendary up to greater. Cooldown 18s.";
case 5:return "Ranged hunter — holds 4–6 tiles, firing lightning every 3s. Chain Tempest jumps between 2–5 engaged enemies, losing 25% damage each jump. Chain cooldown 14s.";
case 6:return "Arcane Reservoir — spends 15 mana to restore 12–27 owner mana. In melee, siphons enemy mana instead. Legendary siphons can interrupt spells using normal interruption rules. Cooldown 10s.";
case 7:return "Guardian Roar — draws a vulnerable enemy off its owner and reduces incoming melee damage by 12–24% for 6s. Works alongside Colossal Rage. Cooldown 18s.";
case 8:return "Ashen Wound — ranged fire strike suppresses enemy healing for 3–6s. Keeps innate self/owner Healing and fire breath. Cooldown 18s.";
case 9:return "Sanguine Rescue — physical life drain heals its owner below half health, otherwise the steed. Healing cannot exceed actual damage dealt. Cooldown 12s.";
case 10:return "Tidal Jet — cold strike at range 6, drains 8–20 stamina; 12s cooldown. Living Shell reduces melee damage below half health. Amphibious with cargo.";
default:return "This species keeps its native abilities.";
}}
    internal static bool Active(BaseCreature pet) {var mount=pet as BaseMount;return pet!=null&&!pet.Deleted&&Kind(pet)!=0&&pet.Controlled&&!pet.Summoned&&!pet.IsDeadPet&&pet.Alive&&!pet.Frozen&&!pet.Paralyzed&&pet.ControlMaster!=null&&!pet.ControlMaster.Deleted&&pet.ControlMaster.Alive&&pet.Map!=null&&pet.Map!=Map.Internal&&pet.ControlMaster.Map==pet.Map&&pet.InRange(pet.ControlMaster,18)&&(mount==null||mount.Rider==null);}
    internal static bool Enemy(BaseCreature pet,Mobile target,bool secondary=false){var creature=target as BaseCreature;return Active(pet)&&creature!=null&&!creature.Deleted&&creature.Alive&&!creature.Controlled&&!creature.Summoned&&!creature.Blessed&&!(creature is BaseVendor)&&!creature.IsDeadPet&&creature.Owners.Count==0&&target.Map==pet.Map&&Math.Abs(pet.Z-target.Z)<=16&&pet.CanSee(target)&&pet.InLOS(target)&&pet.CanBeHarmful(target,false)&&pet.ControlMaster.CanBeHarmful(target,false)&&(!secondary||target.Combatant==pet||target.Combatant==pet.ControlMaster||pet.Combatant==target||pet.ControlMaster.Combatant==target);}
    internal static List<BaseCreature> NearbyCreatures(Map map,Point3D point,int radius){var result=new List<BaseCreature>();var mobiles=map.GetMobilesInRange(point,radius);try{foreach(Mobile m in mobiles)if(m is BaseCreature)result.Add((BaseCreature)m);}finally{mobiles.Free();}return result;}
    internal static HavenPetSignatureState Find(BaseCreature pet) => pet==null||pet.Backpack==null?null:pet.Backpack.FindItemByType(typeof(HavenPetSignatureState),true) as HavenPetSignatureState;
    internal static HavenPetSignatureState Ensure(BaseCreature pet)
    {
        var state=Find(pet);
        if(state==null)
        {
            if(pet.Backpack==null) { pet.AddItem(new Backpack()); }
            state=new HavenPetSignatureState();pet.Backpack.DropItem(state);
        }
        return state;
    }
    internal static bool Ready(BaseCreature pet) => Find(pet)?.Next > DateTime.UtcNow ? false : true;
    public static void OnAttack(BaseCreature pet,Mobile target) => Activate(pet,target);
    public static void Think(BaseCreature pet)
    {
        if(Kind(pet)==0) { return; }

        if(!Active(pet)) { Find(pet)?.Clear();return; }

        HavenPetAppearance.Shimmer(pet);
        if(!Ready(pet)) { return; }
        if((Kind(pet)==4 || Kind(pet)==6)) { Support(pet);return; }
        if((Kind(pet)==1||Kind(pet)==5||Kind(pet)==8||Kind(pet)==10) && pet.ControlOrder!=OrderType.Stay && pet.ControlOrder!=OrderType.Stop && pet.Combatant is Mobile enemy)
        { Activate(pet,enemy); }
    }
    internal static bool Support(BaseCreature pet)
    {
        if(!Active(pet) || !Ready(pet)) { return false; }
        var owner=pet.ControlMaster;var tier=Tier(pet);
        if(!pet.InRange(owner,12) || !pet.InLOS(owner)) { return false; }
        if(pet is HavenVerdantLlama)
        {
            if(!NeedsGrove(pet,owner)) { return false; }
            var state=Ensure(pet);state.Next=DateTime.UtcNow+TimeSpan.FromSeconds(18);state.ClearField();
            state.Field=new HavenSignatureField(pet,owner,true,tier);state.Field.MoveToWorld(owner.Location,owner.Map);state.Field.Start();return true;
        }
        if(pet is HavenStormhorn && owner.ManaMax-owner.Mana>=8 && pet.Mana>=15)
        {
            var state=Ensure(pet);state.Next=DateTime.UtcNow+TimeSpan.FromSeconds(10);pet.Mana-=15;
            owner.Mana=Math.Min(owner.ManaMax,owner.Mana+12+tier*5);owner.FixedParticles(0x375A,10,15,5017,EffectLayer.Waist);return true;
        }
        return false;
    }
    private static bool NeedsGrove(BaseCreature pet,Mobile owner)
    {
        if(owner.Hits<owner.HitsMax || owner.Poisoned) { return true; }
        foreach(var ally in NearbyCreatures(owner.Map,owner.Location,3))
        { if(ally.Controlled && ally.ControlMaster==owner && !ally.IsDeadPet && ally.Alive && (ally.Hits<ally.HitsMax || ally.Poisoned)) { return true; } }
        return false;
    }
    internal static bool Activate(BaseCreature pet,Mobile target)
    {
        if(!Enemy(pet,target) || !Ready(pet)) { return false; }
        var kind=Kind(pet);var tier=Tier(pet);var ranged=(kind==1||kind==5||kind==8||kind==10);
        if(!pet.InRange(target,ranged ? 6 : 2)) { return false; }
        if(kind==4) { return Support(pet); }
        if(kind==6 && target.Mana<=0) { return Support(pet); }
        if(kind==2 && !HavenPetHex.Apply(pet,(BaseCreature)target,0,5+3*tier,8)) { return false; }
        var state=Ensure(pet);var seconds=(kind==1||kind==4||kind==7||kind==8) ? 18 : (kind==3||kind==5) ? 14 : kind==6 ? 10 : 12;
        state.Next=DateTime.UtcNow+TimeSpan.FromSeconds(seconds);pet.DoHarmful(target);
        switch(kind)
        {
            case 1:
                state.ClearField();state.Field=new HavenSignatureField(pet,pet.ControlMaster,false,tier);
                state.Field.MoveToWorld(target.Location,target.Map);state.Field.Start();break;
            case 2:
                target.FixedParticles(0x375A,10,15,5017,EffectLayer.Head);
                if(target.Hits*3<target.HitsMax) { Damage(pet,target,12+tier*6,ResistanceType.Physical); }
                break;
            case 3:
                Chill(pet,(BaseCreature)target,tier);var remaining=Math.Max(0,tier-1);
                foreach(var nearby in Nearby(pet,target,3))
                { if(remaining--<=0) { break; } Chill(pet,nearby,tier); }
                break;
            case 5:
                Chain(pet,target,tier);break;
            case 6:
                var mana=Math.Min(target.Mana,12+tier*5);target.Mana-=mana;
                var owner=pet.ControlMaster;
                if(pet.InRange(owner,12) && pet.InLOS(owner)) { owner.Mana=Math.Min(owner.ManaMax,owner.Mana+mana); }
                if(tier==3 && target.Spell is Spell spell && spell.IsCasting && target is BaseCreature && !((BaseCreature)target).BardImmune)
                { spell.Disturb(DisturbType.Hurt,false,true); }
                target.FixedParticles(0x374A,10,15,5013,EffectLayer.Head);break;
            case 7:
                state.GuardUntil=DateTime.UtcNow+TimeSpan.FromSeconds(6);
                if(target is BaseCreature victim && !victim.BardImmune && victim.Combatant==pet.ControlMaster) { victim.Combatant=pet; }
                pet.PlaySound(0xA6);pet.FixedParticles(0x376A,10,15,5017,EffectLayer.Waist);break;
            case 8:
                Damage(pet,target,18+tier*6,ResistanceType.Fire);
                if(!target.Deleted && target.Alive && !MortalStrike.IsWounded(target)) { MortalStrike.BeginWound(target,TimeSpan.FromSeconds(3+tier)); }
                break;
            case 10:
                Damage(pet,target,20+tier*6,ResistanceType.Cold);
                if(!target.Deleted && target.Alive) { target.Stam=Math.Max(0,target.Stam-8-tier*4); target.FixedEffect(0x374A,10,12); }
                break;
            case 9:
                var dealt=Damage(pet,target,12+tier*5,ResistanceType.Physical);var patient=pet.ControlMaster;
                if(patient.Hits*2>=patient.HitsMax || !pet.InRange(patient,12) || !pet.InLOS(patient)) { patient=pet; }
                if(dealt>0 && !patient.Poisoned && !MortalStrike.IsWounded(patient)) { patient.Heal(dealt,pet); }
                break;
        }
        return true;
    }
    private static void Chill(BaseCreature pet,BaseCreature target,int tier)
    {
        pet.DoHarmful(target);Damage(pet,target,12+tier*4,ResistanceType.Cold);
        if(target.Deleted || !target.Alive) { return; }
        if(HavenPetHex.Apply(pet,target,1,Clamp(target.RawDex/5+tier*3,5,30),6)) { target.Stam=Math.Max(0,target.Stam-10-tier*5); }
    }
    private static List<BaseCreature> Nearby(BaseCreature pet,Mobile center,int radius)
    {
        var list=new List<BaseCreature>();
        foreach(var enemy in NearbyCreatures(center.Map,center.Location,radius))
        { if(enemy!=center && Enemy(pet,enemy,true) && center.InLOS(enemy)) { list.Add(enemy); } }
        return list;
    }
    private static void Chain(BaseCreature pet,Mobile first,int tier)
    {
        var hit=new HashSet<Mobile>();var current=first;var damage=28+tier*6;
        for(var jump=0;jump<2+tier && current!=null;jump++)
        {
            hit.Add(current);pet.DoHarmful(current);current.BoltEffect(0);
            // Capture neighbours before a killing hit deletes the current mobile.
            var candidates=Nearby(pet,current,4);Damage(pet,current,damage,ResistanceType.Energy);damage=damage*3/4;
            current=null;
            foreach(var next in candidates) { if(!hit.Contains(next)) { current=next;break; } }
        }
    }
    internal static int Damage(BaseCreature pet,Mobile target,int amount,ResistanceType type)
    {
        var before=target.Hits;
        AOS.Damage(target,pet,amount,type==ResistanceType.Physical ? 100 : 0,type==ResistanceType.Fire ? 100 : 0,type==ResistanceType.Cold ? 100 : 0,0,type==ResistanceType.Energy ? 100 : 0);
        return Clamp(before-Math.Max(0,target.Hits),0,amount);
    }
    internal static int GuardPercent(BaseCreature pet) => Active(pet) && Kind(pet)==7 && Find(pet)?.GuardUntil>DateTime.UtcNow ? 12+Tier(pet)*4 : 0;
    public static void AddProperties(BaseCreature pet,ObjectPropertyList list)
    {
        if(Kind(pet)!=0) { list.Add($"{"Species signature:"} {Describe(pet)}"); }
        HavenPetDefenses.AddProperties(pet,list);HavenLegendaryPetSkills.AddProperties(pet,list);
    }
}


public partial class HavenPetSignatureState : Item
{
    internal DateTime Next,GuardUntil,NextShimmer;
    internal HavenSignatureField Field;
    [Constructable] public HavenPetSignatureState():base(1) { Name="pet signature state";Visible=false;Movable=false;Weight=0; }

    internal void ClearField() { Field?.Delete();Field=null; }
    internal void Clear() { ClearField();GuardUntil=DateTime.MinValue; }
    public override void OnDelete() { Clear();base.OnDelete(); }
    public HavenPetSignatureState(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}
}
