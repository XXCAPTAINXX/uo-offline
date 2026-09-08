// =========================================================================
// OfflineWallet.cs — account-level offline progression currency.
//
// Sovereigns are the offline UO Store currency. ModernUO's built-in store
// endpoint is currently only a stub, so this file deliberately separates
// the persistent currency layer from the future ServUO UO Store port.
//
// The balance is account-wide, not character-wide. Players get:
//   * 250 Sovereigns the first time the account enters the world
//   * 100 Sovereigns on the first login of each UTC day
//   * small Sovereign awards for kills in the Newbie Training zone
//
// Commands:
//   [Wallet
//   [StarterKit
//   [AddSovereigns <amount>      (GM test/admin)
// =========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ModernUO.Serialization;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Multis.Deeds;

namespace Server.CustomBots
{
    public static class OfflineWalletSystem
    {
        private sealed class WalletRecord
        {
            public int Sovereigns { get; set; }
            public int CleanupPoints { get; set; }
            public bool StarterGranted { get; set; }
            public DateTime LastDailyUtc { get; set; }

            // A free starter home is a one-time character grant. Travel book,
            // wallet and QoL bags are recoverable; house deeds are not.
            public HashSet<uint> StarterHomeCharacters { get; set; } = new();
        }

        private static Dictionary<string, WalletRecord> _wallets =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _dirty;

        private const int StarterSovereigns = 250;
        private const int DailySovereigns = 100;

        private static string WalletPath =>
            Path.Combine(Core.BaseDirectory, "Data", "OfflineWallets.json");

        public static void Configure()
        {
            CommandSystem.Register("Wallet", AccessLevel.Player, Wallet_OnCommand);
            CommandSystem.Register("StarterKit", AccessLevel.Player, StarterKit_OnCommand);
            CommandSystem.Register("AddSovereigns", AccessLevel.GameMaster, AddSovereigns_OnCommand);

            EventSink.WorldLoad += Load;
            EventSink.WorldSave += Save;
            EventSink.Connected += OnConnected;

            Timer.DelayCall(
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1),
                FlushIfDirty
            );
        }

        private static string AccountKey(Mobile m) =>
            m?.Account?.Username?.Trim().ToLowerInvariant();

        private static WalletRecord RecordFor(Mobile m, bool create = true)
        {
            var key = AccountKey(m);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (_wallets.TryGetValue(key, out var record))
            {
                return record;
            }

            if (!create)
            {
                return null;
            }

            record = new WalletRecord();
            _wallets[key] = record;
            _dirty = true;
            return record;
        }

        public static int Sovereigns(Mobile m) => RecordFor(m, false)?.Sovereigns ?? 0;
        public static int CleanupPoints(Mobile m) => RecordFor(m, false)?.CleanupPoints ?? 0;

        public static int CreditCleanup(Mobile m, int amount, string reason = null)
        {
            if (m == null || amount <= 0)
            {
                return CleanupPoints(m);
            }

            var record = RecordFor(m);
            if (record == null)
            {
                return 0;
            }

            long next = (long)record.CleanupPoints + amount;
            record.CleanupPoints = next > int.MaxValue ? int.MaxValue : (int)next;
            _dirty = true;

            var suffix = string.IsNullOrWhiteSpace(reason) ? "" : $" ({reason})";
            m.SendMessage(0x59, $"+{amount} Cleanup Points{suffix}. Total: {record.CleanupPoints:N0}");
            return record.CleanupPoints;
        }

        public static int Credit(Mobile m, int amount, string reason = null, bool message = true)
        {
            if (m == null || amount <= 0)
            {
                return Sovereigns(m);
            }

            var record = RecordFor(m);
            if (record == null)
            {
                return 0;
            }

            long next = (long)record.Sovereigns + amount;
            record.Sovereigns = next > int.MaxValue ? int.MaxValue : (int)next;
            _dirty = true;

            if (message)
            {
                var suffix = string.IsNullOrWhiteSpace(reason) ? "" : $" ({reason})";
                m.SendMessage(0x35, $"+{amount} Sovereigns{suffix}. Wallet: {record.Sovereigns:N0}");
            }

            return record.Sovereigns;
        }

        public static bool TrySpend(Mobile m, int amount)
        {
            if (m == null || amount < 0)
            {
                return false;
            }

            var record = RecordFor(m);
            if (record == null || record.Sovereigns < amount)
            {
                return false;
            }

            record.Sovereigns -= amount;
            _dirty = true;
            return true;
        }

        public static void OnNewbieKill(PlayerMobile killer, BaseCreature killed)
        {
            if (killer == null || killed == null || killed.Deleted ||
                !NewbiePlayability.IsInNewbieTraining(killer) ||
                killed.Summoned || killed.ControlMaster != null || killed.NoKillAwards)
            {
                return;
            }

            // Beginner creatures are deliberately generous enough that a
            // solo/offline player can actually use the future UO Store.
            int reward = Math.Clamp(5 + killed.Fame / 500, 5, 20);
            Credit(killer, reward, "newbie hunt");
        }

