# Haven feature inventory — September 10, 2026

For the detailed missing-feature comparison, see [Original Haven versus the modern preview](OLD-VS-MODERN.md).

There are now two builds. **Original Haven (port 2593)** contains the accumulated custom systems and your existing character/island. **The separate ServUO preview (port 2699)** uses a fresh modern world and a newly ported companion. This is not a conversion of your old save. Features listed under the original build are not automatically present in the preview.

This inventory was checked against the implementation tracker, player guide, feature documents and the modern preview source. It distinguishes implemented work from requests that still need work. It is not a new live playtest of every original feature. Individual presentation, balance and multiplayer checks remain in the [player checklist](PLAYER-TEST-CHECKLIST.md).

## Modern preview: available for tonight's test

| Area | Implemented and checked |
|---|---|
| Modern foundation | Pinned ServUO pub57 with modern Classic data and six map facets; separate save, account and loopback listener. |
| Populated world | Native generation of towns, NPCs, vendors, doors, decoration and expansion travel links; 6,835 spawners, six Doom controllers, 17 Shadowguard instances, 14 verified Blackthorn links. A second clean setup and reload passed. |
| Modern systems | Native Shadowguard rooms, Blackthorn, Doom, SA/Underworld and High Seas scripts are present. Foundation tests exercised room setup, Imbuing, kitchen items and boat movement. This is not certification of a complete client playthrough. |
| Test controls | `[preview` supplies a one-time gear/120-skill test kit and travel to ten locations. Armor, mana-leech weapons, shield, bow, spellbooks, Parry III primer, bandages, gold check and Resource Ledger. |
| Companion | `[c` / `[companion`; one bonded Alden per character; Follow, Guard, Stay, Attack, healing, owner pack and party membership. |
| Combat roles | Warrior, native Magery Caster and Archer; preserved role equipment, real ranged attack tests, commands cancel queued casting. Guard avoids wild tameables. |
| Spoken orders | `all follow me`, `all guard me`, `all stay`, `all stop`, `all kill`, `all attack`, `all heal me`, and named orders. |
| Timed missions | Five-, fifteen- and thirty-minute Supply, Mining, Lumber, Leather, Malas and Abyss jobs. Longer jobs yield more; gathering skills unlock material tiers. These are timed jobs, not physical expeditions. |
| Mission recovery | Offline deadlines, persistent reports, merged gold and pending rewards; no duplicate payout on restart. |
| Resource books | Player and companion ledgers; loose resources, native commodity deeds, nested bags, selectable withdrawals and transfers. Virtual balances use no extra pack slots. Overflow remains queued. |
| Operations | Start/save/stop scripts, separate client configuration, GitHub source and E: backups. Network account authentication and relay passed; the user approved the Windows prompt, and real-client login, New Haven rendering and the test-kit claim now work. |

