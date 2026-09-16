# Champion starts and role preview — 2026-09-11

Fixed a reproducible shared start-radius mismatch: camp decorations can be opened from positions outside the invisible controller's eight-tile range. All four variants now accept a nearby linked decoration (same facet, three tiles and LOS) as well as the original controller range.

StartError reports separate unavailable/account, selection, active encounter, cooldown, distance, nearby combat target and recent aggression reasons. Only non-expired aggression records block starting. A deleted/dead/off-facet/distant old target is not treated as a currently engaged nearby enemy. Recent aggression still blocks; no criminal state is cleared. User's exact live failing predicate was not captured before the change, so do not claim it was definitively distance alone.

Camp uses flat rectangular buttons. Roles menu now has choices on the left, detailed preview on the right, active-role indication and a separate Use-role action. Selecting a preview does not change the role. Existing SetRole restrictions retained.

Tests: clean isolated build and full suite COMPLETE. All four StartError variants allow the reproduced marker-edge position; distance reason verified; ten-minute-old aggression and distant target allow starting; actual Begin spawns five enemies for each normal variant and fifteen for Challenge. Challenge completion and reward tests continue to pass.
Deployed live after clean save and hash-verified backup E:/Backups/Haven/Prototypes/servuo-before-champ-start-roles-20260911-203859. Build zero warnings/errors; startup, account login, server list and relay passed.
