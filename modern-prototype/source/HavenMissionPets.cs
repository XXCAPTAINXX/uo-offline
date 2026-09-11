using System;
using Server;
using Server.Mobiles;
namespace Server.HavenPrototype {
internal static class HavenRarePetAbility
{
    internal static void Skills(BaseCreature pet, double cap)
    {
        foreach (var name in new[] { SkillName.Wrestling, SkillName.Tactics, SkillName.MagicResist })
        { pet.Skills[name].Cap = cap; pet.Skills[name].Base = cap; }
    }
}

public class HavenEmberwing : ForestOstard
{
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}

    [Constructable]
    public HavenEmberwing()
    {
        Name = "an emberwing ostard"; Hue = 0x489; MinTameSkill = 65; ControlSlots = 2;
        SetStr(180); SetDex(120); SetInt(80); SetHits(220); SetDamage(8, 12);
        HavenRarePetAbility.Skills(this, 100);
    }
 public override int GetResistance(ResistanceType type){return HavenPetDefenses.Resistance(this,type,base.GetResistance(type));}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public override void OnThink(){base.OnThink();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public HavenEmberwing(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}

public class HavenMoonfang : DireWolf
{
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}

    [Constructable]
    public HavenMoonfang()
    {
        Name = "a moonfang wolf"; Hue = 0x47E; MinTameSkill = 95; ControlSlots = 2;
        SetStr(250); SetDex(150); SetInt(150); SetHits(350); SetDamage(10, 16);
        HavenRarePetAbility.Skills(this, 110);
    }
 public override void OnThink(){base.OnThink();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public HavenMoonfang(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}

public class HavenStormscale : Drake
{
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}
 protected override BaseAI ForcedAI {get{return AI==AIType.AI_Mage?null:new HavenStormscaleAI(this);}}

    [Constructable]
    public HavenStormscale()
    {
        Name = "a stormscale drake"; Hue = 0x482; MinTameSkill = 110; ControlSlots = 3;
        SetStr(450); SetDex(170); SetInt(250); SetHits(550); SetDamage(14, 20);
        HavenRarePetAbility.Skills(this, 120);
    }
 public override int GetResistance(ResistanceType type){return HavenPetDefenses.Resistance(this,type,base.GetResistance(type));}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public override void OnThink(){base.OnThink();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public HavenStormscale(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}

public class HavenFrostmane : Horse
{
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}

    [Constructable]
    public HavenFrostmane()
    {
        Name = "a frostmane steed"; Hue = 0x47F; MinTameSkill = 70; ControlSlots = 2;
        SetStr(220); SetDex(140); SetInt(100); SetHits(260); SetDamage(9, 14);
        SetResistance(ResistanceType.Cold, 70);
        HavenRarePetAbility.Skills(this, 100);
    }
 public override int GetResistance(ResistanceType type){return HavenPetDefenses.Resistance(this,type,base.GetResistance(type));}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public override void OnThink(){base.OnThink();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public HavenFrostmane(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}

public class HavenVerdantLlama : RidableLlama
{
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}

    [Constructable]
    public HavenVerdantLlama()
    {
        Name = "a verdant llama"; Hue = 0x59B; MinTameSkill = 80; ControlSlots = 2;
        SetStr(240); SetDex(140); SetInt(160); SetHits(320); SetDamage(9, 14);
        HavenRarePetAbility.Skills(this, 110);
    }
 public override void OnThink(){base.OnThink();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public HavenVerdantLlama(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}

public class HavenStormhorn : Kirin
{
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}

    [Constructable]
    public HavenStormhorn()
    {
        Name = "a stormhorn kirin"; Hue = 0x482; MinTameSkill = 105; ControlSlots = 3;
        SetStr(400); SetDex(170); SetInt(350); SetHits(450); SetDamage(12, 18);
        HavenRarePetAbility.Skills(this, 120);
        Skills.Magery.Cap = Skills.EvalInt.Cap = 120;
        Skills.Magery.Base = Skills.EvalInt.Base = 110;
    }
    public override bool AllowFemaleRider => true;
 public override void OnThink(){base.OnThink();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public HavenStormhorn(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}

}
