# CUB, travel, challenge and skills release — 2026-09-11

HavenTrashBag and public chest continue to use native CUB valuation and three-minute disposal. Pending credit now persists across saves/restarts; removing items (including nested contents) clears their pending attribution. Deposit messages explain pending points or zero-value trash, and the wallet displays the native CUB balance. No retrospective points inferred from already deleted items. Legacy version-zero trash contents have no saved depositor to recover.

Patch 0017 bypasses criminal-only travel blocks when HavenPreview is enabled. Covers Recall, Gate Travel, Sacred Journey, SpellHelper, public moongates, ordinary teleporters, house teleporter tiles, Serpent's Jawbone, Bracelet of Binding, Eodon reward travel, whirlpools and both crystal portals. Runebooks and atlases invoke the patched spells. Existing combat, region, weight, sigil and destination rules stay in place. Home retains the separately authorized unrestricted combat/criminal exception. Haven Abyss travel removes its duplicate criminal-only check.

Challenge mode: three waves of 15 mixed enemies and all three bosses; all three themed reward bundles plus one random bonus bundle, 80 Marks per participant. Includes 40,000 gold, 1,000 resources in deeds, 20 native 105/110 Power Scrolls, four Alacrity and four Transcendence scrolls, and guaranteed Corsair ship ammunition. Normal variants unchanged.

Companion stats default to All skills, alphabetical, with 12 rows per page and Previous/Next navigation. Optional Used/Trainable filters and sort retained. Earlier staged weapon-skill/role-filter correction is included in this file.

Validation: isolated build and runtime suite COMPLETE. Verified delayed CUB credit, retrieval cancellation, nested retrieval, public chest deposit attribution, serialization of pending credit, no duplicate credit; criminal-only native helper/moongate travel; home exception; all skills rows/default; challenge populations and 80 Marks/four gold bundles. Native criminal guards statically audited, including runebook/atlas call paths. Not every travel destination or client UI was exercised.

Deployment backup: E:/Backups/Haven/Prototypes/servuo-before-cub-travel-challenge-20260911-195908. Save hashes matched. Only five custom source files and native patch 0017 deployed; other staged roles/timer/repair work remains separate.
Live rebuild: zero warnings/errors. Server restarted on 2699; account login, populated server list and game relay passed.
Follow-up live: Challenge adds one guaranteed Astral Shard in the fourth parcel. Runtime award test passed. Backup E:/Backups/Haven/Prototypes/servuo-before-challenge-shard-20260911-200326; build and login passed.
