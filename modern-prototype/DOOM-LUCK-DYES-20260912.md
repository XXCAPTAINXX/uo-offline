# Doom Luck, dye palette and Sovereign lantern — September 12, 2026

Published to the ServUO preview on port 2699. The separate ModernUO server was unchanged.

- Doom points: 25% higher base gain, and Luck contributes 50% more. Formula: fame / 2 × (1 + 1.5 × sqrt(Luck) / 100) × 1.25, rounded down. At 2,500 Luck this yields 45.8% more points than the previous formula. Existing artifact-chance multiplier is unchanged. Non-Haven behavior is unchanged.
- Universal dye tub: 50 pages covering hues 1–3000, direct hue entry, preview, and remove-dye option. Dyes also open this palette for the universal tub. Ownership is rechecked when applying a color.
- Prospector's Lantern: 100 Sovereigns in UO Store / Misc. Consuming it grants an account-wide 25% relative increase to custom legendary world-drop chance for one real-time hour. It cannot stack; [fortune reports remaining time. It does not change dungeon artifacts, pet rarity or item strength. No buff-bar icon is included.

## Restart and backup

Clean save and shutdown completed before backup at approximately 21:49 EDT. Backup: E:/Backups/Haven/Prototypes/servuo-before-doom-luck-dyes-20260912-214948. Includes Saves, SHA-256 save manifest, previous Scripts.dll and replaced source files. No historical or test Saves were imported.

Updated four custom sources (HavenUniversalDyeTub, HavenProspectorsLantern, HavenAdvancedGear, HavenDoomStatus), native Dyes and GauntletPoints, and rebuilt Scripts.dll.

## Validation

Isolated and live Release builds: zero warnings and errors. Isolated runtime checks passed all 3,000 hue selections, invalid hue rejection, item dyeing, stale ownership protection, lantern activation, duplicate-consumption prevention and expiration. The tests do not purchase an item from the player's account. In-game visual review has not been performed for this batch.

Restart completed around 21:51 EDT; port 2699 listener and account login/server list/game relay probe passed.
