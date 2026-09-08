// =========================================================================
// StarterHub.cs — New Haven starter gear/resources and weapon choice.
//
// New characters receive one Starter Adventurer Pack:
//   * Starter Adventurer Robe
//   * Full 64-spell magery spellbook
//   * Starter Weapon Voucher (Broadsword/Kryss/Mace/Bow)
//
// Near New Haven bank:
//   * Starter Gear Stone
//   * Starter Resource Stone
//
// Purchases are wallet-first: account/bank gold via Banker.Withdraw(), then
// physical backpack gold as a compatibility fallback.
// =========================================================================

using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.CustomBots
{
    public static class StarterHub
    {
        // South exterior wall of the New Haven bank (bank region ends at y=2580).
        // Keep the interior clear and make the convenience strip obvious.
        private static readonly Point3D HitchingPostAnchor = new(3482, 2582, 20);
        private static readonly Point3D GearStoneAnchor = new(3484, 2582, 20);
        private static readonly Point3D ResourceStoneAnchor = new(3486, 2582, 20);
        private static readonly Point3D OrganizerStoneAnchor = new(3488, 2582, 20);

        public static void Configure()
        {
            EventSink.WorldLoad += () => Timer.DelayCall(TimeSpan.FromSeconds(4), EnsureWorld);
        }

        public static void EnsureWorld()
        {
            if (!Core.ML)
            {
                return;
            }

            EnsureStone<NewHavenHitchingPost>(HitchingPostAnchor);
            EnsureStone<StarterGearStone>(GearStoneAnchor);
            EnsureStone<StarterResourceStone>(ResourceStoneAnchor);
            EnsureStone<StarterOrganizerStone>(OrganizerStoneAnchor);
        }

        private static void EnsureStone<T>(Point3D preferred) where T : Item, new()
        {
            var loc = FindSafe(Map.Trammel, preferred, 5);

            foreach (var item in World.Items.Values)
            {
                if (item is not T existing || existing.Deleted)
                {
                    continue;
                }

                if (existing.Map != Map.Trammel || !Utility.InRange(existing.Location, loc, 2))
                {
                    existing.MoveToWorld(loc, Map.Trammel);
                }

                existing.Movable = false;
                return;
            }

            var stone = new T { Movable = false };
            stone.MoveToWorld(loc, Map.Trammel);
            Console.WriteLine($"[starter-hub] placed {typeof(T).Name} at Trammel {loc}");
        }

        private static Point3D FindSafe(Map map, Point3D preferred, int radius)
        {
            if (map.CanSpawnMobile(preferred.X, preferred.Y, preferred.Z))
            {
                return preferred;
            }

            int avg = map.GetAverageZ(preferred.X, preferred.Y);
            if (map.CanSpawnMobile(preferred.X, preferred.Y, avg))
            {
                return new Point3D(preferred.X, preferred.Y, avg);
            }

            for (int r = 1; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                        {
                            continue;
                        }

                        int x = preferred.X + dx;
                        int y = preferred.Y + dy;

                        if (map.CanSpawnMobile(x, y, preferred.Z))
                        {
                            return new Point3D(x, y, preferred.Z);
                        }

                        int z = map.GetAverageZ(x, y);
                        if (map.CanSpawnMobile(x, y, z))
                        {
                            return new Point3D(x, y, z);
                        }
                    }
                }
            }

            return preferred;
        }

        public static bool TryPay(Mobile from, int cost)
        {
            if (from == null || cost < 0)
            {
                return false;
            }

            if (cost == 0)
            {
                return true;
            }

            if (Banker.Withdraw(from, cost))
            {
                return true;
            }

            return from.Backpack?.ConsumeTotal(typeof(Gold), cost) == true;
        }

        public static bool Buy(Mobile from, int cost, Func<Item> factory, string description)
        {
            if (from?.Backpack == null)
            {
                return false;
            }

            if (!TryPay(from, cost))
            {
                from.SendMessage($"You need {cost:N0} gold in your account/bank wallet.");
                return false;
            }

            var item = factory();
            if (item == null || !from.AddToBackpack(item))
            {
                item?.Delete();
                Banker.Deposit(from, cost);
                from.SendMessage("Make room in your backpack. Your purchase was refunded.");
                return false;
            }

            from.SendMessage(0x35, $"Purchased {description} for {cost:N0} gold.");
            return true;
        }

        public static Item CreateStarterWeapon(int choice)
        {
            BaseWeapon weapon = choice switch
            {
                1 => new EvolvingStarterBroadsword(),
                2 => new EvolvingStarterKryss(),
                3 => new EvolvingStarterMace(),
                4 => new EvolvingStarterBow(),
                _ => null
            };

            if (weapon == null)
            {
                return null;
            }

            if (weapon is Bow)
            {
                var pack = new Bag { Name = "Starter Archery Set" };
                pack.DropItem(weapon);
                pack.DropItem(new Arrow(250));
                return pack;
            }

            return weapon;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class StarterAdventurerRobe : Robe
    {
        [Constructible]
        public StarterAdventurerRobe() : base(0x489)
        {
            Name = "Starter Adventurer Robe";
            LootType = LootType.Blessed;

            Attributes.BonusStr = 5;
            Attributes.BonusDex = 5;
            Attributes.BonusInt = 5;

            Attributes.RegenHits = 1;
            Attributes.RegenStam = 1;
            Attributes.RegenMana = 1;

            Attributes.LowerManaCost = 5;
            Attributes.NightSight = 1;

        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Starter item");
            list.Add("+5 Strength / Dexterity / Intelligence");
            list.Add("+1 Hit / Stamina / Mana Regeneration");
            list.Add("Lower Mana Cost 5%");
            list.Add("Night Sight");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class StarterFullSpellbook : Spellbook
    {
        [Constructible]
        public StarterFullSpellbook() : base(ulong.MaxValue)
        {
            Name = "Starter Full Spellbook";
            LootType = LootType.Blessed;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add("All 64 Magery spells");
            list.Add("Starter item");
            AddEvolutionProperties(list);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class StarterWeaponVoucher : Item
    {
        [Constructible]
        public StarterWeaponVoucher() : base(0x14F0)
        {
            Name = "Starter Weapon Voucher";
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from?.Backpack == null || !IsChildOf(from.Backpack))
            {
                from?.SendMessage("The weapon voucher must be in your backpack.");
                return;
            }

            from.SendGump(new StarterWeaponGump(this));
        }
    }

    public class StarterWeaponGump : StaticGump<StarterWeaponGump>
    {
        private readonly StarterWeaponVoucher _voucher;

        public override bool Singleton => true;

        public StarterWeaponGump(StarterWeaponVoucher voucher) : base(80, 80)
        {
            _voucher = voucher;
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 360, 245, 5054);
            builder.AddBackground(10, 10, 340, 225, 3000);

            builder.AddHtml(30, 20, 300, 24, "<CENTER><B>Choose Your Starter Weapon</B></CENTER>");

            AddChoice(ref builder, 30, 60, 1, "Broadsword - Swords");
            AddChoice(ref builder, 30, 95, 2, "Kryss - Fencing");
            AddChoice(ref builder, 30, 130, 3, "Mace - Mace Fighting");
            AddChoice(ref builder, 30, 165, 4, "Bow + 250 arrows - Archery");

            builder.AddButton(130, 205, 4005, 4007, 0);
            builder.AddHtml(165, 207, 100, 20, "Cancel");
        }

        private static void AddChoice(ref StaticGumpBuilder builder, int x, int y, int id, string text)
        {
            builder.AddButton(x, y, 4005, 4007, id);
            builder.AddHtml(x + 35, y + 2, 270, 20, text);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            if (info.ButtonID is < 1 or > 4 ||
                _voucher?.Deleted != false ||
                from?.Backpack == null ||
                !_voucher.IsChildOf(from.Backpack))
            {
                return;
            }

            var reward = StarterHub.CreateStarterWeapon(info.ButtonID);
            if (reward == null)
            {
                return;
            }

            if (!from.AddToBackpack(reward))
            {
                reward.Delete();
                from.SendMessage("Make room in your backpack first.");
                return;
            }

            _voucher.Delete();
            from.SendMessage(0x35, "Your starter weapon has been placed in your backpack.");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class StarterGearStone : Item
    {
        [Constructible]
        public StarterGearStone() : base(0xED4)
        {
            Name = "Starter Gear Stone";
            Hue = 0x489;
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.SendMessage("You are too far away.");
                return;
            }

            from.SendGump(new StarterGearStoneGump());
        }
    }

    public class StarterGearStoneGump : StaticGump<StarterGearStoneGump>
    {
        public override bool Singleton => true;

        public StarterGearStoneGump() : base(100, 80)
        {
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 420, 390, 5054);
            builder.AddBackground(10, 10, 400, 370, 3000);

            builder.AddHtml(25, 20, 370, 24, "<CENTER><B>New Haven Starter Gear</B></CENTER>");
            builder.AddHtml(25, 45, 370, 20, "Payment comes from your account/bank wallet first.");

            AddBuy(ref builder, 25, 75, 1, "Starter Adventurer Robe", 500);
            AddBuy(ref builder, 25, 110, 2, "Full 64-spell Magery Spellbook", 1000);
            AddBuy(ref builder, 25, 145, 3, "Starter Weapon Voucher", 500);
            AddBuy(ref builder, 25, 180, 4, "Starter Fortune Earrings (100 LRC / 200 Luck)", 2500);
            AddBuy(ref builder, 25, 215, 5, "90% Reagent Pouch", 750);
            AddBuy(ref builder, 25, 250, 6, "Gold Repair Bench", 5000);
            AddBuy(ref builder, 25, 285, 7, "Britannia Cleanup Bag", 50);
            AddBuy(ref builder, 25, 320, 8, "Blessed Travel Book", 250);

            builder.AddButton(160, 350, 4005, 4007, 0);
            builder.AddHtml(195, 352, 100, 20, "Close");
        }

        private static void AddBuy(ref StaticGumpBuilder builder, int x, int y, int id, string text, int price)
        {
            builder.AddButton(x, y, 4005, 4007, id);
            builder.AddHtml(x + 35, y + 2, 350, 20, $"{text} - {price:N0} gp");
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    StarterHub.Buy(from, 500, () => new EvolvingStarterAdventurerRobe(), "Starter Adventurer Robe");
                    break;
                case 2:
                    StarterHub.Buy(from, 1000, () => new EvolvingStarterFullSpellbook(), "Full Spellbook");
                    break;
                case 3:
                    StarterHub.Buy(from, 500, () => new StarterWeaponVoucher(), "Starter Weapon Voucher");
                    break;
                case 4:
                    StarterHub.Buy(from, 2500, () => new StarterFortuneEarrings(), "Starter Fortune Earrings");
                    break;
                case 5:
                    StarterHub.Buy(from, 750, () => new StarterReagentPouch(), "Reagent Pouch");
                    break;
                case 6:
                    StarterHub.Buy(from, 5000, () => new GoldRepairBench(), "Gold Repair Bench");
                    break;
                case 7:
                    StarterHub.Buy(from, 50, () => new BritanniaCleanupBag(), "Britannia Cleanup Bag");
                    break;
                case 8:
                    StarterHub.Buy(from, 250, () => new OfflineTravelBook(), "Blessed Travel Book");
                    break;
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class StarterResourceStone : Item
    {
        [Constructible]
        public StarterResourceStone() : base(0xED4)
        {
            Name = "Starter Resource Stone";
            Hue = 0x59B;
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.SendMessage("You are too far away.");
                return;
            }

            from.SendGump(new StarterResourceStoneGump());
        }
    }

    public class StarterResourceStoneGump : StaticGump<StarterResourceStoneGump>
    {
        public override bool Singleton => true;

        public StarterResourceStoneGump() : base(120, 90)
        {
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 420, 390, 5054);
            builder.AddBackground(10, 10, 400, 370, 3000);

            builder.AddHtml(25, 20, 370, 24, "<CENTER><B>New Haven Starter Resources</B></CENTER>");
            builder.AddHtml(25, 45, 370, 20, "Low-cost early supplies; account/bank wallet accepted.");

            AddBuy(ref builder, 25, 75, 1, "50 of each standard Magery reagent", 1000);
            AddBuy(ref builder, 25, 110, 2, "500 Bandages", 1500);
            AddBuy(ref builder, 25, 145, 3, "500 Arrows", 1000);
            AddBuy(ref builder, 25, 180, 4, "250 Iron Ingots", 2000);
            AddBuy(ref builder, 25, 215, 5, "250 Boards", 2000);
            AddBuy(ref builder, 25, 250, 6, "250 Leather", 2000);
            AddBuy(ref builder, 25, 285, 7, "250 Cloth", 1000);
            AddBuy(ref builder, 25, 320, 8, "Alchemy starter bundle", 1500);

            builder.AddButton(160, 350, 4005, 4007, 0);
            builder.AddHtml(195, 352, 100, 20, "Close");
        }

        private static void AddBuy(ref StaticGumpBuilder builder, int x, int y, int id, string text, int price)
        {
            builder.AddButton(x, y, 4005, 4007, id);
            builder.AddHtml(x + 35, y + 2, 350, 20, $"{text} - {price:N0} gp");
        }

        private static Item AlchemyBundle()
        {
            var bag = new Bag { Name = "Alchemy Starter Bundle" };
            bag.DropItem(new Bottle(100));
            bag.DropItem(new MortarPestle());
            bag.DropItem(new BagOfReagents(25));
            return bag;
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    StarterHub.Buy(from, 1000, () => new BagOfReagents(50), "Magery reagent bundle");
                    break;
                case 2:
                    StarterHub.Buy(from, 1500, () => new Bandage(500), "500 Bandages");
                    break;
                case 3:
                    StarterHub.Buy(from, 1000, () => new Arrow(500), "500 Arrows");
                    break;
                case 4:
                    StarterHub.Buy(from, 2000, () => new IronIngot(250), "250 Iron Ingots");
                    break;
                case 5:
                    StarterHub.Buy(from, 2000, () => new Board(250), "250 Boards");
                    break;
                case 6:
                    StarterHub.Buy(from, 2000, () => new Leather(250), "250 Leather");
                    break;
                case 7:
                    StarterHub.Buy(from, 1000, () => new Cloth(250), "250 Cloth");
                    break;
                case 8:
                    StarterHub.Buy(from, 1500, AlchemyBundle, "Alchemy Starter Bundle");
                    break;
            }
        }
    }
}
