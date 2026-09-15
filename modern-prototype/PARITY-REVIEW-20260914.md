# Old-server feature parity — 2026-09-14

This compares the original ModernUO custom source under `playerbots/source/CustomBots/UOOffline` with the current ServUO implementation under `modern-prototype/source`. It is feature restoration, not a character/save conversion. Preserve the current player's progress and newer requested improvements. Existing unrelated working-copy changes are not included in this release.

The source inventory covers 179 original custom files and 173 current custom files. Only 34 filenames match directly; that does **not** mean the remaining features are missing, because many have been renamed, consolidated or replaced by native ServUO systems. `PARITY-SOURCE-INVENTORY-20260914.csv` is a review index, not proof of functional parity. Older restoration documents contain stale staged/not-deployed statements and should be cross-checked against release notes and live sources.

## First verified batch

| Area | Original behavior | Current change / remaining difference |
|---|---|---|
| Bulk gear repair | Repair equipped and nested weapons, armor, clothing and jewelry | Current free repair now includes clothing and jewelry. The existing free pricing is retained rather than reintroducing the original 50-gold ordinary repair fee. |
| Lost maximum durability | Restore normal maximum for 250 gold per nonstarter item; starters free | Added to Haven supplies > Repair service. Shows item count and bank-gold quote; refuses a higher stale quote or insufficient funds without mutation. Keeps current 255 maximum for supported evolving gear, and never reduces a higher maximum. |
| Companion animal harvesting | Earned animal corpses only, safe after combat, collect/cut hides, persistent toggle | Added Nearby hides toggle. Gathers within two tiles, requires both owner and Jenna fully healthy, no combat/poison, and Follow/Guard. Native carve uses the owner's established loot rights. No strangers' corpses or controlled/bonded/summoned creatures. Wider six-tile autonomous movement from the original is still pending. |

## Behavior comparison queue

| Area | Existing evidence | Remaining work |
|---|---|---|
| Evolving gear and jewelry | HavenAdvancedGear, HavenEquipmentEvolution, matching rings, Concord, shield-warrior gear; earlier release notes | Compare every original quest and boss-specific acquisition path. Finding an item type in the gear catalog does not prove its reward path exists. |
| Pets | Custom species, native training bridge, rarity/innates, book, recovery and assigned companions | Compare original defenses, skills, ticket transitions and all acquisition sites without undoing the player's newer overcap/ability rules. |
| Missions | Gathering, 12 tame routes, regional resources, Doom reconnaissance and offline/AFK settings exist | Compare resource quantities, skill gates, durations, journal history and reward lifecycle against original code. |
| Companion roles | Warrior, caster, healer, bard, archer and Armory puzzle helper exist | Compare support decisions and noncombat conveniences. Preserve the recent combat bar, gear, female Jenna and dungeon helper changes. |
| Named boss content | Native ServUO bosses plus current custom encounter loot | Compare original HavenScalis*, HavenCoraCorgul, HavenAbyssTrial and their unique rewards with native equivalents. Not yet verified as missing or matched. |
| Market and guild | Native vendors and a current guild resource ledger | Compare original HavenMarket*, HavenGuildCrew, HavenGuildCrafting and bank/storage behavior. Do not assume the native vendor system replaces these features. |
| Island | Current custom courtyard, cove, services, storage browser and decoration tools | Review original service coverage and visual differences separately; do not replace the user's current house with the old castle. |

## Validation

GearRepairParitySmoke passed: all four gear families including nested equipment; repeat no-op; unrelated gear untouched; 250-gold quote, insufficient funds, stale quote rejection, exact charge, property retention and free starter restoration to 255.

CompanionSkinningSmoke passed: native earned animal corpse carving, hides converted to leather in Jenna's backpack, no duplicate harvest, no harvesting while injured/in combat, rejection of an unearned corpse. The automatic scan is intentionally limited to two tiles; no claim of full original hunting movement parity is made.

Audit and live build results and deployment are reported in the task. This is the first parity batch; the overall comparison remains unfinished.
