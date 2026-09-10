using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.UOOffline;

internal static class HavenRarePetAbility
{
    internal static void Skills(BaseCreature pet, double cap)
    {
        foreach (var name in new[] { SkillName.Wrestling, SkillName.Tactics, SkillName.MagicResist })
        { pet.Skills[name].Cap = cap; pet.Skills[name].Base = cap; }
    }
}

[SerializationGenerator(0)]
public partial class HavenEmberwing : ForestOstard
{

    [Constructible]
    public HavenEmberwing()
    {
        Name = "an emberwing ostard"; Hue = 0x489; MinTameSkill = 65; ControlSlots = 2;
        SetStr(180); SetDex(120); SetInt(80); SetHits(220); SetDamage(8, 12);
        HavenRarePetAbility.Skills(this, 100);
    }
}

[SerializationGenerator(0)]
public partial class HavenMoonfang : DireWolf
{

    [Constructible]
    public HavenMoonfang()
    {
        Name = "a moonfang wolf"; Hue = 0x47E; MinTameSkill = 95; ControlSlots = 2;
        SetStr(250); SetDex(150); SetInt(150); SetHits(350); SetDamage(10, 16);
        HavenRarePetAbility.Skills(this, 110);
    }
}

[SerializationGenerator(0)]
public partial class HavenStormscale : Drake
{

    [Constructible]
    public HavenStormscale()
    {
        Name = "a stormscale drake"; Hue = 0x482; MinTameSkill = 110; ControlSlots = 3;
        SetStr(450); SetDex(170); SetInt(250); SetHits(550); SetDamage(14, 20);
        HavenRarePetAbility.Skills(this, 120);
    }
}
