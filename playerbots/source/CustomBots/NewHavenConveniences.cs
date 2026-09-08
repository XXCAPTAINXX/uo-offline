// =========================================================================
// NewHavenConveniences.cs — compact New Haven pet and progression utilities.
// =========================================================================

using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.CustomBots
{
    [SerializationGenerator(0, false)]
    public partial class NewHavenHitchingPost : Item
    {
        [Constructible]
        public NewHavenHitchingPost() : base(0x14E7)
        {
            Name = "New Haven Hitching Post - Free Pet Shrinking";
            Hue = 0x59B;
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from?.Backpack == null || !from.CheckAlive())
            {
                return;
            }

            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.SendMessage("You are too far away from the hitching post.");
                return;
            }

            from.SendMessage(0x35, "Target one of your living pets to shrink it for free.");
            from.Target = new HitchingTarget();
        }

        private sealed class HitchingTarget : Target
        {
            public HitchingTarget() : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from?.Backpack == null || targeted is not BaseCreature pet)
                {
                    from?.SendMessage("That is not a pet.");
                    return;
                }

                if (!pet.Controlled || pet.ControlMaster != from)
                {
                    from.SendMessage("You may only shrink a pet that you control.");
                    return;
                }

                if (pet.Body.IsHuman || pet.IsDeadPet || pet.Summoned)
                {
                    from.SendMessage("That creature cannot be shrunk.");
                    return;
                }

                if ((pet is PackLlama or PackHorse or Beetle) && pet.Backpack?.Items.Count > 0)
                {
                    from.SendMessage("Unload the pack animal before shrinking it.");
                    return;
                }

                if (pet.Combatant != null || pet.Aggressors.Count > 0 || pet.Aggressed.Count > 0)
                {
                    from.SendMessage("Your pet must be out of combat before it can be shrunk.");
                    return;
                }

                var token = new ShrunkenPet(pet, from);
                if (!from.AddToBackpack(token))
                {
                    token.Delete();
                    from.SendMessage("Make room in your backpack first.");
                    return;
                }

                pet.ControlTarget = null;
                pet.ControlOrder = OrderType.Stay;
                pet.Internalize();
                pet.SetControlMaster(null);
                pet.IsStabled = false;
                pet.StabledBy = null;

                from.SendMessage(0x35, $"{pet.Name ?? "Your pet"} has been safely shrunk.");
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class ShrunkenPet : Item
    {
        [SerializableField(0)]
        private BaseCreature _pet;

        [SerializableField(1)]
        private Mobile _owner;

        public ShrunkenPet(BaseCreature pet, Mobile owner) : base(ShrinkTable.Lookup(pet))
        {
            _pet = pet;
            _owner = owner;
            Name = $"Shrunken Pet: {pet?.Name ?? "pet"}";
            Hue = (pet?.Hue ?? 0) & 0x0FFF;
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (_pet?.Deleted == false)
            {
                list.Add($"Pet: {_pet.Name}");
                list.Add("Double-click to restore this pet");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("The shrunken pet must be in your backpack.");
                return;
            }

            if (_owner != null && _owner != from)
            {
                from.SendMessage("This shrunken pet belongs to another character.");
                return;
            }

            if (_pet?.Deleted != false)
            {
                from.SendMessage("The pet represented by this item no longer exists.");
                Delete();
                return;
            }

            if (!from.CheckAlive())
            {
                return;
            }

            if (from.Followers + _pet.ControlSlots > from.FollowersMax)
            {
                from.SendMessage("You have too many followers to restore that pet.");
                return;
            }

            _pet.SetControlMaster(from);
            _pet.ControlTarget = from;
            _pet.ControlOrder = OrderType.Follow;
            _pet.IsStabled = false;
            _pet.StabledBy = null;
            _pet.MoveToWorld(from.Location, from.Map);

            if (Core.SE)
            {
                _pet.Loyalty = BaseCreature.MaxLoyalty;
            }

            from.SendMessage(0x35, $"{_pet.Name ?? "Your pet"} has been restored.");
            _pet = null;
            Delete();
        }
    }

    public abstract class CollectingArchive : Bag
    {
        protected CollectingArchive(string name, int hue)
        {
            Name = name;
            Hue = hue;
            LootType = LootType.Blessed;
            Weight = 1.0;
            ItemID = 0x2252;
        }

        protected abstract bool Accepts(Item item);

        public override bool CheckHold(
            Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (!Accepts(item))
            {
                if (message)
                {
                    m?.SendMessage("That item does not belong in this organizer.");
                }

                return false;
            }

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("This organizer must be in your backpack.");
                return;
            }

            int collected = CollectFrom(from.Backpack);

            if (collected > 0)
            {
                from.SendMessage(0x35, $"Collected {collected:N0} compatible item{(collected == 1 ? "" : "s")}.");
            }

            base.OnDoubleClick(from);
        }

        private int CollectFrom(Container source)
        {
            var found = new List<Item>();
            Scan(source, found);

            int count = 0;
            foreach (var item in found)
            {
                if (item?.Deleted == false && Accepts(item))
                {
                    DropItem(item);
                    count++;
                }
            }

            return count;
        }

        private void Scan(Container source, List<Item> found)
        {
            foreach (var item in source.Items)
            {
                if (item == this)
                {
                    continue;
                }

                if (Accepts(item))
                {
                    found.Add(item);
                }
                else if (item is Container nested)
                {
                    Scan(nested, found);
                }
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class ChampionsArchive : CollectingArchive
    {
        [Constructible]
        public ChampionsArchive() : base("Champion's Archive", 0x489)
        {
        }

        protected override bool Accepts(Item item) =>
            item is SpecialScroll or ChampionSkull or ArmsAndWeaponsPrimer;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Stores Power/Stat/Alacrity/Transcendence scrolls, primers, and champion skulls");
            list.Add("Double-click to collect compatible items from your backpack");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PeerlessKeyVault : CollectingArchive
    {
        private static readonly HashSet<string> KnownKeyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "IrksBrain",
            "SabrixEye",
            "SabrixsEye",
            "LissithsSilk",
            "MaleficClaw",
            "DryadsBlessing",
            "BlightedCotton",
            "GelatanousSkull",
            "PiecesOfCrystal",
            "CrushedCrystals",
            "ScatteredCrystals",
            "ShatteredCrystals",
            "SpeckledPoisonSac",
            "SpleenOfThePutrefier",
            "PartiallyDigestedTorso",
            "DisintegratingThesisNotes",
            "ParoxysmusKey",
            "PrismOfLightAdmissionTicket",
            "RareSerpentEgg",
            "DraconicOrb",
            "MasterKey"
        };

        [Constructible]
        public PeerlessKeyVault() : base("Peerless Key Vault - Keys Never Expire", 0x47E)
        {
        }

        protected override bool Accepts(Item item)
        {
            if (item == null)
            {
                return false;
            }

            for (Type type = item.GetType(); type != null; type = type.BaseType)
            {
                if (KnownKeyNames.Contains(type.Name) ||
                    type.Name.Contains("PeerlessKey", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Stores Peerless encounter keys");
            list.Add("Double-click to collect compatible keys from your backpack");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class StarterOrganizerStone : Item
    {
        [Constructible]
        public StarterOrganizerStone() : base(0xED4)
        {
            Name = "Progression Organizer Stone";
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

            from.SendGump(new StarterOrganizerStoneGump());
        }
    }

    public class StarterOrganizerStoneGump : StaticGump<StarterOrganizerStoneGump>
    {
        public override bool Singleton => true;

        public StarterOrganizerStoneGump() : base(140, 110)
        {
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 390, 180, 5054);
            builder.AddBackground(10, 10, 370, 160, 3000);
            builder.AddHtml(25, 20, 340, 24, "<CENTER><B>Progression Organizers</B></CENTER>");

            builder.AddButton(25, 60, 4005, 4007, 1);
            builder.AddHtml(60, 62, 290, 20, "Champion's Archive - 250 gp");

            builder.AddButton(25, 95, 4005, 4007, 2);
            builder.AddHtml(60, 97, 290, 20, "Peerless Key Vault - 250 gp");

            builder.AddButton(140, 135, 4005, 4007, 0);
            builder.AddHtml(175, 137, 90, 20, "Close");
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    StarterHub.Buy(from, 250, () => new ChampionsArchive(), "Champion's Archive");
                    break;
                case 2:
                    StarterHub.Buy(from, 250, () => new PeerlessKeyVault(), "Peerless Key Vault");
                    break;
            }
        }
    }
}
