using System;
using System.Collections.Generic;
using Server.CustomBots;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public static class HavenBotLoot
{
    internal static PlayerMobile PlayerOwner(PlayerBot bot)
    {
        var party = Party.Get(bot);
        if (party == null) { return null; }
        if (party.Leader is PlayerMobile { Deleted: false, Player: true } leader && leader is not PlayerBot) { return leader; }
        foreach (var member in party.Members)
        { if (member.Mobile is PlayerMobile { Deleted: false, Player: true } player && player is not PlayerBot) { return player; } }
        return null;
    }
    internal static void Receive(PlayerBot bot, Item item)
    {
        if (bot?.Deleted != false || item?.Deleted != false) { return; }
        if (item.Parent != null && item.RootParent != bot && (item.Parent is not Corpse corpse || !CanLoot(bot, corpse))) { return; }
        var owner = PlayerOwner(bot);
        if (owner != null)
        {
            if (owner.Backpack == null) { owner.AddItem(new Backpack()); }
            foreach (var companion in owner.GetMobilesInRange<HavenCompanion>(24))
            {
                if (!companion.Deleted && companion.Alive && !companion.IsDeadPet && companion.BoundOwner == owner &&
                    companion.ControlMaster == owner && companion.Backpack?.TryDropItem(owner, item, false) == true) { return; }
            }
            // Earned loot must not vanish or fall at the bot's feet if both packs are full.
            owner.Backpack.DropItem(item);
            return;
        }
        if (bot.Backpack == null) { bot.AddItem(new Backpack()); }
        bot.Backpack.DropItem(item);
        HavenMarketProduction.Consign(bot, item);
    }
    internal static bool CanLoot(PlayerBot bot, Corpse corpse, int range = 2)
    {
        if (bot?.Deleted != false || !bot.Alive || corpse?.Deleted != false || corpse.Owner is not BaseCreature owner ||
            owner.Controlled || owner.Summoned || owner.Owners.Count > 0 || corpse.Map != bot.Map || !bot.InRange(corpse, range) || !bot.InLOS(corpse) || corpse.IsCriminalAction(bot)) { return false; }
        var party = Party.Get(bot);
        foreach (var attacker in corpse.Aggressors)
        {
            var master = attacker is BaseCreature pet ? pet.GetMaster() : attacker;
            if (master == bot || master != null && party != null && Party.Get(master) == party) { return true; }
        }
        return false;
    }
    internal static void Collect(PlayerBot bot)
    {
        if (!bot.Alive || bot.Combatant is Mobile { Alive: true }) { return; }
        foreach (var corpse in bot.GetItemsInRange<Corpse>(2))
        {
            if (!CanLoot(bot, corpse) || !corpse.Items.Exists(i => i.Movable && !i.IsVirtualItem)) { continue; }
            foreach (var item in corpse.Items.ToArray())
            {
                if (item.Deleted || !item.Movable || item.IsVirtualItem || !corpse.CheckLoot(bot, item)) { continue; }
                Receive(bot, item);
            }
            return;
        }
    }
}

