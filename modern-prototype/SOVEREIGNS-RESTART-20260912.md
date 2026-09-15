# Sovereigns restart — September 12, 2026

Haven clean-saved and stopped at approximately 20:14 EDT and reopened at approximately 20:15 EDT on port 2699. Login, populated server list and game relay verification passed. Both isolated and live builds completed with zero errors and warnings.

Added native UO Store Sovereign earnings: welcome (25), town discoveries (25), dungeon discoveries (50), each skill reaching 50 (5) and 100 (25), each base stat reaching 100 (10), first pet (15), first nearby bonded pet (25), monster-count milestones and qualifying bosses (50 or 100). Companion and pet kill ownership is resolved to the player. One-time milestones are recorded on the account to prevent repeated or alternate-character claims. Boss death credit is deduplicated per account and creature.

Existing skill/stat milestones are credited when an alive player is online, within 30 seconds, or immediately through `[sovereigns`. That command also displays the native store balance. Past kills and unrecorded exploration cannot be reconstructed; their tracking starts with this release. No arbitrary currency grant was made.

The isolated regression verified native store funding and deduction, milestone backfill, repeated checks, alternate-character deduplication, boss and first-kill rewards, duplicate death protection and rejection of controlled pets. The player's actual store purchase was not performed.

Stopped-world backup: `E:/Backups/Haven/Prototypes/servuo-before-sovereigns-20260912-201423`. All backed-up save files were SHA-256 compared. Deployment added only `Scripts/HavenSovereigns.cs` and rebuilt the live `Scripts.dll`. No save import or island changes occurred. The universal dye palette expansion remains pending.
