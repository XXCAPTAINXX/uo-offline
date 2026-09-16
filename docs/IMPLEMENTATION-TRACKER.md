# Haven implementation tracker

The player explicitly released the deployment hold on 2026-09-09 and authorized a thorough review, deployment, restarts and troubleshooting. The implemented release is installed and its new world controllers are active. Live save/restart verification passed, followed by another successful automatic save. TazUO starts with the matching private map data and is left at its login screen; in-game visual acceptance remains unverified. See [the deployment report](DEPLOYMENT-2026-09-09.md).

Canonical source is at `haven-fixes`, isolated verification at `verification/HavenMasteryVerify`, and independent native patch audit at `verification/HavenPatchAudit`. Existing uncommitted work is retained. Historical “staged only,” “deployment hold,” and “no deployment performed” notes in earlier feature documents describe their original verification dates and are superseded by this release status. They do not restrict the newly authorized rollout.

## Current release — 2026-09-09

### Mastery defense and friend testing readiness

Saving Throw now checks native disarm attacks. Resilience protects eligible owners/party pets against new poison, bleed, mortal-wound and Curse effects with documented bounded compatibility formulas. **1,020 content tests passed against newly prepared private maps**; native patch 0051 applied cleanly to the independent pinned-engine audit checkout. Added a fresh-install map preparation command without workspace-only reference dependencies and an administrator command for original dungeon setup. See the [friend installation guide](FRIENDS-TESTING.md) and [player checklist](PLAYER-TEST-CHECKLIST.md). Live deployment evidence is in [the readiness report](DEPLOYMENT-FRIENDS-READINESS-2026-09-09.md).

### Ledger transfers and book slot fix deployed

Resource Ledgers now offer **Transfer all...** to merge all stored resources into a chosen ledger in the owner's backpack. Champion's Codex contents no longer consume backpack slots; existing inventory totals correct themselves on world load. Both filled books use only their own slot. **1,016 content tests passed**, and live startup/save preserved the companion assignment and journal. See [deployment evidence](DEPLOYMENT-LEDGER-TRANSFER-2026-09-09.md) and [controls](COMPANION-MISSION-REPORTS.md).

### Companion-carried Resource Ledger deployed

Supported mission resource deeds now deposit directly into an existing ledger carried in the companion's pack, including nested bags and AFK/offline rewards. Reports preserve exact deposit receipts. Nearby bound owners can open the carried ledger and withdraw into their own pack. **1,009 tests passed**, with live save/startup and assignment/journal preservation verified. See [deployment evidence](DEPLOYMENT-COMPANION-LEDGER-2026-09-09.md) and [controls](COMPANION-MISSION-REPORTS.md).

### Companion reports and regional resources deployed

Mission field reports now track actual rewards, skills, stats and gear progression across normal, AFK and offline runs. Added four Malas/Abyss resource routes with Resource Ledger-compatible deeds. **999 content tests passed**, all 319 installed payload files verified, and actual saved-world mission receipt/reload checks passed. Live save/restart preserved Alden's ongoing gear assignment and journal. See [controls](COMPANION-MISSION-REPORTS.md) and [deployment evidence](DEPLOYMENT-MISSION-REPORTS-2026-09-09.md). Tracking begins at installation; earlier runs are identified without invented loot details.

### Offline companion gear assignment deployed

At the player's request, Alden Ashford (52621), owned by the played Rictor Quake (5176), was assigned normal five-minute gear missions while logged out, ending on the next login. His previous finished expedition was collected once; existing equipment was preserved. **992 content tests passed with zero failures/skips**. Saved-world rehearsal and live reload retained the assignment, deadline and pack contents. See [behavior and controls](COMPANION-OFFLINE-GEAR-GRIND.md). The final bundle is `artifacts/HavenGearAssignmentRelease20260909b`; its final correction returns a controlled companion from mission storage on login without restoring shrunken pets through their tokens. No client or map update was needed.

### Original dungeon locations deployed

Shadowguard has moved to its original Eodon fortress and native Ter Mur rooms; Blackthorn uses the familiar Trammel castle stairs and original dungeon. The Atlas, commands and board destinations follow the move. **987 content tests passed with zero failures or skips**. Actual saved-world rehearsal and live migration/save/restart/repeated migration passed; world initialization reported zero errors and client startup reached the login screen. Existing progression and estate ownership were preserved. See [deployment evidence and rollback](DEPLOYMENT-ORIGINAL-DUNGEONS-2026-09-09.md) and [migration details](ORIGINAL-DUNGEON-MIGRATION.md). In-game visual acceptance remains unverified.

### Frontier increment deployed

