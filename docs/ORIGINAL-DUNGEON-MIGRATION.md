# Original Shadowguard and Blackthorn locations

The player requested the familiar original locations rather than the frontier islands. This migration preserves the compatible encounter mechanics, earned seals, completion counts, Minax credits and doubloons while replacing their physical setting. Deployment evidence is recorded separately.

## Original places

- Shadowguard: Minax's fortress in Eodon, on Ter Mur. The entrance crystal uses **501,2192,50**. Arrival/recovery uses **505,2192,26**; the modern map has a decorative surface one unit above the traditional 25-height kick point.
- Bar, Orchard, Armory, Fountain and Belfry use standard Shadowguard instance centers: **96,2016,-20; 224,2016,-20; 352,2016,-20; 480,2016,-20; 160,2080,-20**. The Roof uses **64,2336,0**. Their boundaries, scenery and arrival offsets fit the original rooms. The Belfry's dragon platform is 22 units above its floor.
- Blackthorn: the normal Trammel castle stairs at **1477,1471–1475,-8** lead into **6432,2677–2681,0**. Return stairs are **6440,2677–2681,20**. The invasion uses the existing Britain town fragment around **6317,2555,0**, with three waves and existing Haven rewards. Native corridors and foyer doors are restored.
- Only walkable stair source/arrival pairs receive new passages. Existing stock Blackthorn passages are reused; stock exits are corrected to the clear center castle landing at **1477,1473,-8**, avoiding blocked outer tiles. Unrelated conflicting teleporters still stop migration before any changes.
- `[shadowguard`, `[blackthorn`, `[frontiers` and the Atlas point at these locations after successful migration. Chelonia, the private estate, Commons and pirate boat are unchanged.

Shadowguard keeps the explicitly requested easy Orchard matching, simplified Fountain steps, and companion puzzle assistance. This move does not silently substitute a complete official-era encounter/loot port. Original room scenery and locations are restored; the retained Haven encounter rules remain documented in [FRONTIER-EXPEDITIONS.md](FRONTIER-EXPEDITIONS.md).

## Map staging

`tools/map/stage_original_dungeons.py` reads the user's existing modern client UOP maps and copies selected land/static blocks into independent staged copies of the current server maps. It validates hashed UOP chunk ordering and lengths. No proprietary binary assets are checked into source control.

Trammel selections cover Blackthorn's dungeon (6208,2304,320,512) and castle stairs (1472,1464,16,24). Ter Mur selection (0,1280,1024,1280) restores Eodon, including Shadowguard's entrance and instance areas. Existing Abyss expedition sites lie outside that selection. Other Haven/custom-island map blocks are retained.

Tile definitions used by imported geometry and room scenery are aligned with the modern client; other records retain the server definitions. Both server and private TazUO data receive matching map/static/tiledata files. The private Ter Mur UOP overrides are disabled in favor of those matching MUL files. The shared EA client installation remains untouched. Navigation caches must be rebuilt against these files.

## Migration safety

Migration requires a complete existing installation, inactive encounters, usable original entrance terrain and empty Shadowguard instance footprints. Conflicting stair teleporters are rejected before any arena is changed. Only recorded encounter fixtures are removed. Loose characters, followers and dropped movable items/corpses are moved from retired encounter areas to the appropriate entrance; offline character return coordinates are migrated as well. Existing character-owned progression records are retained. Repeating migration is a no-op once all rooms and Blackthorn have moved.

Room cleanup uses its own current facet and exit location, including during saved-world recovery. Blackthorn's original dungeon region is recreated on load and unregistered when its controller is deleted. Newly created stairs and doors remain part of the owning hub's saved fixture list; reused stock stairs retain their existing ownership. A coherent rollback requires pre-migration Saves, assemblies/source, server/client maps and navigation caches together.

## Attribution

`HavenShadowScenery.cs` adapts the component arrays from ServUO's BarAddon, OrchardAddon, ArmoryAddon, FountainAddon and BelfryAddon. Those derived room-layout data remain under ServUO's GNU GPL terms, rather than the repository's default MIT license. The license is included at [third-party/ServUO-LICENSE.txt](third-party/ServUO-LICENSE.txt).

Reference revision: **d76bf4443cf76d081ddaf8f57c87ff33749256af** of [ServUO](https://github.com/ServUO/ServUO/tree/d76bf4443cf76d081ddaf8f57c87ff33749256af/Scripts/Services/Expansions/Time%20Of%20Legends/Shadowguard). Blackthorn stair, door and invasion coordinates were checked against that revision's `Data/teleporters.csv` and `Scripts/Services/Revamped Dungeons/BlackthornDungeon` definitions.
