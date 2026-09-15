using Server;
using Server.Items;

namespace Server.HavenPrototype {


public class HavenChampionPendant : GoldNecklace
{
    public HavenChampionPendant(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    [Constructable]
    public HavenChampionPendant()
    {
        Name = "Haven champion's pendant";
        Hue = 0x489; LootType = LootType.Blessed;
        Attributes.Luck = 1000;
        Attributes.BonusStr = Attributes.BonusDex = Attributes.BonusInt = 20;
        Attributes.RegenHits = Attributes.RegenStam = Attributes.RegenMana = 5;
        Attributes.WeaponDamage = Attributes.SpellDamage = 40;
        Attributes.AttackChance = Attributes.DefendChance = 15;
        Attributes.WeaponSpeed = 10;
        Attributes.CastSpeed = 1;
        Attributes.CastRecovery = 3;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 100;
    }
}

}