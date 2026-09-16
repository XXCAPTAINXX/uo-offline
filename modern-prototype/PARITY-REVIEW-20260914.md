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

### September 15: automatic companion party and persistent combat bar

Every two seconds, online owners' existing companions reconcile party membership and restore a missing combat bar. Existing bars are left untouched to avoid redraw/position resets. Membership follows the owner's party, including when the owner is not leader, through travel/death/missions; native ten-member capacity is respected without evicting others. No replacement companion is created by maintenance. Manual Join party delegates to the same ownership check.

The combat bar no longer disappears into the mission timer while away. Its Close button is replaced by Party. Server-side test with connected NetState verifies party creation/disband recovery/idempotence, missing bar restoration, unchanged existing bar instance, and visibility while on mission. All preceding batch regression tests passed. Native party membership is reconstructed on reconnect rather than changing native party serialization.

### September 15: mission equipment training and gain reports

Completed trips now offer original-style 10 XP per nominal mission minute to each equipped item with current HavenEquipmentEvolution or HavenAdvancedGear progression, retaining existing level caps. Recognized advanced gear attaches its existing progression automatically; ordinary gear and backpack contents receive none. Equipment with both record types uses the equipment-evolution path once. Duration preview states the XP rate.

Completion snapshots skills before existing route training and appends actual positive skill deltas, total equipment XP, item count and gained levels to the normal report/history. Core completion guard prevents replay; early recalls skip training. Test: five-minute Mining mission awards exactly 50 XP to equipped evolving gear, reports Mining gain, repeated completion does not duplicate, early recall adds no XP. Existing history serialization and party/bar tests pass. Old uncapped stat growth/TrainingMinutes systems remain a separate parity decision and were not added here.

### September 15: mission workflow batch

Mission UI remembers its last gathering/taming selection and nominal duration in per-companion account tags; role screens do not overwrite that choice. Repeat last uses the last successfully dispatched route/duration and the standard dispatch checks, including active-trip rejection. Manual and offline trips update the saved repeat route. Collect all retries normal resource, gold, Doom item and pet-ticket delivery, with pending totals visible below the existing footer. Enlarged panel and scrollable reward details retain plain rectangular controls.

Gathering and taming completion training now respects Up/Locked/Down skill settings; capped skills retain existing cap behavior. Existing rates and completion rewards are otherwise unchanged.

Tests passed for remembered values after reopening, role-screen isolation, repeat route preservation, active-trip rejection, locked gathering, locked/down taming, and duplicate-free collect-all gold. Prior mission XP/history, party/bar, merchant, forge and boss regression checks also passed. UI layout inspected in source; client visual check not performed.

### September 15: assigned pet recovery safety

Assignment cleanup previously ignored SetControlMaster failure when the owner's follower slots were full. It now transfers normally only on success at a valid owner location; otherwise it dismounts, clears combat/control, and stores the exact pet in the owner's native stables with StabledBy set. Bonding and pet identity are retained. No duplicate stable entries are added. Hidden assignment records are virtual and no longer count against container item slots.

Runtime tests verify full-follower fallback, exact bonded pet retention, repeat deletion safety, normal return after reassignment, and virtual record status. Native stabling uses existing save serialization; this batch does not reconstruct pets lost before the fix.

### September 15: expanded mission gameplay and selected offline rotations

Supply runs now snapshot enchanted equipment at dispatch: 1/3/6/15 pieces for 5/15/30/60 minutes. Total gold ranges are 1,000-1,500 / 3,300-4,950 / 6,900-10,350 / 15,000-22,500; existing Marks remain. Completed loot goes loose into Jenna's backpack. Full-pack overflow retains exact items in a protected serialized parcel, collected without rerolls. Early recall deletes only unearned spoils; companion deletion recovers earned equipment to the owner's bank, with persisted owner links and orphan cleanup.

Duration bonuses now give equipped progression gear 50/165/345/750 XP per completed trip. Gathering training uses the same duration bonus, respects skill locks/caps, and Leather trains Wrestling and Tactics alongside Animal Lore. Preview/history and delivery descriptions reflect actual rewards.

Offline rotation has a paginated route selector, Select all/Clear all, and skill eligibility descriptions. Empty selections do not dispatch; saved selections affect the next trip, without canceling the active one. Older plans default to all routes. Hidden plan records are virtual.

Validation: audit regression suite and MissionBatchSmoke passed, including real dispatch, completion, duplicate prevention, overflow, cancellation, skill gains, selection serialization and deletion recovery. MissionBatchPersistence passed a real test-world save and separate-process reload, preserving exact scheduled/earned item identities, gold, owner/companion links and rotation. Audit saves were not copied to live. Independent code/runtime critic approved 8/10; in-client visual review remains outstanding. Live build completed with zero warnings/errors. No uncapped stat growth was added.

### September 15: house-storage browsing and protected deposit preview

