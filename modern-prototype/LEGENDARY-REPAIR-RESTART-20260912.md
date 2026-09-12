# Legendary pet skill protection — September 12, 2026

## Cause and correction
Native AnimalTaming.ScaleSkills lowered both base values and caps of the separately rolled Legendary skills. A successful tame could therefore reduce a 125–150 roll below 125, and repeat taming could reduce it again. The lore description also still described the obsolete 50% chance of 1–3 skills.

Native patch 0025 skips only the recorded Legendary skill selections during taming skill scaling, gated to Haven preview. Ordinary skills retain native scaling. Legendary rolls remain 3–5 existing eligible skills at 125–150, with matching or higher caps.

Startup repair checks existing Legendary pets, including internal/ticket pets. Recorded rolls below 125 are raised to 125 and caps raised as needed. Already higher values remain. Skill selections are preserved; repeated repairs do not add points or reroll skills. Historical original roll values were not saved, so this restores the promised minimum rather than inventing their former numbers. Lore text now describes the current rules.

## Restart and validation
Clean save and restart around 04:20 EDT. Deployed HavenLegendaryPetSkills.cs, HavenLoreCompatibility.cs and patches/0025-protect-legendary-pet-skills.patch to Scripts/Skills/AnimalTaming.cs. No island code deployed.

Isolated regression tests passed for first/repeat-tame protection, repairing damaged base/cap values, preserving selected skills and idempotent repair. Inventory test reached COMPLETE. Verification and live Release builds passed with zero warnings/errors. Authenticated login/server-list/game-relay check passed after startup. In-game lore display was not manually inspected.

Backup: E:/Backups/Haven/Prototypes/servuo-before-legendary-repair-20260912-041931
All 43 save files SHA256-matched their backups. Config, scripts, engine sources and root files also copied. Original server untouched.
