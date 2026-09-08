# UORespawn integration

Modern Sandbox uses **UORespawn 2.0.1.4** as its dynamic general-world population system.

## Pin

- Source: `Kita72/UORespawnProject`
- Commit: `26ad18a910dbc1318079035a87084d191ff173f9`
- License: MIT
- Upstream DefaultPack version: 1.0.0

The pin is deliberate. Do not silently track upstream; update the commit only after the ModernUO compatibility build and runtime spawn checks pass.

## What the installer consumes

Only two upstream release artifacts are downloaded:

- `UORespawnApp/Data/SERVER/MUO/UORespawnServer.zip`
- `UORespawnApp/Data/PACKS/Approved/DefaultPack.zip`

The full editor repository is not cloned during normal installation.

The server module is installed into:

`ModernUO/Projects/UOContent/Custom/UORespawnServer/`

The initial pack is installed into:

`ModernUO/Distribution/Data/UORespawn/INPUT/`

The upstream MIT license is copied to:

`ModernUO/Distribution/ThirdPartyLicenses/UORespawn-MIT.txt`

## DefaultPack scope

The upstream pack describes itself as an ecology-based spawn system across all six standard maps, using tile and region archetypes plus Common, Uncommon, Rare, Water, Weather and Timed frequencies.

Pack files include:

- `UOR_BoxSpawn.bin`
- `UOR_RegionSpawn.bin`
- `UOR_TileSpawn.bin`
- `UOR_VendorSpawn.bin`
- `UOR_SpawnSettings.csv`

The installer seeds these only when an existing region pack is absent. Player/editor changes are therefore preserved on reinstall.

## PlayerBot protection

UORespawn creates per-player respawn controllers from connected `PlayerMobile` events. UO Offline's `PlayerBot` also inherits `PlayerMobile`.

The installer patches the pinned UORespawn event predicates so a `Server.CustomBots.PlayerBot` can never be registered as a respawn anchor. This is an explicit safety invariant even though ambient bots normally do not create client Connected events.

If the pinned upstream event-hook text changes, installation deliberately fails instead of silently dropping this protection.

## Native ModernUO spawns

ModernUO still ships its own JSON spawn library. Do not broadly generate those files on top of UORespawn unless intentionally testing a hybrid world; doing so can duplicate ordinary wildlife/town/vendor population.

Native engine/scripted content remains available for encounters that should not be replaced by generic ecology. We will audit and retain champion, quest, Doom, ML/SA and other special systems separately as the modern world pass continues.

## Administration

Administrator command:

`[UORespawn`

Useful upstream diagnostics include spawn statistics/debug views. Use the control panel to tune density/chances before changing source code.
