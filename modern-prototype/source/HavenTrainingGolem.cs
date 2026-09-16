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
            EventSink.ServerStarted += () => EnsureStation();
        }
        public static bool EnsureStation()
        {
            if (!HavenPreview.Enabled) return false;
            if (World.Mobiles.Values.OfType<HavenTrainingGolem>().Any(g => !g.Deleted && g.Map == Map.Trammel && g.InRange(Station,4))) return true;
            if (!Map.Trammel.CanFit(Station,16,false,true)) return false;
            new HavenTrainingGolem().MoveToWorld(Station,Map.Trammel); return true;
        }
        public static event Action<HavenTrainingGolem,Mobile,int> PracticeDamage;
        Mobile _trainee;
        DateTime _until;
        public static bool PracticePet(Mobile m){var c=m as BaseCreature;return c!=null&&c.Controlled&&c.ControlMaster!=null&&!c.IsDeadPet;}
        public static bool CheckPracticeParry(Mobile m){return m.CheckSkill(SkillName.Parry,Math.Max(0.10,Math.Min(0.95,m.Skills[SkillName.Parry].Value/400.0)));}
        public static int LimitDamage(Mobile target,int damage){return target==null?0:Math.Max(0,Math.Min(Math.Min(1,damage),target.Hits-1));}
        bool Valid(Mobile m){return m!=null&&!m.Deleted&&m.Alive&&m.Hits>1&&(m.Player||PracticePet(m))&&m.Map==Map&&InRange(m,2)&&InLOS(m);}
        void Begin(Mobile m){if(Valid(m)){_trainee=m;_until=DateTime.UtcNow.AddSeconds(15);Combatant=m;}}
        public override void AggressiveAction(Mobile aggressor,bool criminal){base.AggressiveAction(aggressor,criminal);Begin(aggressor);}
        public override void AlterMeleeDamageTo(Mobile to,ref int damage){damage=LimitDamage(to,damage);}
        [Constructable]
        public HavenTrainingGolem() : base(AIType.AI_Melee, FightMode.None, 1, 1, 0.2, 0.4)
        {
            Name = "Haven practice golem";
            Title = "the patient sparring partner";
            Body = 752;
            Hue = 2101;
            SetStr(1000); SetDex(10); SetInt(10); SetHits(30000);
            SetDamage(1, 1);
            SetSkill(SkillName.Wrestling, 120);
            SetSkill(SkillName.MagicResist, 120);
            CantWalk = true;
            Tamable = false;
            NoKillAwards = true;
            Karma = 0;
        }
        public HavenTrainingGolem(Serial serial) : base(serial) { }
        public override bool AlwaysAttackable { get { return true; } }
        public override bool CanBeHarmful(IDamageable target, bool message, bool ignoreOurBlessedness) { return target == _trainee && DateTime.UtcNow < _until && Valid(target as Mobile); }
        public override bool OnBeforeDeath() { Hits = HitsMax; return false; }
        public override void OnDamage(int amount,Mobile from,bool willKill){base.OnDamage(amount,from,willKill);Begin(from);var handler=PracticeDamage;if(handler!=null)handler(this,from,amount);}
        public override void OnThink()
        {
            Hits = HitsMax;
            if(!Valid(_trainee)||DateTime.UtcNow>=_until){_trainee=null;Combatant=null;}
            else Combatant=_trainee;
            base.OnThink();
        }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Sparring target: returns attacks for at most 1 damage per hit.");
            list.Add("Trains player and pet defenses; stops before a lethal hit. No loot.");
        }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); SetDamage(1,1); CantWalk=true; }
    }
}
