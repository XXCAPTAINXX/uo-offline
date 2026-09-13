# Wandering encounters restored — September 13, 2026

Published to the ServUO preview at 01:16 EDT, port 2699. Original ModernUO server was untouched.

Ported the existing `playerbots/source/CustomBots/UOOffline/HavenWanderingEncounters.cs` system to native ServUO serialization and APIs. Progress starts on this server; this is a code port, not an import of the previous server's world save.

## Behavior

- `[encounters` opens progress, recent history, rewards, and enable/disable controls. `[encounters off` safely cancels an active invasion.
- Moving players get a 50% encounter roll every 12–20 minutes. No starts while idle, in combat, casting, houses, guarded regions, or dungeon regions.
- Three waves followed by a champion; wins increase difficulty up to 20, retreats/deaths/timeouts lower it. Logout/restart cancellation has no difficulty penalty.
- Invaders are hostile, leashed, and cannot harm targets inside houses or guarded regions. Spawn positions require native ground checks, line of sight, and no houses/guarded regions.
- Participant-only spoils chest lasts 20 minutes. Existing gold, shards, crafting loot, encounter points, and rare animal rewards are retained with native-server equivalents. Clears also grant 1–5 Sovereigns through the current account reward API.
- Journals persist on their owner as invisible, immovable Blessed items. Interrupted encounters cancel and clean up after reload.

## Encounter-only gear

One optional gear item per champion clear, in the protected spoils chest. Tier 1–4: none. Tier 5 starts at 12%, rising 1.2 percentage points per tier to 30% at tier 20.

- Tier 5+: Ironwake bulwark, Tidecasting ring, Deepcasting bracelet.
- Tier 12+: Tidecaller spellbook and Drowned grimoire join the pool.

These five pieces were removed from the special-reward stone. Existing copies are unchanged; all retain level 1–20 growth. Stormguard shield and four starter caster pieces remain at the stone. Cargo rewards remain with the dock trader.

## Verification and restart

- Isolated full encounter: 19 kills at tier 12, all three waves and champion complete, tier advances to 13, 80 clear points, participant-only chest.
- Duplicate-start rejection, no-penalty cancellation, retreat difficulty reduction, hostile invaders.
- Gear chance boundaries across all 20 tiers, evolving item attachment, five remaining starter catalog entries.
- Separate-process native save/reload retains journal progress and removes interrupted invasion mobs without penalty.
- Clean live save/shutdown; fresh Saves backup with SHA-256 manifest, prior Scripts.dll and changed existing source. Backup location recorded in workspace `verification/wandering-backup.txt`.
- Live build: zero warnings/errors. Startup listening on 2699. Account login/server-list/game-relay probe passed.
- No live client combat or visual inspection claimed. No client assets or island geometry changed.
