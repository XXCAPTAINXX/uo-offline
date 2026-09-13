# Cove hostility fix — September 13, 2026

Live Haven cleanly saved and restarted on port 2699. Build zero warnings/errors; login/server-list/game-relay probe passed. Original server untouched. Backup: E:/Backups/Haven/Prototypes/servuo-before-cove-hostility-20260913-003418 (Saves, SHA256 manifest, prior Scripts.dll and both replaced source files).

HavenCoveEnemy now explicitly reports AlwaysMurderer, making all spawned and existing cove enemies red regardless of humanoid body. Isolated Notoriety.Compute assertions passed for every enemy in all sixteen theme/stage combinations. Enemies stay within eleven tiles, inside the twelve-tile damage boundary; legacy displaced enemies return to camp. Movement-boundary and recovery assertions passed across all sixteen combinations. Spawns and challenge payouts remain passing.

Updated the cove board description for three crews plus challenge. No world reset, test save import, client art change or island geometry deployment.
