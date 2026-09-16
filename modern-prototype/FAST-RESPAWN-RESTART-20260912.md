# Faster custom pet respawns — September 12, 2026

The preview server was cleanly saved and restarted at approximately 02:53 EDT. It is online on port 2699; authenticated login, server list and game relay passed.

## Changes deployed
- Frostbound bear: five-minute replacement delay reduced to 15 seconds, checked every two seconds (roughly 15–19 seconds after taming or death).
- Chelonia tortoises: same 15-second replacement delay and two-second check. Existing population limits and rarity rolls remain intact.
- Vampiric steeds: native spawner interval reduced from 30–60 to 10–15 seconds. Native spawner cleanup excludes controlled pets from its population.
- Ancient Hunt: repeat cooldown reduced from ten minutes to 15 seconds. Its guardian waves still precede the hellhound encounter; readiness now displays seconds.
- Existing saved long cooldowns are capped at 15 seconds during loading. Tamed pets remain preserved.

Only HavenSnowBearDen.cs, HavenChelonia.cs, HavenPetHabitats.cs and HavenAbyssTrial.cs were deployed. Pet book follow-up and island work were not included in this restart.

## Validation and recovery
Verification and live Release builds passed with zero warnings and errors. Authenticated login probe passed. No manual in-game tame/respawn timing was performed during this deployment.

Backup: E:/Backups/Haven/Prototypes/servuo-before-fast-respawn-20260912-025225
All 43 save files matched their backup SHA256 hashes. Config, scripts, engine source and root files were also copied. The original server was untouched.
