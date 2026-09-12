# Pet book death fix — September 12, 2026

Following the pet/Codex release, the user reported that death emptied the pet book. Investigation found the native PlayerMobile pre-death pass recursively moves blessed/insured items out of nested containers into the main backpack. Pet tickets are blessed, so this displaced them from the sanctuary. This path does not delete the tickets or their stored pets.

## Fix and recovery

The death pass now leaves contents nested inside the owner's pet book and Champion's Codex. The blessed archive itself is still moved safely to the main backpack if it was inside an ordinary bag. Ordinary blessed-item protection is unchanged.

Opening `[petbook` now gathers valid, owned loose pet tickets from the backpack, including unbonded and Legendary tickets, back into the book. It preserves the exact ticket and pet; it does not generate replacements or reroll skills. Tickets already in another pet book, inaccessible containers, and unavailable/controlled pets are excluded. Transfers remain within the same backpack and respect the book's own capacity, allowing recovery even when death's flattening left the backpack over its normal limits.

The user should reopen `[petbook` after reconnecting. Automatic recovery was verified with a displaced Legendary ticket; the user's final recovered count still needs in-game confirmation. No world rollback or replacement pets were created.

## Restart and verification

The server saved and stopped cleanly around 18:57 EDT. A fresh backup contains Saves, Scripts, Config and root files, with all 43 save files SHA256-verified. Only HavenPetBook.cs, HavenProtectedArchives.cs and the single PlayerMobile archive-preservation hook were deployed. Live compilation passed with zero warnings and zero errors. The preview restarted on port 2699 around 18:58 EDT; authenticated login, server list and game relay passed. Startup error output was empty. The original ModernUO server was not restarted.

An isolated actual PlayerMobile death test verified that the pet stayed in its book, all 150 Codex scrolls stayed archived, and an ordinary blessed item still moved safely to the backpack. The archives began nested in an ordinary bag. After resurrection, opening the book recovered a deliberately displaced Legendary ticket and the same stored pet. A separate process then reloaded the world and verified original book/ticket/pet serials and all 150 scrolls.

Backup: `E:/Backups/Haven/Prototypes/servuo-before-archive-death-fix-20260912-185751`.

The backup includes `SAVE-SHA256.txt`. Deployment metadata is in `verification/archive-death-release.json` in the workspace. This backup contains the current pet/Codex release; the earlier backup predates that release. Prefer a forward fix or targeted recovery over a world rollback, which would discard later play.

The earlier restart and its complete feature report are documented in `PET-CODEX-RESTART-20260912.md`.
