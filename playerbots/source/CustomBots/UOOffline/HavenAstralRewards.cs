using System;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.UOOffline;

public static class HavenWalletCommands
{
    public static void Initialize()
    {
        CommandSystem.Register("Wallet", AccessLevel.Player, e =>
        {
            var wallet = e.Mobile.Backpack?.FindItemByType<AdventurersWallet>();
            if (wallet == null) { e.Mobile.SendMessage("Keep your adventurer's wallet in your backpack."); return; }
            e.Mobile.CloseGump<HavenWalletGump>();
            e.Mobile.SendGump(new HavenWalletGump(wallet));
        });
        CommandSystem.Register("Withdraw", AccessLevel.Player, e => Withdraw(e.Mobile, e.ArgString));
        EventSink.Speech += OnSpeech;
    }

    internal static void Withdraw(Mobile from, string amountText)
    {
        var wallet = from.Backpack?.FindItemByType<AdventurersWallet>();
        if (wallet == null) { from.SendMessage("Keep your adventurer's wallet in your backpack."); return; }
        if (!int.TryParse(amountText.Trim(), out var amount)) { from.SendMessage("Say withdraw 1000, or use [withdraw 1000."); return; }
        wallet.Withdraw(from, amount);
    }

    internal static void OnSpeech(SpeechEventArgs e)
    {
        if (e.Handled || !e.Mobile.Player || e.Mobile.Backpack?.FindItemByType<AdventurersWallet>() == null) { return; }
        var words = e.Speech.Trim();
        if (!words.StartsWith("withdraw ", StringComparison.OrdinalIgnoreCase)) { return; }
        e.Handled = true;
        Withdraw(e.Mobile, words[9..]);
    }
}

public static class HavenAstralRewards
{
    public static void OnMonsterKilled(BaseCreature creature, Mobile player)
    {
        if (!Eligible(creature, player)) { return; }
        var chance = creature.HitsMax >= 4000 ? 1.0 : creature.HitsMax >= 1000 ? 0.15 : 0.05;
        if (Utility.RandomDouble() >= chance) { return; }
        Award(player, creature.HitsMax >= 4000 ? 3 : 1);
    }

    internal static bool Eligible(BaseCreature creature, Mobile player) =>
        player is PlayerMobile and not Server.CustomBots.PlayerBot && player.Alive &&
        player.Map == creature.Map && player.InRange(creature, 18) &&
        !creature.Controlled && !creature.Summoned && !creature.IsBonded && !creature.NoKillAwards &&
        creature is not BaseVendor && creature.Owners.Count == 0 && creature.HitsMax >= 100 && creature.Karma < 0;

    internal static bool Award(Mobile player, int amount)
    {
        var wallet = player.Backpack?.FindItemByType<AdventurersWallet>();
        if (wallet != null)
        {
            if (amount <= 0 || amount > long.MaxValue - wallet.AstralShards) { return false; }
            wallet.AstralShards += amount;
            player.SendMessage(0x482, $"You found {amount} Astral shard(s)! Wallet total: {wallet.AstralShards:N0}.");
            return true;
        }
        var shards = new AstralShard(amount);
        if (player.Backpack?.TryDropItem(player, shards, false) == true) { return true; }
        shards.MoveToWorld(player.Location, player.Map);
        return true;
    }

    internal static bool Buy(Mobile from, AdventurersWallet wallet, int index)
    {
        if (wallet.Deleted || from.Backpack == null || !wallet.IsChildOf(from.Backpack) || index < 0 || index > 2) { return false; }
        var price = (index + 1) * 20;
        if (wallet.AstralShards < price) { from.SendMessage($"You need {price} Astral shards."); return false; }
        var item = Create(index);
        if (!from.Backpack.TryDropItem(from, item, false)) { item.Delete(); from.SendMessage("Make room in your backpack; no shards were spent."); return false; }
        wallet.AstralShards -= price;
        from.SendMessage($"Purchased {item.Name} for {price} Astral shards.");
        return true;
    }

    internal static Item Create(int index) => index switch
    {
        0 => new AstralWeaversRing(),
        1 => new AstralGuardianMantle(),
        _ => new AstralFortuneEarrings()
    };

    public sealed class Menu : ItemListMenu, IHavenShop
    {
        private readonly AdventurersWallet _wallet;
        public Menu(AdventurersWallet wallet) : base("Astral treasures — paid only with wallet shards", [
            new ItemListEntry("Weaver's ring — 20 shards", 0x108A, 0x482),
            new ItemListEntry("Guardian mantle — 40 shards", 0x1515, 0x482),
            new ItemListEntry("Fortune earrings — 60 shards", 0x1087, 0x482)]) => _wallet = wallet;
        public Item CreateItem(int index) => Create(index);
        public override void OnResponse(NetState state, int index) => Buy(state.Mobile, _wallet, index);
    }
}

[SerializationGenerator(0)]
public partial class AstralShard : Item
{
    [Constructible]
    public AstralShard(int amount = 1) : base(0x1F19) { Name = "Astral shard"; Hue = 0x482; Stackable = true; Weight = 0; Amount = amount; }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack == null || !IsChildOf(from.Backpack)) { return; }
        var wallet = from.Backpack.FindItemByType<AdventurersWallet>();
        if (wallet != null && Amount <= long.MaxValue - wallet.AstralShards) { wallet.AstralShards += Amount; Delete(); }
    }
}

[SerializationGenerator(0)]
public partial class AstralWeaversRing : GoldRing
{
    [Constructible]
    public AstralWeaversRing()
    {
        Name = "Astral weaver's ring"; Hue = 0x482; LootType = LootType.Blessed;
        Attributes.SpellDamage = 30; Attributes.LowerManaCost = 10; Attributes.RegenMana = 3;
        SkillBonuses.SetValues(0, SkillName.Magery, 15); SkillBonuses.SetValues(1, SkillName.Spellweaving, 15);
    }
}

[SerializationGenerator(0)]
public partial class AstralGuardianMantle : Cloak
{
    [Constructible]
    public AstralGuardianMantle()
    {
        Name = "Astral guardian's mantle"; Hue = 0x482; LootType = LootType.Blessed;
        Attributes.RegenHits = 5; Attributes.DefendChance = 15; Attributes.Luck = 300;
    }
}

[SerializationGenerator(0)]
public partial class AstralFortuneEarrings : GoldEarrings
{
    [Constructible]
    public AstralFortuneEarrings()
    {
        Name = "Astral fortune earrings"; Hue = 0x482; LootType = LootType.Blessed;
        Attributes.LowerRegCost = 100; Attributes.Luck = 400; Attributes.RegenMana = 4;
    }
}
