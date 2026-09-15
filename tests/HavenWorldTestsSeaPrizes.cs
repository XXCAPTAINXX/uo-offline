using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsSeaPrizes
{
    public HavenWorldTestsSeaPrizes() { _ = new HavenWorldTestsMarket(); }
    [SkippableTheory]
    [InlineData(false)] [InlineData(true)]
    public void PrizeHullPlacesSailsTurnsAndDryDocks(bool orc)
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Body = 400 }; owner.AddItem(new Backpack());
        BaseBoatDeed deed = orc ? new HavenOrcishShipDeed() : new HavenBritannianShipDeed();
        owner.Backpack.DropItem(deed);
        owner.MoveToWorld(new Point3D(4237, 2990, -5), Map.Trammel);
        BaseBoat boat = null;
        try
        {
            var place = new Point3D(4280, 3030, -5);
            deed.OnPlacement(owner, place);
            Assert.True(deed.Deleted);
            boat = BaseBoat.FindBoatAt(place, Map.Trammel);
            Assert.IsAssignableFrom<HavenPrizeShip>(boat);
            Assert.True(MultiData.GetComponents(boat.ItemID).Width > 5);
            var deck = new Point3D(boat.X, boat.Y - 2, boat.Z + (orc ? 14 : 18));
            Assert.True(Map.Trammel.CanFit(deck, 16, false, false, true), $"No walkable deck: {deck}");
            owner.MoveToWorld(deck, Map.Trammel);
            Assert.True(boat.Contains(owner)); boat.Anchored = false;
            Assert.True(boat.Move(Direction.North, 2, 4, false));
            Assert.Equal(deck.Y - 2, owner.Y);
            Assert.True(boat.SetFacing(Direction.East));
            boat.UpdateComponents();
            Assert.Equal(boat.Z + (orc ? 11 : 15), boat.PPlank.Z);
            boat.Anchored = true; owner.MoveToWorld(new Point3D(boat.X + 17, boat.Y, 0), Map.Trammel);
            var model = boat.DockedBoat;
            Assert.Equal(boat.NorthID, model.MultiId);
            var restored = model.Boat; Assert.Equal(boat.GetType(), restored.GetType()); restored.Delete(); model.Delete();
        }
        finally { boat?.Delete(); deed.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void ShipDropsOnlyEligibleSeaBossesAndCannotAwardTwice()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Body = 400 }; owner.AddItem(new Backpack());
        var scalis = new HavenScalis(); var corgul = new HavenCorgul(); var cora = new HavenCora(); var leviathan = new Leviathan();
        try
        {
            foreach (var m in new Mobile[] { owner, scalis, corgul, cora, leviathan }) { m.MoveToWorld(HavenFishingFleet.Harbor, Map.Trammel); }
            Assert.False(HavenSeaBossShips.Award(cora, owner, 0, 0));
            Assert.True(HavenSeaBossShips.Award(scalis, owner, 0, 0));
            Assert.False(HavenSeaBossShips.Award(scalis, owner, 0, 0));
            Assert.True(HavenSeaBossShips.Award(corgul, owner, 0, 1));
            Assert.False(HavenSeaBossShips.Award(leviathan, owner, .05, 1));
            Assert.Single(owner.Backpack.Items.OfType<HavenBritannianShipDeed>());
            Assert.Single(owner.Backpack.Items.OfType<HavenOrcishShipDeed>());
        }
        finally { owner.Delete(); scalis.Delete(); corgul.Delete(); cora.Delete(); leviathan.Delete(); }
    }
    [SkippableFact]
    public void OldPetScrollExchangesOnceWithoutLosingSkillOrTier()
    {
        TileDataRequirement.SkipIfMissing(); var owner = new PlayerMobile { Body = 400 }; owner.AddItem(new Backpack());
        var old = new HavenPetPowerScroll(SkillName.Tactics, 110); owner.Backpack.DropItem(old);
        try
        {
            Assert.True(old.Exchange(owner)); Assert.False(old.Exchange(owner));
            var scroll = Assert.Single(owner.Backpack.Items.OfType<PowerScroll>());
            Assert.Equal(SkillName.Tactics, scroll.Skill); Assert.Equal(110, scroll.Value);
        }
        finally { owner.Delete(); }
    }
    [SkippableFact]
    public void SettlementIsIdempotentAndKeepsHousingAreaClear()
    {
        TileDataRequirement.SkipIfMissing(); var owner = new PlayerMobile { Body = 400 }; owner.AddItem(new Backpack());
        var island = new HavenPirateEstate();
        try
        {
            island.MoveToWorld(HavenPirateEstate.Site, Map.Trammel); island.Build(owner); island.DecorateSettlement();
            var count = island.Fixtures.Count;
            Assert.True(count > 180);
            Assert.Contains(island.Fixtures, i => i.Name == "Carrot patch");
            Assert.Contains(island.Fixtures, i => i is AppleTreeAddon);
            island.DecorateSettlement(); Assert.Equal(count, island.Fixtures.Count);
            Assert.DoesNotContain(island.Fixtures, i => i.X >= island.X + 48 && i.X < island.X + 88 && i.Y >= island.Y + 48 && i.Y < island.Y + 88);
        }
        finally { island.Delete(); owner.Delete(); }
    }
}
