# Original Haven versus the modern preview

Update after the comparison: the new preview now has seven New Haven service stones (normal starter supplies, arcane supplies, companion recruitment, travel, repair, optional test training and guide), `[?` command help and `[haventest` player feedback. These do not add the missing evolving gear, currency rewards, custom pets or mini champion. Current combined suite:103checks.

Comparison date: September 10, 2026, evening build. Original: ModernUO, port 2593. New: ServUO preview, port 2699. **The preview is a modern foundation plus a small Haven port, not feature parity with the original.**

Evidence: original `Projects/UOContent/CustomBots/UOOffline` source (172 files), the original implementation tracker/player inventory, the six deployed `Haven*.cs` preview files, the preview native script tree and the 94-check test record. File counts are not feature counts or a completion percentage. Original source and historical deployment reports do not prove every feature currently works perfectly; this comparison did not playtest every original feature or decode both live saves.

“Missing” below means the **Haven-specific implementation/behavior has not been ported**, even where ServUO supplies an ordinary native alternative. Existing progress has not been imported. Later uncommitted original changes are identified separately rather than assumed deployed.

## 1. Existing progress — not migrated

- Rictor Quake, skills/caps/stats, learned masteries and quest progress.
- Original Alden, his equipment, level, trained skills, reports and assignments.
- Owned pets, training, rarity, bonds, dyes, tickets and shrunken animals.
- Backpack/bank/house possessions and all wallet, book and ledger balances.
- R.E.C. membership/leadership, guild assets, bots and market stock.
- Island, pirate headquarters, house ownership/design, storage and decorations.
- Discovery, achievement, champion and encounter history.

Haven Explorer is a separate test character. No completed save converter exists. See [migration gates](../modern-prototype/MIGRATION.md).

## 2. Companion — partial port

**Present:** one owner-bound companion, Warrior/Magery Caster/Archer, native movement/combat, Greater Heal, follow/guard/stay/attack, pet-style spoken commands, tameable exclusions, party membership, native owner damage credit and a large pack.

**Missing from the new companion:**

- Bard role and both effects of its chosen mastery; support for party pets through those songs.
- Spellweaving rotations, persistent circle/arcane-focus bonus, custom Wildfire/Thunderstorm selection and damage-to-mana behavior.
- Full cure/resurrection support and original role-specific healing/support logic.
- Companion leveling, original broad skill/stat progression, evolving equipment/weapons and gear grinding.
- Original equipment-management interface and compact separate combat-control gump.
- Twelve-tile pack access: the preview currently uses two tiles.
- Tame assist that calms, tames and returns a claim ticket.
- Assigned pets, their commands, combat/mount use, mission parking and related menus.
- Custom dungeon puzzle assistance. The new Shadowguard patch only fixes party roof eligibility and save loading; it is not puzzle-solving AI.
- Original skinning/leather-processing helpers and water-following utilities.
- Corpse auto-looting or player-loot delivery into the companion pack. Native damage attribution alone is not that feature.

## 3. Missions and AFK — partial port

**Present:** timed Supply/Mining/Lumber/Leather/Malas/Abyss jobs; 5/15/30 minutes; tiered gathering resources; a carried ledger; merged gold; offline completion; queued overflow and a simple last report.

**Missing:**

- Sixty-minute option and original extra completion bonuses/search formulas.
- Gear expeditions, offline gear assignment and equipment XP rewards.
- Taming missions, pet tickets, higher-rarity searches and bonus taming supplies.
- Sticky manual AFK mode, repeated automatic dispatch, highest-eligible-job choice and selected/mixed focus cycles.
- Full original mission catalog, including separate regional subroutes and rare Abyss ingredient route.
- Detailed multi-report journal with loot destinations, before/after stats/skills, gear changes and return summary.
- Original catch-up handling/reporting of older runs and exact resource deposit receipts.

The preview jobs are timed simulations. They do not physically send a worker into a dungeon or resource area.

## 4. Custom pets — not ported

