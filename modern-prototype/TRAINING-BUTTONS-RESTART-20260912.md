# Training button restart — September 12, 2026

Haven was clean-saved at approximately 19:44:42 EDT and returned online at approximately 19:45:10 EDT on port 2699. The account login, server-list, and game-relay probe passed.

The reported failure was reproduced in the live TazUO client: clicking the visible Strength “Choose upgrade” label left the training screen unchanged. The previous server-side validation change had not fixed that interaction.

Training and upgrade-confirmation screens now use one visible native arrow per action. The arrow occupies its own space beside the label, with no decorative HTML or tiled image covering its click target. Upgrade rows are labeled “Upgrade.” The pet-screen background and training rules remain unchanged.

Validation:

- All ten stat-row buttons were sent through the real `0xB1` client-response packet handler in an isolated copy of the live server. Each opened its upgrade-confirmation gump, and no training points were spent.
- Each upgrade row has exactly one native arrow reply button with the expected ID.
- The existing Strength-purchase, loyalty, and recovery checks continued to pass.
- Test and live builds completed with zero errors and zero warnings.
- The live server successfully loaded its existing save and passed the login probe.

Only `Scripts/HavenPetTrainingGump.cs` and the rebuilt `Scripts.dll` were deployed. No save was imported, no pet statistics were changed, and no training purchase was made on the player's behalf.

The clean save and replaced code were backed up at `E:/Backups/Haven/Prototypes/servuo-before-training-buttons-20260912-194442`. All save files were compared by SHA-256 before deployment. The original ModernUO server and island build were not changed.

Local evidence: `verification/training-buttons-release.json` and `verification/pet-training-buttons-result.txt`. The player was asked to reconnect and reopen training for a post-deployment client click check.
