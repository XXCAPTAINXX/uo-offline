# Original Animal Lore and persistent timer — 2026-09-11

Ported original playerbots/source/CustomBots/UOOffline/HavenAnimalLoreGump.cs and HavenPetLoreGump.cs into ServUO. Restores full overview: loyalty/training bars, stats/vitals/attribute regen, resistance and damage distribution, follower slots, taming/barding, food/pack instinct, combat ratings and lore/magic skill/cap columns. Abilities/Rarity/Training/Care/Story card pages restored, with original paging and scroll-contained text. Ten species stories plus fallback copied verbatim (automated comparison passed). Flat gray labeled buttons retained.

Required compatibility changes: native training profiles/ability list and native training entry point; HealChance for native bandage capability rather than unavailable ModernUO CanHeal/CanHealOwner; current owner-bound tickets for stored inspection; C#7.3 and Gump API adaptations. Overview viewing does not mutate rarity/legendary rolls or hue. Old market-stall preview permission cannot be copied because that custom market API is absent. No claim to have imported old custom training mechanics.

Timer is260x76, non-closable by right click, with mission/countdown and two labeled controls. Maintenance reopens it while a connected owner's companion is on a mission and closes it after mission ends; same rule handles reconnect. Existing per-instance refresh remains. No additional saved-world fields.

Tests: original story string equality, overview combat/lore columns, all five lore tabs, active mission timer opens/restores/closes on recall via connected local socket fixture; full regression suite COMPLETE. Clean isolated build. Final bear Rage description matched original text before live compilation.
`nDeployed with backup E:/Backups/Haven/Prototypes/servuo-before-original-lore-timer-20260911-204717. Live build and startup passed. Subsequent authenticated login probe passed after the short-travel-cooldown deployment; both changes remain installed.
