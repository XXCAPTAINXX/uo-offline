using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.ContextMenus;
using System.Collections.Generic;
namespace Server.HavenPrototype {
public partial class HavenSnowBear : BaseMount {
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}
 DateTime _rageUntil,_nextRage;
[Constructable]
    public HavenSnowBear() : base("a frostbound bear",0xD5,0x3EC5,AIType.AI_Melee,FightMode.Aggressor,10,1,0.2,0.4)
    {
        Name = "a frostbound bear"; BaseSoundID = 0xA3;
        SetStr(450, 550); SetDex(180, 210); SetInt(100, 150); SetHits(600, 750);
        SetDamage(17, 23); SetDamageType(ResistanceType.Physical, 50); SetDamageType(ResistanceType.Cold, 50);
        SetResistance(ResistanceType.Physical, 50, 60); SetResistance(ResistanceType.Fire, 25, 35);
        SetResistance(ResistanceType.Cold, 65, 75); SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 45, 55);
        HavenRarePetAbility.Skills(this, 105);
        Skills.Anatomy.Base = 90;
        EnsureNativeHealing();
        Tamable = true; MinTameSkill = 100; ControlSlots = 3; Fame = 8000; Karma = 0;
    }
    public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish | FoodType.FruitsAndVegies;
    public override int Meat => 4;
    public override int Hides => 15;
    public override bool StatLossAfterTame => false;
    internal bool Raging => DateTime.UtcNow < _rageUntil;
    internal bool TryRage()
    {
        if (Deleted || !Alive || IsDeadPet || Rider != null || Hits <= 0 || Hits * 2 >= HitsMax || DateTime.UtcNow < _nextRage) { return false; }
        _rageUntil = DateTime.UtcNow + TimeSpan.FromSeconds(10); _nextRage = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        PlaySound(0xA6); FixedParticles(0x375A, 10, 15, 5017, EffectLayer.Waist);
        Emote("*roars with colossal rage*"); return true;
    }
    public override void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
        base.AlterMeleeDamageTo(to, ref damage);
        TryRage();
        if (Raging) { damage += damage / 2; }
    }
    public override void AlterMeleeDamageFrom(Mobile from,ref int damage)
    {
        base.AlterMeleeDamageFrom(from,ref damage);
        damage=damage*(100-HavenPetSignatures.GuardPercent(this))/100;
    }

 public override int GetResistance(ResistanceType type){int tier=HavenPetDefenses.Tier(this);return Math.Max(base.GetResistance(type),type==ResistanceType.Physical?65+tier*5:type==ResistanceType.Cold?75+tier*5:0);}
 public override void OnThink(){if(Rider!=null)return;base.OnThink();SupportHealing();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public HavenSnowBear(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(1);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);int version=r.ReadInt();if(version<1)EnsureNativeHealing();}
}
public partial class HavenAncientHellhound : HellHound, IMount {
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}
[Constructable]
    public HavenAncientHellhound()
    {
        RangePerception=12;Name = "an ancient hellhound"; Body = 1069; Hue = 0;
        SetStr(450, 550); SetDex(180, 210); SetInt(180, 220); SetHits(500, 650);
        SetDamage(17, 23);
        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 70, 75);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 45, 55);
        HavenRarePetAbility.Skills(this, 110);
        Skills.Healing.Cap = 120; Skills.Healing.Base = 110;
        Skills.Anatomy.Cap = 120; Skills.Anatomy.Base = 100;
        ControlSlots = 3; MinTameSkill = 110; Tamable = true;
    }
 public override bool StatLossAfterTame {get{return false;}}
 DateTime _nextHeal; bool _healing;
 void Support(){if(!Controlled||IsDeadPet||!Alive||_healing||DateTime.UtcNow<_nextHeal)return;Mobile patient=ControlMaster;if(patient==null||!patient.Alive||patient.Map!=Map||!InRange(patient,12)||!InLOS(patient)||(!patient.Poisoned&&patient.Hits==patient.HitsMax))patient=this;if(!patient.Poisoned&&patient.Hits==patient.HitsMax)return;HealStart(patient);}
 public override void HealStart(Mobile patient){if(_healing||DateTime.UtcNow<_nextHeal||!Controlled||IsDeadPet||!Alive||patient==null||!patient.Alive||(patient!=this&&patient!=ControlMaster)||patient.Map!=Map||!InRange(patient,12)||!InLOS(patient))return;_healing=true;_nextHeal=DateTime.UtcNow.AddSeconds(8);Timer.DelayCall(TimeSpan.FromSeconds(2),()=>{_healing=false;if(Deleted||!Alive||IsDeadPet||!Controlled||patient.Deleted||!patient.Alive||(patient!=this&&patient!=ControlMaster)||patient.Map!=Map||!InRange(patient,12)||!InLOS(patient))return;base.Heal(patient);});}
 public override void OnThink(){if(Rider!=null)return;HavenHellhoundBreath.Ensure(this);base.OnThink();HavenHellhoundBreath.Think(this);HavenPetSignatures.Think(this);Support();}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public HavenAncientHellhound(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(1);w.Write(_rider);w.Write(_mountItem);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);int version=r.ReadInt();if(version>=1){_rider=r.ReadMobile();_mountItem=r.ReadItem() as HavenHellhoundMountItem;Timer.DelayCall(TimeSpan.Zero,ValidateRider);}}
}
public class VampiricSteed : BaseMount {
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}
 public static void LifeDrain(Mobile attacker,int damage){var pet=attacker as VampiricSteed;if(pet==null||damage<=0||pet.Hits>=pet.HitsMax||Utility.RandomDouble()>=0.30)return;pet.Heal(Math.Max(1,Math.Min(10,damage/4)));pet.FixedParticles(0x376A,9,32,5005,EffectLayer.Waist);}

