# Independent Haven test installs

This testing branch is **`fix/haven-world-services`** of `XXCAPTAINXX/uo-offline`. Each friend installs a separate server and starts a fresh world. The source package contains code, patches, tools and documentation; it does not include the developer's characters, accounts, saves or UO game assets.

Use the [player test checklist](PLAYER-TEST-CHECKLIST.md), starting with S01–S08. Record the exact source commit with `git rev-parse --short HEAD`. This is an ML-compatible Haven build with custom later-era features, not a complete official expansion implementation. See the limitations below.

## 1. Obtain and install the testing source

On Windows, install Git, then:

```powershell
git clone --branch fix/haven-world-services --single-branch https://github.com/XXCAPTAINXX/uo-offline.git haven-testing
cd haven-testing
.\install.bat
```

Alternatively, extract the matching source ZIP and run `install.bat`. Choose a new install directory. The installer downloads/builds its pinned ModernUO version and applies the numbered patches, including 0051. Allow it to finish, then close the client and stop that new server normally before changing its data configuration.

The launcher updater currently follows **`haven-rc4`**, not this testing branch. Decline update prompts during this test cycle; otherwise an RC update may replace the build you are testing. Test updates should come from this same branch/source snapshot and use a normal saved-world backup. Do not mix files from different release snapshots.

## 2. Prepare the matching private maps

Install Python 3.11 or newer. You need your own classic 7.0.23.1 data installed by the installer, plus your own updated EA **Classic** client assets containing the original Shadowguard/Blackthorn maps. The Enhanced Client's data format is not supported by this tool.

From the downloaded repository, run the following with your actual paths. The classic data directory must directly contain `map1.mul`, `map1x.mul` and `tiledata.mul`; some installers create a second `7.0.23.1` subfolder.

```powershell
python tools/map/prepare_haven_test_data.py `
  "C:/HavenTest/UOData/7.0.23.1/7.0.23.1" `
  "C:/Program Files (x86)/Electronic Arts/Ultima Online Classic" `
  "C:/HavenTestPrivateData"
```

Use a **new output folder outside both source directories**. The tool validates the fixed island sites, creates the Commons/private island/frontier terrain, restores selected original dungeon blocks and prepares matching server/client geometry. It reads the checked-in room layouts; no files from the developer's workspace are required. It copies supported graphics into a private client-data folder and omits conflicting modern map UOPs/diffs. It never edits either source client installation or a game profile. Keep this generated game data on your own machine; the repository/ZIP contains only the preparation code.

The final `haven-test-data.json` must say `matching_private_data_ready`. If preparation fails, preserve the error and report it; do not use an incomplete output.

## 3. Point the new server and client at those files

With that test server stopped, edit its `ModernUO/Distribution/Configuration/modernuo.json`. Set `dataDirectories` to the generated **server-data** folder first, followed by the original classic-data folder as a fallback. Keep the existing account, listener and other settings.

```json
"dataDirectories": [
  "C:/HavenTestPrivateData/server-data",
  "C:/HavenTest/UOData/7.0.23.1/7.0.23.1"
]
```

Use a current TazUO-compatible client profile with its UO data-directory setting (`ultimaonlinedirectory`) set to `C:/HavenTestPrivateData/client-data`, and connect to `127.0.0.1`, port `2593`. A modern client profile is needed for the later artwork; the installer's old asset directory alone is not the full Haven visual setup. This guide does not install or reconfigure TazUO automatically.

For an install that has already started, move its generated `ModernUO/Distribution/Data/Pathfinding` cache aside while stopped so it rebuilds for these maps. Keep it as a backup; do not reuse a cache from another map set. Leave the configured ML ruleset in place rather than turning on every later expansion flag.

## 4. Set up the new world

Start the new server and create your own accounts through its first-start flow. Use its administrative character for setup. Finish the normal first-time world setup, then wait for `[WorldStatus` to report readiness. Additional setup commands, in this order:

```text
[HavenIslandsBuild
[HavenDoomSetup
[HavenAbyssSetup
[HavenAbyssRestore
[HavenSnowSetup
[HavenFrontiersSetup
[HavenOriginalDungeonsSetup
[save
```

Read each response. Missing terrain, occupied footprints or partial installations should be reported, not worked around by deleting world objects. The island belongs to the character running its creation command; choose that character deliberately. Once setup is complete, use a character with **Player** access for the gameplay tests. Staff access can bypass ownership, skill, money and travel checks.

Start with `[bank`, `[c`, `[market`, `[frontiers` and the normal travel book. `[home` requires ownership of the private island. Each fresh server currently has one Commons/private-estate installation, not an automatically generated island for every account.

## Verified here and still requiring friend testing

- The full content suite passes against freshly generated private maps, including native combat hooks, book balances/slots, mission receipts and dungeon geometry checks.
- All 51 native patches apply to a fresh pinned-engine checkout, whose native source matches the tested tree. This check caught and fixed a patch line-ending problem before sharing.
- Private server/client geometry copies are hash-verified by the preparation tool.
- The running personal server receives a backed-up save/restart deployment separately.
- A full first-time install on a friend's computer, their client profile/graphics and every real dungeon play-through still need the checklist. This is a test build, not a claim that the whole installer-to-gameplay flow has been certified on another PC.

## Later: playing together on one server

Before changing network access, finish the checklist's two-player section. We still need to choose hosting, account creation policy, backup/restore procedure, update windows, supported client build and how new players receive island/home access. Then configure the server listener and advertised address, test connectivity, and open only the chosen game port. Avoid exposing the local release inbox, remote desktop or administrative tools. No network exposure is enabled by this package.

Known feature limits include compatible rather than complete official Shadowguard/Blackthorn encounter and artifact tables, incomplete native SA Imbuing/boss systems, anchored rather than moving cannon/naval raids, remaining bot pathfinding and guild recipe provisioning work, and shared-server fairness tests that are not yet complete.
