using System;
using System.Linq;
using Server;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsFrontiers
{
    public HavenWorldTestsFrontiers() { _ = new HavenWorldTests(); }
    private static PlayerMobile Player(Point3D point)
    {
        var player = new PlayerMobile { Player = true, Body = 400, RawStr = 200, RawDex = 100, RawInt = 100 };
        player.AddItem(new Backpack()); player.MoveToWorld(point, Map.Trammel); return player;
    }
    private static HavenShadowChamber Chamber(HavenShadowRoom room)
    {
        var chamber = new HavenShadowChamber(); var i = (int)room;
        chamber.MoveToWorld(new Point3D(4722 + i % 3 * 38, 3169 + i / 3 * 40, 0), Map.Trammel); chamber.Build(room); return chamber;
    }
    private static HavenShadowNode Node(HavenShadowChamber room, int kind, int key = 0)
        => room.Puzzle.OfType<HavenShadowNode>().Single(n => n.Kind == kind && n.Key == key);
    private static void Near(Mobile player, Item node) => player.MoveToWorld(new Point3D(node.X, node.Y + 1, node.Z), node.Map);
    private static void Kill(HavenShadowChamber room, HavenShadowActor actor) { room.Killed(actor); actor.Delete(); }

    [SkippableFact]
    public void AllFrontierTerrainAndNativeShipDeckAreUsableAndInstallIsIdempotent()
    {
        TileDataRequirement.SkipIfMissing();
        Assert.True(HavenFrontierHub.TerrainReady()); Assert.True(HavenFrontierHub.Install(false));
        var hub = Assert.Single(HavenFrontierHub.Registry);
        try
        {
            Assert.Equal(6, HavenShadowChamber.Registry.Count); Assert.Equal(2, HavenFrontierBattle.Registry.Count);
            Assert.Single(HavenChelonia.Registry); Assert.True(HavenFrontierHub.Install(false));
            foreach (var room in HavenShadowChamber.Registry)
            { Assert.True(Map.Trammel.CanFit(room.Arrival, 16, checkMobiles: false), $"{room.Room} arrival"); }
            var board = HavenFrontierBattle.Registry.Single(b => b.Pirate); var player = Player(board.Location);
            try
            {
                Assert.True(board.Start(player)); Assert.NotNull(board.Vessel); Assert.Equal(board.Center.Z, player.Z);
                Assert.True(board.Nearby(player)); Assert.True(board.Vessel.Contains(player.X, player.Y));
                var visitor = Player(player.Location);
                try { board.Cancel(); Assert.Equal(HavenChelonia.Landing, visitor.Location); }
                finally { visitor.Delete(); }
                Assert.Null(board.Vessel); Assert.Equal(HavenChelonia.Landing, player.Location);
            }
            finally { player.Delete(); }
        }
        finally { hub.Delete(); }
        Assert.Empty(HavenShadowChamber.Registry); Assert.Empty(HavenFrontierBattle.Registry); Assert.Empty(HavenChelonia.Registry);
    }
    [SkippableFact]
    public void TortoiseSwimsWalksHasCargoAndKeepsLegendaryTrainingAndDexterity()
    {
        TileDataRequirement.SkipIfMissing(); var owner = Player(HavenChelonia.Site); var stranger = Player(HavenChelonia.Site); var pet = new HavenChelonian();
        try
        {
            Assert.True(pet.CanSwim); Assert.False(pet.CantWalk); Assert.Equal(1294, pet.Body.BodyID);
            Assert.InRange(pet.RawDex, 180, 210); Assert.Equal(pet.Dex, pet.StamMax); Assert.False(pet.StatLossAfterTame);
            HavenPetRarity.Apply(pet, 3); Assert.Equal(1, pet.ControlSlots); Assert.True(HavenTamingMissions.IsCustomPet(pet));
            Assert.True(pet.SetControlMaster(owner)); pet.MoveToWorld(owner.Location, owner.Map);
            Assert.True(pet.Manage(owner)); Assert.False(pet.Manage(stranger)); Assert.False(pet.IsSnoop(owner));
            var bag = new Bag(); pet.Backpack.DropItem(bag); Assert.True(pet.CheckNonlocalLift(owner, bag)); Assert.False(pet.CheckNonlocalLift(stranger, bag));
            pet.Hits = pet.HitsMax / 3; var damage = 100; pet.AlterMeleeDamageFrom(stranger, ref damage); Assert.Equal(65, damage);
            Assert.Equal(10, HavenPetSignatures.Kind(pet)); Assert.Equal(0x47, HavenPetAppearance.NaturalHue(pet));
        }
        finally { pet.Delete(); owner.Delete(); stranger.Delete(); }
    }

    [SkippableFact]
    public void FrontierSetupOnlyMovesUnownedAquaticWildlifeAndRefusesPetsOrObjects()
    {
        TileDataRequirement.SkipIfMissing(); var original = new Point3D(4729, 3147, -5);
        var wild = new WaterElemental(); var owned = new Dog(); var owner = Player(HavenRecovery.BankLocation);
        var blocker = new Static(0x1E5E);
        try
        {
            wild.MoveToWorld(original, Map.Trammel); owned.SetControlMaster(owner); owned.MoveToWorld(original, Map.Trammel);
            Assert.False(HavenFrontierHub.Install()); Assert.Equal(original, wild.Location); Assert.Equal(original, owned.Location);
            owned.MoveToWorld(owner.Location, owner.Map); blocker.MoveToWorld(original, Map.Trammel);
            Assert.False(HavenFrontierHub.Install()); Assert.Equal(original, wild.Location); blocker.Delete();
            wild.Owners.Add(owner); Assert.False(HavenFrontierHub.Install()); Assert.Equal(original, wild.Location); wild.Owners.Remove(owner);
            Assert.True(HavenFrontierHub.Install()); Assert.NotEqual(original, wild.Location); Assert.Equal(-5, wild.Z); Assert.Equal(wild.Location, wild.Home);
            Assert.False(wild.Deleted); Assert.False(owned.Deleted); Assert.Equal(owner.Location, owned.Location);
        }
        finally
        {
            foreach (var hub in HavenFrontierHub.Registry.ToArray()) { hub.Delete(); }
            blocker.Delete(); wild.Delete(); owned.Delete(); owner.Delete();
        }
    }
    [SkippableFact]
    public void TortoiseTidalJetHurtsSeaTargetsAndNeverStrangersOrOtherPets()
    {
        TileDataRequirement.SkipIfMissing(); var owner = Player(new Point3D(4090, 3580, -5)); var pet = new HavenChelonian(); var enemy = new SeaSerpent(); var other = new Dog();
        try
        {
            pet.SetControlMaster(owner); pet.MoveToWorld(owner.Location, owner.Map); other.SetControlMaster(owner); other.MoveToWorld(owner.Location, owner.Map);
            enemy.MoveToWorld(new Point3D(owner.X + 3, owner.Y, -5), owner.Map); pet.Combatant = enemy;
            var hits = enemy.Hits; Assert.True(HavenPetSignatures.Activate(pet, enemy)); Assert.True(enemy.Hits < hits);
            Assert.False(HavenPetSignatures.Activate(pet, enemy)); Assert.False(HavenPetSignatures.Enemy(pet, owner)); Assert.False(HavenPetSignatures.Enemy(pet, other));
        }
        finally { other.Delete(); enemy.Delete(); pet.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void OrchardIsClearlyMatchedAndCannotAwardTwiceOrToAnOutsider()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Orchard); var player = Player(HavenFrontierSupport.ShadowLanding); var stranger = Player(HavenFrontierSupport.ShadowLanding);
        try
        {
            Assert.True(room.Start(player)); Assert.False(room.Start(stranger));
            var first = Node(room, 1); var opposite = Node(room, 1, 1); Near(stranger, first);
            Assert.False(HavenShadowPuzzles.Use(room, stranger, first, opposite)); Near(player, first);
            Assert.False(HavenShadowPuzzles.Use(room, player, first, Node(room, 1, 3)));
            for (var i = 0; i < 16; i += 2)
            {
                var tree = Node(room, 1, i); var pair = Node(room, 1, i + 1); Near(player, tree);
                Assert.Contains("→", tree.Name); Assert.True(HavenShadowPuzzles.Use(room, player, tree, pair));
                if (i == 0) { Assert.False(HavenShadowPuzzles.Use(room, player, tree, pair)); }
            }
            Assert.False(room.Active); Assert.Equal(2, HavenFrontierRecord.Get(player).Rooms); Assert.Equal(0, HavenFrontierRecord.Get(stranger).Rooms);
            room.Finish(true); Assert.Equal(2, HavenFrontierRecord.Get(player).Rooms); Assert.Empty(room.Puzzle);
        }
        finally { room.Delete(); player.Delete(); stranger.Delete(); }
    }
    [SkippableFact]
    public void CompanionUsesOrchardActionsAndReturnsToFollowAfterCompletion()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Orchard); var player = Player(HavenFrontierSupport.ShadowLanding); var pet = new HavenCompanion { BoundOwner = player };
        try
        {
            pet.SetControlMaster(player); pet.ControlOrder = OrderType.Follow; pet.MoveToWorld(player.Location, player.Map); pet.Hits = pet.HitsMax;
            Assert.True(room.Start(player)); Assert.Contains(pet, room.Followers); Assert.True(room.OrderPuzzle(player, true));
            for (var i = 0; i < 16; i += 2) { Near(pet, Node(room, 1, i)); Assert.True(HavenShadowPuzzles.Assist(room, pet)); }
            Assert.Equal(2, HavenFrontierRecord.Get(player).Rooms); Assert.False(room.Active); Assert.Equal(OrderType.Follow, pet.ControlOrder);
        }
        finally { room.Delete(); pet.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void BarNeedsThreeBottleHitsPerPirateAndMishapDoesNotCount()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Bar); var player = Player(HavenFrontierSupport.ShadowLanding);
        try
        {
            Assert.True(room.Start(player)); var rack = Node(room, 0); Near(player, rack);
            foreach (var pirate in room.Actors.ToArray())
            {
                Assert.True(pirate.Blessed); Assert.True(HavenShadowPuzzles.Use(room, player, rack, pirate, 0)); Assert.Equal(0, pirate.PuzzleHits);
                for (var i = 0; i < 3; i++) { Assert.True(HavenShadowPuzzles.Use(room, player, rack, pirate, 1)); }
            }
            Assert.False(room.Active); Assert.Equal(1, HavenFrontierRecord.Get(player).Rooms);
        }
        finally { room.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void ArmoryRequiresEarnedPhylacteriesAndFountainRequiresEarnedConnectedPieces()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(HavenFrontierSupport.ShadowLanding); var room = Chamber(HavenShadowRoom.Armory);
        try
        {
            Assert.True(room.Start(player)); var altar = Node(room, 2); Near(player, altar); var armor = room.Actors.First(a => a.Role == 2);
            Assert.False(HavenShadowPuzzles.Use(room, player, altar, armor));
            foreach (var guard in room.Actors.Where(a => a.Role == 1).ToArray()) { Kill(room, guard); }
            Assert.Equal(3, room.Supplies);
            foreach (var suit in room.Actors.ToArray()) { Assert.True(HavenShadowPuzzles.Use(room, player, altar, suit)); }
            Assert.Equal(4, HavenFrontierRecord.Get(player).Rooms);
        }
        finally { room.Delete(); }
        room = Chamber(HavenShadowRoom.Fountain);
        try
        {
            Assert.True(room.Start(player)); var first = Node(room, 3); Near(player, first); Assert.False(HavenShadowPuzzles.Use(room, player, first, null));
            foreach (var elemental in room.Actors.ToArray()) { Kill(room, elemental); }
            Assert.Equal(16, room.Supplies);
            for (var i = 0; i < 16; i++) { var canal = Node(room, 3, i); Near(player, canal); Assert.True(HavenShadowPuzzles.Use(room, player, canal, null)); }
            Assert.True(room.Active); var valve = Node(room, 4); Near(player, valve); Assert.True(HavenShadowPuzzles.Use(room, player, valve, null));
            Assert.Equal(12, HavenFrontierRecord.Get(player).Rooms);
        }
        finally { room.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void BelfryRequiresDrakesAndItsRaisedDragonFloorWorks()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Belfry); var player = Player(HavenFrontierSupport.ShadowLanding);
        try
        {
            Assert.True(room.Start(player)); var bell = Node(room, 5); Near(player, bell);
            Assert.True(HavenShadowPuzzles.Use(room, player, bell, null)); Assert.False(HavenShadowPuzzles.Use(room, player, bell, null));
            foreach (var drake in room.Actors.ToArray()) { Assert.Equal(4, drake.Role); Kill(room, drake); }
            Assert.True(HavenShadowPuzzles.Use(room, player, bell, null)); Assert.Equal(12, player.Z);
            Assert.True(player.Map.CanFit(player.Location, 16, checkMobiles: false));
            var dragon = Assert.Single(room.Actors); Assert.Equal(5, dragon.Role); Kill(room, dragon);
            Assert.Equal(16, HavenFrontierRecord.Get(player).Rooms);
        }
        finally { room.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void RoofNeedsAllSealsAndRewardsOnceAfterFourBossesThenResetsSeals()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Roof); var player = Player(HavenFrontierSupport.ShadowLanding);
        try
        {
            Assert.False(room.Start(player)); var record = HavenFrontierRecord.Get(player); record.Rooms = 31; Assert.True(room.Start(player));
            for (var i = 0; i < 4; i++) { var boss = Assert.Single(room.Actors); Assert.Equal(10 + i, boss.Role); Kill(room, boss); }
            Assert.Equal(1, record.Roofs); Assert.Equal(0, record.Rooms); Assert.Equal(50000, player.Backpack.GetAmount(typeof(Gold)));
            room.Finish(true); Assert.Equal(1, record.Roofs); Assert.Equal(50000, player.Backpack.GetAmount(typeof(Gold)));
        }
        finally { room.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void AnonOnlyAbsorbsMatchingPureDamageFromParticipants()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Roof); var player = Player(HavenFrontierSupport.ShadowLanding);
        try
        {
            HavenFrontierRecord.Get(player).Rooms = 31; Assert.True(room.Start(player));
            Kill(room, Assert.Single(room.Actors)); Kill(room, Assert.Single(room.Actors)); var anon = Assert.Single(room.Actors);
            anon.Element = 1; anon.Hits -= 100; var hits = anon.Hits;
            Assert.Equal(0, AOS.Damage(anon, player, 50, 0, 100, 0, 0, 0)); Assert.Equal(hits + 50, anon.Hits);
            hits = anon.Hits; Assert.True(AOS.Damage(anon, player, 50, 50, 50, 0, 0, 0) > 0); Assert.True(anon.Hits < hits);
        }
        finally { room.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void BlackthornCaptainUnlocksOnlyAfterOwnGuardAndBeaconAfterBothCaptains()
    {
        TileDataRequirement.SkipIfMissing(); var battle = new HavenFrontierBattle(); battle.MoveToWorld(HavenFrontierSupport.RiftLanding, Map.Trammel); battle.Build(false);
        var player = Player(battle.Location);
        try
        {
            Assert.True(battle.Start(player)); player.MoveToWorld(battle.Center, battle.Map); battle.Credit(player);
            for (var phase = 1; phase <= 3; phase++)
            {
                Assert.Equal(phase, battle.Phase);
                foreach (var group in new[] { 0, 1 })
                {
                    var captain = battle.Enemies.Single(e => e.Role == 1 && e.Group == group); Assert.True(captain.Blessed);
                    foreach (var guard in battle.Enemies.Where(e => e.Role == 0 && e.Group == group).ToArray()) { battle.Killed(guard); guard.Delete(); }
                    Assert.False(captain.Blessed); Assert.True(battle.Enemies.Single(e => e.Role == 2).Blessed);
                    battle.Killed(captain); captain.Delete();
                }
                var beacon = Assert.Single(battle.Enemies); Assert.False(beacon.Blessed); battle.Killed(beacon); beacon.Delete();
            }
            var record = HavenFrontierRecord.Get(player); Assert.Equal(1, record.Rifts); Assert.Equal(18, record.MinaxCredits);
            Assert.Equal(40000, player.Backpack.GetAmount(typeof(Gold))); battle.Complete(); Assert.Equal(40000, player.Backpack.GetAmount(typeof(Gold)));
        }
        finally { battle.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void SavedRoomRecoveryEjectsWithoutRewardAndPreservesCompletedSeals()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Orchard); var player = Player(HavenFrontierSupport.ShadowLanding);
        try
        {
            HavenFrontierRecord.Get(player).Rooms = 1; Assert.True(room.Start(player));
            // Real saved references must survive byte serialization and recover without paying.
            var writer = new BufferWriter(true); room.Serialize(writer); var data = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            var restored = new HavenShadowChamber(World.NewItem);
            try
            {
                var reader = new BufferReader(data); restored.Deserialize(reader); Assert.Equal(data.Length, reader.Position);
                Assert.True(restored.Active); restored.Recover(); // After the world has resolved all cross-entity references.
                Assert.False(restored.Active); Assert.Empty(restored.Actors); Assert.Empty(restored.Puzzle);
                Assert.Equal(1, HavenFrontierRecord.Get(player).Rooms); Assert.Equal(HavenFrontierSupport.ShadowLanding, player.Location);
            }
            finally { restored.Delete(); }
        }
        finally { room.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void RelicPurchaseIsOwnedAffordableAndRejectsFullPacksWithoutCharging()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(HavenFrontierSupport.RiftLanding); var stranger = Player(player.Location);
        try
        {
            var record = HavenFrontierRecord.Get(player); record.MinaxCredits = 50;
            Assert.False(record.Buy(stranger, 0, false)); player.Backpack.MaxItems = 1;
            var blocker = new Bag(); player.Backpack.DropItem(blocker);
            Assert.False(record.Buy(player, 0, false)); Assert.Equal(50, record.MinaxCredits); blocker.Delete();
            player.Backpack.MaxItems = 125; Assert.True(record.Buy(player, 0, false)); Assert.Equal(0, record.MinaxCredits); Assert.False(record.Buy(player, 0, false));
            var book = Assert.Single(player.Backpack.Items); Assert.True(HavenLegendaryArtifact.IsLegendary(book)); Assert.True(HavenGearExperience.IsSpecial(book));
        }
        finally { player.Delete(); stranger.Delete(); }
    }

    [SkippableFact]
    public void PartyLeaderRequiresEveryPlayersSealsAndPaysBothPlayersOnce()
    {
        TileDataRequirement.SkipIfMissing(); var room = Chamber(HavenShadowRoom.Roof); var leader = Player(HavenFrontierSupport.ShadowLanding); var friend = Player(leader.Location);
        var party = new Party(leader); leader.Party = party; party.Add(friend);
        try
        {
            HavenFrontierRecord.Get(leader).Rooms = 31; Assert.False(room.Start(friend)); Assert.False(room.Start(leader));
            HavenFrontierRecord.Get(friend).Rooms = 31; Assert.True(room.Start(leader));
            for (var i = 0; i < 4; i++) { Kill(room, Assert.Single(room.Actors)); }
            Assert.Equal(1, HavenFrontierRecord.Get(leader).Roofs); Assert.Equal(1, HavenFrontierRecord.Get(friend).Roofs);
            Assert.Equal(50000, friend.Backpack.GetAmount(typeof(Gold)));
        }
        finally { room.Delete(); party.Remove(friend); leader.Party = null; leader.Delete(); friend.Delete(); }
    }

    [SkippableFact]
    public void ActualEnemyDeathRecordsCompanionOwnerAndAdvancesBlackthorn()
    {
        TileDataRequirement.SkipIfMissing(); var battle = new HavenFrontierBattle(); battle.MoveToWorld(HavenFrontierSupport.RiftLanding, Map.Trammel); battle.Build(false);
        var player = Player(battle.Location); var companion = new HavenCompanion { BoundOwner = player };
        try
        {
            Assert.True(battle.Start(player)); player.MoveToWorld(battle.Center, Map.Trammel);
            companion.SetControlMaster(player); companion.MoveToWorld(player.Location, player.Map);
            var guard = battle.Enemies.First(e => e.Role == 0); guard.Damage(25, companion);
            Assert.Contains(player, battle.Participants); guard.Kill(); Assert.DoesNotContain(guard, battle.Enemies);
            guard.Corpse?.Delete();
            var captain = battle.Enemies.First(e => e.Role == 1); Assert.False(player.CanBeHarmful(captain, false));
        }
        finally { battle.Delete(); companion.Delete(); player.Delete(); }
    }

    [SkippableFact]
    public void PirateCompletionDeliversDeedsAndCargoRedeemsOnlyAtItsBoard()
    {
        TileDataRequirement.SkipIfMissing(); var battle = new HavenFrontierBattle();
        battle.MoveToWorld(new Point3D(HavenChelonia.Landing.X + 5, HavenChelonia.Landing.Y, 0), Map.Trammel); battle.Build(true);
        var player = Player(battle.Location);
        try
        {
            Assert.True(battle.Start(player)); battle.Credit(player);
            Assert.IsType<HavenCorsairGalleon>(battle.Vessel); Assert.Equal(13,player.Z);
            Assert.True(battle.Map.CanFit(player.Location,16,checkMobiles:false));
            Assert.All(battle.Enemies,e => Assert.True(battle.Map.CanFit(e.Location,16,checkMobiles:false), $"Invalid galleon spawn {e.Location}"));
            for (var phase = 0; phase < 2; phase++)
            {
                foreach (var guard in battle.Enemies.Where(e => e.Role == 0).ToArray()) { battle.Killed(guard); guard.Delete(); }
                var captain = battle.Enemies.Single(e => e.Role == 1); battle.Killed(captain); captain.Delete();
                var seal = Assert.Single(battle.Enemies); battle.Killed(seal); seal.Delete();
            }
            Assert.Equal(1, HavenFrontierRecord.Get(player).Voyages); Assert.Null(battle.Vessel);
            Assert.Equal(2, player.Backpack.Items.OfType<CommodityDeed>().Count());
            var cargo = Assert.Single(player.Backpack.Items.OfType<HavenMaritimeCargo>()); var value = cargo.Value;
            cargo.OnDoubleClick(player); Assert.False(cargo.Deleted); // landing is five tiles away
            player.MoveToWorld(battle.Location, battle.Map); cargo.OnDoubleClick(player); Assert.True(cargo.Deleted);
            Assert.Equal(value, HavenFrontierRecord.Get(player).Doubloons); cargo.OnDoubleClick(player);
            Assert.Equal(value, HavenFrontierRecord.Get(player).Doubloons);
        }
        finally { battle.Delete(); player.Delete(); }
    }
}
