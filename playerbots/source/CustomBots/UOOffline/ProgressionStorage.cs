using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class ProgressionArchive : Bag
{
    public override string DefaultName => "Champion's Codex";

    [Constructible]
    public ProgressionArchive()
    {
        ItemID = 0x2259;
        Hue = 0x489;
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Keep the Champion's Codex in your backpack while using it.");
            return false;
        }

        if (!Accepts(dropped))
        {
            from.SendMessage("That does not belong in the Champion's Codex.");
            return false;
        }

        return base.OnDragDrop(from, dropped);
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (!Accepts(item))
        {
            from.SendMessage("That does not belong in the Champion's Codex.");
            return false;
        }

        return base.OnDragDropInto(from, item, p);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("The Champion's Codex must be in your backpack.");
            return;
        }

        var collected = CollectAll(from);
        if (collected > 0)
        {
            from.SendMessage($"The codex collected {collected} progression item(s) from your backpack.");
        }

        ChampionCodexGump.DisplayTo(from, this);
    }

    internal int CollectAll(Mobile from)
    {
        var pack = from.Backpack;
        if (pack == null)
        {
            return 0;
        }

        var candidates = new List<Item>();
        FindCandidates(pack, candidates);

        var collected = 0;
        foreach (var item in candidates)
        {
            if (item.Deleted || item.IsChildOf(this))
            {
                continue;
            }

            DropItem(item);
            collected++;
        }

        return collected;
    }

    private void FindCandidates(Container container, List<Item> candidates)
    {
        for (var i = 0; i < container.Items.Count; i++)
        {
            var item = container.Items[i];

            if (item == this || item.Deleted)
            {
                continue;
            }

            if (Accepts(item))
            {
                candidates.Add(item);
                continue;
            }

            if (item is Container child && child is not ProgressionArchive && child is not PeerlessKeyVault)
            {
                FindCandidates(child, candidates);
            }
        }
    }

    public static bool Accepts(Item item)
    {
        if (item is PowerScroll or StatCapScroll or ScrollofTranscendence or ScrollofAlacrity or ChampionSkull or HavenMark)
        {
            return true;
        }

        // ModernUO's pinned version does not yet contain mastery primers or
        // scroll binders. Recognize the conventional type names so later
        // content drops can use this archive without another migration.
        var typeName = item?.GetType().Name ?? string.Empty;

        return typeName.Contains("MasteryPrimer", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("ScrollBinder", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("ChampionToken", StringComparison.OrdinalIgnoreCase);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add("Stores Power/Stat/Transcendence/Alacrity scrolls");
        list.Add("Stores Champion Skulls, Haven Marks, primers and binders");
        list.Add("Double-click to collect items and open the scroll ledger");
    }
}

[SerializationGenerator(0)]
public partial class PeerlessKeyVault : Bag
{
    private static readonly HashSet<string> KnownKeyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Dread Horn
        "BlightedCotton",
        "ThornyBriar",
        "GnawsFang",
        "IrksBrain",
        "LissithsSilk",
        "SabrixsEye",
        "EssenceOfTheWind",

        // Common RunUO/ServUO peerless naming patterns. Exact classes are
        // accepted automatically if they are introduced by later ML content.
        "TravestyKey",
        "CitadelKey",
        "ParoxysmusKey",
        "MelisandeKey",
        "GrizzleKey",
        "EffusionKey"
    };

    public override string DefaultName => "peerless key vault";

    [Constructible]
    public PeerlessKeyVault()
    {
        ItemID = 0x9A8;
        Hue = 0x497;
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Keep the Peerless key vault in your backpack while using it.");
            return false;
        }

        if (!Accepts(dropped))
        {
            from.SendMessage("That is not recognized as a Peerless key.");
            return false;
        }

        return base.OnDragDrop(from, dropped);
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (!Accepts(item))
        {
            from.SendMessage("That is not recognized as a Peerless key.");
            return false;
        }

        return base.OnDragDropInto(from, item, p);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("The Peerless key vault must be in your backpack.");
            return;
        }

        var collected = CollectAll(from);
        if (collected > 0)
        {
            from.SendMessage($"The vault collected {collected} Peerless key item(s).");
        }

        base.OnDoubleClick(from);
    }

    private int CollectAll(Mobile from)
    {
        var pack = from.Backpack;
        if (pack == null)
        {
            return 0;
        }

        var candidates = new List<Item>();
        FindCandidates(pack, candidates);

        foreach (var item in candidates)
        {
            DropItem(item);
        }

        return candidates.Count;
    }

    private void FindCandidates(Container container, List<Item> candidates)
    {
        for (var i = 0; i < container.Items.Count; i++)
        {
            var item = container.Items[i];

            if (item == this || item.Deleted)
            {
                continue;
            }

            if (Accepts(item))
            {
                candidates.Add(item);
                continue;
            }

            if (item is Container child && child is not PeerlessKeyVault && child is not ProgressionArchive)
            {
                FindCandidates(child, candidates);
            }
        }
    }

    public static bool Accepts(Item item)
    {
        if (item == null)
        {
            return false;
        }

        var typeName = item.GetType().Name;
        if (KnownKeyTypes.Contains(typeName))
        {
            return true;
        }

        return typeName.Contains("Peerless", StringComparison.OrdinalIgnoreCase) &&
               (typeName.Contains("Key", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("Essence", StringComparison.OrdinalIgnoreCase));
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add("Dedicated storage for Peerless encounter keys");
        list.Add("Double-click to collect recognized keys from your backpack");
        list.Add("UO Offline rule: Peerless keys do not expire");
    }
}
