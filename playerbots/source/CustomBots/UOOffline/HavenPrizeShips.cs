using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.UOOffline;

// Actual High Seas hull multis, with this shard's native sailing, keys, hold and dry-docking.
// Hull IDs and deck elevations match the client/ServUO definitions; cannons are not part of this port.
[SerializationGenerator(0)]
public abstract partial class HavenPrizeShip : BaseBoat
{
    protected abstract int DeckHeight { get; }
    protected HavenPrizeShip()
    {
        PPlank.Z = SPlank.Z = DeckHeight - 3;
        Hold.Z = TillerMan.Z = DeckHeight;
    }
    public override int HoldDistance => 8;
    public override int TillerManDistance => -6;
    public override Point2D StarboardOffset => new(3, 0);
    public override Point2D PortOffset => new(-3, 0);
    public override Point3D MarkOffset => new(0, -2, DeckHeight);
    [AfterDeserialization]
    private void RestoreDeckFixtures()
    {
        if (PPlank != null) { PPlank.Z = Z + DeckHeight - 3; }
        if (SPlank != null) { SPlank.Z = Z + DeckHeight - 3; }
        if (Hold != null) { Hold.Z = Z + DeckHeight; }
        if (TillerMan != null) { TillerMan.Z = Z + DeckHeight; }
    }
    public override void UpdateComponents() { base.UpdateComponents(); RestoreDeckFixtures(); }
}
[SerializationGenerator(0)]
public partial class HavenBritannianShip : HavenPrizeShip
{
    [Constructible] public HavenBritannianShip() { Name = "Britannian ship"; ShipName = "Britannian prize ship"; }
    public override int NorthID => 0x40;
    public override int EastID => 0x41;
    public override int SouthID => 0x42;
    public override int WestID => 0x43;
    protected override int DeckHeight => 18;
    public override BaseDockedBoat DockedBoat => new HavenDockedBritannianShip(this);
}
[SerializationGenerator(0)]
public partial class HavenOrcishShip : HavenPrizeShip
{
    [Constructible] public HavenOrcishShip() { Name = "Orcish galleon"; ShipName = "Orcish prize ship"; }
    public override int NorthID => 0x18;
    public override int EastID => 0x19;
    public override int SouthID => 0x1A;
    public override int WestID => 0x1B;
    protected override int DeckHeight => 14;
    public override BaseDockedBoat DockedBoat => new HavenDockedOrcishShip(this);
}
[SerializationGenerator(0)]
public partial class HavenBritannianShipDeed : BaseBoatDeed
{
    [Constructible] public HavenBritannianShipDeed() : base(0x40, new Point3D(0, 0, 0)) { Name = "Britannian ship deed"; }
    public override BaseBoat Boat => new HavenBritannianShip();
}
[SerializationGenerator(0)]
public partial class HavenOrcishShipDeed : BaseBoatDeed
{
    [Constructible] public HavenOrcishShipDeed() : base(0x18, new Point3D(0, 0, 0)) { Name = "Orcish galleon deed"; }
    public override BaseBoat Boat => new HavenOrcishShip();
}
[SerializationGenerator(0)]
public partial class HavenDockedBritannianShip : BaseDockedBoat
{
    public HavenDockedBritannianShip(BaseBoat boat) : base(0x40, Point3D.Zero, boat) { Name = "docked Britannian ship"; }
    public override BaseBoat Boat => new HavenBritannianShip();
}
[SerializationGenerator(0)]
public partial class HavenDockedOrcishShip : BaseDockedBoat
{
    public HavenDockedOrcishShip(BaseBoat boat) : base(0x18, Point3D.Zero, boat) { Name = "docked Orcish galleon"; }
    public override BaseBoat Boat => new HavenOrcishShip();
}
public static class HavenSeaBossShips
{
    internal const double DropChance = .05;
    private static readonly ConditionalWeakTable<BaseCreature, HashSet<Serial>> Attempts = new();
    internal static bool IsSeaBoss(BaseCreature creature) => creature is HavenScalis and not HavenCora or Leviathan ||
        creature is HavenFrontierEnemy { Role: 1, Battle.Pirate: true };
    internal static bool Award(BaseCreature boss, PlayerMobile player, double roll, int choice)
    {
        if (!IsSeaBoss(boss) || !HavenWorldDiscoveries.Eligible(boss, player) ||
            !Attempts.GetOrCreateValue(boss).Add(player.Serial) || roll >= DropChance) { return false; }
        Item deed = (choice & 1) == 0 ? new HavenBritannianShipDeed() : new HavenOrcishShipDeed();
        HavenFrontierSupport.Deliver(player, deed);
        player.SendMessage($"A rare sea prize: {deed.Name}!"); return true;
    }
    public static void OnDeath(BaseCreature creature)
    {
        if (!IsSeaBoss(creature)) { return; }
        if (creature is HavenScalis scalis)
        {
            foreach (var player in scalis.RewardRecipients()) { Award(creature, player, Utility.RandomDouble(), Utility.Random(2)); }
        }
        else
        {
            var owners = new HashSet<PlayerMobile>();
            foreach (var right in BaseCreature.GetLootingRights(creature.DamageEntries, creature.HitsMax))
            {
                if (!right.m_HasRight || right.m_Mobile is not PlayerMobile player) { continue; }
                if (player is Server.CustomBots.PlayerBot bot) { player = HavenBotLoot.PlayerOwner(bot) ?? bot; }
                if (owners.Add(player)) { Award(creature, player, Utility.RandomDouble(), Utility.Random(2)); }
            }
        }
    }
}