- Haven rarity tiers, improved rare odds, rarity colors, Legendary shimmer and species presentation.
- Legendary one-slot starts and chance of over-cap starting skills.
- Rarity innate abilities separate from trained skills.
- Species signatures: elemental ground pools, chain lightning, defensive roars, healing/mana support, marks, suppression and rescue effects.
- Custom elemental immunity/resistance floors, ranged Stormscale behavior and faster custom training/leveling rules.
- Haven training/ability/cap menus, custom lore/history pages and compact hover presentation.
- Claim tickets with early bonding eligibility, ticket/shrunken lore, shrinking/statue handling and pet dyes.
- Custom pet packs and hotkey command helpers.
- Ancient Hellhound hunt, Frostbound mountable rage bear and snow den, Chelonian land/sea tortoises and custom sea mount utilities.

Native taming, Animal Lore, standard pets, pet training and power scroll mechanics exist in ServUO. Those are not missing; their presence does not reproduce Haven's custom pets or balance.

## 5. Gear and progression — not ported

- Evolving starter weapons/spellbook, earrings, cape, sash and quest equipment.
- Over-skilled Haven quest catch-up claims (`[HavenRewards`).
- Astral purchases and leveling, jewelry sets and extra follower-cap gear benefits.
- Haven random Legendary Artifact XP/leveling and special artifact growth.
- Doom artifact reforging/progression.
- Haven rewards for less-used clothing slots and expanded Haven Mark equipment.
- Original starter-to-endgame mana-leech tuning and build-specific reward progression.
- Gilded Pathfinder treasure tool, Mercy's Endless Bandage and custom useful world-drop rewards.

The preview test kit supplies strong gear and leech weapons, but that is not an earned progression system. Native artifacts, loot and crafting exist. Native “Legendary Artifact” labeling does not mean an item has Haven XP/leveling. Shield-warrior/basher work also exists in later original repository changes; its final deployment/balance is not established by this comparison.

## 6. Champions and wandering encounters — not ported

- Fast Haven mini champion, themed wave sets and resource-themed loot.
- Haven gold/scroll/mark/shard payouts, Alacrity/Transcendence additions and per-participant pack rewards.
- Custom deed/wallet/ledger routing and quicker ordinary-corpse cleanup.
- Home-island Blackwake pirate mini champion.
- Random wandering encounter system, difficulty scaling, escape/failure handling and bounded mobs.
- Encounter journal, statistics, points exchange and special tameable outcomes.

Native champion systems and scroll drops are separate from this custom content.

## 7. Currency, achievements and convenience — not ported

- Gold/Haven Mark/Astral wallet and its supported purchases, bank payments, guild fees, repairs/training/tithing integrations.
- Expanded Haven Mark exchange (documented 54 offers).
- Haven Sovereign earnings from discoveries, achievements, pets/skills/stats and eligible boss kills.
- Custom Sovereign store/catalog and achievement/discovery history; separate facet discovery awards.
- `[mystats`, original custom skill/stat budget and free-skill exceptions.
- Haven newcomer luck/training bonuses, expanded personal bank policy, recovery helpers and repair/service network.
- Searchable in-game Haven field guide and custom explanation of commands/rewards.

Native banks, money, vendors, repair, store-related code or account systems should not be mistaken for these Haven integrations. The native store's availability/catalog is not certified by this comparison.

## 8. Scrolls, runes and storage — partial port

**Present:** a new player/companion Resource Ledger, virtual balances, standard commodity-deed and loose-resource absorption, nested-bag targeting, chosen-quantity withdrawals and transfers. The new catalog has 47 resource IDs. Original balances are not imported.

**Missing:**

- Champion's Codex, its item-slot behavior, individual upgrade and supported reverse/split recipes, and old pet-scroll compatibility handling.
- Full original resource catalog and compatibility with every custom deed/item type.
- Gatherer's satchel and other custom carrying bags, gem/supply category menus and their targeted collection behavior.
- Blank recall rune pouch, Mark-on-pouch behavior and `[rune` extraction.
- House receiving/master chest, profession stores, global search/withdrawals, auto sorting and guild/house permissions.
- Original companion cleanup commands such as `[CompanionStore` for accumulated deeds.

ServUO has native PowerScrollBook, ScrollBinderDeed, Runebook and RunicAtlas classes. They do not establish parity with the custom codex or rune pouch.

## 9. Economy, bots and guild crews — not ported

- Haven Commons/community center and its custom vendor mall.
- Global `[market`, finite stock, filters, item previews and wallet purchases.
- Producer/broker economy, market quality/pricing rules and custom listings.
- Crafting/BOD workers, pet taming/supplies and supported earned dungeon goods.
- Physical dungeon crews, player join/leave interaction and custom loot routing.
- Fishing-fleet workers and boat-related market production.
- Fellowship/guild crew roster, workers, jobs, training, gear, crafting resources, work history, treasury and permissions.
- Original party-bot healing/cure/resurrection and party-pet support.