public static class HavenDungeonCourtesy
{
    private sealed class Offer
    {
        public PlayerMobile Player;
        public DateTime Until;
        public PlayerBotBehavior Resume;
    }
    private static readonly Dictionary<PlayerBot, Offer> Offers = new();
    private static readonly Dictionary<PlayerBot, DateTime> Next = new();
    internal static void Forget(PlayerBot bot) { Offers.Remove(bot); Next.Remove(bot); }
    internal static bool IsDungeon(PlayerBot bot) => DungeonRegistry.IsInDungeon(bot) || HavenDungeonCrew.For(bot) != null;
    internal static bool Tick(PlayerBot bot)
    {
        if (HavenBotLoot.PlayerOwner(bot) != null) { Offers.Remove(bot); HavenBotLoot.Collect(bot); return false; }
        if (Offers.TryGetValue(bot, out var offer))
        {
            if (Core.Now >= offer.Until || offer.Player.Deleted || offer.Player.Map != bot.Map)
            { Reply(bot, offer.Player, false); }
            return true;
        }
        if (!bot.Alive || bot.LifecycleExempt && HavenDungeonCrew.For(bot) == null || HavenGuildCrew.Retained(bot) || !IsDungeon(bot) ||
            Next.TryGetValue(bot, out var due) && Core.Now < due) { return false; }
        foreach (var human in bot.GetMobilesInRange<PlayerMobile>(18))
        {
            if (human is PlayerBot || human.NetState == null || !human.Player || human.Hidden || !human.Alive) { continue; }
            OfferTo(bot, human); return true;
        }
        return false;
    }
    internal static void OfferTo(PlayerBot bot, PlayerMobile player)
    {
        if (Offers.ContainsKey(bot) || bot?.Deleted != false || player?.Deleted != false) { return; }
        var crew = HavenDungeonCrew.For(bot);
        if (crew != null && crew.Workers.Count > 0 && crew.Workers[0] != bot) { bot = crew.Workers[0]; }
        if (Offers.ContainsKey(bot)) { return; }
        Offers[bot] = new Offer { Player = player, Until = Core.Now + TimeSpan.FromSeconds(30), Resume = bot.Behavior };
        crew?.Hold();
        bot.Combatant = null; bot.Warmode = false; bot.Behavior = new IdleBehavior();
        bot.Say("We can help your party, or leave the dungeon to you. Our party loot will be yours.");
        player.SendGump(new HavenDungeonOfferGump(bot));
    }
    internal static bool Reply(PlayerBot bot, PlayerMobile player, bool join)
    {
        if (!Offers.TryGetValue(bot, out var offer) || offer.Player != player) { return false; }
        Offers.Remove(bot); Next[bot] = Core.Now + TimeSpan.FromMinutes(10);
        var crew = HavenDungeonCrew.For(bot);
        if (join && Core.Now <= offer.Until && bot.Alive && player?.Deleted == false && player.Alive && player.Map == bot.Map)
        {
            var existing = Party.Get(player);
            if (player.Party != null && existing == null) { join = false; }
            if (join && (existing == null || existing.Leader == player && existing.Members.Count + existing.Candidates.Count < Party.Capacity))
            {
                if (crew != null) { return crew.Join(player); }
                // Leave the NPC party before joining the real player's native party.
                BotPartyManager.DisbandInvolving(bot);
                var oldParty = Party.Get(bot); oldParty?.Remove(bot);
                var party = existing ?? new Party(player); player.Party = party;
                if (party.Members.Count + party.Candidates.Count < Party.Capacity)
                { party.Add(bot); bot.Behavior = new PlayerGroupBehavior(); return true; }
            }
            player.SendMessage("Only the party leader can recruit, and your party needs free slots. The bots will leave you the area.");
        }
        if (crew != null) { crew.End(false); }
        else { Leave(bot); }
        return true;
    }
    internal static void Leave(PlayerBot bot)
    {
        if (bot?.Deleted != false || HavenBotLoot.PlayerOwner(bot) != null || HavenGuildCrew.Retained(bot)) { return; }
        BotPartyManager.DisbandInvolving(bot); Party.Get(bot)?.Remove(bot);
        bot.Combatant = null; bot.Warmode = false;
        var point = new Point3D(3503, 2572, 20);
        point = HavenOriginalDungeons.SpawnPoint(Map.Trammel, point, point);
        bot.MoveToWorld(point, Map.Trammel); bot.Behavior = bot.Alive ? new TravelerBehavior() : new GhostBehavior();
        Next[bot] = Core.Now + TimeSpan.FromMinutes(10);
    }
    internal static bool Waiting(PlayerBot bot)
    {
        if (Offers.ContainsKey(bot)) { return true; }
        var crew = HavenDungeonCrew.For(bot);
        return crew != null && crew.Workers.Count > 0 && Offers.ContainsKey(crew.Workers[0]);
    }
}

public sealed class HavenDungeonOfferGump : Gump
{
    private readonly PlayerBot _bot;
    internal HavenDungeonOfferGump(PlayerBot bot) : base(70, 70)
    {
        _bot = bot; AddBackground(0, 0, 500, 240, 9270);
        AddLabel(25, 20, 1152, "Dungeon adventurers");
        AddHtml(25, 55, 445, 85, $"{Utility.FixHtml(bot.Name)} offers to help. Party loot goes to the real party leader's nearby companion, or their backpack. Decline or ignore for 30 seconds and the bots leave this area.", true, false);
        AddButton(25, 170, 4005, 4007, 1); AddLabel(65, 170, 1152, "Join my party");
        AddButton(270, 170, 4017, 4019, 0); AddLabel(310, 170, 1152, "Please leave");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    { if (sender.Mobile is PlayerMobile player) { HavenDungeonCourtesy.Reply(_bot, player, info.ButtonID == 1); } }
}
