# Mission reports and regional resource routes — deployed

Installed September 9, 2026, with 999 content tests passing and no failures or skips. All 319 installed payload files match the frozen release manifest. Canonical and verification custom source were checked for equality before packaging. Existing uncommitted work was retained; no commit or push was made.

Companions now retain ten field reports with actual loot receipts, mission counts, skill/stat changes and equipped gear progression. Normal, AFK and offline missions share reporting. Four resource routes cover Malas necromantic reagents, Doom bones, Abyss essences and supported Abyss ingredients. Regional rewards use commodity deeds accepted by the existing Resource Ledger. Daemon bones were appended to its catalog without changing existing balance indices. See [controls and behavior](COMPANION-MISSION-REPORTS.md).

Validation includes actual route rewards absorbed, combined and withdrawn through the ledger, stable mission identifiers, eligibility checks, report serialization and duplicate-return protection. The full content suite also exercises catalog withdrawal and saved balances. A fresh live-save rehearsal completed a real scheduled mission, recording 1,130 gold, five Haven marks and leather gloves, with stat changes. That exact report survived another save/restart. Those were rehearsal rewards, not a claim about the live player's loot.

Live startup, save, restart and a final completed save at 21:48:39 EDT succeeded. Alden Ashford (52621), owned by player Rictor Quake (5176), retained his active five-minute gear assignment, four completed runs and the 21:48:59 EDT next deadline. His new report correctly identifies four earlier untracked runs. The journal and assignment matched exactly across the controlled restart. Existing world controller and estate counts remained intact. Final server PID: 27060, listening on port 2593; stderr was empty. Client files and maps were unchanged. In-game visual acceptance remains unverified.

Release bundle: `artifacts/HavenMissionReportRelease20260909`. Evidence includes `live-before-update.json`, `live-save.json`, `live-reload.json`, `live-final.json`, rehearsal receipts and the final test log.

Installed UOContent.dll SHA-256: `1BC2F711831FAAB10502345B3A4EC99DB26AAB57DF650C78C819D8D003725851`.

Pre-update backup: `D:\Uo Offline\uo-modernuo\haven-world-backups\mission-reports-20260909-214541`. Deployment metadata matches the installed assembly. Rollback must stop the server and restore the coherent backed-up saves, source and assemblies together because the update introduces saved journal/route types; `artifacts/mission-report-release.py rollback` performs those checks and preserves failed state.
