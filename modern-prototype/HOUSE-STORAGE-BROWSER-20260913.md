# House storage browser and protected companion deposit

Connected house-storage chests now open a 12-row browser with recursive root/category views, case-insensitive search through accessible bags, name/quantity/category sorting, containing-bag labels, quantity counts, hover properties, bag navigation and partial-stack withdrawals. Native rectangular buttons are retained. Search and navigation do not change the stored items.

Take on a stack opens custom quantity plus 1/10/100/all choices. Native LiftItemDupe separates a stack; failed delivery restores its original amount and removes the temporary remainder. Every action revalidates account ownership, range, storage ancestry and locked/trapped container restrictions. Custom storage devices are treated as items rather than traversed into their managed contents.

Deposit companion loot is available on the house browser. The companion menu also has Deposit house loot, which targets a connected house chest. The owner must be within the station's normal three-tile range and the companion must pass normal pack-access checks.

The deposit moves loose eligible items and items inside accessible ordinary bags, leaving the bags with the companion. It keeps ALL weapons, armor, clothing and jewelry (including ordinary loot gear), evolving gear, blessed/newbie/immovable items, instruments, tools, books, ledgers, pet items, bandages, ammunition, keys, lockpicks and recognized travel/rune/compass/dye/quiver/whistle utilities. Protected containers are not emptied. Full storage leaves undelivered items with the companion. Neither equipped gear nor ledger balances are transferred.

Validation: native test world passed protected nested deposit, foreign-account rejection, full-storage retention, nested withdrawal, stale replay, partial quantity/remainder accounting, failed partial-transfer restoration, and browser sort/empty/page construction. Independent critic review improved from 7/10 to 8/10 after recursive categories and quantity withdrawal were added. The critic identified no remaining must-fix findings; this was source/layout review, not client visual inspection.