Ordinary NPC vendors, native guilds/parties and BOD systems exist. The autonomous Haven economy does not. Fully capable moving combat fleets and general dungeon-solving AI were unfinished on the original too.

## 10. Dungeons and sea content — native foundation present, custom layer missing

| Content | New preview | Haven additions missing |
| --- | --- | --- |
| Doom | Native controllers; six-stage cycle/reset and reload tested | Custom artifact evolution/reforging and reward integration |
| Shadowguard | Native 17 instances; Bar/Roof callbacks tested; bound-companion roof eligibility fix | Easy custom Orchard, original puzzle assistance, custom rewards/progression/history |
| Blackthorn | Native dungeon scripts and 14 specific travel links verified | Haven personal-credit/reward layer, custom frontier relics, bot participation/loot rules |
| Abyss/Underworld | Native modern scripts, map/setup and Imbuing | Custom thirteen-site restoration rewards, bridge/route changes, Ancient Hellhound encounter and custom travel helpers |
| Scalis/Cora/Corgul/Charybdis | Native boss classes present; not every summon/reward path playtested | Haven hunt helpers, custom evolving rewards, extra ship-deed drop rules |
| High Seas | Native galleons; Britannian movement/owner/cargo reload tested | Corsair anchored raids, exposed cargo seals, patrol board, custom turn-ins/doubloons/relics and bot fleet |
| Sea pets | Native systems are not a substitute for custom pets | Sea mount/tortoise, SOS/cargo/navigation helpers and companion water following |

The new native content may replace old compatibility work rather than require a literal port. Preserve the requested experience and rewards while using the modern implementation where suitable. Boss-class presence alone does not prove a live spawn or complete encounter works.

## 11. Travel, home, housing and decoration — not ported

- Haven Atlas/travel-book destinations and custom recovery/service routes; `[preview` only provides ten test destinations.
- Owner `[home`, island assignment/recovery and the saved island layout.
- R.E.C. pirate headquarters, its castle-sized plot/design, interlinked buildings/floors and direct floor access.
- Workshops, storage, guild/map rooms, barn, cookhouse, shed, gardens, orchard, gathering areas, docks and patrol board.
- Home mini champion and Chelonia/snow custom destinations.
- Island decoration, paths, roofs/windows/porches, furniture placement and all later artistic refinements.
- Custom housing placement flexibility/prices and installed refillable station arrangement.
- Treasure-map location library, decoded-map targeting/rune lookup, house-library option and custom Malas/Ter Mur routing changes.

Native custom housing, veteran rewards, kitchen-set items, maps and Runic Atlases exist. Your built property, custom storage and island do not. Later library/treasure-site changes in the original repository need separate deployment confirmation. Island visual refinement was unfinished; the user has now requested an independent critic and iteration to 8/10 when work resumes.

## 12. Not missing from the new preview

- Six modern facets, populated towns/vendors/spawns and native expansion foundations.
- Native player masteries/Book of Masteries, modern spell systems and pet training scripts.
- Native Imbuing, modern crafting/artifacts, veteran/kitchen items and galleons.
- The basic companion, spoken orders, three combat roles, owner healing and party damage credit.
- Six timed mission categories and functional player/companion resource ledgers.
- Start/save/stop tooling, separate client shortcut, GitHub source and verified E: backups.

These areas still have manual gameplay/multiplayer checks pending. The 94 automated checks are focused regression tests, not proof of complete game parity.

## Suggested port order

1. Inventory/export original progress and implement a rehearsed converter; keep the original playable throughout.
2. Wallet/bank/storage/codex and gear/pet saved types, so possessions can survive conversion with correct balances and owners.
3. Companion leveling/equipment, Bard/Spellweaving, resurrection, AFK/reporting and taming/assigned pets.
4. Custom pets, evolving gear, Haven rewards, mini champions and wandering encounters.
5. Home/island/storage, travel, achievements/store and world service placement.
6. Bot/guild economy, physical dungeon/naval crews and final balance/visual review.

This is an ordered backlog, not a commitment that each stage fits one session. For tonight's usable subset, see [the playtest checklist](../modern-prototype/TONIGHT.md).

