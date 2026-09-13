using System;
using System.Linq;
using ModernUO.Serialization;
using Server.CustomBots;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenMarketExpansion
{
    public static void Initialize()
    {
        Timer.DelayCall(TimeSpan.FromSeconds(25), () =>
        {
            foreach (var center in HavenCommunityCenter.Registry)
            {
                if (!center.Deleted) { center.EnsureMarketTrades(); }
            }
        });
        CommandSystem.Register("minax", AccessLevel.Player, e =>
        {
            if (e.Length != 1 || !int.TryParse(e.GetString(0), out var amount) || !HavenMinaxCreditNote.Withdraw(e.Mobile, amount))
            { e.Mobile.SendMessage("Use [minax <amount> to withdraw 1–60,000 earned credits as a tradeable note. You need that balance and backpack space."); }
            else { e.Mobile.SendMessage("Minax credit note placed in your backpack."); }
        });
    }

    internal static HavenMarketStall Find(HavenMarketTrade trade)
    {
        foreach (var stall in HavenMarketStall.Registry)
        { if (!stall.Deleted && stall.Trade == trade && stall.Stock.Count < 24) { return stall; } }
        return null;
    }

    internal static void Work(HavenMarketStall stall)
    {
        var job = Get(stall);
        job.Step(stall, Utility.RandomDouble());
    }
    internal static HavenMarketExpedition Get(HavenMarketStall stall)
    {
        foreach (var item in stall.Items) { if (item is HavenMarketExpedition job) { return job; } }
        var created = new HavenMarketExpedition(); stall.AddItem(created); return created;
    }
}

// Only workshop goods use timed production. Dungeon stock comes from physical encounter rewards.
[SerializationGenerator(0)]
public partial class HavenMarketExpedition : Item
{
    [SerializableField(0)] private int _route;
    [SerializableField(1)] private int _progress;
    [SerializableField(2)] private int _credits;
    [SerializableField(3)] private int _completed;
    [SerializableField(4)] private int _failed;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenMarketExpedition() : base(1) { Visible = false; Movable = false; Weight = 0; }
    internal static string RouteName(int route) => route switch
    { 0 => "Blackthorn", 1 => "Shadowguard", 2 => "Doom", 3 => "Stygian Abyss", _ => "Pirate waters" };
    internal bool Step(HavenMarketStall stall, double roll)
    {
        if (Deleted || Parent != stall || stall.Deleted || stall.Artisan?.Deleted != false || !stall.Artisan.Alive || stall.Stock.Count >= 24) { return false; }
        if (stall.Trade == HavenMarketTrade.PetSupplies) { return Supply(stall, roll); }
        if (stall.Trade != HavenMarketTrade.DungeonSupplies) { return false; }
        if (HavenDungeonCrew.Dispatch(stall, Route)) { Route = (Route + 1) % 5; return true; }
        // Try another destination later if occupied or no qualified volunteers are available.
        Route = (Route + 1) % 5;
        return false;
    }
    private bool Supply(HavenMarketStall stall, double roll)
    {
        var pack = stall.Artisan.Backpack;
        if (pack == null || stall.Artisan.Skills.Tinkering.Base < 80 || stall.Artisan.Skills.Alchemy.Base < 80) { return false; }
        if (++Progress < 5) { return false; }
        Progress = 0;
        var index = Completed % 3;
        var type = index == 0 ? typeof(Ginseng) : index == 1 ? typeof(Leather) : typeof(IronIngot);
        if (pack.GetAmount(type) < 20)
        {
            var material = BotItemFactory.Create(type.FullName); material.Amount = 20; pack.DropItem(material);
            return false;
        }
        pack.ConsumeTotal(type, 20);
        if (roll >= 0.85) { Failed++; return false; }
        Item item = index == 0 ? new HavenBondingPotion() : index == 1 ? new HavenPetLeash() : new HavenHouseHitchingPost();
        List(stall, item, index == 0 ? 2500 : index == 1 ? 5000 : 15000, "Pet-supply workshop");
        Completed++;
        return true;
    }
    private void List(HavenMarketStall stall, Item item, int price, string source)
    {
        HavenMarketProvenance.Attach(item, source, (Parent as HavenMarketStall)?.Artisan?.Name ?? "Haven expedition crew");
        if (!stall.ListItem(item, price)) { item.Delete(); }
    }
}
