# Island rooms, training services, infinite lockpick — September 13, 2026

Live at 02:10 EDT. Clean save/shutdown, fresh Saves backup and SHA-256 manifest, old DLL and replaced source under backup recorded in verification/island-services-backup.txt. Build: zero warnings/errors. Native migration logged ISLAND DESIGN RELEASE PASS and saved. Login/server list/relay probe passed. Original server and client asset files untouched.

## Live changes

- Existing custom courtyard compound retained. Added coherent galley/council furniture, usable storage chests, workshop tools and grouped equipment, wooden stable dividers and bedding, crew furnishings, gallery chart seating, lighting and real gallery stairs. Courtyard planting uses continuous soil tiles. Council rug and entrance framing added. Obsolete gallery ladder graphics hidden after native walking test succeeds.
- Cove entrance: excess 19 managed paving tiles removed, expedition board beside approach, paired lamps/banners. Native harbor-to-cove routes pass. Existing encounter mechanics retained.
- Island home workshop practice chest at local (10,0,7), world (4206,2868,7). Owner access required.
- Public community trainer at local (10,27,0), world (3978,2885,0). Double-click to set the lock to current skill; use lockpicks, or target with Remove Trap. Harmless practice traps. Repeated startup does not duplicate trainers.
- Corrected training lock's zero-level edge at 20 Lockpicking, which native code otherwise treats as unpickable.
- Unbreakable lockpick: nonstacking, 0.1 stone, unlimited uses; native skill requirements, checks, gains and guardian restrictions retained. Requires backpack ownership during delayed use. Independent 8% chance in tier 5+ roaming invasion spoils. Not sold by the stone.

## Verification

Isolated tests: all house walking/ladder/storage routes, cove approach connections, repeated migrations, both trainer placements with no duplicate, 1000 lockpick failure callbacks without breakage, high skill requirement still blocks unlocking, and no trap damage. Regenerated native-art house renders in verification/IslandFinishRenders. Critic reviewed the previous iteration at 7/10 house / roughly 8 usability; final polish added council rug and continuous bed. No claim of a whole-island 8/10 score or final in-client visual validation.

House migration restores existing fixture positions/art/visibility and structure on failure, deletes only its newly created fixtures, and marks completion last. New placements reject untracked property occupying their cells. Cove is idempotent, checks unknown items, and rolls back moves/paving if route validation fails. No player-owned contents deleted.
