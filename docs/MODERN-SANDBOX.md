# Modern Sandbox baseline

The `modern-evolution` branch is the modern-era development line.

## Current baseline

- ModernUO pin: `114dbba6e25f0e97e8537e54025d7bfa87c03a39`
- Expansion: Endless Journey (ID 11)
- Maps enabled: Felucca, Trammel, Ilshenar, Malas, Tokuno, Ter Mur
- Modern UO data: required
- T2A map replacement: OFF by default
- Nerun pre-T2A spawn map: skipped in Modern mode
- UORespawn 2.0.1.4 dynamic population: enabled in Modern mode
- UORespawn DefaultPack: ecology/regions/tiles/vendors for all six maps
- PlayerBots are explicitly excluded from UORespawn spawn-anchor tracking
- Classic T2A remains available as an explicit legacy profile

## World population model

Modern Sandbox no longer uses the old Nerun T2A spawn map.

General world population comes from the pinned UORespawn ModernUO module and its DefaultPack. UORespawn is player-centric: creatures are searched/spawned around actual connected players according to map, terrain, region, time, weather and pack rules.

The installer seeds `Distribution/Data/UORespawn/INPUT/` only when no existing UORespawn pack is present. Re-running the installer therefore preserves edited spawn data.

Do not use ModernUO's broad **Generate Spawners** world-building action on a normal Modern Sandbox unless you intentionally want to layer static JSON spawns on top of UORespawn. ModernUO's native scripted systems remain available for special encounters/content as we continue the modernization.

Use `[UORespawn` as an Administrator to open the UORespawn control panel. Its spawn editor can later be linked directly to the installed ModernUO tree.

## Windows

Run `install.bat` normally for Modern Sandbox.

The GUI's **Legacy T2A mode** checkbox is OFF by default. Leave it off for the modern shard.

Modern mode searches for a fully patched official Ultima Online Classic installation and accepts both legacy MUL and current UOP-backed art/map layouts. It deliberately does not download the old 7.0.23.1 client data.

If current UO data is not found, install/patch the official Classic Client, then re-run the installer.

Console legacy mode:

```powershell
powershell -ExecutionPolicy Bypass -File install.ps1 -ClassicT2A
```

## Linux / Steam Deck

Modern mode is also the default:

```bash
./install.sh
```

For an intentionally old T2A install:

```bash
./install.sh --classic-t2a
```

On Linux, `UO_DATA` may be set to the patched UO data directory when auto-detection cannot find a Wine/Steam install.

## TazUO

TazUO can connect directly to this local shard. Configure a TazUO profile/settings with:

- IP: `127.0.0.1`
- Port: `2593`
- Ultima Online directory: your fully patched current official UO Classic data directory
- Client version: the version of that patched client

Do not point Modern Sandbox at the old 7.0.23.1 T2A data directory.

## Build status

GitHub Actions validates the PowerShell and Bash installer syntax, checks out the pinned ModernUO version, applies the UO Offline engine patches, copies PlayerBots, installs the pinned UORespawn ModernUO server module and six-facet pack, and builds UOContent.

The integrated ModernUO + PlayerBots + Guild Storage + UORespawn build passed CI on September 8, 2026. Runtime/client/world-content testing is still required before treating every facet as fully validated.
