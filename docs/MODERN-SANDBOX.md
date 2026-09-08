# Modern Sandbox baseline

The `modern-evolution` branch is the modern-era development line.

## Current baseline

- ModernUO pin: `114dbba6e25f0e97e8537e54025d7bfa87c03a39`
- Expansion: Endless Journey (ID 11)
- Maps enabled: Felucca, Trammel, Ilshenar, Malas, Tokuno, Ter Mur
- Modern UO data: required
- T2A map replacement: OFF by default
- Nerun pre-T2A spawn map: skipped in Modern mode
- ModernUO expansion-aware JSON spawns: used in Modern mode
- Classic T2A remains available as an explicit legacy profile

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

A GitHub Actions compile workflow is included at `.github/workflows/modern-build.yml`.

The workflow checks out the pinned ModernUO version, applies the UO Offline engine patches when they still apply, copies PlayerBots into UOContent, and builds the UOContent project.

A successful compile is required before calling this baseline tested. Runtime/client testing is still required after that.
