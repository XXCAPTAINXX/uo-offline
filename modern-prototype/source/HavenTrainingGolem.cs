using System;
using System.Linq;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public class HavenTrainingGolem : BaseCreature
    {
        private static readonly Point3D Station = new Point3D(3469, 2601, 10);
        public static void Initialize()
        {
            EventSink.ServerStarted += () => {
                if (!HavenPreview.Enabled || World.Mobiles.Values.OfType<HavenTrainingGolem>().Any(g => !g.Deleted)) return;
                if (!Map.Trammel.CanFit(Station, 16, false, true))
                { Console.WriteLine("Haven training station is blocked; golem not placed."); return; }
                new HavenTrainingGolem().MoveToWorld(Station, Map.Trammel);
            };
        }
        [Constructable]
        public HavenTrainingGolem() : base(AIType.AI_Melee, FightMode.None, 1, 1, 0.2, 0.4)
        {
            Name = "Haven practice golem";
            Title = "the patient sparring partner";
            Body = 752;
            Hue = 2101;
            SetStr(1000); SetDex(10); SetInt(10); SetHits(30000);
            SetDamage(0, 0);
            SetSkill(SkillName.Wrestling, 120);
            SetSkill(SkillName.MagicResist, 120);
            CantWalk = true;
            Tamable = false;
            NoKillAwards = true;
            Karma = 0;
        }
        public HavenTrainingGolem(Serial serial) : base(serial) { }
        public override bool AlwaysAttackable { get { return true; } }
        public override bool CanBeHarmful(IDamageable target, bool message, bool ignoreOurBlessedness) { return false; }
        public override bool OnBeforeDeath() { Hits = HitsMax; return false; }
        public override void OnThink()
        {
            Combatant = null;
            Hits = HitsMax;
            // No base combat AI: this target never swings, casts or pursues a trainee.
        }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Training target: never attacks, rapidly restores health.");
            list.Add("No loot or kill rewards. Practice with weapons, spells and pets.");
        }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
