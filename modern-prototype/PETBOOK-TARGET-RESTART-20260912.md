# Pet sanctuary book update — September 12, 2026

## Changes
Use [petbook, select Add pet ticket..., then target an owned ticket in your backpack. Both bonded and unbonded tickets may be added. Stored tickets are excluded from exchange. Bonded pets still automatically enter the book when shrunk, and release returns the exact pet.

The book now uses book art 0xFF4 with hue 0x59D and the name Pet sanctuary book. Existing books using the old shared 0x2259 icon migrate on loading. Ownership, backpack access, valid stored-pet state and book capacity are checked when targeting.

## Restart and verification
Only HavenPetBook.cs deployed. Clean save and restart around 03:08 EDT; Release builds passed with zero warnings/errors. Isolated inventory tests reached COMPLETE, including targeted unbonded ticket protection and exact bonded pet release. Authenticated login probe passed after restart. Client appearance still needs in-game visual confirmation.

Backup: E:/Backups/Haven/Prototypes/servuo-before-book-target-20260912-030716
All 43 save files SHA256-matched their backup. Config, scripts, engine sources and root files also copied. Original server untouched; island prototypes not deployed.