The earlier frontier release added six compatible Shadowguard rooms with companion puzzle assistance, Blackthorn invasion waves, anchored corsair boarding/cargo, and Chelonia's amphibious tortoises. The Orchard is intentionally easy with explicit matching hints. All **984 content tests passed without failures or skips**; real-save rehearsal and live setup/save/restart/repeated setup passed. Its substitute Shadowguard/Blackthorn islands were superseded by the original-location migration above. See [the frontier deployment report](DEPLOYMENT-FRONTIERS-2026-09-09.md) and [mechanics](FRONTIER-EXPEDITIONS.md). Full official encounter/loot ports, arbitrary puzzle AI, moving naval/cannon systems and remaining SA/mastery work are still pending.

### Earlier complete staged release

- Preflight passed **967 content tests** and **820 server tests**; the server suite reported **17 skipped**. The skips are not counted as passes.
- A rehearsal used an actual live-save copy, exercised installation, saved and reloaded it, and checked repeated installation for duplicates. Release fixes include Stormscale/MageAI compatibility, native Doom cleanup preserving unrelated items at the same height, and poison initialization isolation in tests.
- Frozen deployment bundle: `artifacts/HavenCompleteRelease20260909`. Coherent pre-release backup: `D:/Uo Offline/uo-modernuo/haven-world-backups/complete-20260909-1902`.
- Live installation confirmed **one Commons**, **one estate with 251 owned fixtures**, **six Doom gauntlet controllers**, **one Ancient Hunt**, **thirteen Abyss expedition sites with two director-owned fixtures**, and **one Frostbound den**. The estate belongs to the played Rictor Quake character, **serial 5176**.
- Matching island terrain is installed for the server and a private TazUO data directory. The private client copy preserves newer graphics and avoids changing the shared EA installation used by other shard profiles. Independent hash checks passed; the profile changed only its data-directory setting. TazUO reached the login screen successfully.
- The live post-install save completed at 19:08:29 EDT; the controlled restart loaded all 143,485 saved items and 34,742 mobiles. All installation counts and ownership persisted. Population initialization preserved 5,357 spawners, supplied 45 banks and reported zero errors. The next automatic save completed at 19:10 EDT. In-game presentation and gameplay acceptance remain separate from these verified server checks.
- At this earlier release, Shadowguard, Blackthorn and pirate vessels were pending. The frontier increment above supplies playable compatible versions. Full later-era ports, complete Stygian Abyss expansion systems, native Imbuing/unraveling and remaining mastery edge cases are still **pending**.

## Constraints retained

- Player skill cap 1,000; Animal Taming, Animal Lore, Focus and Snooping are free skills. Base stat cap 300 before bonuses/scrolls.
- Preserve Haven training/luck rules, Old Haven boss progression, wallet purchasing, free evolving starter claims, and existing bank/recovery destinations.
- Special Haven/Astral/Legendary rewards and companion equipment evolve. Ordinary equipment does not automatically evolve.
- Custom pet rarities affect custom species only. Legendary custom pets start at one slot; an earned player follower-cap increase comes from special gear progression, not pet training.
- Explicit companion AFK mode remains on until disabled. Mission outputs are direct items or commodity deeds, not accumulating reward bags.
- Bots must earn/gather/craft goods; artifact brokers cannot invent named artifacts. Preserve purchases, player-assigned gear and persistent worker identities.
- Newer-era content must have real mechanics, safe entry/exit, cleanup, persistence and rewards before being described as working. Existing models/art may be used for compatible features; new client art requires separately staged assets.

## Work areas

| Area | Baseline evidence / gap | Completion criteria |
|---|---|---|
| Existing Haven services, wallet, starter rewards, pets, missions and progression | Existing source and regression tests; old notes contain superseded behavior | Keep current rules; repair newly found failures rather than reintroducing older decisions |
| Wandering encounters | Waves, champion, leash, point shop, custom pet summons, journal saved and tested | Review cancellation, ownership, participant credit and economy interactions |
| Travel, discovery rewards, carrying bags, water mount | Installed release code and tested behavior; in-client route/UI review still pending | Verify supported destinations, SOS/cargo safety, resource weight and companion following |
| Market | 13 finite-stock producers/brokers, BOD work, wallet purchases and global search implemented; Commons installed | Verify live search/inspection/purchase presentation; preserve finite stock and reject stale purchases |
| Guild crews | Four persistent workers, two party limit, gathering and deeds | Useful board/roster, officers and treasury permissions, explicit jobs/crafting, safe gear access, work history and ownership recovery |
| Party bot AI | Capability-based healing/cure/resurrection and party pet support implemented | Verify live behavior, targeting and resource limits; continue improving encounter-specific decisions |
| Masteries | 45-entry compatibility catalog, paired companion songs, native Saving Throw and Resilience defense hooks | In-game effects/buff presentation and remaining spell-specific compatibility differences |
| Doom | Six native controllers installed; artifact reforging implemented | Complete live entry/exit and post-restart checks; artifacts remain earned; never reset an existing partial encounter automatically |
| Pirate island | Owner estate, house plot, harvesting, camp and dock installed | Complete live visual/boarding checks; add bespoke pirate encounters/cargo/rewards and preserve owner's housing and boat clearance |
| Pirate vessels | Compatible anchored boat raids, boarding, combat, cargo, turn-ins and preservation on cleanup deployed | Live play-through; moving naval AI/cannons remain pending |
| Blackthorn | Compatible invasion wing at the original castle/dungeon location, minions/captains/beacons and personal rewards deployed | Live pacing/visual review; complete official encounter/artifact tables remain pending |
| Shadowguard / major dungeons | Six compatible rooms on the original Eodon/Ter Mur map, party ownership, bosses/rewards, exits/recovery and companion puzzle handlers deployed | Live presentation/pathing/balance review; full official encounter/artifact tables and arbitrary puzzle handlers remain pending |
| Presentation and rollout | Deployment authorized and installed from frozen bundle; live save/restart/client checks underway | Finish post-restart persistence and visual acceptance, retain coherent backup and write final release evidence |

