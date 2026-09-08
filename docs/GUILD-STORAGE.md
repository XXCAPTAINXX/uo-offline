# Guild Storage MVP

This feature lives on the `modern-evolution` branch.

## Quick test

1. Build/run UO Offline from the `modern-evolution` branch.
2. Log in with a character.
3. Type `[GuildStorageKit`.
4. Double-click **Guild Storage Kit** in your backpack.
5. Open **Complete Guild Storage Set**.
6. Place the **Master Guild Chest** and the profession chests in the same house/workshop.
7. Drop items into the Master Guild Chest.

The Master Chest automatically routes items to linked profession storage within 24 tiles.

- Unknown items go to **Guild Unsorted Storage**.
- Known items whose profession destination is unavailable go to **Guild Overflow Storage**.
- If neither fallback is available, the item remains in the Master Guild Chest.
- The sorter never silently deletes deposited items.

## Commands

- `[GuildStorageKit` — player-level test command; gives a one-click storage kit.
- `[GuildStorageSet` — Game Master shortcut; creates the complete storage bundle immediately.
- `[GuildStorageLink` — target a Master Guild Chest to link nearby unlinked/replacement profession chests and re-sort current contents.
- `[GuildStorageMissing` — target a Master Guild Chest to create replacements for missing profession chest types.
- `[GuildStorageSort` — re-sort items already sitting directly in the Master Chest.
- `[GuildStorageStatus` — report linked chest counts and stored-item counts.

## Included storage roles

- Resource Warehouse
- Smithy Crate
- Tailor Crate
- Carpenter Crate
- Tinker Crate
- Alchemy Cabinet
- Scribe Cabinet
- Pantry
- Tamer Supply Chest
- Armory
- Treasury
- Unsorted Storage
- Overflow Storage

Multiple chests of the same profession are supported.

## Architecture note

The storage system is persistent because the chests are normal ModernUO world items.

Recruited PlayerBots need a separate persistence layer. UO Offline currently treats accountless PlayerBots as transient and purges stale bots at startup, so permanent guild recruitment must preserve identity/progression in a durable record and reconstruct the same bot after restart.
