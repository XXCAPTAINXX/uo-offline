# Haven recovery and pet training restart — September 12, 2026

Haven is online on port 2699. The server was clean-saved and stopped at approximately 19:07 EDT to preserve the missing-item evidence, and reopened at approximately 19:25 EDT. Account login, populated server list, and game relay checks passed.

## Recovered belongings

Rictor Quake's bank now contains **Recovered belongings - September 12**, holding 71 recovered items, including the two bags and their contents. Four associated equipment-progression records were recovered as well:

- Bracelet of Fortune: level 17, 1,691 experience.
- Boarding Shield: level 19, 1,823 experience.
- Cyclone Scimitar: level 5, 457 experience.
- Legendary CrescentBlade: level 1, 0 experience.

The recovery also includes the armor, clothing, treasure maps, and scrolls on the original corpse. Their original attributes, loot types, and item data were restored. The separate level-1 bracelet already in the bank was preserved.

The missing corpse had decayed from the current world. Its contents came from the full 18:57 backup, and were imported into the newest 19:07 clean save. This was a targeted item recovery, not a world rollback. All original serials were checked against the current world first. Two bag serials and one progression-record serial had been reused by unrelated objects; those three recovered objects received new serials with their references remapped. The unrelated objects were preserved. The recovered weapons, armor, and jewelry retain their original serials.

An initial recovery rehearsal used a test build with newer island serialization. Live startup rejected that record before accepting players. That candidate save was discarded, and recovery was repeated from the untouched 19:07 backup using a dedicated copy of the live source. The corrected world passed reload and live startup. No newer island build was released.

## Gameplay changes

- Bonded pets always report full loyalty and no longer lose it to hourly decay or command failures. Existing low loyalty no longer penalizes their control chance. Unbonded loyalty behavior is unchanged.
- Training buttons report why an interaction is rejected instead of silently returning. Messages cover death, ownership, storage, mounting, distance, line of sight, and unavailable training.
- Training purchases explain combat cooldown, incomplete progress, follower capacity, taming requirements, stat budgets, insufficient points, and missing power scrolls.
- Corpse summoning finds all the owner's surviving accessible corpses, including older ones when the latest corpse is empty. Corpses remain available for ten minutes after summoning.
- Self-looting continues after an item fails the backpack capacity check, so a heavy bag no longer prevents later equipment from being recovered. Items that do not fit remain on the corpse.

The hellhound's saved training state was complete, with 1,501 points and sufficient follower capacity and taming skill. An isolated copy successfully purchased +1 Strength for three points and gained exactly one follower slot. That test purchase was not saved to the live character. Its Dexterity and Stamina are already above their native training limit; their existing values are preserved. The exact cause of the reported unresponsive buttons has not yet been reproduced in the game client; the user has been asked to reopen the menu and report its response.

## Validation and backups

- Live build: zero errors, zero warnings.
- Dedicated live-code recovery: all 71 belongings present inside the recovery bag after a fresh process reload; all four progression records retain their levels and experience.
- Regression tests: actual hellhound Strength purchase, bonded versus unbonded loyalty, mounted and remote training explanations, older-corpse recovery, and equipment recovery after an overweight bag all passed.
- No test fixtures or practice stat purchases were saved into the released world.
- Original stopped save: `E:/Backups/Haven/Prototypes/missing-items-current-20260912-190715/Saves`; all 43 files matched the stopped live save by SHA-256 before replacement. Replaced source files and the old Scripts.dll are under the same backup's `code-before` directory.
- Original item source: `E:/Backups/Haven/Prototypes/servuo-before-archive-death-fix-20260912-185751/Saves`.
- Recovery bundle: `E:/Backups/Haven/Prototypes/recovered-corpse-20260912`, including serialized items, manifest, and serial mapping.
- Corrected live save: all 43 files matched the verified recovery output by SHA-256 after deployment.
- Local evidence: `verification/missing-items-before-SHA256.json`, `verification/missing-items-restored-SHA256.json`, `verification/recovery-live-code-SHA256.json`, and `verification/missing-items-live-regression-result.txt`.

Released code consists of `HavenPetTrainingGump.cs`, `HavenRecovery.cs`, the bonded-loyalty change in `BaseCreature.cs` (patch 0029), and the corpse self-looting change in `Corpse.cs` (patch 0030). The original ModernUO server on port 2593 was not changed.