Storage now supports unordered multiword search, gear subfilters (weapons, armor, shields, clothing, jewelry and spellbooks), descending names and stack-weight sorting in addition to quantity/category. Spellbooks appear under gear for comparison. Original 850x650 window size is retained with eleven rows per page; plain rectangular controls and item property hover remain.

Both house-screen and companion-target deposits show a paginated preview before moving loot. Confirmation snapshots exact item identities and quantities, rechecks ownership/access/protection, skips changed stacks/new loot, and leaves blocked items with Jenna. Gear, books, tools, ledgers, bandages and ammo remain protected. Browser-origin deposits return to the same search, folder and filters. No serialization changes.

HouseStorageBrowserSmoke passed native storage access, full-store retention, nested withdrawal, failed split rollback, foreign/stale rejection, multiword gear filtering, reverse sorting, changed/new-stack exclusion, newly protected parent exclusion and repeat confirmation behavior. Audit build passed with zero warnings/errors. Independent static critic scored 8/10 with no safety blocker; clarified protected-supplies wording and stack-weight label, and retained original window dimensions following review. No in-client visual inspection claimed.

### September 15: pet taming skill progression

New first tames now cap every nonrolled skill at 100 and apply native taming loss to a starting value limited to 100 (normally at most 90; native paralysis/greater-dragon reductions remain). Random legendary skill caps remain at their rolled 125-150 ceilings, but their current levels and supporting skill levels also drop and require training. Rarity no longer awards free 105/110/120 combat caps. Native power-scroll training requirements are retained.

Mission pets apply rarity before taming loss, then persist a prepared flag (ticket serialization v3). Older never-owned mission tickets are normalized on successful first claim. Stored pets and already prepared tickets are not reduced again. Legendary record v2 persists its tamed state so startup repair cannot refill trained skill levels; existing owned pets are excluded from repair too. Retaming retains existing/purchased caps and native percentage loss. Existing owned pets are not retroactively reset because old free caps cannot reliably be distinguished from purchased upgrades.

Patch 0053 hooks native AnimalTaming.ScaleSkills and removes the normal-taming GreaterDragon Magery refill in Haven. GM-only PetTrainTest retains its separate native Magery refill. Lore text now explains training loss and scroll caps.

PetTamingProgressionSmoke passed all rarities, normal and rolled caps, supporting skills, legendary serialization/repair, retained 120 cap with 110-to-99 retame loss, mission generation, actual ticket claim without double loss, and stored 110/120 pet recovery without mutation. Build passed with zero warnings/errors. Independent review identified retame clipping; corrected before deployment. No retroactive character/pet reset performed.

### September 15: revised new-pet starting skills

Per the player's correction, first-tame starting levels are now Tactics/Wrestling/Anatomy 60.0-69.9, and other previously active skills 0.0-5.0; inactive skills remain zero. This replaces the earlier percentage-based first-tame levels for all rarities and species. Ordinary caps100 and random overcap ceilings are unchanged. Retames keep percentage loss; stored pets retain their exact skills.

Ticket v4 stores preparation version2, so never-owned mission pets prepared under the earlier rules receive the new starting values once on successful claim. Newly prepared tickets and stored pets avoid rerolls. Existing owned pets are not reset. Lore descriptions updated. Native runtime tests passed exact ranges for all rarities, mission pets, restart repair serialization, cap retention, ticket claims and stored-pet preservation; audit/live builds and login probe verify deployment.

### September 15: trainable low-skill legendary abilities

Granted innate abilities now receive native probabilistic skill checks on valid attempts, including their granted supporting skills. One shared six-second practice cooldown per pet; skill locks and individual caps are respected. Combat practice requires an engaged eligible enemy, and healing practice requires an actual started healing attempt. This does not raise caps or assign skill levels. Detection practice above100 requires a hidden combat target; ordinary idle scans retain native checks.

Magery progresses from Magic Arrow to Lightning to Energy Bolt; Mysticism starts with Nether Bolt before Eagle Strike; Spellweaving uses Thunderstorm before Word of Death. Hard minimum skill requirements block the effect while permitting practice for an otherwise valid, mana-supported attempt. Existing high-skill ability choices remain. Beginner Thunderstorm filters native splash targets through the same engaged-enemy checks, excluding players, pets and unrelated creatures.

PetAbilityPracticeSmoke passed real native gains from zero for primary/supporting skills, locks/caps, granted-action/hostile-target checks, shared cooldown, spell tier choices, advanced spell rejection at low skill, and utility gains beyond100. Existing LegendaryInnateSmoke passed Discordance and all seven native casting schools. Timed Thunderstorm damage test passed for engaged enemy with owner/bystander unchanged; test horse disables automatic stabling to keep it present during the timed fixture. Independent reviewer findings on utility gains and AoE filtering were resolved; final review found no further blocker. Audit/live builds clean; login probe used for deployment verification.

### September 15: controlled story arrival and Jenna introduction

