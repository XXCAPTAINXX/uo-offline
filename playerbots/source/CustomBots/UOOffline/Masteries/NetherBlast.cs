using Server.Engines.BuffIcons;
using ModernUO.Serialization;
using System;
using Server;
using Server.Spells;
using Server.Network;
using Server.Mobiles;
using Server.Misc;
using System.Collections.Generic;
using Server.Items;
using System.Linq;

namespace Server.Spells.SkillMasteries
{
    public partial class NetherBlastSpell : SkillMasterySpell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Nether Blast", "In Vas Por Grav",
                204,
				9061,
                Reagent.DragonsBlood,
                Reagent.DaemonBone
            );

        public override double RequiredSkill { get { return 90; } }
        public override double UpKeep { get { return 0; } }
        public override int RequiredMana { get { return 40; } }
        public override bool PartyEffects { get { return false; } }
        public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds(2.0); } }
        public override double TickTime { get { return 1; } }

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

        public List<InternalItem> Items { get; set; } = new List<InternalItem>();

        public NetherBlastSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void SendCastEffect()
        {
            Caster.FixedEffect(0x37C4, 87, (int)(GetCastDelay().TotalSeconds * 28), 0x66C, 3);
        }

        public override bool CheckCast()
        {
            if (HasSpell(Caster, this.GetType()))
            {
                Caster.SendMessage("You cannot use this ability until the last one has expired.");
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            Caster.Target = new MasteryTarget(this, 10, true, Server.Targeting.TargetFlags.None);
        }

        protected override void OnTarget(object o)
        {
            if (o is IPoint3D)
            {
                IPoint3D p = o as IPoint3D;

                SpellHelper.Turn(Caster, p);

                if (SpellHelper.CheckTown(Caster, Caster) && CheckSequence())
                {
                    double skill = ((Caster.Skills[CastSkill].Value * 2) + Caster.Skills[DamageSkill].Value) / 3;
                    TimeSpan duration = TimeSpan.FromSeconds(1.0 + (skill / 40.0));

                    Direction d = Utility.GetDirection(Caster.Location, new Point3D(p));
                    Point3D loc = Caster.Location;

                    for (int i = 0; i < 5; ++i)
                    {
                        Server.Timer.DelayCall(TimeSpan.FromMilliseconds(i*250), () =>
                            {
                                int x = loc.X;
                                int y = loc.Y;
                                int z = loc.Z;

                                Movement.Movement.Offset(d, ref x, ref y);

                                loc = new Point3D(x, y, z);

                                bool canFit = SpellHelper.AdjustField(ref loc, Caster.Map, 12, false);

                                if (canFit)
                                {
                                    var item = new InternalItem(Caster, 0x37CC, loc, Caster.Map);
                                    item.ProcessDelta();
                                    Effects.SendLocationParticles(EffectItem.Create(loc, Caster.Map, EffectItem.DefaultDuration), 0x376A, 9, 10, 5048);

                                    Items.Add(item);
                                }
                            });
                    }

                    Expires = Core.Now + duration;
                    BeginTimer();

                    Caster.PlaySound(0x211);
                }
            }
        }

        public override bool OnTick()
        {
            Dictionary<Mobile, InternalItem> list = new Dictionary<Mobile, InternalItem>();

            foreach (var item in Items.Where(i => !i.Deleted))
            {
                using var targets = MasteryTargeting.Acquire(Caster, item.Location, item.Map, 1);
                foreach (var m in targets)
                {
                    if (m.Z + 16 <= item.Z || item.Z + 12 <= m.Z) { continue; }
                    if (!list.ContainsKey(m))
                    {
                        list.Add(m, item);
                    }
                }
            }

            foreach (var kvp in list)
            {
                DoDamage(kvp.Key, kvp.Value);
            }

            list?.Clear();
            return base.OnTick();
        }

        public override void OnExpire()
        {
            foreach (var item in Items)
            {
                item.Delete();
            }
        }

        private bool DoDamage(Mobile m, InternalItem item)
        {
            if (item.Visible && Caster != null && (!Core.AOS || m != Caster) && SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false))
            {
                if (SpellHelper.CanRevealCaster(m))
                    Caster.RevealingAction();

                SkillName damageSkill = Caster.Skills[SkillName.Focus].Value > Caster.Skills[SkillName.Imbuing].Value ? SkillName.Focus : SkillName.Imbuing;

                double skill = ((Caster.Skills[SkillName.Mysticism].Value) + Caster.Skills[damageSkill].Value * 2) / 3;
                skill /= m.Player ? 3.5 : 2;

                int damage = (int)skill + Utility.RandomMinMax(-3, 3);
                damage *= (int)GetDamageScalar(m);

                int sdiBonus = MasteryTargeting.SpellDamageBonus(Caster, m, CastSkill, Caster.Player && m.Player);

                damage *= (100 + sdiBonus);
                damage /= 100;

                AOS.Damage(m, Caster, damage, false, 0, 0, 0, 0, 0, 100, 0);

                m.FixedParticles(0x374A, 1, 15, 9502, 97, 3, (EffectLayer)255);

                int manaRip = Math.Min(m.Mana, damage / 4);

                if (manaRip > 0)
                {
                    m.Mana -= manaRip;
                    Caster.Mana += manaRip;
                }
            }

            return true;
        }

        [DispellableField]
        [SerializationGenerator(0)]
        public partial class InternalItem : Item
        {
            public override bool BlocksFit { get { return true; } }

            [Constructible]
            public InternalItem(Mobile caster, int itemID, Point3D loc, Map map)
                : base(itemID)
            {
                Visible = false;
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);

                if (caster.InLOS(this))
                    Visible = true;
                else
                    Delete();

                if (Deleted)
                    return;
            }

            [AfterDeserialization]
            private void RemoveSavedEffect() { Delete(); }
        }
    }
}
