using Server.Items;

namespace Server.HavenPrototype
{
    public interface IHavenAreaWeapon { int Element { get; } }
    public static class HavenAreaWeapons
    {
        public static void Apply(BaseWeapon weapon, int level)
        {
            var area = weapon as IHavenAreaWeapon;
            if (area == null) return;
            int chance = 20 + (System.Math.Max(1, System.Math.Min(20, level)) - 1) * 3;
            if (area.Element == 0) weapon.WeaponAttributes.HitEnergyArea = chance;
            else if (area.Element == 1) weapon.WeaponAttributes.HitFireArea = chance;
            else weapon.WeaponAttributes.HitColdArea = chance;
        }
        public static void InitializeWeapon(BaseWeapon weapon, string name, int hue)
        {
            weapon.Name = name; weapon.Hue = hue;
            weapon.Attributes.WeaponDamage = 30; weapon.Attributes.WeaponSpeed = 10;
            weapon.WeaponAttributes.HitLeechMana = 30;
            weapon.WeaponAttributes.HitLeechHits = 20;
            HavenAdvancedGear.Attach(weapon, 6);
        }
        public static BaseWeapon Create(int index) { return index == 0 ? (BaseWeapon)new HavenStormblade() : index == 1 ? new HavenCindermaul() : (BaseWeapon)new HavenFrostwakeBow(); }
    }
    public class HavenStormblade : Longsword, IHavenAreaWeapon
    {
        public int Element { get { return 0; } }
        [Constructable] public HavenStormblade() { HavenAreaWeapons.InitializeWeapon(this, "Stormblade", 0x482); }
        public HavenStormblade(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class HavenCindermaul : WarHammer, IHavenAreaWeapon
    {
        public int Element { get { return 1; } }
        [Constructable] public HavenCindermaul() { HavenAreaWeapons.InitializeWeapon(this, "Cindermaul", 0x489); }
        public HavenCindermaul(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class HavenFrostwakeBow : Bow, IHavenAreaWeapon
    {
        public int Element { get { return 2; } }
        [Constructable] public HavenFrostwakeBow() { HavenAreaWeapons.InitializeWeapon(this, "Frostwake Bow", 0x47E); }
        public HavenFrostwakeBow(Serial serial) : base(serial) { }
        public override void DoAreaAttack(Mobile from, Mobile defender, int damageGiven, int sound, int hue, int phys, int fire, int cold, int pois, int nrgy)
        {
            if(defender==null||defender.Deleted||defender.Map!=from.Map)return;
            var targets=Server.Spells.SpellHelper.AcquireIndirectTargets(from,defender,defender.Map,5);
            int count=0;foreach(var target in targets){count++;from.DoHarmful(target,true);target.FixedEffect(0x3779,1,15,hue,0);AOS.Damage(target,from,damageGiven/2,phys,fire,cold,pois,nrgy,Server.DamageType.SpellAOE);}
            if(count>0)Effects.PlaySound(defender.Location,defender.Map,sound);
            if(ProcessingMultipleHits)BlockHitEffects=true;
        }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
}
