# Preview restart — 2026-09-12

Deployed queued changes through commit 88bb373. Preview online at 127.0.0.1:2699; authenticated account login/server-list/game-relay probe passed. Original server unchanged.

- Cyclone Scimitar: one-handed Swordsmanship, native Whirlwind/Bladeweave, level20 progression, 150 Marks under Special Rewards > Weapons and existing legendary weapon pool.
- Travel stone opens destinations directly, removes test preparation and unrelated actions, and updates login wording.
- Pet training: native ability categories; hover descriptions where native description IDs exist; finer Mana adjustments (+/-2 and +/-20); Max affordable uses remaining points and respects caps.
- [mystats: personal vitals, regeneration, resistances, equipment bonuses, all skills with values/caps and free-skill markers.
- [? command guide: grouped current commands, parchment layout and flat navigation.
- Role selection: relevant trained/effective/cap skill values for each role.

Clean save/stop succeeded. A stale shell exit-code check interrupted the wrapper after stopping; no files had been copied. Resumed only after confirming the preview was stopped.

Backup: E:/Backups/Haven/Prototypes/servuo-before-stats-training-20260912-000405. All 43 save files matched SHA-256. Ten production files deployed, no native patch changes. Full isolated regression COMPLETE; live Release build zero warnings/errors. Startup and authenticated login passed. Client visual acceptance is not claimed.

Additional Cyclone Mace/Kryss and mini-champion weapon rewards were implemented after this restart and are NOT part of this deployed bundle.
