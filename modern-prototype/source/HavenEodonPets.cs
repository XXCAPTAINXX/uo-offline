using System;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public abstract class HavenEodonMount : BaseMount
    {
        protected HavenEodonMount(string name, int body) : base(name, body, body, AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            // TazUO resolves unmapped mount items with AnimID zero to their graphic ID.
            // These IDs therefore select the original Eodon bodies, not replacement horse art.
            Tamable = true; MinTameSkill = 102; ControlSlots = 3; Fame = 11000; Karma = 0;
            SetDamageType(ResistanceType.Physical, 100);
            SetSkill(SkillName.Wrestling, 100, 110); SetSkill(SkillName.Tactics, 100, 110);
            SetSkill(SkillName.Anatomy, 90, 100); SetSkill(SkillName.MagicResist, 95, 105);
            SetSkill(SkillName.DetectHidden, 75); SetSkill(SkillName.Focus, 100);
        }
        protected HavenEodonMount(Serial serial) : base(serial) { }
        public override TrainingDefinition TrainingDefinition { get { return HavenPetTrainingBridge.Definition(this); } }
        public override bool StatLossAfterTame { get { return false; } }
        public override bool CanAngerOnTame { get { return true; } }
        public override int GetIdleSound() { return 0x673; }
        public override int GetAngerSound() { return 0x670; }
        public override int GetHurtSound() { return 0x672; }
        public override int GetDeathSound() { return 0x671; }
        public override void OnThink() { if (Rider != null) return; base.OnThink(); HavenPetSignatures.Think(this); }
        public override void OnGaveMeleeAttack(Mobile target) { base.OnGaveMeleeAttack(target); HavenPetSignatures.OnAttack(this, target); }
        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); HavenPetSignatures.AddProperties(this, list); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    [CorpseName("a stonehorn triceratops corpse")]
    public class HavenStonehornTriceratops : HavenEodonMount
    {
        [Constructable] public HavenStonehornTriceratops() : base("a stonehorn triceratops", 0x587)
        {
            SetStr(650, 700); SetDex(150, 180); SetInt(200, 250); SetHits(800, 900); SetDamage(18, 24);
            SetResistance(ResistanceType.Physical, 65, 75); SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 45, 55); SetResistance(ResistanceType.Poison, 50, 60); SetResistance(ResistanceType.Energy, 40, 50);
            SetSkill(SkillName.Parry, 100, 110);
        }
        public HavenStonehornTriceratops(Serial serial) : base(serial) { }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies; } }
        public override void AlterMeleeDamageFrom(Mobile from, ref int damage) { base.AlterMeleeDamageFrom(from, ref damage); damage = damage * (100 - HavenPetSignatures.GuardPercent(this)) / 100; }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    [CorpseName("a sunfang tiger corpse")]
    public class HavenSunfangTiger : HavenEodonMount
    {
        [Constructable] public HavenSunfangTiger() : base("a sunfang saber-toothed tiger", 0x588)
        {
            ControlSlots = 2;
            SetStr(480, 540); SetDex(250, 300); SetInt(180, 240); SetHits(550, 650); SetDamage(19, 25);
            SetResistance(ResistanceType.Physical, 50, 60); SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 50, 60); SetResistance(ResistanceType.Poison, 35, 45); SetResistance(ResistanceType.Energy, 40, 50);
            SetSkill(SkillName.Parry, 90, 100);
        }
        public HavenSunfangTiger(Serial serial) : base(serial) { }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
