# Doom status restart — September 12, 2026

Haven clean-saved and stopped at approximately 20:32:18 EDT and reopened by 20:32:43 EDT on port 2699. Account login, populated server list and game relay checks passed. Isolated and live builds passed with zero warnings and errors.

Added the player command `[doom`. It reads the character's existing native DoomGauntlet points and displays the artifact chance for that point total using the same exponential formula as the native kill handler, capped at 100%. The message explains that the next eligible boss adds points before rolling, Luck affects point gains, and receiving an artifact resets points. No reward chances, stored points or loot tables were changed.

Reviewed the calculation against `Services/PointsSystems/GauntletPoints.cs` and checked that no existing command registration conflicts. Live in-client command rendering has not been verified.

Backup: `E:/Backups/Haven/Prototypes/servuo-before-doom-status-20260912-203218`. All stopped-world save copies were SHA-256 verified. Deployment added only `Scripts/HavenDoomStatus.cs` and rebuilt `Scripts.dll`. No save import or island changes occurred.
