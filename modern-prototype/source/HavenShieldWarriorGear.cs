using System;
using Server;
using Server.Items;
namespace Server.HavenPrototype {
public interface IHavenShieldWarriorGear { int Tier {get;} }
public static class HavenShieldWarriorGear {
public static readonly string[] Tiers={"Recruit","Warden","Dreadnought"};
public static readonly string[] Pieces={"bulwark shield","arming sword","war mace","guard's gorget","duelist's ring"};
    internal static void Outfit(Item item, int tier)
    {
        tier=Math.Max(0,Math.Min(2,tier));item.Hue=new[]{0x8A5,0x59B,0x482}[tier];item.LootType=LootType.Blessed;
        var a=HavenAdvancedGear.Attributes(item);
        if(item is BaseWeapon weapon)
        {
            a.WeaponDamage=20+tier*20;a.WeaponSpeed=10+tier*10;
            weapon.WeaponAttributes.HitLowerDefend=10+tier*20;weapon.WeaponAttributes.HitLowerAttack=tier*20;
            weapon.WeaponAttributes.HitLeechMana=15+tier*20;weapon.WeaponAttributes.HitLeechHits=15+tier*15;
            weapon.WeaponAttributes.HitPhysicalArea=tier*20;weapon.WeaponAttributes.SelfRepair=3;
            if(tier==2){weapon.Slayer=SlayerName.Repond;weapon.Slayer2=SlayerName.Silver;}
        }
        else if(item is BaseShield shield)
        {
            a.DefendChance=5+tier*7;a.AttackChance=5+tier*5;a.BonusStam=5+tier*5;a.LowerManaCost=2+tier*3;
            a.RegenHits=1+tier;shield.SkillBonuses.SetValues(0,SkillName.Parry,5+tier*5);
            shield.ArmorAttributes.SelfRepair=3;
        }
        else if(item is BaseArmor armor)
        {
            a.BonusHits=5+tier*5;a.BonusStam=5+tier*5;a.LowerManaCost=4+tier*2;
            a.RegenStam=2+tier;a.RegenMana=1+tier;armor.ArmorAttributes.SelfRepair=3;
            armor.PhysicalBonus=5+tier*4;armor.FireBonus=armor.ColdBonus=armor.PoisonBonus=armor.EnergyBonus=3+tier*3;
        }
        else if(item is BaseJewel jewel)
        {
            a.AttackChance=5+tier*5;a.DefendChance=5+tier*5;a.WeaponSpeed=5+tier*5;
            jewel.SkillBonuses.SetValues(0,SkillName.Swords,5+tier*5);
            jewel.SkillBonuses.SetValues(1,SkillName.Macing,5+tier*5);
            jewel.SkillBonuses.SetValues(2,SkillName.Tactics,5+tier*5);
        }
    }
public static Item Create(int tier,int piece){switch(piece){case 0:return new HavenBulwarkShield(tier);case 1:return new HavenBasherSword(tier);case 2:return new HavenBasherMace(tier);case 3:return new HavenBasherGorget(tier);default:return new HavenBasherRing(tier);}}
}
public class HavenBulwarkShield:HeaterShield,IHavenShieldWarriorGear {
 public int Tier {get;private set;}
 [Constructable] public HavenBulwarkShield():this(0){}
 [Constructable] public HavenBulwarkShield(int tier){Tier=Math.Max(0,Math.Min(2,tier));Name=HavenShieldWarriorGear.Tiers[Tier]+" "+HavenShieldWarriorGear.Pieces[0];HavenShieldWarriorGear.Outfit(this,Tier);}
 public HavenBulwarkShield(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Tier);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Tier=r.ReadInt();}
}
public class HavenBasherSword:Longsword,IHavenShieldWarriorGear {
 public int Tier {get;private set;}
 [Constructable] public HavenBasherSword():this(0){}
 [Constructable] public HavenBasherSword(int tier){Tier=Math.Max(0,Math.Min(2,tier));Name=HavenShieldWarriorGear.Tiers[Tier]+" "+HavenShieldWarriorGear.Pieces[1];HavenShieldWarriorGear.Outfit(this,Tier);}
 public HavenBasherSword(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Tier);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Tier=r.ReadInt();}
}
public class HavenBasherMace:WarMace,IHavenShieldWarriorGear {
 public int Tier {get;private set;}
 [Constructable] public HavenBasherMace():this(0){}
 [Constructable] public HavenBasherMace(int tier){Tier=Math.Max(0,Math.Min(2,tier));Name=HavenShieldWarriorGear.Tiers[Tier]+" "+HavenShieldWarriorGear.Pieces[2];HavenShieldWarriorGear.Outfit(this,Tier);}
 public HavenBasherMace(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Tier);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Tier=r.ReadInt();}
}
public class HavenBasherGorget:PlateGorget,IHavenShieldWarriorGear {
 public int Tier {get;private set;}
 [Constructable] public HavenBasherGorget():this(0){}
 [Constructable] public HavenBasherGorget(int tier){Tier=Math.Max(0,Math.Min(2,tier));Name=HavenShieldWarriorGear.Tiers[Tier]+" "+HavenShieldWarriorGear.Pieces[3];HavenShieldWarriorGear.Outfit(this,Tier);}
 public HavenBasherGorget(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Tier);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Tier=r.ReadInt();}
}
public class HavenBasherRing:GoldRing,IHavenShieldWarriorGear {
 public int Tier {get;private set;}
 [Constructable] public HavenBasherRing():this(0){}
 [Constructable] public HavenBasherRing(int tier){Tier=Math.Max(0,Math.Min(2,tier));Name=HavenShieldWarriorGear.Tiers[Tier]+" "+HavenShieldWarriorGear.Pieces[4];HavenShieldWarriorGear.Outfit(this,Tier);}
 public HavenBasherRing(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Tier);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Tier=r.ReadInt();}
}
}
