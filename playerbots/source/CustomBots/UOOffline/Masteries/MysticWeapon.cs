using Server.Engines.BuffIcons;
using System;
using Server;
using Server.Spells;
using Server.Network;
using Server.Mobiles;
using Server.Items;
using System.Collections.Generic;

namespace Server.Spells.SkillMasteries
{
    public class MysticWeaponSpell : SkillMasterySpell
    {
        public static string ModName = "MysticWeapon";

        private static SpellInfo m_Info = new SpellInfo(
                "Mystic Weapon", "Vas Ylem Wis",
                -1,
                9002,
                Reagent.FertileDirt,
                Reagent.Bone
            );

        public override double RequiredSkill { get { return 90; } }
        public override int RequiredMana { get { return 40; } }
        public override bool PartyEffects { get { return false; } }

        public override SkillName CastSkill { get { return SkillName.Mysticism; } }
        public override SkillName DamageSkill
        {
            get
            {
                if (Caster.Skills[SkillName.Focus].Value > Caster.Skills[SkillName.Imbuing].Value)
                    return SkillName.Focus;

                return SkillName.Imbuing;
            }
        }

        private BaseWeapon _Weapon;

        public MysticWeaponSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void SendCastEffect()
        {
            Caster.FixedEffect(0x37C4, 87, (int)(GetCastDelay().TotalSeconds * 28), 0x66C, 3);
        }

        public override bool CheckCast()
        {
            BaseWeapon weapon = GetWeapon();

            if (weapon == null || weapon is Fists)
            {
                Caster.SendLocalizedMessage(1060179); //You must be wielding a weapon to use this ability!
                return false;
            }
            else if (GetSpell<MysticWeaponSpell>(Caster) != null)
            {
                Caster.SendMessage("That weapon is already under these effects.");
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            BaseWeapon weapon = GetWeapon();

            if (weapon == null || weapon is Fists)
                Caster.SendLocalizedMessage(1060179); //You must be wielding a weapon to use this ability!
            else if (CheckSequence())
            {
                double skill = ((Caster.Skills[CastSkill].Value * 1.5) + Caster.Skills[DamageSkill].Value);
                double duration = (skill + (GetMasteryLevel() * 50)) * 2;


                MasteryBuffInfo.AddBuff(Caster, new MasteryBuffInfo(BuffIcon.MysticWeapon, 1155899, 1156055, TimeSpan.FromSeconds(duration), Caster));

                Effects.SendLocationParticles(EffectItem.Create(Caster.Location, Caster.Map, EffectItem.DefaultDuration), 0x36CB, 1, 14, 0x55C, 7, 9915, 0);
                Caster.PlaySound(0x64E);


                weapon.InvalidateProperties();

                Expires = Core.Now + TimeSpan.FromSeconds(duration);
                BeginTimer();

                _Weapon = weapon;
            }

            FinishSequence();
        }

        public static double WeaponSkill(Mobile mobile, BaseWeapon weapon, double normal)
        {
            var spell = GetSpell<MysticWeaponSpell>(mobile);
            return spell?._Weapon == weapon && weapon != null ? Math.Max(normal, mobile.Skills.Mysticism.Value - 25) : normal;
        }

        public override void OnWeaponRemoved(BaseWeapon weapon)
        {
            Expire();
        }

        public override void EndEffects()
        {
            MasteryBuffInfo.RemoveBuff(Caster, BuffIcon.MysticWeapon);


            _Weapon?.InvalidateProperties();
            _Weapon = null;

            Caster.SendLocalizedMessage(1115273); // The enchantment on your weapon has expired.
            Caster.PlaySound(0x1ED);
        }
    }
}