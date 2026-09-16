# Haven backups on E:

Private recovery root: `E:\Backups\Haven`. These backups contain player accounts,
world saves and private client data; share the separate friend-test source ZIP.

| Folder | Contents |
| --- | --- |
| `Snapshots/YYYYMMDD-HHMMSS` | Verified full recovery checkpoints: server, saved world, configuration, maps, client, source history, release packages and evidence |
| `World/Backups/Automatic` | Server-generated saved-world backups after each five-minute save; each contains the preceding completed save |
| `World/Archives` | Built-in hourly/daily/monthly compressed world archives |
| `World/Temp` | Temporary archive work only |
| `Deployments` | Future pre-update backups from the standard update script |
| `Releases` | Tested post-update payloads, source archives and deployment evidence retained alongside their pre-update backups |
| `LATEST.json` | Most recent complete snapshot and verification result |

The existing server retention settings remain: 30 days for raw backups, 24 hourly,
30 daily and 12 monthly archive periods. Dated full snapshots are never automatically
pruned. Old D: backups are preserved and copied inside the initial snapshot's
`install/haven-world-backups` and `install/ModernUO/Distribution/Backups`/`Archives`.

## Creating a full checkpoint

Use Python 3.12+ and `scripts/backup-haven.py`. Pass the same new dated `--snapshot`
directory to its three phases:

1. `static`: copies and SHA-256 checks the install outside Distribution, current
   development source including Git history and uncommitted files, release artifacts,
   and the TazUO launcher/client profiles. Do not edit/deploy code or maps during this phase.
2. Confirm a completed world save through the operator inbox or server console, then
   stop the specific Haven server process. Apply any intended configuration changes.
   Run `runtime` to copy and hash-check the entire Distribution directory while stopped.
   Restart the server even if this phase fails; failed backups must not be used.
3. `verify`: rereads every backed-up file and checks both phase manifests before
   writing `VERIFIED.json` and updating `LATEST.json`. Check world startup and save logs.

Example phase command (repeat with runtime and verify at the appropriate steps):

```powershell
python ./scripts/backup-haven.py static --snapshot 'E:/Backups/Haven/Snapshots/20260909-230000'
```

Defaults match this computer. Other computers can override `--install`, `--workspace`
and `--client`. The runtime phase refuses to run while port 2593 has a listener.
It does not save or stop the server itself. No full-snapshot scheduler is installed;
take a full checkpoint before significant releases. Continuous world backups are
handled by ModernUO itself, using absolute paths in `Configuration/modernuo.json`.

Excluded: disposable verification/rehearsal copies, the separately managed Zelda
project (`dads-games`), active operator requests, install-root logs, and unrelated
shared game installations. The matching Haven maps and client art are included.

## Restore

1. Read the snapshot's `SOURCE-STATE.json`, `VERIFIED.json` and phase manifests.
   Run its backup script's `verify` phase again before restoring.
2. Stop Haven. Preserve the currently installed world separately before recovery.
3. Restore `install` to `D:\Uo Offline\uo-modernuo`. Use a fresh directory when
   possible; do not merge old and new Saves files. Restore the complete Saves directory
   together with the snapshot's Assemblies and Configuration.
4. Restore `client-launcher` to the original launcher path in `SOURCE-STATE.json`.
   Restore `workspace` to the recorded workspace if development files are needed.
   Use the included matching `UOData` and `TazUO-Haven-Data`; mixed map versions can
   make terrain and dungeon entrances appear broken.
5. Install the recorded .NET SDK/runtime from `global.json` and the server's
   `ModernUO.runtimeconfig.json` if recovering onto another PC. If paths change,
   update `modernuo.json` data and backup paths and the private client profile path.
6. Start ModernUO from Distribution. The local operator helper requires
   `HAVEN_RELEASE_INBOX=D:\Uo Offline\uo-modernuo\HavenReleaseInbox` and that directory.
   Inspect startup for missing types/deserialization errors, connect, check your
   character/companion and save successfully before resuming play.

For a newer world than the full checkpoint, use a complete automatic backup marked
`.backup-complete`, or a verified built-in archive, only with compatible server code.
The archive timestamp identifies when the older completed save was backed up.
Keep the complete world folder intact; never mix individual files from different saves.

An E: snapshot protects the running D: installation. Source and its local backup
both residing on E: still share that drive's failure risk; GitHub holds the tracked
source but not private world/client data.
