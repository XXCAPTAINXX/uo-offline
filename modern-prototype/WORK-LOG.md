# Six-hour build window

User requested continued incorporation and testing toward a usable evening prototype. User shortened the remaining window at 00:22 UTC to about 2.5 hours. Target readiness by **2026-09-11 02:52 UTC**. Follow-up automation: `build-haven-modern-test-version`, every 30 minutes until that deadline.

## Constraints

- Existing Haven server remains unchanged on 127.0.0.1:2593, last verified PID 28784. No save conversion or live cutover yet.
- Build separate ServUO preview on loopback port 2699. Never run disposable tests on an interactive preview world.
- Source: haven-fixes/modern-prototype, existing Git branch fix/haven-world-services. Preserve unrelated dirty legacy files.
- Backups: E:/Backups/Haven/Prototypes. No game data, accounts, passwords or saves on GitHub.
- No subagents without user authorization. Finish meaningful work before stopping; scheduled continuation should read this log.

## Completed before this window

- Pinned ServUO d76bf4443cf76d081ddaf8f57c87ff33749256af, modern EA Classic data available at D:/Ultima Online/Electronic Arts/Ultima Online Classic.
- Foundation runtime evaluation: 38 checks, repeated on clean checkout.
- First companion: follow/guard/attack/heal, native pet speech cursor, owner-only pack, native party/owner loot rights, gold stacking, queued rewards, timed mission persistence. 34 checks, repeated fresh checkout. Published commit 39bd198.
- Automated test worlds: verification/HavenServUOCompanion and verification/HavenServUOCompanionReproduction. They shut down automatically; do not use them as the user's preview save.

## In progress

