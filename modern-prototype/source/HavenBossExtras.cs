using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenBossExtras
    {
        private static readonly ConditionalWeakTable<BaseCreature, HashSet<Mobile>> Paid = new ConditionalWeakTable<BaseCreature, HashSet<Mobile>>();
        public static void Initialize()
        {
            EventSink.CreatureDeath += e => Award(e.Creature as BaseCreature);
        }
        public static void Award(BaseCreature boss)
        {
            if (!HavenPreview.Enabled || boss == null || boss.Controlled || boss.Summoned || boss.NoKillAwards ||
                !(boss is CorgulTheSoulBinder || boss is CoraTheSorceress || boss is Osiredon)) return;
            foreach (var right in boss.GetLootingRights())
            {
                var owner = right.m_Mobile as PlayerMobile;
                if (!right.m_HasRight || right.m_Damage < 600 || owner == null || owner.Deleted || !owner.Alive ||
                    owner.Map != boss.Map || !owner.InRange(boss, 32) || !Paid.GetOrCreateValue(boss).Add(owner)) continue;
                int gold = boss is CorgulTheSoulBinder ? 50000 : boss is CoraTheSorceress ? 30000 : 40000;
                Deliver(owner, new BankCheck(gold));
                Deliver(owner, new AstralShard(10));
                int marks = HavenMarks.Award(owner, 20);
                if (boss is CorgulTheSoulBinder)
                {
                    Deliver(owner, new TreasureMap(6, boss.Map));
                    Deliver(owner, new ScrollOfTranscendence(SkillName.Tactics, boss.Map == Map.Felucca ? 1.0 : 0.5));
                }
                else if (boss is CoraTheSorceress) Deliver(owner, new TreasureMap(5, boss.Map));
                else
                {
                    Deliver(owner, new MessageInABottle(boss.Map));
                    Deliver(owner, new SpecialFishingNet());
                    Deliver(owner, new FishingPole());
                    var forge = ForgeReward(Utility.RandomDouble());
                    if (forge != null) Deliver(owner, forge);
                }
                owner.SendMessage(0x482, "Boss rewards: " + gold.ToString("N0") + " gold, " + marks + " Marks, 10 Astral Shards and bonus supplies. Overflow items are in your bank.");
            }
        }
        public static Item ForgeReward(double roll) { return roll >= 0 && roll < 0.05 ? new HavenSmallSoulForgeDeed() : null; }
        private static void Deliver(PlayerMobile owner, Item item)
        {
            if (owner.Backpack == null || !owner.Backpack.TryDropItem(owner, item, false)) owner.BankBox.DropItem(item);
        }
    }
}
