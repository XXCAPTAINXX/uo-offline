# Companion field reports and regional resource routes

Companions now keep an in-game field report covering their missions. A completed normal mission produces a report; consecutive AFK or offline runs are combined until the companion returns or the assignment ends. The report opens when the owner is back in the world and can also be opened with **`[companionreport`** or **`[c` → Tasks → Reports**. The last ten reports are retained.

Manual AFK mode still stays on until you turn it off; its report opens when you leave AFK. Automatic idle mode ends when you resume activity. Offline assignments end at your next login.

The compact report has Summary, Loot, Skills and Gear tabs with pagination and scrolling where needed. It records mission counts/types, worked minutes, early returns, level and raw-stat changes, all changed base skills, equipment levels/experience and before/after equipment attributes. Loot lists exact quantities and delivery destinations; resource deeds show their contained material quantities, not merely a count of paper deeds. Receipt details are snapshots and remain readable after moving, using or absorbing the items. Natural training during the reporting period is included in the displayed stat/skill changes; XP offered by missions is distinguished from the actual capped item XP shown on the Gear tab.

Existing assignments continue uninterrupted. For a companion already grinding when report tracking is installed, the report starts at installation, identifies the earlier completed run count and does not invent earlier item-level loot details. Subsequent runs are recorded exactly.

## Resource routes

Open **`[c` → Tasks → Resource routes**. Send launches the standard 5/15/30/60-minute duration selector. AFK focus repeats a chosen route; Mixed cycle rotates ordinary gathering, loot, pets and eligible regional routes. Cycle basic focus selects loot, ore, wood, leather or reagents. Run this focus after logout arms an offline assignment; it starts after disconnecting and ends on return. With Mixed cycle selected, the offline order defaults to Grind; the connected AFK cycle still mixes routes. The selected duration is frozen when the offline order is armed. An active offline assignment prevents route changes until the companion returns.

| Route | Combat/resist requirement | Rewards |
|---|---:|---|
| Malas: necromantic reagents | 40 | Bat wings, grave dust, daemon blood, nox crystals, pig iron |
| Malas: Doom bones | 60 | Daemon bones and ordinary bones |
| Stygian Abyss: essences | 80 | Three selected virtue essences |
| Stygian Abyss: rare ingredients | 100 | Three selected supported Abyss materials |

Eligibility uses the lower of Magic Resistance and the best of Tactics, Magery or Archery. Duration bonuses follow the existing mission rules. Regional rewards use the shard's implemented resource types; essences and rare materials feed the existing compatible Abyss artifice recipes. This change does not add a separate native Imbuing system.

Every regional material uses a **Commodity Deed compatible with the Resource Ledger**. Give your companion a Resource Ledger (a nested bag in the shared pack also works) to deposit supported mission deeds automatically, including normal gathering, AFK and offline runs. Reports list the exact quantities with destination **Companion Resource Ledger**. Without an eligible ledger, rewards keep their normal deed delivery. Unsupported rewards are retained as items. If several ledgers are carried, the first one with room for the material receives it.

You can open the carried ledger directly while your companion is nearby on the same map. Only the bound owner can use it from the companion's pack. Absorb pack deeds processes existing deeds in the pack carrying the ledger; Add deed also accepts deeds in your own backpack. Withdrawals always create the requested deed in your own backpack, and a full backpack leaves the balance unchanged. Matching materials combine into balances; material type and tier are preserved. Daemon bones were appended to the catalog without moving any existing saved balance index. Existing books gain the new entry automatically.

To empty the companion's ledger into yours, open his book, choose **Transfer all...**, then target a different Resource Ledger in your backpack. Every stored balance merges into that destination and the source balances become zero. Both books remain available. The transfer is all-or-nothing, rechecks ownership and proximity when you select the destination, requires no space for intermediate deeds and does not create a second mission reward receipt. It also works between two ledgers in your own backpack.

The Resource Ledger and Champion's Codex each occupy one backpack slot. Ledger balances and items archived inside the codex consume no additional slots. Existing codices receive the fix when the updated server rebuilds inventory totals on load. Combining or splitting scrolls inside the codex needs no free backpack slots; withdrawing a scroll or resource deed does require space for the extracted item. Resource weight rules are unchanged.

The source references for the regional themes are the official [Doom guide](https://uo.com/wiki/ultima-online-wiki/world/dungeons/dungeon-doom/), [Umbra guide](https://uo.com/wiki/ultima-online-wiki/world/cities-and-towns/cities-and-towns-umbra/), and [Stygian Abyss mini-champion guide](https://uo.com/wiki/ultima-online-wiki/combat/pvm-player-versus-monster/stygian-abyss-mini-champion-spawns/). Reward quantities and mission requirements are custom Haven balancing choices.

## Persistence and validation

New journal and route records preserve existing expedition, idle-mission and offline-assignment serialization layouts. New mission enum values are appended after all existing pet missions. Journals store snapshots and receipt data rather than live loot references. Timers run on the main game loop and are stopped on deletion. Report access validates the bound owner.

Tests exercise all four routes through generation, deed-book absorption, combining and withdrawal; the complete catalog test also redeems withdrawn deeds and reloads balances. Further checks cover stable mission IDs, skill gating, report quantities, snapshot persistence, no repeated payout, and aggregation of offline runs. Deployment evidence is recorded in `artifacts/HavenMissionReportRelease20260909`.
