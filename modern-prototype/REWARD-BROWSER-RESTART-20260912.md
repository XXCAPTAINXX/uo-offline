# Reward browser restart - September 12, 2026

Haven clean-saved at approximately 20:07:48 EDT and reopened at approximately 20:08:15 EDT on port 2699. The login, server-list, and game-relay probe passed.

The supply/rewards shop now uses the client's native light stone-and-gold buttons instead of black bars. Button names are plain text, so apostrophes and ampersands cannot appear as HTML escape codes. Category labels read “Armor and clothing” and “Travel and storage.” Several missing spaces in level descriptions were corrected.

Rewards are sorted by category and name. Weapons are grouped by family, with filters for Blades, Axes, Maces and staves, and Bows. Catalog identities are retained through filtering and pagination, so selecting a sorted row still references the original item and price.

Validation confirmed every reward remains present exactly once, every weapon-family filter returns the correct entries, selection buttons retain their catalog IDs, and shop buttons use stone artwork and plain labels. Both test and live builds completed with zero errors and warnings. Evidence: `verification/reward-browse-result.txt`.

Only `Scripts/HavenSupplyShops.cs` and the rebuilt `Scripts.dll` were deployed. The current save was backed up and SHA-256 verified before deployment at `E:/Backups/Haven/Prototypes/servuo-before-reward-browser-20260912-200748`. No save import, reward purchase, currency adjustment, or island change was performed in this restart.
