# Fishing fleet

`[fishfleet` shows the two fishing boats, coordinates, current work and cumulative trip/catch/SOS/net/listing counts. Their lay-up area is in verified water south of the pirate island dock, around **4237,2990 Trammel**. The boats are **The Silver Wake** and **The Wandering Tern**. Each has a fisherman and a mage escort.

They physically sail, cast through the native Fishing harvest system, fight sea creatures that engage their crew, recover their own sea loot and sail back along their traveled route. Catches are actual items: elapsed time does not create rewards. Maritime stock appears in `[market` as trips return. Marina Saltwind runs that stall at the Commons.

* The navigation search is spread over game ticks and checks the whole boat hull. Other boats and obstructions stop movement. The route avoids Scalis's usual patrol area.
* Trips normally last up to 18 minutes before the return journey. Hull movement carries the crew and their boat cargo.
* Earned bottles are opened using the native item. SOS sites within 900 tiles are considered; the boat must find a navigable route and reach the actual site. The normal fishing rolls produce wreckage and eventually the chest, consuming the SOS only then. Distant or inaccessible SOS messages can be sold instead.
* Crews can use one earned ordinary special fishing net per trip when healthy and away from nearby human players. White Fabled Fishing Nets are kept for sale; these small fishing crews do not deliberately summon Scalis.
* Ordinary fishing can produce sea serpents carrying rare catches. The crew must defeat those creatures and have loot rights. They do not take another player's corpse loot.
* Earned cargo is recorded and survives saves. A full market causes boats to retain cargo and wait; it never creates replacement loot or deletes unsold goods.
* A passenger or player/guild ownership of a crew member pauses automatic boat work. The fleet has no player charter or sea-boss party invitation interface yet.
* The older ambient dockside SOS story is disabled. Other existing dock fishermen still cast their normal fishing lines.

This is a fishing and salvage fleet, not autonomous cannon warfare or a complete High Seas quest/fish-order port. Unexpected obstructions can leave a vessel waiting for clear water. New boss balance and long unattended voyages still benefit from play-testing.

## Related usability changes

The Book of Masteries now shows only learned masteries, eight per page. It uses dark text, wider rows and a separate active-abilities panel. Learning, 90-skill requirements, primer volumes and switching cooldowns are unchanged.

`[CompanionStore` stores supported resource deeds from your owned companion's pack, including nested bags, into a Resource Ledger already carried by that companion. Unsupported/empty deeds remain in the pack. The existing **Absorb pack deeds** button also remains available in the ledger. These are storage transfers, not new mission receipts or newly earned resources.

## Player checks

1. Open `[fishfleet`, note positions, and check later that boats are traveling/fishing and catch counts rise.
2. After a return, search Maritime in `[market`; inspect and buy actual earned catches with wallet gold.
3. Watch a boat cast, fight a sea serpent or recover an SOS when it obtains a suitable message. Rare catches are not guaranteed.
4. Open a Book of Masteries with only one learned mastery, then with several; check filtering, pages, active selection and ability buttons.
5. Use `[CompanionStore` twice. The first call stores supported deeds; the second must not add the same resources again.

## Sea prizes, island and storage

Scalis, Corgul, Leviathan and Saltfang boarding captains offer a 5% ship-deed roll per eligible player, split equally between Britannian and Orcish hulls. These sail, carry cargo and dry-dock through the native boat system; cannon combat is not included. Hull IDs follow the [ServUO ship definitions](https://github.com/ServUO/ServUO/tree/pub57/Scripts/Services/Expansions/High%20Seas/Multis).

The Resource Ledger now uses dark text on parchment. Companion controls separate Orders, Tasks and Utility, and Stats lists 12 skills per page. Reopen existing menus after updating.

Corsair's Rest gains growing crop patches, fruit trees, a rainwater collection area, camp kitchen and dock supplies. The house plot and original island footprint stay intact.

New pet-training bundles and mission supplies contain ordinary Power Scrolls. Double-click an older pet-only scroll in your pack to exchange it for the identical standard skill/tier. Existing pet training still accepts legacy scrolls directly.
