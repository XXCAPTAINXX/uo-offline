// =========================================================================
// OldHavenWarden.cs — forgiving starter boss for Old Haven.
//
// Intended as an early solo milestone: stronger than the surrounding training
// creatures, but well below a champion-spawn boss. Rewards are useful to a
// fresh character without shortcutting end-game progression.
// =========================================================================

using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.CustomBots
{
    [SerializationGenerator(0, false)]
    public partial class OldHavenWarden : BaseCreature
    {
        [Constructible]
        public OldHavenWarden() : base(AIType.AI_Melee)
        {
            Name = "the Old Haven Warden";
            Body = 1;
            Hue = 0x83EA;
            BaseSoundID = 427;

            SetStr(175, 210);
            SetDex(70, 90);
            SetInt(55, 75);

            SetHits(325, 400);
            SetMana(0);

            SetDamage(8, 13);
            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.MagicResist, 45.0, 60.0);
            SetSkill(SkillName.Tactics, 55.0, 70.0);
            SetSkill(SkillName.Wrestling, 60.0, 75.0);

            Fame = 3500;
            Karma = -3500;
            VirtualArmor = 28;
        }

        public override string CorpseName => "the Old Haven Warden's corpse";

        public override void GenerateLoot()
        {
            PackGold(1200, 2200);
            PackItem(new Bandage(75));
            PackItem(new BagOfReagents(20));

            if (Utility.RandomDouble() < 0.45)
            {
                PackItem(new StarterWeaponVoucher());
            }

            if (Utility.RandomDouble() < 0.20)
            {
                PackItem(new StarterFortuneEarrings());
            }

            AddLoot(LootPack.Average);
            AddLoot(LootPack.Potions);
        }
    }
}