This tracker records pending work as pending. Passing existing tests does not certify missing systems or untested client presentation.

## Historical implementation pass — before deployment

- Added global **[market** search, filtering and confirmed delivery from real finite stall stock. Purchases validate current price, wallet funds, pack space and stock ownership.
- Added Fellowship orders, skills, gear, products and work-history pages; explicit gathering/crafting/training jobs; officer orders and separate treasury permissions. Officer dismissal returns added equipment to the leader's bank when the officer lacks withdrawal authority.
- Added party bot spell/bandage selection for party members and owned pets, bounded targeting and cancellation. Native spell costs and range checks remain in force.
- Added owner-only **[home** and the separate Ancient Hunt in the Abyss. Both are included in the current release. See ANCIENT-HUNT-AND-HOME.md; this is not a full Abyss implementation.
- Guild crafting currently consumes supported catalog resources. Recipes needing supplies outside that catalog (for example bottles or blank scrolls) remain unsupported in the guild ledger; broad job provisioning still needs completion. Market producers use their separate production system.
- Full later-era mastery edge cases, pirate vessels, Blackthorn and Shadowguard remain pending. At this historical stage no deployment had been performed; the current release status above supersedes that hold.
- Historical verification: **216 passed, zero failed, zero skipped** in artifacts/ideas-abyss-full01.log. The live server assembly was unchanged at that stage.

## Historical Abyss, snow pets and mission duration pass

- Added a partial Abyss restoration: thirteen crafting mini-champions, typed resources, compatible artifice, route guide and a tested Fire Temple bridge. Full SA bosses, artifacts and native Imbuing remain pending; see ABYSS-RESTORATION.md.
- Added the mountable Frostbound bear in northern Tokuno with innate Colossal Rage, custom rarity/training integration and an owned persistent spawn. Added wallet-purchased pet dyes with original-color restoration; see SNOW-BEAR-AND-PET-DYES.md.
- Added 5/15/30/60-minute missions, completion bonuses, fixed saved deadlines and AFK duration preferences. Longer taming trips choose the best rarity from additional searches while returning one pet; see COMPANION-MISSION-DURATION.md.
- Historical verification: **228 passed, zero failed, zero skipped** in the complete suite at artifacts/abyss-snow-missions-full01.log, plus one additional passing real-death integration test at artifacts/abyss-real-death01.log. The deployment hold was active then and has since been explicitly released.

## Historical custom pet identity and appearance pass

- Replaced shared rarity combat procs with nine species signatures: burning ground, physical prey marks, cold/Dexterity control, healing grove, chain lightning, mana support, defensive bear roar, healing suppression and life-drain rescue. Learned abilities and existing training remain intact.
- Added actual Legendary elemental immunities to Emberwing/Frostmane/Stormscale, lesser rarity resistance floors and a defensive bear hide. Mixed and armor-ignoring damage remain effective. Training displays innate floors and prevents ineffective purchases.
- Stormscale uses custom ranged AI with four-to-six-tile spacing, regular energy bolts, close-range retreat and normal pet training credit. Corrected handling of native Stop orders and ensured custom AI construction does not allocate a discarded duplicate timer.
- Added four species-specific rarity coat shades for all nine custom species and a restrained Legendary idle shimmer. Explicit dyes override coats; restoring original color restores the rarity shade. Existing pets, reserved ticket pets and shrunken pets refresh through normal use. No custom animation/model files were added.
- Details, mechanics, hue IDs, compatibility limitations and migration behavior are in PET-SIGNATURES.md.
- Historical verification: **242 passed, zero failed, zero skipped**, complete Haven suite in artifacts/pet-signatures-full03.log. Native patch 0048 applied cleanly to the independent audit checkout. At that pre-release stage the live UOContent.dll SHA256 was `631B7E4B698F1C97730693C35376219D7C795DE9ABC5A0EC08F149300264C135`; this is a historical baseline, not the deployed release hash.
- No live deployment, restart, spawn setup or client-data change had occurred at that historical stage. The current release is now installed; client visual review remains in progress.
