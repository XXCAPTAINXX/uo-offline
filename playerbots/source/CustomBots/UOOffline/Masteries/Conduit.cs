using Server.Engines.BuffIcons;
using ModernUO.Serialization;
using System;
using Server;
using Server.Spells;
using Server.Network;
using Server.Mobiles;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Server.Items;

namespace Server.Spells.SkillMasteries
{
	public partial class ConduitSpell : SkillMasterySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Conduit", "Uus Corp Grav",
				204,
				9061,
                Reagent.NoxCrystal,
                Reagent.BatWing,
                Reagent.GraveDust
			);

		public override double RequiredSkill{ get { return 90; } }
		public override double UpKeep { get { return 0; } }
		public override int RequiredMana{ get { return 40; } }
		public override bool PartyEffects { get { return false; } }

        public override SkillName CastSkill { get { return SkillName.Necromancy; } }
        public override SkillName DamageSkill { get { return SkillName.SpiritSpeak; } }

        public int Strength { get; set; }
        public List<Item> Skulls { get; set; }
        public Rectangle2D Zone { get; set; }
        private DateTime _nextSpread;

        // Compatible ML behavior: targeted necromancy damage spreads within the
        // field once per second. Debuffs are not recast, so their costs and
        // timers cannot be duplicated or recursively renewed.
        public static void SpreadDamage(Mobile caster, Mobile victim, int damage, int phys, int fire, int cold, int poison, int energy)
        {
            if (caster?.Deleted != false || victim?.Deleted != false || damage <= 0) { return; }
            var conduit = GetSpell<ConduitSpell>(caster);
            if (conduit == null || Core.Now < conduit._nextSpread || caster.Map != victim.Map || !conduit.Zone.Contains(victim.Location)) { return; }
            conduit._nextSpread = Core.Now + TimeSpan.FromSeconds(1);
            using var targets = Server.Collections.PooledRefList<Mobile>.Create();
            foreach (var other in victim.Map.GetMobilesInBounds<Mobile>(conduit.Zone))
            {
                if (other != victim && other != caster && !other.Deleted && other.Alive && caster.InLOS(other) &&
                    SpellHelper.ValidIndirectTarget(caster, other) && caster.CanBeHarmful(other, false)) { targets.Add(other); }
            }
            var splash = Math.Max(1, AOS.Scale(damage, Math.Clamp(conduit.Strength, 1, 100)));
            foreach (var other in targets)
            {
                caster.DoHarmful(other);
                AOS.Damage(other, caster, splash, false, phys, fire, cold, poison, energy, masteryType: DamageType.SpellAOE);
            }
        }

        public ConduitSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
		{
		}

        public override void OnBeginCast()
        {
            base.OnBeginCast();

            Effects.SendLocationParticles(EffectItem.Create(Caster.Location, Caster.Map, EffectItem.DefaultDuration), 0x36CB, 1, 14, 0x55C, 7, 9915, 0);
        }

		public override void OnCast()
		{
            Caster.Target = new MasteryTarget(this, 10, true, Server.Targeting.TargetFlags.None);
		}

        protected override void OnTarget(object o)
        {
            IPoint3D p = o as IPoint3D;

            if (p != null && CheckSequence())
            {
                Rectangle2D rec = new Rectangle2D(p.X - 3, p.Y - 3, 6, 6);
                Skulls = new List<Item>();

                Item skull = new InternalItem();
                skull.MoveToWorld(new Point3D(rec.X, rec.Y, Caster.Map.GetAverageZ(rec.X, rec.Y)), Caster.Map);
                Skulls.Add(skull);

                skull = new InternalItem();
                skull.MoveToWorld(new Point3D(rec.X + rec.Width, rec.Y + rec.Height, Caster.Map.GetAverageZ(rec.X + rec.Width, rec.Y + rec.Height)), Caster.Map);
                Skulls.Add(skull);

                skull = new InternalItem();
                skull.MoveToWorld(new Point3D(rec.X + rec.Width, rec.Y, Caster.Map.GetAverageZ(rec.X + rec.Width, rec.Y)), Caster.Map);
                Skulls.Add(skull);

                skull = new InternalItem();
                skull.MoveToWorld(new Point3D(rec.X, rec.Y + rec.Height, Caster.Map.GetAverageZ(rec.X, rec.Y + rec.Height)), Caster.Map);
                Skulls.Add(skull);

                skull = new InternalItem();
                skull.MoveToWorld(new Point3D(rec.X + (rec.Width / 2), rec.Y + (rec.Height / 2), Caster.Map.GetAverageZ(rec.X + (rec.Width / 2), rec.Y + (rec.Height / 2))), Caster.Map);
                Skulls.Add(skull);

                Zone = rec;
                Strength = (int)((Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value + (GetMasteryLevel() * 20)) / 3.75);
                Expires = Core.Now + TimeSpan.FromSeconds(Core.TOL ? 6 : 4 + (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 36);

                MasteryBuffInfo.AddBuff(Caster, new MasteryBuffInfo(BuffIcon.Conduit, 1155901, 1156053, Strength.ToString())); //Targeted Necromancy spells used on a target within the Conduit field will affect all valid targets within the field at ~1_PERCT~% strength.

                BeginTimer();
            }
        }

        public override void EndEffects()
        {
            foreach (var skull in Skulls) { if (skull?.Deleted == false) { skull.Delete(); } }
            Skulls.Clear();

            MasteryBuffInfo.RemoveBuff(Caster, BuffIcon.Conduit);
        }

        public static bool CheckAffected(Mobile caster, Mobile victim, Action<Mobile, double> callback)
        {
            if (victim == null || victim.Map == null)
                return false;

            foreach (SkillMasterySpell spell in EnumerateSpells(caster, typeof(ConduitSpell)))
            {
                ConduitSpell conduit = spell as ConduitSpell;

                if (conduit == null)
                    continue;

                if (conduit.Caster.Map == victim.Map && conduit.Zone.Contains(victim.Location))
                {
                    using var toAffect = Server.Collections.PooledRefList<Mobile>.Create();

                    foreach (var m in victim.Map.GetMobilesInBounds<Mobile>(conduit.Zone))
                    {
                        if (m != victim && m.Alive && conduit.Caster.InLOS(m) && SpellHelper.ValidIndirectTarget(caster, m) && conduit.Caster.CanBeHarmful(m, false))
                        {
                            toAffect.Add(m);
                        }
                    }

                    if (toAffect.Count > 0 && callback != null)
                    {
                        foreach (var mobile in toAffect) { callback(mobile, conduit.Strength / 100.0); }
                        return true;
                    }
                }
            }

            return false;
        }

        [SerializationGenerator(0)]
        private partial class InternalItem : Item
        {
            [Constructible]
            public InternalItem()
                : base(Utility.RandomList(0x1853, 0x1858))
            {
            }

            [AfterDeserialization]
            private void RemoveSavedEffect() { Delete(); }
        }
	}
}