        private static void OnConnected(Mobile m)
        {
            if (m == null || m.Deleted || !m.Player || m is PlayerBot)
            {
                return;
            }

            EnsureStarterItems(m);

            var record = RecordFor(m);
            if (record == null)
            {
                return;
            }

            if (!record.StarterGranted)
            {
                record.StarterGranted = true;
                record.Sovereigns += StarterSovereigns;
                _dirty = true;
                m.SendMessage(
                    0x35,
                    $"Starter wallet funded with {StarterSovereigns:N0} Sovereigns. Use [Wallet anytime."
                );
            }

            var today = DateTime.UtcNow.Date;
            if (record.LastDailyUtc.Date < today)
            {
                record.LastDailyUtc = DateTime.UtcNow;
                record.Sovereigns += DailySovereigns;
                _dirty = true;
                m.SendMessage(
                    0x35,
                    $"Daily offline bonus: +{DailySovereigns:N0} Sovereigns. Wallet: {record.Sovereigns:N0}"
                );
            }
        }

        public static void EnsureStarterItems(Mobile m)
        {
            if (m?.Backpack == null)
            {
                return;
            }

            if (m.Backpack.FindItemByType<OfflineTravelBook>() == null)
            {
                m.AddToBackpack(new OfflineTravelBook());
                m.SendMessage("A blessed Travel Book has been placed in your backpack.");
            }

            if (m.Backpack.FindItemByType<AdventurersWallet>() == null)
            {
                m.AddToBackpack(new AdventurersWallet());
                m.SendMessage("An Adventurer's Wallet has been placed in your backpack.");
            }

            if (m.Backpack.FindItemByType<StarterReagentPouch>() == null)
            {
                m.AddToBackpack(new StarterReagentPouch());
                m.SendMessage("A 90% weight-reduction Reagent Pouch has been placed in your backpack.");
            }

            if (m.Backpack.FindItemByType<BritanniaCleanupBag>() == null)
            {
                m.AddToBackpack(new BritanniaCleanupBag());
                m.SendMessage("A Britannia Cleanup Bag has been placed in your backpack.");
            }

            GiveStarterHomeOnce(m);
        }

        private static void GiveStarterHomeOnce(Mobile m)
        {
            var record = RecordFor(m);
            if (record == null)
            {
                return;
            }

            record.StarterHomeCharacters ??= new HashSet<uint>();

            uint serial = m.Serial.Value;
            if (record.StarterHomeCharacters.Contains(serial))
            {
                return;
            }

            var supplies = new Bag
            {
                Name = "Starter Home Supplies"
            };

            var deed = new SmallBrickHouseDeed
            {
                Name = "Starter Small House Deed"
            };

            supplies.DropItem(deed);
            supplies.DropItem(new GoldRepairBench());

            if (m.AddToBackpack(supplies))
            {
                record.StarterHomeCharacters.Add(serial);
                _dirty = true;
                m.SendMessage(
                    0x35,
                    "Starter home granted: Starter Home Supplies contains a Small Brick House deed and Gold Repair Bench. This free home package is issued once per character."
                );
            }
            else
            {
                supplies.Delete();
                m.SendMessage("Make room in your backpack; your starter home package has not been claimed yet.");
            }
        }

        public static void Show(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            long gold = m.Account?.GetTotalGold() ?? 0;
            m.SendMessage(0x35, "=== Adventurer's Wallet ===");
            m.SendMessage($"Sovereigns: {Sovereigns(m):N0}");
            m.SendMessage($"Cleanup Points: {CleanupPoints(m):N0}");
            m.SendMessage($"Account gold: {gold:N0}");
            m.SendMessage("Sovereigns are reserved for the offline UO Store; Cleanup Points will fund the cleanup reward catalog.");
        }

        private static void Wallet_OnCommand(CommandEventArgs e) => Show(e.Mobile);

        private static void StarterKit_OnCommand(CommandEventArgs e)
        {
            EnsureStarterItems(e.Mobile);
            Show(e.Mobile);
        }

        private static void AddSovereigns_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null || e.Arguments.Length != 1 ||
                !int.TryParse(e.Arguments[0], out var amount) || amount <= 0)
            {
                e.Mobile?.SendMessage("Usage: [AddSovereigns <positive amount>");
                return;
            }

            Credit(e.Mobile, amount, "admin");
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(WalletPath))
                {
                    _wallets = new Dictionary<string, WalletRecord>(StringComparer.OrdinalIgnoreCase);
                    return;
                }

                var json = File.ReadAllText(WalletPath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, WalletRecord>>(json);

                _wallets = loaded != null
                    ? new Dictionary<string, WalletRecord>(loaded, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, WalletRecord>(StringComparer.OrdinalIgnoreCase);

                foreach (var record in _wallets.Values)
                {
                    record.StarterHomeCharacters ??= new HashSet<uint>();
                }

                _dirty = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[wallet] load failed: {ex.Message}");
                _wallets = new Dictionary<string, WalletRecord>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(WalletPath)!);
                File.WriteAllText(
                    WalletPath,
                    JsonSerializer.Serialize(
                        _wallets,
                        new JsonSerializerOptions { WriteIndented = true }
                    )
                );
                _dirty = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[wallet] save failed: {ex.Message}");
            }
        }

        private static void FlushIfDirty()
        {
            if (_dirty)
            {
                Save();
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class AdventurersWallet : Item
    {
        [Constructible]
        public AdventurersWallet() : base(0x1E5E)
        {
            Name = "Adventurer's Wallet";
            Weight = 0.0;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from?.Backpack == null || !IsChildOf(from.Backpack))
            {
                from?.SendMessage("The wallet must be in your backpack.");
                return;
            }

            OfflineWalletSystem.Show(from);
        }
    }
}
