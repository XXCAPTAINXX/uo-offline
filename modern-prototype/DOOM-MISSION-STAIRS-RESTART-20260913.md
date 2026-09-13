# Doom reconnaissance, mission selection and stairs

Historical release: the later TAME-DOOM-LOOT-RESTART-20260913.md removes the
Doom Resistance requirement and adds artifact rolls with protected returns.
The stair and mission-selection fixes below remain current.

Doom: artifact reconnaissance is available under companion Gathering missions,
including offline/AFK focus and rotation. It requires 100 Magic Resistance and
100 Tactics, Magery or Archery. Nominal 5/15/30/60 minute runs earn
2,500/7,500/15,000/30,000 real Doom Gauntlet points. Existing luck duration
reduction applies; the points payout is based on the chosen nominal duration.

The normal due-mission completion path awards points once, including to an
offline owner. Early recall awards none. Collecting rewards cannot duplicate
points. No artifact is rolled by the mission: the next eligible Doom boss kill
adds its normal points and rolls normally. Existing artifact reset behavior is
unchanged. Mission ID 26 is appended; older IDs and saved balances remain.

Selecting Doom displays the current drop percentage and projected percentage
after the selected mission. This is a snapshot of the current points balance,
before the next boss's points, not a guarantee if points change meanwhile.
The completion report and [doom show the actual current percentage.

Fixed a hard-coded 12-entry click limit that prevented selecting Heartwood,
Eodon and Doom. Click validation now follows the active tab's actual catalog.

Stair clearance migration moves the original managed receiving chest, export
cargo chest and rope to house-relative (4,8,7), (4,9,7), (4,10,7). The prior
sites were (1,6,7), (2,5,7), (3,5,7). Both chests and all their contents retain
identity, security and shared-vault linkage. New house construction uses the
same revised positions. User-relocated items are not repositioned.

Preflight checks destination occupancy, house bounds and fit. Moves roll back
if native walking checks fail. Checks cover the stair-side lanes at x=1 and 2,
y=3 through 9, stair ascent and routes to all managed storage/stations. The
structural posts remain in place. The one-shot marker saves the world only
after successful verification.

Validation passed in an isolated copy of the latest saved house: all movement
checks, contents/identity preservation and idempotency. Mission checks passed
all four payouts, no duplicate awards, early recall, other-mission exclusion,
requirements and actual NetState/OnResponse clicks on Heartwood/Eodon/Doom.
Existing mission/material regression tests also passed. Release x64 builds
had zero warnings/errors. In-game visual review remains outstanding.

Production backup: E:/Backups/Haven/Prototypes/servuo-before-stair-doom-menu-20260913-095255.
Contains Saves, prior Scripts.dll, replaced sources and SHA256 manifest.
Preview was saved/stopped before source replacement and restarted afterward.
