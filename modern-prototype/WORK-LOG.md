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

