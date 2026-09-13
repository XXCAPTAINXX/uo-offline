using System;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public static partial class HavenFieldGuide
{
    public static void Initialize()
    {
        foreach (var command in new[] { "guide", "havenhelp", "wiki" })
        {
            CommandSystem.Register(command, AccessLevel.Player, e => DisplayTo(e.Mobile, e.ArgString));
        }
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void Welcome(PlayerMobile player)
    {
        if (player is not Server.CustomBots.PlayerBot)
        {
            player.SendMessage("Haven Field Guide: type [guide for getting started, custom systems and commands.");
        }
    }

    internal static string NormalizeQuery(string query)
    {
        query = (query ?? "").Trim();
        return query.Length > 40 ? query[..40] : query;
    }

    internal static bool Matches(int index, string query) => index >= 0 && index < Topics.Length &&
        (query.Length == 0 || Topics[index].Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
         Topics[index].Text.Contains(query, StringComparison.OrdinalIgnoreCase));

    public static void DisplayTo(Mobile from, string query = "", int chapter = 0)
    {
        if (from?.Deleted != false || from.NetState == null)
        {
            return;
        }
        from.CloseGump<HavenFieldGuideGump>();
        from.SendGump(new HavenFieldGuideGump(query, chapter));
    }

    internal static bool ClaimBook(Mobile from)
    {
        if (from?.Deleted != false || !from.Player || from.Backpack == null)
        {
            return false;
        }
        if (from.Backpack.FindItemByType<HavenFieldGuideBook>() != null ||
            from.BankBox?.FindItemByType<HavenFieldGuideBook>() != null)
        {
            from.SendMessage("Your field guide is already in your backpack or bank. [guide always opens it.");
            return false;
        }
        var book = new HavenFieldGuideBook();
        if (!from.Backpack.TryDropItem(from, book, false))
        {
            book.Delete();
            from.SendMessage("Make room for the book, or keep using [guide without carrying one.");
            return false;
        }
        from.SendMessage("Your free Haven Field Guide is in your backpack. Double-click it to read.");
        return true;
    }
}

[SerializationGenerator(0)]
public partial class HavenFieldGuideBook : Item
{
    [Constructible]
    public HavenFieldGuideBook() : base(0xFF1)
    {
        Name = "Haven Field Guide";
        Hue = 0x489;
        Weight = 1;
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack) || from.Map == Map && from.InRange(this, 2))
        {
            HavenFieldGuide.DisplayTo(from);
        }
    }
}

public sealed class HavenFieldGuideGump : Gump
{
    private readonly string _query;
    private readonly int _chapter;

    public HavenFieldGuideGump(string query = "", int chapter = 0) : base(40, 40)
    {
        _query = HavenFieldGuide.NormalizeQuery(query);
        _chapter = -1;
        for (var i = 0; i < HavenFieldGuide.Topics.Length; i++)
        {
            if (HavenFieldGuide.Matches(i, _query) && (_chapter < 0 || i == chapter))
            {
                _chapter = i;
            }
        }
        AddBackground(0, 0, 680, 550, 9270);
        AddItem(20, 20, 0xFF1, 0x489);
        AddLabel(63, 20, 1152, "Haven Field Guide");
        AddLabel(63, 43, 2101, "Your world, your companion, your next adventure");
        AddImageTiled(20, 77, 410, 25, 2624);
        AddTextEntry(26, 79, 394, 22, 1152, 1, _query);
        AddButton(444, 79, 4005, 4007, 1);
        AddLabel(480, 80, 1152, "Search");
        AddButton(563, 79, 4017, 4019, 2);
        AddLabel(599, 80, 1152, "Clear");

        var row = 0;
        for (var i = 0; i < HavenFieldGuide.Topics.Length; i++)
        {
            if (!HavenFieldGuide.Matches(i, _query))
            {
                continue;
            }
            var y = 117 + row++ * 21;
            AddButton(20, y, 4005, 4007, 100 + i);
            AddLabelCropped(55, y + 1, 145, 20, i == _chapter ? 1152 : 2101, HavenFieldGuide.Topics[i].Title);
        }
        if (_chapter >= 0)
        {
            AddLabel(215, 117, 1152, HavenFieldGuide.Topics[_chapter].Title);
            AddHtml(212, 146, 446, 345, HavenFieldGuide.Topics[_chapter].Html, true, true);
        }
        else
        {
            AddHtml(212, 146, 446, 345, "No matching chapter. Try wallet, pet, mastery, repair or a command name. Clear shows all chapters.", true, true);
        }
        AddButton(20, 510, 4014, 4016, 3);
        AddLabel(57, 511, 1152, "Start here");
        AddButton(219, 510, 4005, 4007, 4);
        AddLabel(256, 511, 1152, "Free book");
        AddLabel(408, 511, 2101, "[guide opens this anytime");
        AddButton(630, 510, 4017, 4019, 0);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        switch (info.ButtonID)
        {
            case 0: return;
            case 1: HavenFieldGuide.DisplayTo(from, info.GetTextEntry(1)); return;
            case 2:
            case 3: HavenFieldGuide.DisplayTo(from); return;
            case 4:
                HavenFieldGuide.ClaimBook(from);
                HavenFieldGuide.DisplayTo(from, _query, _chapter);
                return;
            default:
                var chapter = info.ButtonID - 100;
                if (HavenFieldGuide.Matches(chapter, _query))
                {
                    HavenFieldGuide.DisplayTo(from, _query, chapter);
                }
                return;
        }
    }
}
