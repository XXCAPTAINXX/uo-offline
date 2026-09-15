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

## Boss artifact growth batch — 2026-09-15

Native equivalents exist for all 16 equipment pieces in the original HavenScalisLoot.Artifact catalog: Enchanted Coral Bracelet, Leviathan Hide Bracers, Wand of Thundering Glory, Smiling Moon Blade, Corgul's sash and two handbooks, Ring of the Soulbinder, Helm of Vengeance (native class spelling HelmOfVengence), Rune Engraved Pegleg, Culling Blade, Blight of the Tundra, Bracelet of Protection, Brightblade, Hephaestus and Prismatic Lenses.

HavenAdvancedGear now recognizes those exact native types with a separate boss-artifact growth kind. Levels 1–20 grant the original artifact-specific +1 weapon damage and spell damage per gained level, +1 hit/mana regeneration per five-level milestone, and weapon +2 swing speed / +3 mana leech per milestone. The current shared mana-leech floor (100 at level20), shared Luck/stat growth and supported-gear durability behavior remain. Native base properties and acquisition systems are retained. Named artifacts use readable display names. Existing Legendary/Reforged/other progression records are retained rather than replaced.

Existing items are recognized on startup. Newly obtained items attach through the existing equipped kill-XP path. No reward probabilities, native encounter mechanics, gold payouts or soul-forge functionality were changed. The original Scalis/Corgul custom reward rates and Covetous acquisition differences still need separate review; this is growth parity, not a claim that every old boss encounter has been ported.

BossArtifactGrowthSmoke passed for all 16 types through level20, including repeated application/capped XP and retaining an existing Legendary record. Existing record serialization format is unchanged. Audit/live builds passed; no new save/reload fixture or client visual inspection was performed for this batch.

### September 15: shared sea-boss artifact delivery

Fixed BaseSeaChampion's weighted recipient draw to use eligible entries and include the upper boundary. Previously a remote participant could win, or the final boundary could discard an earned artifact. Eligibility now requires the same map. In Haven, full backpacks receive the exact artifact in the bank, and recognized equipment receives its growth record on delivery.

Verification: clean audit build; twenty one-damage draws with an ineligible high-damage attacker; full-pack bank delivery; native pet damage credited to its owner; no eligible recipient receives nothing. Clean live build and login probe are deployment checks. Drop probabilities are unchanged. Corgul's separate guaranteed unique-item recipient selection still needs review; this batch fixes the shared sea-champion draw and delivery only.

### September 15: Corgul guaranteed unique recipient

Corgul's separate guaranteed unique-item draw now filters for looting rights and eligible nearby players before retaining the native top-five selection. Haven-only change; guaranteed unique reward and additional shared/decorative rates remain unchanged. The prior shared sea-boss delivery fix provides bank fallback and growth attachment.

Verification: twelve native Corgul OnBeforeDeath calls with equal pet/remote-player damage delivered the guaranteed unique item to the eligible pet owner's full-pack bank every time; remote backpack stayed empty. Existing weighted-draw, no-eligible-recipient, and pet-credit checks also passed. These are server-side fixture tests, not client combat playthroughs. Old custom Corgul gold/map/transcendence extras still require a separate comparison with current reward hooks.

### September 15: boss supplemental supplies restored

HavenBossExtras restores old physical extra bundles on native boss death: Corgul level-6 treasure map and Tactics transcendence scroll (1.0 Felucca / 0.5 elsewhere); Cora level-5 map; Scalis message in a bottle, special fishing net and fishing pole. Requires native looting rights, at least 600 credited damage, alive and within 32 tiles on the same map. Native looting rights consolidate pet damage. Each boss/player pair pays once; full packs bank items directly. Current native artifact rolls and Sovereign hooks stay intact.

Server fixture checks passed for all three bundles, pet-owner credit, remote-player exclusion, full-pack bank fallback and duplicate-call prevention. Audit/live builds and login probe are deployment checks. Remaining differences include old gold/Marks/shard bundles and Scalis soul-forge integration; those are not restored in this batch, nor is an extra artifact roll layered on native rewards.

### September 15: boss currency bundles restored

Added old supplemental currency payouts to the same deduplicated boss extra award: Corgul 50,000 gold, Cora 30,000, Scalis 40,000; each also pays 20 Marks through the existing capped balance and 10 Astral Shards. Gold is delivered as a bank check (native account banking automatically redeems banked checks). Existing corpse gold, artifacts and Sovereigns are unchanged. Notification reports actual Marks credited.

Tests verify exact total gold including account conversion, exact shard amounts and +20 Marks for each boss, with repeated award calls producing no additional currency. Pet credit and remote exclusion remain covered. Native SmallSoulForge exists but lacks the old custom Haven Abyss artifice menu: rare forge reward remains pending rather than claiming full feature parity.

### September 15: Scalis artifice forge and merchant stock

Scalis now independently rolls 5% per qualifying recipient for a blessed HavenSmallSoulForgeDeed. The placed one-tile forge opens all 11 original Abyss recipes, with readable material labels, two pages, and an explicit spend confirmation. Requires 80 Blacksmithing/Tailoring/Tinkering/Inscription; each consumes 8 essences and 2 of each material. Native multi-type consumption validates all ingredients first. Ordinary gear only; existing evolving gear/artifacts excluded. One virtual serialized marker prevents repeat attunement without taking an inventory slot. Addon/deed/marker serialization implemented; fresh save/reload and visual client inspection remain untested.

Merchant normal catalog stock refreshes on opening/buying and after successful purchases: minimum 1,000 stackable supplies, 100 nonstackable items, 10 animals. Native economy stock floor raised to 1,000; larger configured stock preserved. Resale objects and player-vendor inventory are not manufactured by this helper. Native purchase prices and payment path unchanged.

Tests: all 11 recipes succeed with exact costs, missing ingredients consume nothing, repeated attunement blocked, marker virtual, evolving artifact excluded, forge range and crafting threshold checks, 5% boundary. Actual native Alchemist purchase returns replenished stock, larger economy quantity preserved. Previous sea-boss reward tests still pass. Audit/live builds and login check complete deployment verification.

### September 15: mission usability batch

Added owner-only History access to mission screen. A separate internal serialized record stores the latest 20 completed/early-recalled trips, newest first, including existing reward reports and UTC timestamps. Existing companion save layout is unchanged. History starts with new trips; no fabricated historical backfill. Deleted companion records are cleaned up.

Mission menu and timer early-recall buttons now ask for confirmation. Confirmation snapshots the trip due time and verifies ownership/current trip; stale windows cannot cancel a later mission. Core Recall remains immediate for emergency recovery and existing commands. Forge recipes now show owned/required quantities for all three ingredients.

Tests: real offline dispatch/completion writes one history entry even on repeat completion; early recall writes its report; stale/foreign confirmation rejected; history retains newest 20 in order and round-trips serialization. All existing forge and merchant runtime checks pass. UI compiled and layout reviewed from code; client visual inspection remains outstanding.
