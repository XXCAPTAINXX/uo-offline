# Pet book and smoke teleport deployment — September 12, 2026

Preview online on port 2699 after user-authorized clean restart. Original server untouched.

## Deployed

- `[petbook` creates/opens a free, blessed owner-bound book in your backpack. It lists six pets per page, with Lore and Release controls.
- Shrinking a bonded pet through the existing storage path routes its exact claim into the book. Opening the book collects existing bonded tickets from accessible backpack containers. Companion reclaim uses the same routing.
- Bonded pets are rejected from exchange, whether or not their tickets have been collected into a book. Legendary protection remains.
- Release preserves the exact pet, bond and training; failure to meet follower/landing requirements keeps it stored.
- Smoke bombs teleport to a visible valid point within 12 tiles, require no Magery/Ninjitsu/mana, consume one on success and have a three-second cooldown. Native teleport destination restrictions apply; smoke appears at departure and native pet teleport behavior is used.
- Arcane supplies sells a single stack of 20 for 1,000 gold. Weight is 0.1 stone per bomb (2 stones per 20). Startup updates existing bombs to this weight and makes them stackable.
- Companion natural skill gain amount is doubled in the Haven bonus rectangle on Trammel. Gain chance is not also doubled. Caps and existing over-120 slowing remain; offline mission awards are not multiplied.

## Deployment and validation

- Backup: `E:/Backups/Haven/Prototypes/servuo-before-petbook-20260912-020344`.
- Clean save/shutdown succeeded; SHA256 comparison matched all 43 save files.
- Eight custom source files and native patch 0024 deployed. The stack/weight follow-ups were included while the server was stopped.
- Regression completed: bonded storage and exact release, exchange protection, smoke success/consumption/range/cooldown, and Haven multiplier boundary checks. Initial test failures were corrected in the test fixtures: ground targeting requires LandTarget, and the outside-Haven assertion must explicitly move outside Haven.
- Final live Release build: zero warnings/errors. Authenticated login, server list and game relay passed.
- Stack/weight changes compiled in final live build; no manual client visual/gameplay checks claimed.
- Housing island/community-center work and training-area decoration remain unfinished and were not installed in this restart. Jenna's voice acting remains low priority.