The companion/control/resource suite passed **103 checks** on fresh and reloaded test worlds. See [tonight's instructions](../modern-prototype/TONIGHT.md) for the latest deployment status and the [recorded checks](../modern-prototype/companion-checks.txt). A passing automated check is not a substitute for the final graphical playthrough.

## Original Haven: accumulated custom work

### Everyday systems

- Gold/Haven Mark/Astral shard wallet and supported merchant, repair, training and other payment integrations; wallet/bank payment work.
- Player stats and skill-budget displays, including `[mystats`; custom skill/stat rules and free-skill exceptions.
- New Haven luck/training bonuses, bank recovery, corpse/pet recovery, provisioner and bank service network.
- Larger personal banks: documented 2,000-item capacity, with existing larger limits preserved.
- In-game searchable field guide (`[guide`), player guide, friend-install guide and test checklist.
- Menu readability, spacing, pagination, item previews and concise pet hover information. Visual polish is still iterative.

### Gear and progression

- Evolving starter weapons/spellbook, earrings, cape and sash; supported Haven quest reward upgrades.
- Catch-up claims for eligible training quests your skill has outgrown (`[HavenRewards`).
- Haven jewelry, Astral shard purchases and compatible random Legendary Artifact progression.
- Set bonuses and eligible equipment progression toward an additional follower-cap benefit.
- More equipment for less-used clothing slots and build-oriented rewards; shield-warrior/basher work exists in source and still needs balancing/playtest review.
- Expanded Haven Mark exchange: documented 54 offers, including evolving boots/sash/doublet/apron, runic tools, dyes, supplies and resource deeds.
- Useful rare world rewards, including the Gilded Pathfinder treasure tool and Mercy's Endless Bandage.
- Doom artifact progression/reforging and supported evolving special-boss artifacts.

### Champions and wandering encounters

- Fast Haven mini champion with multiple themed wave sets and crafting resources.
- Larger gold rewards, power scrolls, Haven Marks, Astral shards and possible Alacrity/Transcendence rewards.
- Personal participant rewards into packs, resource deeds to reduce weight, wallet/ledger integration and quicker ordinary-corpse cleanup.
- Home-island Blackwake pirate mini champion away from the headquarters.
- Wandering encounter waves, escalating difficulty, failure/escape behavior and bounded enemy movement.
- Encounter journal, completion/difficulty records, points, rewards and special pet outcomes.

### Power scrolls, runes and storage

- Champion's Codex stores supported scrolls and offers individual upgrade recipes and supported reverse/split recipes.
- Players and pets share standard power scrolls; older pet-only scrolls have compatible exchange/acceptance handling.
- Resource Ledger combines supported deeds, withdraws chosen quantities and transfers balances between books.
- A companion-carried ledger automatically receives supported mission resources; `[CompanionStore` tidies eligible existing deeds.
- Filled ledgers and codices consume only their own backpack slot.
- Gatherer's satchel/resource carrying bags, including gems and other supported supplies; category menus and nested-bag collection.
- Blank recall rune pouch stores runes as a balance; casting Mark on it produces a normal marked rune. `[rune` extracts a blank.
- House receiving chest sorts into profession stores and provides master search/withdrawal access; menus explain accepted goods.
- House/owner/guild access checks, item/bag targeting and ordinary container access where supported.

### Pets

- Custom pet rarity, improved rare rolls, rarity hues and species-specific appearance tones.
- Legendary custom pets start at one slot and can roll over-cap skills.
- Rarity innate bonuses separate from trained abilities; distinct species attacks/defenses, ranged effects, themed ground damage and supported immunities.
- Pet leveling/training improvements, ability selections and power-scroll cap tiers.
- Taming claim tickets, immediate feeding-based bonding eligibility, ticket/living/shrunken pet lore and fuller species lore pages.
- Shrinking tools and species statue work; pet dyes and supported pet packs.
- Mountable snow bear/Frostbound den with its custom combat identity.
- Ancient Hellhound encounter in the Abyss and Chelonian land/sea tortoise content.
- Pet command shortcuts suitable for client hotkeys. Obedience still depends on the pet's requirements.

### Companion and missions

- Melee, archer, caster and bard roles; combat positioning, healing, cure/resurrection support and gear progression.
- Paired bard mastery effects for the chosen mastery, including supported party pets.
- Spellweaving selection improvements, persistent arcane-focus support and damage-to-mana behavior.
- Follow/guard/stay/attack and spoken pet-style commands; avoid automatically attacking wild custom tameables; guard-message spam fixes.
- Compact combat controls, expanded equipment interface work and approved longer-range owner pack access.
- Adjustable mission lengths, resource grades, gear and taming trips, useful taming supplies and direct rewards instead of accumulating bags.
- Manual AFK mode stays enabled until switched off; selected-focus/mixed cycles and separate offline assignments.
- Detailed mission reports: rewards, destinations, skill/stat changes and equipment progress; offline return reporting.
- Gold-stack consolidation and persistent reward handling.
- Tame assist calms and attempts normal taming, preserving a successfully tamed animal in a claim ticket.
- Assigned companion pets: transfer/reclaim, following, combat, mounting and safe mission parking.
- Puzzle assistance for the implemented custom Shadowguard handlers, not arbitrary dungeon puzzles.

### Masteries

- Compatible Book of Masteries, primers, learned-volume selection and ability controls.
- A documented 45-entry catalog with active and passive effects.
- Parry/shield and weapon effects; pet-related and caster effects within the compatibility implementation.
- Bard party support plus Saving Throw/Resilience combat hooks and cleanup.
- This original implementation has documented differences from full later-era native mechanics.

### Economy, bots and guilds

- Haven Commons/community market and a global `[market` directory with finite stock, filters, previews and wallet purchases.
- Crafted gear, furniture, stations, supplies, pet goods and supported earned artifact/currency listings.
- Producer/broker work, BOD processing, taming/pet-supply production and market quality/price work.
- Physical dungeon crew work with player interaction and supported player loot routing.
- Fishing fleet/boat-related worker implementation; full moving combat-fleet AI remains unfinished.
- Guild crews/Fellowship management, persistent workers, gathering/crafting/training orders, gear and work history, treasury/permission handling.
- Party bot healing/cure/resurrection and support improvements. General navigation and complete encounter intelligence still need work.

### Dungeons and sea encounters

- Native Doom gauntlet controllers plus the custom reward/progression integration.
- Compatible Shadowguard Bar, Orchard, Armory, Fountain, Belfry and Roof at original Eodon/Ter Mur locations; easier Orchard and specific puzzle helpers.
- Compatible Blackthorn invasions at the original castle/dungeon locations: minions, captains, beacons and personal credits/rewards.
- Partial Abyss restoration: thirteen crafting sites, essences/ingredients, artifice and route/bridge work.
- Scalis, Cora and Corgul encounters/rewards, including supported Scalis net summoning and small soul forge chance.
- Anchored corsair boarding raids, cargo objectives, turn-ins and evolving relic rewards.
- Supported sea bosses/captains can award Britannian or Orcish ship deeds.
- Sea mount/cargo/fishing/SOS utilities and companion water following within the custom systems.

### Travel, housing and island

- Travel book/Atlas dungeon destinations, recovery routes and Commons access.
- Treasure-map location library, map-target/rune lookup work, Malas/Ter Mur treasure-route work and a house library option. Later source changes need their own deployment/playtest confirmation.
- Owner `[home` travel and clearer failure feedback.
- Castle-sized R.E.C. / Rare Export Company pirate headquarters, using several buildings and floors; island layout/decorating remains in progress.
- Workshops, guild/map areas, quarters, storage, barn/stables, cookhouse and storehouse/shed improvements.
- Functional floor-access revisions, cleared approaches, windows/porches and more organized interiors.
- Gardens, orchard, gathering areas, dock, patrol board and home mini champion.
- Eleven compatible refillable crafting stations, mining cart and producing tree stump; later kitchen set evaluation on the modern foundation.
- More forgiving ground-placement checks and reduced house placement prices.
- Chelonia's separate turtle island and snow-pet destination.

### Sovereigns and operations

- Compatible UO Store (`[store` / `[sovereigns`) and `[achievements` history.
- One-time town/dungeon discoveries, including separate facets; skill/stat/pet milestones and eligible boss/encounter Sovereigns.
- Store rewards such as dyes, bonding/shrinking tools, carrying items, runic tool, endless bandage and treasure tool.
- GitHub source, reproducible build/patch work, player documentation and organized E: backups.
- Separate-install friend testing support. Public shared-server access still needs configuration and multiplayer testing.

## Not finished / not promised for tonight

- Porting the existing character, island, custom pets, wallet/economy and all other custom systems into ServUO.
- A rehearsed save converter that preserves identities, ownership and balances.
- The entire original companion feature set in the new preview: bard/Spellweaving, resurrection, taming, assigned pets, puzzle help and full AFK automation still need porting.
- Full official-content parity for the original foundation, every mastery edge case, all bosses and full cannon/naval AI.
- Independent private-island allocation for every multiplayer account.
- Final island decoration, all interfaces, comprehensive balance and real two-player testing.
- Any requested feature absent from the implemented inventory above should not be assumed complete merely because it appeared in the conversation.

For detailed original behavior, use [PLAYER-GUIDE.md](PLAYER-GUIDE.md), [IMPLEMENTATION-TRACKER.md](IMPLEMENTATION-TRACKER.md), and the linked feature documents. For tonight, use [the modern preview instructions](../modern-prototype/TONIGHT.md).

Modern preview housing addition: [home claims/returns to a new 18x18 lodge with four secure chests, workshop and upstairs quarters. Original island/autosorting remain separate. 108 automated checks passed including housing persistence.