New Haven now anchors the original adventure opening, The Beacon Beyond. Native character creation first tries a verified landing beside the recovered beacon at Trammel3506,2570,14, rather than blindly relying on the client's selected starting city. Haven-only hook, fresh Internal-map player with account and no story stage; original placement remains fallback if the landing cannot be found. Native gear/stat/profession setup remains in place.

Per-character account tags persist pending/completed introduction. Pending new characters receive the opening after login; meeting Jenna beside the beacon claims/reuses their own companion, ensures her female appearance, and joins the existing party helper. Repeated responses cannot recruit again. The first two text pages explain the summoning, vanished expedition and shared goal, then point to [c and exploration in Haven. The island remains a later refuge rather than an unexplained starting possession.

[story and the beacon replay the text for returning characters without moving them, changing skills, claiming a house, or creating a companion. The beacon is an idempotent small nonmovable rune marker, not a completed scenic build. No voice recording/playback or cinematic has been added yet.

ArrivalStorySmoke passed valid spawn, repeated placement protection, beacon idempotence, single female Jenna introduction, existing-character exclusion and replay preserving location/skills. Source integration checked through native CharacterCreated->DoLogin. Audit build passed with zero warnings/errors; no client character-creation walkthrough or visual scene review claimed.
Independent review found an absent-companion introduction edge; Meet now requires Jenna alive, present, unstabled and off mission before advancing. Regression verifies stage remains pending while she is elsewhere.

## 2026-09-15 — A Light That Answers opening quest

Implemented the original Haven beacon investigation for new and returning characters. Begin at the New Haven arrival beacon with Jenna after the first meeting; `[storyquest` reopens the journal. Physical clues, an actual50-damage training objective (player/pet/companion credit), direction arrow, tide/anchor/star sequence, and saved per-character progress lead to2,500 bank gold and20 Marks. Repeated responses cannot duplicate payout. The next island chapter remains future work.

Jenna offers one character-bound evolving cape and one basic controlled trail horse with a blessed bonding apple through journal reward buttons. The apple works only for its original character and exact living quest horse; feeding bonds immediately. Claims remain available if pack space/follower capacity is insufficient. Completed characters can claim one Expedition Fountain of Life deed: house addon, instant one-for-one conversion of every ordinary bandage deposited, no charges/timer; native enhanced bandages and house security preserved. Uses existing art pending a separate custom-art client update.

Haven golem now retaliates against its current player/controlled-pet trainee instead of being passive. It stays stationary, stops on departure/timeout, clamps final native AOS damage to1 and cannot deliver a lethal hit. Low-Wrestling pets can practice Parry against this golem through native skill checks. No loot or kill XP. Startup ensures the actual quest station exists, rather than accepting an unrelated house golem.

Leveling tooltips now distinguish cumulative XP from XP remaining to the next level, explain qualifying XP sources, and show the next weapon mana-leech floor. Starter weapons also show next-level damage increase. Native properties retain current bonuses. Companion clothing correctly describes Jenna's combat/mission XP.

Five original Jenna dialogue lines use temporary Microsoft Zira synthetic speech with subtitles, private player audio, mute/replay and overlap throttling. IDs32760–32764 installed; ICQ32766 untouched. TazUO's custom loader requires headerless PCM16 mono22050Hz in files named `.mp3`; named WAV masters and reproducible synthesis/packaging/install scripts included. Client restart required. No in-game listening/visual-art review claimed.

Validation: audit and live Release builds0 errors/0 warnings; BeaconQuestSmoke passed native player/pet/Jenna damage credit, low-Wrestling Parry path,1000 incoming test clamped to<=1 and0 at1HP, one-time gifts, owner/horse bonding restrictions, out-of-order clue and absent-Jenna refusal, Mark-cap refusal, sequence reset, payout deduplication,60,234 ordinary-bandage conversion plus7 existing enhanced preserved, repeated conversion idempotence, all six journal pages and mute. Independent static critic approved pending those runtime checks; its companion-clothing tooltip correction was applied. Audio waveform validation passed8.65–11.42 seconds with no clipping.

Deployment backup: `E:/Backups/Haven/Prototypes/servuo-before-beacon-quest-20260915-225636`. Native patch0055 adds final damage clamp and golem-only pet Parry eligibility. No audit saves copied to live.

Approved next art direction: tide/anchor/star expedition motif; portable Wayfinder's Beacon with story-earned destinations, restored portal, custom Expedition Fountain. These new graphics and beacon travel functionality are not implemented in this release.

## 2026-09-15 — Starter horse training
Jenna's Haven trail horse now exposes the full existing pet-training catalog: all native magic/mastery choices, special abilities, weapon moves and area effects. It can train from 1 to 5 follower slots. Existing horses receive the expanded limits on load; ownership, bonding, stats, skills and purchased training remain intact. Ordinary horses are unchanged. Skill caps still require the existing power-scroll/training progression.

Validation: extended BeaconQuestSmoke compares the horse's definition against every native training-table option and checks 1-to-5 slot limits. Full quest/bonding/fountain/sparring regression passed; audit and live Release builds checked before deployment.
