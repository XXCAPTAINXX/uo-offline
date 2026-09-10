using System;
using Server.Engines.Craft;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.UOOffline;

public static class HavenSovereignStore
{
    internal sealed record Offer(string Name, int Price, int Icon, string Description, Func<Item> Create);
    internal static readonly Offer[] Offers =
    [
        new("Pet bonding potion", 50, 0xF0E, "Instantly bonds your own living, bondable pet nearby. The potion is consumed only on success.", () => new HavenBondingPotion()),
        new("Pet dye", 75, 0xFAB, "Choose a cosmetic pet color, or restore its original color. One use; stats are unchanged.", () => new HavenPetDye()),
        new("Pet shrinking leash", 100, 0x14F8, "Shrink your pet for safe transport. Use the resulting statue to bring it back.", () => new HavenPetLeash()),
        new("Wayfarer's rune pouch", 100, 0xE79, "Stores up to 60,000 blank recall runes without using extra pack slots. Cast Mark on it to make a marked rune.", () => new HavenRunePouch()),
        new("Gatherer's resource satchel", 250, 0xE76, "Carries crafting resources, gems, fish and bandages at one tenth their normal weight. Equipment and other bags cannot go inside.", () => new HavenResourceSatchel()),
        new("Agapite runic hammer", 350, 0x13E3, "25 uses for runic blacksmith crafting. Normal crafting skill and material requirements apply.", () => new RunicHammer(CraftResource.Agapite, 25)),
        new("Mercy's Endless Bandage", 500, 0xE21, "A reusable bandage that is never consumed. Normal Healing and Veterinary skill checks still apply. Keep it in your pack.", () => new HavenEndlessBandage()),
        new("The Gilded Pathfinder", 750, 0xF39, "A golden shovel that takes you beside the dig site of a decoded, unfinished treasure map in your pack. Travel restrictions, digging and guardians still apply.", () => new HavenGoldenShovel())
    ];
    public static void DisplayTo(Mobile from, int tab = 0, int page = 0)
    {
        var account = HavenSovereignAccount.Get(from);
        if (account == null) { return; }
        from.CloseGump<StoreGump>(); from.SendGump(new StoreGump(from, tab, page));
    }
    internal static bool Buy(Mobile from, int index)
    {
        if (!HavenSovereigns.IsPlayer(from) || !from.Alive || from.Backpack == null || index < 0 || index >= Offers.Length) { return false; }
        var account = HavenSovereignAccount.Get(from); var offer = Offers[index];
        if (account.Balance < offer.Price) { from.SendMessage("You do not have enough Sovereigns."); return false; }
        var item = offer.Create();
        // Reserve funds before delivery. Refund if the backpack refuses the item.
        account.Balance -= offer.Price;
        var delivered = false;
        try
        {
            delivered = from.Backpack.TryDropItem(from, item, false);
            if (!delivered) { from.SendMessage("Make room in your backpack. No Sovereigns were spent."); return false; }
            account.Log($"-{offer.Price:N0}  Purchased {offer.Name}");
            from.SendMessage(0x482, $"Purchased {offer.Name} for {offer.Price:N0} Sovereigns."); return true;
        }
        finally { if (!delivered) { account.Balance += offer.Price; item.Delete(); } }
    }

