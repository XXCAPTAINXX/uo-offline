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

Local evidence: `verification/training-buttons-release.json` and `verification/pet-training-buttons-result.txt`. The player reopened training and confirmed the arrow buttons worked; the live Strength confirmation screen was also observed. The player then requested better-looking buttons, leading to the second restart below.


## Framed-button visual update - approximately 19:49 EDT

After the player confirmed the click fix, the arrow-and-strip presentation was replaced with complete native framed buttons. Adjustment and upgrade controls use blue buttons; the purchase action uses a green **Train pet** button; finishing a stage uses a red button. Wider navigation controls use a framed brown surface. Plain centered labels replace the HTML overlays, preserving the button surface as the click target.

The main training window is taller and uses wider row spacing so the larger buttons do not overlap. Training costs, caps, and purchase validation are unchanged.

The server clean-saved at approximately 19:49:12 EDT and returned online at approximately 19:49:40 EDT. Save files and the prior source/DLL were backed up at `E:/Backups/Haven/Prototypes/servuo-before-framed-training-buttons-20260912-194912`; save copies were SHA-256 verified. Only the training gump source and rebuilt Scripts.dll were deployed, with no imported save or practice purchase.

All ten framed upgrade controls passed the real client-response packet-handler test without spending points. The live build again completed with zero errors and warnings, and login/server-list/game-relay checks passed. Evidence is in `verification/pet-training-framed-buttons-result.txt` and `verification/framed-training-buttons-release.json`. Final client appearance remains available for the player's review after reconnecting.