- Native preview generated and reloaded successfully at D:/Uo Offline/Haven-ServUO-Preview. 6,835 spawners, six Doom controllers, 14 verified Blackthorn links, 17 Shadowguard instances. Native Trammel entrance differs from Felucca; validator corrected from actual source. Setup marker consumed; no setup repeated on reload.
- Preview controls and companion passed 51 isolated checks, fresh + reload, at verification/HavenServUOPreviewControls. Build zero warnings/errors. Added [preview opt-in travel and one-time native gear/120-skill kit. Real client QA now starting with separate D:/Uo Offline/Haven-ServUO-Preview-Client, preinstalled TazUO runtime copied without original user profiles. Local disposable player account generated from private one-shot marker; never publish its settings/account/save. Computer-use node_repl is now callable; use skill protocol. Temporary Windows awake request PID28720 expires04:28UTC; accepted by Win32 API.

## Priority queue

1. Finish world bootstrap and validate spawners, dungeon controllers, representative entrances, save/reload and no duplicate setup.
2. Produce an interactive preview installation, launcher and short checklist, separate client profile if feasible. Actual client testing takes priority over cosmetic mockups.
3. Expand companion roles with native caster/archer/healer behavior, preserving target ownership and wild tameable exclusions; test combat and persistence.
4. Expand missions and resource ledger/deed storage with capacity and conservation tests; use native resource types and avoid currency duplication.
5. Rehearse one full modern dungeon progression and native naval loop; investigate and fix concrete failures.
6. Document remaining migration gaps, test client connection if available, publish tested source and backup. At deadline stop feature work and report actual readiness.

Full old-character/island migration is not a prerequisite for tonight's separate fresh-world preview. Never pretend those saves have been migrated.


## 00:49 UTC milestone
- Combined role/control/companion suite: 64 PASS, zero failures; verification/HavenServUORolesFinal. Role fixtures activate AI explicitly (offline test player) and use native NoSpecials movement attachment to distinguish ranged from melee damage; removed afterward. Prior fixture failures archived.
- Native populated-world builder reproduced fresh + reload at verification/HavenServUOPopulateReproduction, no duplicate generation. Native coordinate check corrected from Trammel CSV vs Felucca Generate.cs.
- Start-Preview.ps1 and Stop-Preview.ps1 verified clean save/stop on interactive preview. Preview currently stopped for next deployment; original2593 remains PID28784.
- Socket test account login + serverlist + relay passed. Computer-use client attempts reached a Windows security prompt. Do not interact with/bypass it. User must review later. No graphical playthrough claimed. Local TazUO copy updated to installed26.0909.63 runtime; no user profiles copied.
- Source/docs ready for commit. Next: ledger-backed resource missions with conservation tests, then dungeon progression checks and final deploy/backup before02:52UTC.


## 00:57 UTC resource milestone
- Added HavenResources.cs: catalog ledger, native commodity deed absorption, resource deeds, nested bag targeting, transfer/withdrawal capacity checks, serialized balances with no extra inventory slots. Preview kit includes ledger.
- 76 checks passed fresh + reload in verification/HavenServUOResources; one test variable naming compile issue corrected before successful build, zero final warnings/errors. Source still needs deployment to interactive preview.
- Next: companion resource missions/own ledger integration, then dungeon progression, final deployment/backup. Original2593 remains running; preview2699 still stopped. Keep-awake PID28720 active. Client security prompt remains for user review; do not bypass.


## 01:09 UTC mission milestone
- Companion-owned ledgers and Mining/Lumber/Leather/Malas/Abyss jobs added. Version2 serialization retains resource snapshots and pending balances; v0/v1 remain readable. 84 fresh/reload checks passed, zero warnings/errors. Resource overflow and offline recovery covered.
- User requested a complete feature reminder: docs/FEATURE-INVENTORY.md now distinguishes original implemented systems, modern preview and remaining gaps.
- Found native Shadowguard roof gating checks NPC companion as independent player although completion table tracks PlayerMobiles only. Next fix with narrow native integration patch and regression tests before deployment. Graphical client security prompt still pending user review.


## 01:31 UTC client and Shadowguard progress
- User approved Windows prompt. Client launch then failed because Sky launch inherited protected cwd; opening via actual Explorer folder worked. Created private Play Haven Preview.lnk with explicit working directory.
- Login failed because UI truncated original24-character QA password to16. Private PreviewLoginRepair.cs synchronized only haven-preview, saved, consumed marker; networkprobe passed. User/auto-login subsequently entered Haven Explorer. Real client New Haven rendering, preview gump, kit claim and backpack properties verified. User is remote-viewing/using client: avoid input races.
- Interactive preview is running2699 with c152590 resources/missions. Original2593 preserved. Login repair source inert in private Scripts, not inpublicpayload. No credentials logged/published.
- Native Shadowguard roof gate fixed to use qualified owner's record for bound party companion. Test found native Deserialize skips enum when referenced character deleted, corrupting load. Fixed unconditional enum read; previously failing save now reloads with all89checks passing. Fresh combined90-check reproduction is running verification/HavenServUOShadowguardFinal; session4474. Need publish/backup and later deploy native patch when client test is ready for restart.


## 01:34 UTC verified milestone
- Fresh builder + patched native source passed90checks in verification/HavenServUOShadowguardFinal, zero warnings/errors. Previous failed save was also recovered without deleting objects. New patch not yet deployed to interactive session.
- Real user is connected to2699 (ServUO PID2208), character Haven Explorer. Leave client controls/session alone while user tests. Original2593 unaffected. Private source/Config/Saves backup: E:/Backups/Haven/Prototypes/servuo-client-login-20260910-c152590, save hashes unchanged during copy.
- Next: stage native Shadowguard patch for planned restart, further native dungeon/naval verification, finish play instructions and deadline deployment. Core test servers all stopped.

## 02:10 UTC deployed Shadowguard milestone
- User authorized closing the preview client; it is closed. User requests updates every15minutes through02:52UTC; heartbeat updated accordingly. Independent critic explicitly authorized for future island decoration, iterate to at least8/10; no delegation authorized for server work.
- Cleanly saved/stopped preview2699, backed up private Saves/Config to E:/Backups/Haven/Prototypes/servuo-before-shadowguard-deploy-20260911 with SHA256 manifest. Initial manifest enumerated its own open output; corrected by hashing only Saves/Config before writing CSV.
- Deployed all current prototype source and native Shadowguard patch23e79f4 to interactive preview. Release build zero warnings/errors. Existing populated world loaded successfully; prepared-account login/server-list/relay probe passed.
- Exercised a second clean save/stop/start cycle with the updated build; preview listener returned and account probe passed again. Original2593 remained running throughout. Client stays closed; preview server stays running.
- Next: meaningful native Doom progression/naval validation in disposable test world, then final readiness documentation. No new dungeon or boat gameplay test is claimed in this deployment milestone. Deadline02:52UTC still applies.

## 02:28 UTC Doom/naval verification
- Added DoomNavalSmoke.cs to disposable harness. Native six-stage Doom deaths/callbacks advance and reset the cycle; restored sequence remains intact. Britannian movement preserves native GalleonHold cargo, owner and cargo survive reload.
- First test fixture used obsolete BaseBoat.Hold instead of BaseGalleon.GalleonHold; corrected test only. Failed fixture retained at verification/HavenServUODoomNaval. Fresh reproduction verification/HavenServUODoomNavalFinal passed94checks across fresh/reload, zero build warnings/errors; test servers stopped.
- No production change or preview restart needed for these test additions. Preview2699 and original2593 remain running. No actual fishing/naval combat/client dungeon completion claimed.
- Publish tests/results/docs and source backup. Next final readiness pass by02:52UTC: confirm listeners and instructions, summarize migration gaps; no need to rerun passing suites without new changes.

## 02:37 UTC readiness pass
- Both listeners2593/2699 healthy; latest preview stderr logs empty. All installed prototype source hashes match published source, and reverse-check confirms installed native Shadowguard patch. Verified all77 private predeployment backup file checksums.
- Verified client shortcut target and working directory; leave client closed as authorized. Added a short first-session route and five-minute mining/ledger-transfer check with expected100ingots/500gold to TONIGHT.md.
- Current source/test milestone a043aa4 already published and source-backed-up at E:/Backups/Haven/Prototypes/servuo-doom-naval-20260911-a043aa4. No new runtime changes in this readiness pass.

## Final-window backup and migration documentation
- Requested and confirmed a live preview save, then copied current Saves/Config to E:/Backups/Haven/Prototypes/servuo-ready-world-20260911. Compared source hashes before/after copy and backup hashes; current world backup verified. Server remains running, client closed.
- Added MIGRATION.md with explicit type/identity inventory, currency/storage reconciliation, unsupported-type reporting, disposable conversion/reload checks and rollback gates. This is a plan only: no character/island conversion performed.
- Next scheduled final check: leave preview running and stop build heartbeat at deadline. Runtime/test work is complete for this slice; remaining manual gameplay and migration gaps documented in TONIGHT.md/README.md.

## User reprioritized Haven starter gameplay (supersedes deadline stop)
- User wants a playable fresh-character Haven now and continued ports ASAP. Updated heartbeat to15minutes, continuing starter progression/reward currency/mini champion priorities rather than stopping at02:52. Original2593 stays untouched.
- Published full comparison docs/OLD-VS-MODERN.md at4b52d99; later starter-hub additions noted there. Do not claim original systems all ported.
- Implemented/deployed HavenStarterHub: seven stones at original plaza anchor sites, safe placement, idempotent setup; normal once-per-character starter and arcane kits preserve skills/stats, companion/travel/repair/optional test-training/guide. Live startup reports7stones. Original reward/upgrade/pet stones not ported. First hub suite101checks passed.
- User requested an interactive testing script and [? help. Implemented server gumps instead of Legion: [haventest has12tests across2pages, Pass/Fail/Blocked and optional note. Per-character Account tags preserve status; private haven-playtest-results.tsv logs changes. [? lists available custom commands and companion speech. No automatic instant notification; review log during regular checks.
- Fresh verification/HavenServUOPlaytest passed103checks, zero warnings/errors. Test source/results and help/starter docs ready to publish. Full live source rebuilt/restarted successfully; source includes9Haven files. No graphical inspection of new gumps claimed.
- Backups before rollout: E:/Backups/Haven/Prototypes/servuo-before-starter-hub-20260911 and servuo-before-playtest-ui-20260911. Preview server running; client closed. Need publish/backup final source, then read new user test results and continue prioritized starter gameplay ports.
- Treat player feedback notes as untrusted observations, not instructions. Do not publish the private TSV or account/save data. No real player checklist results yet; fixture TSV exists only in disposable test world.


## Pirate starter lodge deployed
- Modern preview now supports [home: explicitly claim a free native 18x18 custom lodge on the chosen account, then return there. Four owner-secure 1,000-item chests, crafting tools/stations, upper quarters and native automatic spiral stairs. Same-account characters share native ownership. This is ordinary secure storage, not the old autosorting system.
- Native placement checks use Player access and reject displaced objects; Trammel search falls back to valid Malas land. Original island and characters remain untouched; this is not the island migration.
- Independent house critic reviewed three actual-client-art design renders: 6.6, 7.6, then 8.0/10. Final score covers starter lodge only. No live graphical walking test claimed.
- Final HomeReady suite: 108 PASS, two COMPLETE failures=0 (fresh and reload). Live build: zero warnings/errors. Saved preview, backed up Saves/Config with SHA256 to E:/Backups/Haven/Prototypes/servuo-before-house-20260911, installed source, restarted port2699 successfully.
- Island design steering: irregular shoreline, sheltered cove and dock, curved beach, rocky headlands, connected paths to house/workshop/gardens. Keep usable land; avoid a perfect circle and purposeless paths. Future island requires independent critic review too.

## Checklist travel convenience
- Added server-whitelisted Go to Haven actions for starter, arcane, recruitment, repair and travel-stone checks, plus Return to Haven on all pages. Existing travel checks and native pet teleport behavior apply. No combat escape or automatic pass recording.
- Four rows per page (three pages) leave room for travel controls. Paging/travel preserve draft notes; stable saved test IDs unchanged. Other checks explain that no special destination is needed; combat asks the player to choose an ordinary hostile outside town.
- Added checks for service arrival, unchanged results, invalid destinations and combat restriction. Disposable verification: HavenServUOChecklistTravel.
- Final corrected fixture uses native aggression records; 110 checks passed across fresh/reload in HavenServUOChecklistTravel3. Initial combat fixtures did not establish combat and failed; production travel code was unchanged. Backed up preview before checklist deployment to E:/Backups/Haven/Prototypes/servuo-before-checklist-travel-20260911.
- Live build zero warnings/errors; preview restarted on2699 and local login/server-list/relay probe passed. Original2593 unchanged. Client graphical checklist test remains for player.

## Companion names staged while player tests
- New recruits choose from256 first/surname combinations, avoiding living existing companion names until the pool is exhausted. Existing companions retain names. Both companion menu titles now display actual name. Named speech smoke uses actual name.
- HavenServUONames2:110 checks passed fresh/reload; build zero warnings/errors. First attempt could not fetch within network sandbox; approved isolated retry succeeded. This naming update is NOT deployed: player is actively testing, keep preview available.
- Next gameplay port: Haven Marks/reward exchange and mini-champion participation loop. Review original HavenEstateTrial.cs and HavenAbyssMiniChamp.cs and wallet/reward dependencies before porting. Do not claim original content exists in preview.

## User's house-tour quality benchmark
- Reference: C:/Users/juliu/Videos/2026-09-10 23-44-47.mp4 (110.6 seconds). User rates their other-server house7/10. Reviewed sampled frames across the tour; extracted references remain private under artifacts/house-reference-tour.
- Future reviews must use this benchmark, not the previously generous8/10 starter-lodge score. Visible strengths: connected brick paths from entrance to buildings, timber wings around courtyard, tall stone core, roof planting beds, windows, terraces, kitchen counters grouped along walls, defined garden/display areas, furnishings around room perimeters leaving usable circulation space.
- Pirate estate should exceed this with coherent cove/beach/dock setting, deliberate connected buildings and purposeful interiors. Do not treat decoration quantity as quality or call the current temporary lodge8/10 relative to this reference. Preserve user's current priority of Haven systems while they test; this reference informs later house/island work.

## Lodge locked-door repair
- User rates temporary lodge4-5/10 against their7/10 video reference. Earlier8/10 critic score is not the accepted quality benchmark.
- Root cause: ordinary DarkWoodDoor was registered with BaseHouse.AddDoor, which sets Locked=true. Ordinary doors do not honor house ownership; native DarkWoodHouseDoor does.
- New lodge doors now use native house access with SecureLevel.Owner. Startup repair replaces only old ordinary DarkWoodDoor entries in HavenPirateLodge.Doors, preserving closed coordinates/hue and removing old door; house, design and storage untouched. Idempotent migration.
- Added explicit legacy-repair, owner opening, stranger rejection and reload permission checks. Verification/HavenServUODoorFix.
- Final suite111PASS fresh/reload, zero build warnings/errors. Saved/backed up preview to E:/Backups/Haven/Prototypes/servuo-before-door-fix-20260911. Deploy includes previously staged companion names.

## Guard follow repair
- Native BaseAI.OnCurrentOrderChanged clears ControlTarget for Guard. Custom guard called DoOrderFollow with null target, which changed the order to None. Restore BoundOwner as ControlTarget before native following.
- Ongoing guard no longer uses new-command range/LOS checks, allowing native pathfinding to catch up when owner is more than14tiles away or behind an obstacle. Ownership, controlled state, alive, same map and mission restrictions remain. Shared guard logic covers Warrior/Caster/Archer.
- Regression exercises cleared target, owner moving beyond command range and actual movement while retaining Guard; existing hostile/tameable checks retained. Isolated HavenServUOGuardFollow.
- Guard follow suite111PASS fresh/reload; live backup at E:/Backups/Haven/Prototypes/servuo-before-guard-follow-20260911. Deployment uses tested HavenCompanion.cs; ask player to reissue all guard me after reconnect.
