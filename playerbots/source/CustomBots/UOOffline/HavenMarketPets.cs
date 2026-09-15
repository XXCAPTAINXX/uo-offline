using System;
using ModernUO.Serialization;
using Server.CustomBots;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

// A transferable listing holds the actual pet. It never rerolls on preview or purchase.
[SerializationGenerator(0)]
public partial class HavenMarketPetTicket : ShrunkenPet
{
    [Constructible]
    public HavenMarketPetTicket() { }
    public override string DefaultName => Pet?.Deleted == false ? $"Market pet: {Pet.Name}" : "empty market pet ticket";

    internal bool Claim(Mobile from)
    {
        var pet = Pet;
        if (Deleted || from?.Deleted != false || !from.Player || !from.Alive || from.Backpack == null ||
            !IsChildOf(from.Backpack) || pet?.Deleted != false || !pet.Alive || pet.IsDeadPet ||
            pet.Controlled || pet.ControlMaster != null || pet.Map != Map.Internal || Owner != null ||
            from.Map == null || from.Map == Map.Internal || from.Followers + pet.ControlSlots > from.FollowersMax)
        {
            return false;
        }
        // Ownership is transferred only after the ordinary follower checks succeed.
        if (!pet.SetControlMaster(from)) { return false; }
        Pet = null;
        pet.IsStabled = false;
        pet.StabledBy = null;
        if (!pet.Owners.Contains(from)) { pet.Owners.Add(from); }
        pet.ControlOrder = OrderType.Follow;
        pet.ControlTarget = from;
        pet.Loyalty = BaseCreature.MaxLoyalty;
        pet.BondingBegin = Core.Now - pet.BondingDelay - TimeSpan.FromSeconds(1);
        pet.MoveToWorld(from.Location, from.Map);
        from.SendMessage("Your purchased pet is ready. Feed it suitable food to bond when you meet its normal taming requirement.");
        Delete();
        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!Claim(from)) { from.SendMessage("Keep the ticket in your backpack and free the required follower slots. The ticket was kept."); }
    }

    public override void OnAfterDelete()
    {
        // A market ticket has no former player owner to restore to. Avoid an orphaned internal pet.
        var pet = Pet;
        Pet = null;
        if (pet?.Deleted == false && !pet.Controlled && pet.Map == Map.Internal) { pet.Delete(); }
        base.OnAfterDelete();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Transferable market pet; use Animal Lore after purchase to inspect."}");
        if (Pet is { Deleted: false } pet)
        {
            list.Add($"{"STR / DEX / INT:"} {pet.RawStr} / {pet.RawDex} / {pet.RawInt}");
            list.Add($"{"Hits / Stamina / Mana:"} {pet.HitsMax} / {pet.StamMax} / {pet.ManaMax}");
            list.Add($"{"Taming requirement:"} {pet.MinTameSkill:F1}");
            list.Add($"{"Wrestling / Tactics / Resist:"} {pet.Skills.Wrestling.Base:F1} / {pet.Skills.Tactics.Base:F1} / {pet.Skills.MagicResist.Base:F1}");
        }
    }
}

public static class HavenMarketPets
{
    internal static bool Consign(PlayerBot tamer, BaseCreature pet)
    {
        if (tamer?.Deleted != false || HavenGuildCrew.Retained(tamer) || BotPlayerParty.InPlayerParty(tamer) || pet?.Deleted != false || !pet.Alive || pet.IsDeadPet ||
            !pet.Controlled || pet.ControlMaster != tamer || pet.Summoned || pet.IsBonded || pet.IsStabled)
        {
            return false;
        }
        foreach (var owner in pet.Owners)
        {
            if (owner != tamer) { return false; }
        }
        var stall = HavenMarketExpansion.Find(HavenMarketTrade.Pets);
        if (stall == null || stall.Stock.Count >= 24) { return false; }
        var ticket = new HavenMarketPetTicket { Pet = pet, Hue = pet.Hue };
        var tier = pet.Backpack?.FindItemByType<HavenPetRarity>()?.Tier ?? 0;
        var price = Math.Max(1500, (int)(pet.MinTameSkill * 150)) * (tier + 1);
        if (!stall.ListItem(ticket, price))
        {
            ticket.Pet = null;
            ticket.Delete();
            return false;
        }
        pet.SetControlMaster(null);
        pet.ControlTarget = null;
        pet.Owners.Remove(tamer);
        pet.Internalize();
        pet.IsStabled = true;
        pet.StabledBy = null;
        HavenMarketProvenance.Attach(ticket, "Wild taming", tamer.Name);
        return true;
    }
}

[SerializationGenerator(0)]
public partial class HavenMinaxCreditNote : Item
{
    [Constructible]
    public HavenMinaxCreditNote() : base(0x14F0)
    {
        Name = "Minax credit note";
        Hue = 0x455;
        Weight = 0.01;
        Stackable = true;
    }
    public override void OnDoubleClick(Mobile from) => Redeem(from);
    internal bool Redeem(Mobile from)
    {
        if (Deleted || Amount <= 0 || from is not PlayerMobile || !from.Alive || !IsChildOf(from.Backpack)) { return false; }
        var record = HavenFrontierRecord.Get(from);
        if (record.MinaxCredits > int.MaxValue - Amount) { return false; }
        record.MinaxCredits += Amount;
        from.SendMessage($"Redeemed {Amount:N0} Minax credits. Spend them through [expeditions.");
        Delete();
        return true;
    }
    internal static bool Withdraw(Mobile from, int amount)
    {
        if (from is not PlayerMobile || !from.Alive || from.Backpack == null || amount is < 1 or > 60000) { return false; }
        var record = HavenFrontierRecord.Get(from);
        if (record.MinaxCredits < amount) { return false; }
        var note = new HavenMinaxCreditNote { Amount = amount };
        if (!from.Backpack.TryDropItem(from, note, false)) { note.Delete(); return false; }
        record.MinaxCredits -= amount;
        return true;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Double-click to add these credits to your Blackthorn expedition record."}");
    }
}

[SerializationGenerator(0)]
public partial class HavenMarketProvenance : Item
{
    [SerializableField(0)] private string _source;
    [SerializableField(1)] private string _producer;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenMarketProvenance() : base(1) { Visible = false; Movable = false; Weight = 0; }
    internal static void Attach(Item item, string source, string producer)
    {
        if (Describe(item) == null) { item.AddItem(new HavenMarketProvenance { Source = source, Producer = producer }); }
    }
    internal static string Describe(Item item)
    {
        foreach (var child in item.Items)
        {
            if (child is HavenMarketProvenance provenance) { return $"{provenance.Source} by {provenance.Producer}"; }
        }
        return null;
    }
}
