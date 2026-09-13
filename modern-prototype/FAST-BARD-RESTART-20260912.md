# Faster companion bard response — September 12, 2026

## Deployed changes
Only HavenCompanionTaming.cs changed on the preview server.
- Combat bard attempt interval: 20 seconds to 3 seconds.
- Taming-assist peacemaking attempt interval: 12 seconds to 3 seconds.
- Combat bard selection prioritizes the owner's current combat target, falling back to the companion's target when the owner has none.

Range, line of sight, skill checks, immunity, and existing spell/target busy checks remain. This is an attempt interval, not guaranteed success or an exact reaction time. The actual taming attempt timer stays at ten seconds; this change accelerates calming assistance. Player bard cooldowns are unchanged.

## Restart and validation
Clean save and restart around 03:12 EDT. Isolated and live Release builds passed with zero warnings/errors; authenticated login checked after startup. Native handlers inspected to verify the companion's direct calls are governed by its own attempt timers. No manual in-game reaction-time test was performed.

Backup: E:/Backups/Haven/Prototypes/servuo-before-fast-bard-20260912-031116
All 43 save files verified by SHA256 comparison. Config, scripts, engine sources and root files also copied. Original server untouched. Island work excluded.
