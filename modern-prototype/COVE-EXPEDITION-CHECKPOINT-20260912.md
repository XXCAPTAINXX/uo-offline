# Blackwake cove expedition checkpoint — September 12, 2026

Isolated prototype only. No live restart or deployment.

## Implemented
- A separate Blackwake cove expedition at Trammel 4214,2922 beside the sheltered inlet. Three waves of five deckhands, boarding guards and quartermasters, followed by Admiral Blackwake (3,600 HP).
- Pirate outfits and a dedicated menu with start/status/pending rewards. Combat targeting excludes houses and targets more than 12 tiles from the cove marker.
- Reuses the native Haven expedition state machine and damage-owner participation credit. Each qualifying participant receives the existing 10,000-gold/resource/scroll reward set and 20 Marks, plus 25 cannonballs, powder charges and fuse cords. Boss treasure maps and weapon/shield sets retain chance rolls.
- HavenMiniChamp now provides overridable site, spawn, enemy and island-reward hooks. Default Haven behavior remains unchanged. Its Find/Ensure path explicitly selects the original camp class, keeping cove instances out of Haven relocation.
- Plaza service maintenance is bounded to the Haven plaza; it no longer selects or removes service stones/posts across all Trammel.
- Encounter deserialization clears previous enemy/participant collections before restoring saved entries.

## Verification
Final Release build: zero warnings/errors. Native isolated test passed complete wave/boss progression, pet-owner participation credit, 20 Marks, ship supplies, cooldown, active-state serialization roundtrip, separation from Haven camp lookup, and island service preservation during plaza maintenance. Existing island route, housing placement, boat corridor and patrol checks also passed.

Test harness advances the three-second inter-wave scheduling directly to verify progression. Serialization was tested by a writer/reader roundtrip, not a full process restart. In-game difficulty, appearance, boundary behavior and multiplayer balance still require testing. Verification runtime exited without saving; normal DataPath restored and island test marker disabled.

## Remaining island integration
Persistent house ownership/access and connected private receiving/storage; polished community-center placement; matching modern client/server terrain deployment; client walkthroughs, boat boarding, multiplayer encounters and full-world save/reload validation. The cove and world patrols are implemented in the isolated prototype but not available on the live server yet.
