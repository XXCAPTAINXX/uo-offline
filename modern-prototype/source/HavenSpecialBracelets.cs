using Server;
using Server.Items;
namespace Server.HavenPrototype {

public class BraceletOfTheVanguard : GoldBracelet
{
    public BraceletOfTheVanguard(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfTheVanguard()
    {
        Name="bracelet of the vanguard";
        Hue = 0x972;
        Attributes.BonusStr = 5;
        Attributes.BonusHits = 5;
        Attributes.WeaponDamage = 10;
    }
}


public class BraceletOfArcaneFocus : GoldBracelet
{
    public BraceletOfArcaneFocus(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfArcaneFocus()
    {
        Name="bracelet of arcane focus";
        Hue = 0x482;
        Attributes.BonusInt = 5;
        Attributes.BonusMana = 8;
        Attributes.LowerManaCost = 5;
    }
}


public class BraceletOfTheWind : GoldBracelet
{
    public BraceletOfTheWind(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfTheWind()
    {
        Name="bracelet of the wind";
        Hue = 0x47F;
        Attributes.BonusDex = 5;
        Attributes.BonusStam = 8;
        Attributes.AttackChance = 5;
    }
}


public class BraceletOfTheBeastmaster : GoldBracelet
{
    public BraceletOfTheBeastmaster(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfTheBeastmaster()
    {
        Name="bracelet of the beastmaster";
        Hue = 0x59B;
        SkillBonuses.SetValues(0, SkillName.AnimalTaming, 5.0);
        SkillBonuses.SetValues(1, SkillName.AnimalLore, 5.0);
        SkillBonuses.SetValues(2, SkillName.Veterinary, 5.0);
    }
}


public class BraceletOfTheVirtuoso : GoldBracelet
{
    public BraceletOfTheVirtuoso(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfTheVirtuoso()
    {
        Name="bracelet of the virtuoso";
        Hue = 0x489;
        SkillBonuses.SetValues(0, SkillName.Musicianship, 5.0);
        SkillBonuses.SetValues(1, SkillName.Discordance, 3.0);
        SkillBonuses.SetValues(2, SkillName.Peacemaking, 3.0);
        SkillBonuses.SetValues(3, SkillName.Provocation, 3.0);
    }
}


public class BraceletOfTheArtisan : GoldBracelet
{
    public BraceletOfTheArtisan(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfTheArtisan()
    {
        Name="bracelet of the artisan";
        Hue = 0x96D;
        SkillBonuses.SetValues(0, SkillName.Blacksmith, 5.0);
        SkillBonuses.SetValues(1, SkillName.Tailoring, 5.0);
        SkillBonuses.SetValues(2, SkillName.Tinkering, 5.0);
    }
}


public class BraceletOfFortune : GoldBracelet
{
    public BraceletOfFortune(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfFortune()
    {
        Name="bracelet of fortune";
        Hue = 0x8A5;
        Attributes.Luck = 150;
    }
}


public class BraceletOfTheGuardian : GoldBracelet
{
    public BraceletOfTheGuardian(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfTheGuardian()
    {
        Name="bracelet of the guardian";
        Hue = 0x497;
        Attributes.DefendChance = 10;
        Attributes.RegenHits = 2;
        Resistances.Physical = 3;
        Resistances.Fire = 3;
        Resistances.Cold = 3;
        Resistances.Poison = 3;
        Resistances.Energy = 3;
    }
}


public class BraceletOfTheNight : GoldBracelet
{
    public BraceletOfTheNight(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    

    [Constructable]
    public BraceletOfTheNight()
    {
        Name="bracelet of the night";
        Hue = 0x455;
        Attributes.NightSight = 1;
        Attributes.RegenHits = 2;
        Attributes.LowerRegCost = 10;
        Attributes.BonusHits = 5;
    }
}

}