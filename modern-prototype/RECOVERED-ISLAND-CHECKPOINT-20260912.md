# Recovered island checkpoint — September 12, 2026

The original R.E.C. courtyard compound has been ported from playerbots/source/CustomBots/UOOffline/HavenPirateHouseLayout.cs and HavenPirateCompound.cs. This replaces the mistaken stock-castle direction with the existing 31x31 custom plot: captain's house, workshop loft, barn/crew loft, galleries, bridges and courtyard. The architecture is preserved. A feed barrel moved one tile to clear a ladder landing.

## Implemented in the isolated test world

- 2,555 native building components, 70 functional/decorative fixtures and eight ladder links. Original workshop and living-area positions retained. Cargo chests are usable containers.
- Shared estate storage supports both the legacy castle and recovered foundation. Migration transfers the original vault object, preserving nested item identities. It copies house access lists and public/private status. Ladder access includes friends/co-owners; connected storage remains account-private.
- Migration refuses unknown placed possessions, vendors and occupants. It remains explicitly test-only. A forced failure restores the original estate/stations/vault and removes newly created fixtures.
- Island travel recognizes either estate type.
- Three native closed waypoint patrol loops replace random wandering. Each segment is checked with native movement and stays outside houses and within the encounter bounds. Temporarily obstructed route preparation retries rather than crashing. Out-of-bounds raiders walk toward their patrol instead of being teleported home.
- A visible Blackwake expedition board links to the existing cove encounter. A paved approach connects the harbor arrival route to it. This does not create another boss or alter rewards.

## Verification completed

- Release build: zero errors and zero warnings.
- Actual native house packet compressed/decoded: all 2,555 unique components preserved.
- Native movement traversal from outside the entrance, using LOS-aware ladder transitions, reaches ladders and functional fixtures/storage.
- Forced rollback: original vault and nested objects unchanged; no newly created world items left over.
- Separate-process save/reload: owner, all 70 fixtures, three linked storage stations and exact nested vault preserved.
- Patrol segment movement, closed links, safe bounds and saved reload passed for all three encounter sites.
- Cove board encounter link, trail access and saved reload passed.
- Critic: recovered-house checkpoint 8/10. Subsequent outside-entry and LOS checks address the critic's remaining automated-verification gaps. No in-client visual or interaction approval is claimed.

Native-art renders: verification/RecoveredHouseRenders/exterior.jpg, ground.jpg and upper.jpg. These use exported server tiles, not screenshots. The existing render helper was adapted for the larger compound.

## Staging and remaining work

Test runtime: verification/HavenPlazaRefine (port 2733), stopped after checks. Test data: verification/HavenRecoveredHouseData. Reserved 31x31 foundation ID 0x18A8 is staged in verification/HavenRecoveredHouseMultis. Original pre-migration test saves remain in verification/HavenRecoveredHouseBeforeSave; the first successful custom-house reload snapshot is in verification/HavenRecoveredHouseReloadPassed.

The live server was not restarted or changed in this checkpoint. Its latest deployment remains Peacemaking attempt gains, commit 5a92670.

Before release: inspect the custom build, roof hiding, clickable ladders/storage, cove board and patrol behavior in the separate game client with matching large-plot assets. Prepare the production migration/rollback procedure, validate current live possessions and install matching server/client multi files while safely stopped. Produce the required full restart report after any live deployment. Original auto-sorting profession storage and other old special furniture systems are not represented as restored here.
