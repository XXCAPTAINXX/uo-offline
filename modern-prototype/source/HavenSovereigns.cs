using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Commands;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.HavenPrototype
{
    public static class HavenSovereigns
    {
        const string Prefix = "Haven.Sovereigns.v1.";
        static readonly ConditionalWeakTable<BaseCreature, HashSet<Account>> Credits = new ConditionalWeakTable<BaseCreature, HashSet<Account>>();
        static readonly int[] KillMilestones = { 1, 25, 100, 500, 1000, 5000, 10000 };
        public static void Initialize()
        {
            CommandSystem.Register("sovereigns", AccessLevel.Player, e => {
                var player = e.Mobile as PlayerMobile; if (!HavenPreview.Enabled || player == null) return;
                CheckProgress(player);
                player.SendMessage("UO Store balance: " + player.AccountSovereigns.ToString("N0") + " Sovereigns. Earn through skill milestones, discoveries, pet bonds, monster milestones and bosses.");
            });
            Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), () => {
                if (HavenPreview.Enabled) foreach (var net in NetState.Instances.ToArray()) CheckProgress(net.Mobile as PlayerMobile);
            });
            EventSink.CreatureDeath += e => {
                if (!HavenPreview.Enabled) return;
                var creature = e.Creature as BaseCreature; if (creature == null) return;
                foreach (var right in creature.GetLootingRights().Where(r => r.m_HasRight)) OnMonsterKilled(creature, Owner(right.m_Mobile, creature));
                OnMonsterKilled(creature, Owner(e.Killer, creature));
            };
        }
        static PlayerMobile Owner(Mobile mobile, BaseCreature victim)
        {
            if (mobile == null) return null;
            mobile = mobile.GetDamageMaster(victim) ?? mobile;
            var companion = mobile as HavenCompanion;
            return (companion == null ? mobile : companion.BoundOwner) as PlayerMobile;
        }
        public static bool Award(PlayerMobile player, string key, string title, int amount)
        {
            var account = player == null ? null : player.Account as Account;
            if (!HavenPreview.Enabled || account == null || player.Deleted || amount <= 0 || account.Sovereigns > int.MaxValue - amount || (key != null && account.GetTag(Prefix + key) != null)) return false;
            if (!player.DepositSovereigns(amount)) return false;
            if (key != null) account.SetTag(Prefix + key, "claimed");
            account.SetTag(Prefix + "LastReward", title + ": +" + amount);
            return true;
        }
        public static void CheckProgress(PlayerMobile player)
        {
            if (!HavenPreview.Enabled || player == null || player.Deleted || !player.Alive || player.Map == null || player.Map == Map.Internal || !(player.Account is Account)) return;
            int before = player.AccountSovereigns;
            Award(player, "welcome", "Welcome to Haven", 25);
            Region destination = null;
            for (var region = player.Region; region != null; region = region.Parent)
                if ((region is TownRegion || region is DungeonRegion) && !string.IsNullOrWhiteSpace(region.Name)) destination = region;
            if (destination != null) Award(player, "visit:" + player.Map.MapID + ":" + destination.GetType().Name + ":" + destination.Name, "Discovered " + destination.Name, destination is DungeonRegion ? 50 : 25);
            for (int i = 0; i < player.Skills.Length; i++)
            {
                var skill = player.Skills[i];
                if (skill.Base >= 50) Award(player, "skill:" + i + ":50", skill.Name + " 50", 5);
                if (skill.Base >= 100) Award(player, "skill:" + i + ":100", skill.Name + " 100", 25);
            }
            if (player.RawStr >= 100) Award(player, "stat:str", "100 base Strength", 10);
            if (player.RawDex >= 100) Award(player, "stat:dex", "100 base Dexterity", 10);
            if (player.RawInt >= 100) Award(player, "stat:int", "100 base Intelligence", 10);
            var nearby = player.GetMobilesInRange(12);
            try { foreach (Mobile mobile in nearby) { var pet = mobile as BaseCreature; if (pet == null || !pet.Controlled || pet.ControlMaster != player || pet.Summoned || pet is HavenCompanion) continue; Award(player, "pet:first", "An animal companion", 15); if (pet.IsBonded) Award(player, "pet:bond", "A lasting bond", 25); } }
            finally { nearby.Free(); }
            int earned = player.AccountSovereigns - before;
            if (earned > 0) player.SendMessage(0x482, "Milestones earned " + earned.ToString("N0") + " Sovereigns. UO Store balance: " + player.AccountSovereigns.ToString("N0") + ".");
        }
        public static void OnMonsterKilled(BaseCreature creature, PlayerMobile player)
        {
            if (!HavenStarterGear.Eligible(creature, player) || creature.IsInvulnerable || creature.Blessed || creature is HavenCompanion) return;
            var account = player.Account as Account; if (account == null || !Credits.GetOrCreateValue(creature).Add(account)) return;
            int kills; int.TryParse(account.GetTag(Prefix + "Kills"), out kills); kills = kills == int.MaxValue ? kills : kills + 1; account.SetTag(Prefix + "Kills", kills.ToString());
            int before = player.AccountSovereigns;
            foreach (int milestone in KillMilestones) if (kills >= milestone) Award(player, "kills:" + milestone, "Defeated " + milestone + " monsters", milestone == 1 ? 10 : milestone / 10 + 10);
            if (creature is Server.Mobiles.BaseChampion || creature.HitsMax >= 4000) Award(player, null, "Boss defeated: " + creature.Name, creature.HitsMax >= 20000 ? 100 : 50);
            if (player.AccountSovereigns > before) player.SendMessage(0x482, "+" + (player.AccountSovereigns - before) + " Sovereigns. UO Store balance: " + player.AccountSovereigns.ToString("N0") + ".");
        }
    }
}

