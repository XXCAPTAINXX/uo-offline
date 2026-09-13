# Stored steed respawn correction — September 12, 2026

## Evidence and fix
A copy of the actual saved world showed Haven spawner 0x400302D4 tracking steed 0x00000664 on Map.Internal, with one previous owner and Controlled=false. Ticket storage clears control, so native Defrag did not release its slot. Earlier tests covered controlled pets but missed stored pets.

The custom spawner now detaches tracking for previously owned or internalized pets before native cleanup and availability checks. It does not delete or alter those pets. The 10–15-second timer remains.

Re-running that same world save produced wild steed 0x000085A1 at Trammel (3675,2410,13). The original stored 0x00000664 remained intact. This verifies the reported saved-world failure rather than only a newly constructed spawner.

## Restart
Only HavenPetHabitats.cs deployed. Clean save, backup and restart around 03:02 EDT. Verification and live builds passed with zero warnings/errors; authenticated login checked after startup. Original server untouched. Island and pet-book UI changes excluded.

Backup: E:/Backups/Haven/Prototypes/servuo-before-stored-steed-20260912-030127
All 43 save files verified against backup SHA256 hashes; config, scripts, engine sources and root files also backed up.

The in-game view still needs user confirmation; successful replacement was observed in the isolated copy of the saved world.