[Constructable]
    public VampiricSteed() : base("a vampiric steed",0x74,0x3EA7,AIType.AI_Mage,FightMode.Aggressor,10,1,0.2,0.4)
    {
        BaseSoundID = 0xA8;
        Hue = 0x497;

        Body = 116;
        ItemID = 16039;

        SetStr(420, 470);
        SetDex(90, 110);
        SetInt(100, 135);

        SetHits(280, 330);
        SetDamage(14, 20);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 25);
        SetDamageType(ResistanceType.Energy, 25);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 25, 35);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 35, 45);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.EvalInt, 35.0, 55.0);
        SetSkill(SkillName.Magery, 35.0, 55.0);
        SetSkill(SkillName.MagicResist, 80.0, 95.0);
        SetSkill(SkillName.Tactics, 90.0, 100.0);
        SetSkill(SkillName.Wrestling, 82.0, 95.0);

        Fame = 12000;
        Karma = -12000;
        VirtualArmor = 50;

        Tamable = true;
        MinTameSkill = 95.1;
        ControlSlots = 2;
    }
    internal bool IsGentleIslandSteed => !Controlled && !Summoned && Map == Map.Trammel &&
        X >= 3314 && X < 3814 && Y >= 2345 && Y < 3095;

    public override int HitsMax => IsGentleIslandSteed ? Math.Min(120, base.HitsMax) : base.HitsMax;

    internal void UpdateIslandDifficulty()
    {
        if (Controlled && RawDex < 180)
        {
            SetDex(180, 210);
            Stam = StamMax;
        }
        var desiredAI = IsGentleIslandSteed ? AIType.AI_Melee : AIType.AI_Mage;
        if (AI != desiredAI) { AI = desiredAI; }
        if (Hits > HitsMax) { Hits = HitsMax; }
    }

    public override void OnThink()
    {
        UpdateIslandDifficulty();
        base.OnThink();HavenPetSignatures.Think(this);
    }

    public override void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
        base.AlterMeleeDamageTo(to, ref damage);
        if (IsGentleIslandSteed && damage > 0) { damage = Math.Max(1,Math.Min(8,damage*2/5)); }
    }

 public override FoodType FavoriteFood {get{return FoodType.Meat;}}
 public override bool CanAngerOnTame {get{return true;}}
 public override int Meat {get{return 3;}}
 public override int Hides {get{return 8;}}
 public override void GenerateLoot(){AddLoot(LootPack.Rich);AddLoot(LootPack.Average);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public VampiricSteed(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}
public class HavenChelonian : BaseCreature {
 public override TrainingDefinition TrainingDefinition {get{return HavenPetTrainingBridge.Definition(this);}}
[Constructable]
    public HavenChelonian() : base(AIType.AI_Melee,FightMode.Aggressor,10,1,0.2,0.4)
    {
        Name = "a Chelonian tide tortoise"; Body = 1294; BaseSoundID = 0x5A;
        CanSwim = true; CantWalk = false; Tamable = true; MinTameSkill = 90; ControlSlots = 3;
        SetStr(400, 480); SetDex(180, 210); SetInt(160, 200); SetHits(650, 800); SetDamage(15, 21);
        SetDamageType(ResistanceType.Physical, 70); SetDamageType(ResistanceType.Cold, 30);
        SetResistance(ResistanceType.Physical, 65, 70); SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Cold, 65, 75); SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 40, 50); HavenRarePetAbility.Skills(this, 105);
        Skills.Anatomy.Base = 90;
        Backpack?.Delete(); AddItem(new StrongBackpack { Movable = false });
    }
    public override bool StatLossAfterTame => false;
    public override FoodType FavoriteFood => FoodType.Fish | FoodType.FruitsAndVegies;
    public override int Meat => 4;

    internal bool Manage(Mobile from) => from?.Deleted == false && from.Alive && Controlled &&
        from == ControlMaster && from.Map == Map && from.InRange(this, 3);
    public override bool IsSnoop(Mobile from) => !Manage(from);
    public override bool CheckNonlocalLift(Mobile from, Item item) => Manage(from);
    public override bool CheckNonlocalDrop(Mobile from, Item item, Item target) => Manage(from);
    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> entries)
    { base.GetContextMenuEntries(from, entries); PackAnimal.GetContextMenuEntries(this, from, entries); }
    public override bool OnDragDrop(Mobile from, Item item)
    { return CheckFeed(from, item) || Manage(from) && Backpack.TryDropItem(from, item, false); }
    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        base.AlterMeleeDamageFrom(from, ref damage);
        if (Controlled && !IsDeadPet && Hits > 0 && Hits * 2 < HitsMax)
        { damage = damage * (80 - HavenPetSignatures.Tier(this) * 5) / 100; }
    }
 public override void OnThink(){base.OnThink();HavenPetSignatures.Think(this);}
 public override void OnGaveMeleeAttack(Mobile target){base.OnGaveMeleeAttack(target);HavenPetSignatures.OnAttack(this,target);}
 public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenPetSignatures.AddProperties(this,list);}
 public HavenChelonian(Serial serial):base(serial){}
 public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
 public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
}
}
