# Haven world and service repair

This update targets `haven-rc4` and its existing saved items. It does not migrate
the world to the separate `modern-evolution` development line.

## What changes

- On startup, missing native vendor, wildlife and dungeon spawners are restored
  in small batches. Existing spawners and their living NPCs are preserved;
  stopped spawners restart and empty ones repopulate.
- Trammel uses the New Haven spawn layout. Other enabled facets retain the
  matching expansion data. Twenty-six Haven quest instructors and three groups
  of beginner animals, mounts and practice creatures are repaired explicitly.
- Every bank represented by a native banker spawn receives a supply stone,
  upgrade stone, reward stone, pet hitching post and dungeon portal. Placement
  searches for walkable tiles and preserves existing nearby service objects.
- `[GmPanel` is a 620 × 560 window with World, Spawn bots, Travel and Cleanup
  tabs. **Repair missing creatures, NPCs and bank services** reruns the repair;
  **Refresh progress** displays the current counts and errors.
- `[WorldStatus` displays progress. `[RepairWorld` is the GM command equivalent
  of the repair button. World repair runs automatically without either command.
- Supplies, rewards and dungeon travel use six readable rows per page with
  Previous, Next and Close controls. Purchase menus remain open after buying.
  Upgrades display the next tier and cost before the player chooses to buy.

Allow about a minute after startup for the queued population to finish; duration
depends on machine speed and how much is missing. Errors are counted in the
status and detailed in the server log. The normal world save persists changes.

## Validation

Built against the pinned ModernUO commit
`e7f85d404d52e0def1fb342b3dc185894a57017d` with all Haven patches.
Nine regression tests pass locally, including native map placement using the
installed 7.0.23.1 client data. The tests check bank service coverage and repeat
placement, a live banker surviving repeat repair, living Haven instructors and
training creatures, expansion selection, tab bounds and long-menu navigation.

CI runs the same suite. The three tests requiring copyrighted client map data
are skipped when that data is unavailable. Set `MODERNUO_TEST_DATA_DIR` to a
client data folder to run them locally. The suite also needs
`HAVEN_WORLD_DATA` pointing at ModernUO's Distribution directory and the
`SolutionDir` MSBuild property set to the ModernUO root so native test data copies.

In-game acceptance: wait for `[WorldStatus` to finish, visit New Haven and a bank
on another enabled facet, buy supplies, inspect the upgrade cost, browse dungeon
destinations, and verify that repeating Repair World preserves existing NPCs.