    public sealed class StoreGump : Gump
    {
        private readonly Mobile _owner;
        private readonly int _tab;
        private readonly int _page;
        private readonly int _selected;
        private bool _answered;
        public StoreGump(Mobile owner, int tab = 0, int page = 0, int selected = -1) : base(50, 45)
        {
            _owner = owner; _tab = tab == 1 ? 1 : 0; _selected = selected >= 0 && selected < Offers.Length ? selected : -1;
            var account = HavenSovereignAccount.Get(owner);
            var pageCount = _tab == 0 ? (Offers.Length + 3) / 4 : Math.Max(1, (account.History.Count + 7) / 8);
            _page = Math.Clamp(page, 0, pageCount - 1);
            AddPage(0); AddBackground(0, 0, 600, 500, 9270);
            AddLabel(26, 18, 1152, "HAVEN SOVEREIGN STORE");
            AddLabel(26, 44, 1152, $"Balance: {account.Balance:N0} Sovereigns");
            AddLabel(326, 44, 1152, $"Lifetime earned: {account.Earned:N0}");
            Button(26, 78, 1, "Rewards"); Button(195, 78, 2, "Achievements & history");
            if (_selected >= 0)
            {
                var offer = Offers[_selected];
                AddItem(32, 132, offer.Icon); AddLabel(82, 128, 1152, offer.Name);
                AddLabel(82, 158, 1152, $"Price: {offer.Price:N0} Sovereigns");
                AddHtml(28, 205, 540, 140, $"<BASEFONT COLOR=#FFFFFF>{offer.Description}</BASEFONT>", false, true);
                Button(28, 382, 500, "Confirm purchase"); Button(300, 382, 1, "Back to rewards");
            }
            else if (_tab == 0)
            {
                for (var row = 0; row < 4; row++)
                {
                    var index = _page * 4 + row; if (index >= Offers.Length) { break; }
                    var offer = Offers[index]; var y = 125 + row * 65;
                    AddItem(30, y + 2, offer.Icon); AddLabel(82, y, 1152, offer.Name);
                    AddLabel(82, y + 24, 1152, $"{offer.Price:N0} Sovereigns"); Button(455, y + 12, 100 + index, "Details");
                }
                AddLabel(28, 400, 1152, "Earn through exploration, skill milestones, encounters and bosses.");
            }
            else
            {
                AddLabel(28, 112, 1152, $"{account.Achievements.Count:N0} achievements | {account.Kills:N0} credited monster kills");
                AddHtml(28, 140, 540, 75, "<BASEFONT COLOR=#FFFFFF>Towns +10; dungeons +20 once per place and facet. Skills 50/100: +5/+25. Pets, stats and kill milestones also pay. Major bosses: +50 to +100. Existing skill milestones count automatically.</BASEFONT>", false, true);
                for (var row = 0; row < 8; row++)
                {
                    var index = account.History.Count - 1 - (_page * 8 + row); if (index < 0) { break; }
                    var line = account.History[index];
                    AddLabelCropped(28, 227 + row * 23, 540, 22, 1152, line);
                    AddTooltip(1042971, line);
                }
            }
            if (_selected < 0)
            {
                if (_page > 0) { Button(28, 436, 3, "Previous"); }
                AddLabel(224, 438, 1152, $"Page {_page + 1} / {pageCount}");
                if (_page + 1 < pageCount) { Button(380, 436, 4, "Next"); }
            }
            Button(486, 466, 0, "Close");
        }
        private void Button(int x, int y, int id, string text)
        { AddButton(x, y, 4005, 4007, id, GumpButtonType.Reply, 0); AddLabel(x + 34, y, 1152, text); }
        public override void OnResponse(NetState sender, in RelayInfo info)
        { Respond(sender.Mobile, info.ButtonID); }
        internal void Respond(Mobile sender, int id)
        {
            if (_answered || sender != _owner || !HavenSovereigns.IsPlayer(_owner)) { return; }
            _answered = true;
            if (id == 0) { return; }
            if (id == 500 && _selected >= 0) { Buy(_owner, _selected); DisplayTo(_owner); }
            else if (id == 1 || id == 2) { DisplayTo(_owner, id - 1); }
            else if (id == 3 || id == 4) { DisplayTo(_owner, _tab, _page + (id == 3 ? -1 : 1)); }
            else if (_selected < 0 && _tab == 0 && id >= 100 + _page * 4 && id < 100 + Math.Min(Offers.Length, (_page + 1) * 4))
            { _owner.SendGump(new StoreGump(_owner, 0, _page, id - 100)); }
        }
    }
}
