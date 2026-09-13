# R.E.C. — Rare Export Company headquarters

The old fixed castle is replaced by a **31-by-31 customizable foundation** at **4196, 2868, Trammel** on the owner's island. The extra southern stair row makes the component footprint 31 by 32. The building uses normal editable house components, not a static castle multi.

- Ground floor: automatic receiving/sorting chest, linked profession storage, smithy and soul forge, tailoring, carpentry/tinkering, repair bench, galley and pet supplies.
- Middle floor: guild hall, mapmaking and alchemy, with an open front gallery.
- Upper floor: captain's cabin, armory, treasury and two lookout rooms around an open deck.
- Double-click the ship's stair on any floor to choose a deck. Nearby pets accompany you.
- Double-click the house sign and use the normal house customization menu to edit the design. Use the character's Guild menu to create or join R.E.C.; storage follows the owner's guild membership.
- Storage capacity: 10,000 items and 5,000 lockdowns. The original receiving chest and 13 linked profession stores keep their serials and contents.

The island mini champion is on the **north shore at approximately 4198, 2836**, behind the headquarters. Its 12-tile roaming area stays north of the house. Existing trial progress and the same controller are preserved when moving it.

## Installation

Apply content sources and `0061-castle-size-custom-house-packets.patch` after previous patches. Stage the larger foundations with `tools/stage_house_plots.py`; install the generated `multi.idx` / `multi.mul` and, when present, `MultiCollection.uop` into **both the server data folder and the Haven client's configured data folder**, with both processes stopped and backed up. Restart both. Custom IDs `0x1800..0x18A8` cover 19–31 tile plots; this headquarters uses `0x18A8`.

The existing `guild-castle` release action now migrates the owner's old HavenGuildCastle to HavenPirateHeadquarters. It is idempotent, preserves owners/co-owners/friends/security and stored items, and refuses occupied vendor inventories instead of destroying them. Migration is intended for the existing island headquarters; arbitrary player houses are not replaced.

The normal placement tool still lists its existing sizes. The new 31-tile house is already placed for the island owner and can be customized through its sign; this release does not add 169 entries to the generic placement menu.

## Validation

1,104 content tests pass, including migration with real nested valuables, all linked stores, access to every crafting/storage fixture, front entry, three deck destinations, saved design data, and house packets spanning multiple compressed offset chunks.

The same release raises town discoveries to 25 Sovereigns and dungeon discoveries to 50, separately by facet. Earlier discoveries receive the difference once when the character's progress is checked.
