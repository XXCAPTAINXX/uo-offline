using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenFrostmane : Horse
{

    [Constructible]
    public HavenFrostmane()
    {
        Name = "a frostmane steed"; Hue = 0x47F; MinTameSkill = 70; ControlSlots = 2;
        SetStr(220); SetDex(140); SetInt(100); SetHits(260); SetDamage(9, 14);
        SetResistance(ResistanceType.Cold, 70);
        HavenRarePetAbility.Skills(this, 100);
    }
}

[SerializationGenerator(0)]
public partial class HavenVerdantLlama : RidableLlama
{

    [Constructible]
    public HavenVerdantLlama()
    {
        Name = "a verdant llama"; Hue = 0x59B; MinTameSkill = 80; ControlSlots = 2;
        SetStr(240); SetDex(140); SetInt(160); SetHits(320); SetDamage(9, 14);
        HavenRarePetAbility.Skills(this, 110);
    }
}

[SerializationGenerator(0)]
public partial class HavenStormhorn : Kirin
{

    [Constructible]
    public HavenStormhorn()
    {
        Name = "a stormhorn kirin"; Hue = 0x482; MinTameSkill = 105; ControlSlots = 3;
        SetStr(400); SetDex(170); SetInt(350); SetHits(450); SetDamage(12, 18);
        HavenRarePetAbility.Skills(this, 120);
        Skills.Magery.Cap = Skills.EvalInt.Cap = 120;
        Skills.Magery.Base = Skills.EvalInt.Base = 110;
    }
    public override bool AllowFemaleRider => true;
}
