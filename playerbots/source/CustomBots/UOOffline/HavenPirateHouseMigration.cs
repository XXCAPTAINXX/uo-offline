using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.UOOffline;

public partial class HavenPirateHeadquarters
{
    internal static HavenPirateHeadquarters Install(Mobile owner)
    {
        if (owner?.Deleted != false || owner.Account == null) { throw new InvalidOperationException("An existing player is required."); }
        foreach (var ready in Registry) { if (!ready.Deleted && ready.Owner == owner) { ready.Estate?.MoveTrialToNorthwest(); return ready; } }
        HavenGuildCastle old = null;
        foreach (var house in HavenGuildCastle.Registry) { if (!house.Deleted && house.Owner == owner) { old = house; break; } }
        if (old == null) { throw new InvalidOperationException("The existing island headquarters was not found; no property was changed."); }
        return Replace(old);
    }

    internal static HavenPirateHeadquarters Replace(HavenGuildCastle old)
    {
        if (old?.Deleted != false || old.Owner?.Deleted != false || old.Map == Map.Internal || old.MasterStorage?.Deleted != false)
        { throw new InvalidOperationException("The existing headquarters is not ready to migrate."); }
        if (old.PlayerVendors.Count != 0 || old.PlayerBarkeepers.Count != 0 || old.VendorInventories.Count != 0 || old.InternalizedVendors.Count != 0)
        { throw new InvalidOperationException("Move vendors out before replacing the headquarters; all property was preserved."); }
        if (old.RelocatedEntities.Count != 0) { throw new InvalidOperationException("Restore the house's relocated items before replacement."); }
        var location = old.Location; var map = old.Map;
        var locks = old.LockDowns.ToArray(); var secures = old.Secures.ToArray(); var addons = old.Addons.ToArray();
        var contracts = old.VendorRentalContracts.ToArray(); var furnishings = old.CompanyFixtures.ToArray();
        var locations = new Dictionary<Item, Point3D>();
        using (var entities = old.GetHouseEntities())
        { foreach (var entity in entities) { if (entity is Item item) { locations.TryAdd(item, item.Location); } } }
        var people = new List<Mobile>();
        foreach (var mobile in map.GetMobilesInRange(location, 22)) { if (old.IsInside(mobile)) { people.Add(mobile); } }
        // Loose possessions are kept too, including nested container contents via their original container.
        foreach (var item in map.GetItemsInRange(location, 22))
        {
            if (item == old || item == old.Sign || item is BaseDoor or AddonComponent || item.Parent != null || !old.IsInside(item)) { continue; }
            locations.TryAdd(item, item.Location);
        }
        var legacyControls = furnishings.Where(i => i is HavenCompanyLadder or HavenCompanyCharter).ToArray();
        var house = new HavenPirateHeadquarters(old.Owner) { Estate = old.Estate, Public = old.Public, Price = old.Price };
        var replaced = false;
        try
        {
            // Build and validate the design before detaching anything from the old house.
            if (house.CurrentState.Components.List.Length < 2000 || house.Components.Width != 31) { throw new InvalidOperationException("Incomplete custom-house design."); }
            HousePackets.CreateHouseDesignStateDetailed(house.Serial, house.LastRevision, house.Components);
            house.MoveToWorld(location, map);
            house.CoOwners.AddRange(old.CoOwners); house.Friends.AddRange(old.Friends); house.Access.AddRange(old.Access); house.Bans.AddRange(old.Bans);
            house.MasterStorage = old.MasterStorage;
            house.LockDowns.AddRange(locks); house.Secures.AddRange(secures); house.Addons.AddRange(addons); house.VendorRentalContracts.AddRange(contracts);
            house.CompanyFixtures.AddRange(furnishings);
            old.LockDowns.Clear(); old.Secures.Clear(); old.Addons.Clear(); old.VendorRentalContracts.Clear();
            foreach (var pair in locations)
            {
                if (pair.Key.Deleted || pair.Key is TrashBarrel) { continue; }
                pair.Key.MoveToWorld(new Point3D(pair.Value.X, pair.Value.Y, pair.Value.Z + 1), map);
            }
            if (old.MovingCrate != null) { house.MovingCrate = old.MovingCrate; house.MovingCrate.House = house; old.MovingCrate = null; }
            house.Sign.Name = "R.E.C. - Rare Export Company | Customize this house";
            foreach (var control in legacyControls)
            {
                house.LockDowns.Remove(control); house.CompanyFixtures.Remove(control);
                Item replacement = control is HavenCompanyLadder ? new HavenPirateStair { Headquarters = house } : new HavenPirateCharter { Headquarters = house };
                replacement.MoveToWorld(control.Location, map); replacement.IsLockedDown = true; replacement.Movable = false;
                house.LockDowns.Add(replacement); house.CompanyFixtures.Add(replacement);
            }
            if (house.MasterStorage.FindLinked().Count != 13) { throw new InvalidOperationException("Linked storage validation failed."); }
            // Clear native ownership collections first: deleting an old house normally releases storage and deletes addons.
            old.CompanyFixtures.Clear(); old.MasterStorage = null; old.Estate = null; old.Delete(); replaced = true;
            foreach (var control in legacyControls) { control.Delete(); }
            foreach (var person in people)
            {
                var deck = Math.Clamp((person.Z - location.Z - 6) / 20, 0, 2);
                var safe = new Point3D(location.X, location.Y + 1, location.Z + 7 + deck * 20);
                person.MoveToWorld(safe, map);
            }
            house.Estate?.MoveTrialToNorthwest();
            house.Delta(ItemDelta.Update); house.MarkDirty(); return house;
        }
        catch
        {
            if (replaced) { throw; } // The live-world backup is the recovery source after the final swap.
            foreach (var item in house.CompanyFixtures.ToArray()) { if (!furnishings.Contains(item)) { item.Delete(); } }
            house.CompanyFixtures.Clear(); house.LockDowns.Clear(); house.Secures.Clear(); house.Addons.Clear(); house.VendorRentalContracts.Clear();
            old.LockDowns.Clear(); old.LockDowns.AddRange(locks); old.Secures.Clear(); old.Secures.AddRange(secures);
            old.Addons.Clear(); old.Addons.AddRange(addons); old.VendorRentalContracts.Clear(); old.VendorRentalContracts.AddRange(contracts);
            if (house.MovingCrate != null) { old.MovingCrate = house.MovingCrate; old.MovingCrate.House = old; house.MovingCrate = null; }
            foreach (var pair in locations) { if (!pair.Key.Deleted) { pair.Key.MoveToWorld(pair.Value, map); } }
            house.Delete(); throw;
        }
    }
}
