# Haven play-test checklist

Use a normal **Player** character for gameplay checks. Staff accounts bypass restrictions and can hide bugs. Test on your own fresh world; keep a save before testing death, travel or housing. Mark each item PASS / FAIL / NOT TRIED and include the test ID when reporting a problem. You do not need to finish this in one sitting.

## Start here — one short session

- [ ] **S00 — Field guide.** Use `[guide`, select chapters and search for wallet, pet, mastery and repair. Clear restores all chapters. Claim the free book once, reopen it, and confirm a second claim does not duplicate an existing pack/bank book. Check readability and scrolling at your usual client resolution. Existing characters can open the guide after previously claiming their starter bundle.

- [ ] **S01 — Bank recovery.** Use `[bank` and `[ohshit`. Arrive beside the New Haven resurrection NPC, on a reachable tile. Confirm the corpse summoner and pet resurrection service are reachable.
- [ ] **S02 — Free starter claims.** Use `[StarterKit`, `[Cape`, `[Sash` and `[HavenRewards` where eligible. Inspect earrings, caster and fighter items. Repeating a claim must follow its eligibility rules rather than produce unlimited rewards.
- [ ] **S03 — Shop previews and wallet.** Inspect a bracelet, spellbook and power scroll before buying. See useful stats rather than weight alone; one purchase deducts the displayed wallet amount once and delivers the item.
- [ ] **S04 — Ledger deposit and transfer.** Put a Resource Ledger in your companion's pack. Finish a resource mission. Check the deposited quantity in his report and ledger. Choose **Transfer all...**, target your ledger, and confirm his balance becomes zero and yours rises by exactly that amount. Repeat: no duplication.
- [ ] **S05 — Backpack slots.** Compare backpack item count with empty and filled books. Each Resource Ledger and Champion's Codex uses only one slot. A withdrawn scroll/deed uses a normal slot; putting it back frees that slot.
- [ ] **S06 — Companion controls.** Open `[c`, switch Orders / Stats / Role / Tasks, issue Follow, Guard and Stay. Ordinary commands leave useful controls available. Stats reflect the actual companion, not stale values.
- [ ] **S07 — One real fight.** Switch to a combat role, fight near Haven and move around. The companion keeps up, engages the intended enemy, recovers stamina/mana and heals you when capable. Try a doorway and a turn around a building.
- [ ] **S08 — Save and return.** Save, stop normally and restart your own server. Confirm your companion, books, balances, equipment and quest claims remain intact.

## Companion combat and masteries

- [ ] **C01 — Melee / archer / caster.** Fight with each role. Confirm the correct weapon or spellbook is equipped, ranged roles use useful distance, and switching roles does not lose equipment. Check disarm recovery after the equip block ends.
- [ ] **C02 — Healing and resurrection.** Let yourself and an owned pet take damage. Confirm capable companions heal, cure and resurrect appropriately. Check the companion's own death/recovery and Refresh controls. Healing/Veterinary should finish within the configured two-second maximum.
- [ ] **C03 — Spellweaver.** Watch several fights with clustered enemies and one isolated enemy. Confirm useful spell selection, including Thunderstorm/Wildfire when suitable; the persistent arcane-focus bonus and damage-to-mana support should work without constantly restarting casts.
- [ ] **C04 — Bard pair.** At sufficient skills, the bard should maintain both effects for its chosen mastery, avoid constant instrument spam, and support you and your party's owned pets. Watch buff icons and actual stat/regen changes.
- [ ] **C05 — Buff cleanup.** Walk beyond song range, change maps, leave the party, change the companion's role or send him on a mission. Old effects must end. Return and check that useful buffs resume.
- [ ] **C06 — Mastery book.** Buy the regular Book of Masteries and primers from Training Supplies. Learn a primer, select the skill and use both active abilities. Unlearned abilities and the mastery-switch cooldown should be enforced. Passive entries should not pretend to be castable spells.
- [ ] **C07 — Saving Throw.** With a learned, selected weapon mastery and sufficient base skill, test repeated disarms. Some should be blocked, not every attempt. Switch away and confirm the protection disappears.
- [ ] **C08 — Resilience.** Compare bleed, Mortal Strike and Curse with and without the song. Durations should shorten while protected. Poison resistance is a chance, not immunity. Party pets should benefit; the protection must stop out of range.
- [ ] **C09 — Gear progression.** Compare companion equipment before and after combat and missions. Special evolving gear improves across roles; ordinary gear must not start evolving merely because it was equipped by a player.

## Missions, books and storage

- [ ] **M01 — Normal mission.** Try a five-minute gathering run. Resources arrive as supported deeds or ledger balances, without another reward bag. The report identifies quantities, delivery destination, training and gear changes.
- [ ] **M02 — Duration.** Compare five- and fifteen-minute missions. Confirm the displayed duration, completion bonus and early-return behavior. A failed taming mission must not claim a pet was delivered.
- [ ] **M03 — Regional resources.** Try Malas reagents, Doom bones, Abyss essences and Abyss ingredients once eligible. Verify the displayed skill requirement and actual material types. Absorb and withdraw them from the Resource Ledger.
- [ ] **M04 — Manual AFK.** Enable AFK, perform an unrelated player action and confirm the companion stays in AFK mode until you turn it off. Confirm the selected focus repeats or Mixed cycle varies eligible missions.
- [ ] **M05 — Offline report.** Arm an offline assignment, log out for at least one mission, then log back in. The companion returns and a report opens. Reopening the report or relogging must not award the same loot again.
- [ ] **M06 — Taming tickets.** Select an eligible pet, complete missions, lore the ticket, claim the pet and feed it. Check previewed stats match the pet and it is ready to bond. Common species should not receive custom rarity powers.
- [ ] **M07 — Scroll management.** Collect scrolls into the codex; withdraw one chosen skill/tier. Combine one recipe and split one higher scroll. Unselected skills and tiers stay intact. Try with a full backpack: internal combination works; extraction requires a free slot.
- [ ] **M08 — Capacity failures.** Try withdrawing into a full pack and transferring into the same book. No resources disappear or duplicate. Close/cancel the target cursor; books remain usable.
- [ ] **M09 — Carrying bags.** Put supported resources into the resource satchel and compare carried weight. Remove them and check the full weight returns. Try unrelated equipment: it should not gain resource-only weight reduction.

## Haven, pets and progression

- [ ] **H01 — Services.** Visit multiple town banks and the Commons. Stones, travel, repair, resurrection and stable services are reachable without blocking the town-center walkway.
- [ ] **H02 — Payments.** Try an ordinary merchant, stone purchase, gear upgrade, stable payment and `[tithe` using wallet gold. Withdraw physical gold, then verify the wallet balance. Double-click pickup should collect eligible nearby gold and supported currency.
- [ ] **H03 — Repairs.** Use Repair all with several damaged equipped/carried items. Check the displayed fee, no durability loss on ordinary repairs, and the separate restoration option. Unsupported items should not consume a fee silently.
- [ ] **H04 — Player caps.** Check `[mystats`: base stat cap 300 and counted skill cap 1,000. Taming, Animal Lore, Focus and Snooping should be excluded from the counted skill total.
- [ ] **H05 — Newbie island.** Check the +1,000 luck benefit in its intended area, faster training to 100 and removal outside the area. Find the Old Haven boss on reachable ground; inspect Haven marks, gold and themed gear rewards.
- [ ] **H06 — Mini champion.** Complete a Haven mini champ. Check themed waves, progression speed, personal rewards, larger gold payout, marks/shards and resource deeds. Ordinary corpses should clear faster than the boss corpse. Alacrity/Transcendence are possible rewards, not guaranteed each run.
- [ ] **H07 — Evolving rewards.** Check starter, Haven quest, Haven jewelry, Astral and compatible Legendary Artifact drops before and after progress. Check set bonuses and any earned follower-cap bonus. Ordinary random equipment should remain ordinary.
- [ ] **P01 — Pet training.** Lore a pet, Begin Training, gain progress, then buy a stat/skill/ability upgrade. Skill caps should follow 105/110/115/120 scroll tiers and consume the correct scroll. Maxed pets should not spend points on ineffective upgrades.
- [ ] **P02 — Shrinking and bonding.** Lore both a ticket and a shrunken pet. Use a leash/house post, restore the pet and verify its ownership, training and pack contents. Release a disposable pet and confirm ownership actually clears.
- [ ] **P03 — Species identity.** Compare custom pets of different types and rarities. Observe their distinct effects, rarity colors and over-cap outcomes where rolled. Legendary custom pets begin at one slot; common species should not become custom Legendary pets.
- [ ] **P04 — World pets.** Find the snow bear, Ancient Hellhound encounter, vampiric steeds and Chelonian pets. Check reachable spawns, respawns, taming, feeding and pet dye behavior. Island steed wild tuning and tamed stats should differ as intended.

## Dungeons, exploration and the market

- [ ] **D01 — Doom.** Enter the gauntlet through its normal route, complete rooms/bosses, earn an artifact and use supported reforging. Leave and return; a partially completed event should not be reset by setup.
- [ ] **D02 — Shadowguard.** Use the original Eodon entrance. Complete Bar, Orchard, Armory, Fountain and Belfry, then Roof. Check stairs/platforms and companion puzzle assistance. Orchard hints should make the tree puzzle straightforward. Rewards and exits must work after victory and failure.
- [ ] **D03 — Blackthorn.** Enter through the Trammel castle stairs. Fight the invasion wing, captains and beacons; inspect personal credit/rewards and return through the stairs.
- [ ] **D04 — Abyss.** Use the restored crafting sites and routes, collect essences/ingredients and try a supported artifice recipe. Full native Imbuing and all official SA bosses are not part of this test build.
- [ ] **D05 — Travel and treasure.** Use the travel book's compact destination pages and dungeon entrances. Test a decoded map with the treasure-travel reward, then dig it normally. Check invalid-map handling and companion following.
- [ ] **D06 — Encounters.** Complete one wandering encounter and flee another beyond its boundary. Check cancellation, reward chest/loot, journal, difficulty progression and point spending; no stale mobs or repeatable duplicate rewards.
- [ ] **D07 — Islands and sea.** Use `[home`, visit the dock and housing plot, harvest resources and test boat clearance. Ride the supported water mount, open its cargo, fish an SOS/net and check the companion follows. Try the anchored pirate raid and leave with earned cargo.
- [ ] **E01 — Market.** Use `[market`, search/filter, inspect an item and buy it with wallet gold. Verify the exact item, price and finite stock. Check different trades and BOD-related stock over time.
- [ ] **E02 — Guild workers.** Use `[guildcrew`, recruit, assign gathering/crafting/training and inspect work history. Try a party adventure, then confirm workers resume their jobs. Some recipes still lack automatic provisioning of non-catalog supplies.

## Two-player checks — before opening a shared server

- [ ] **N01 — Ownership.** A second account cannot withdraw another player's books, companion pack, guild treasury or island assets. Test officer permissions separately.
- [ ] **N02 — Group rewards.** Two players and their companions contribute to a mini champ and dungeon battle. Each eligible player receives credit/rewards once; an uninvolved nearby character should not receive them.
- [ ] **N03 — Party support.** Healing, resurrection and bard effects reach the other party member and their pets, then stop when that member leaves or moves out of range.
- [ ] **N04 — Contention.** Both players try to buy the last market item. Exactly one purchase succeeds and only that buyer pays. Try competing dungeon entry and leadership changes.
- [ ] **N05 — Disconnects.** One player disconnects during a mission or encounter. Confirm cleanup, return location, credit and saved ownership; one player's departure must not silently corrupt the other's progress.

## Bot market and dungeon courtesy

- [ ] **M01 — Pet market.** Inspect a ticket before buying, purchase using wallet gold, claim with free slots, and confirm identical stats. A full follower allowance must preserve the ticket.
- [ ] **M02 — Minax notes.** Withdraw with `[minax 10`, redeem it, and confirm an exact round trip. A full backpack must not consume credits.
- [ ] **M03 — Join or leave.** Meet an unassigned dungeon bot. Accept once and check native party membership. Decline another offer or let it expire; that crew should leave.
- [ ] **M04 — Loot delivery.** Kill enemies with grouped bots. Their collected loot should reach your nearby companion, then your own pack when the companion cannot accept it. They must not loot an unrelated player's corpse.
- [ ] **M05 — Real dungeon work.** Observe a bot crew fighting or solving its room; inspect market stock after successful encounters. Time alone must not create dungeon rewards.
- [ ] **M06 — Room ownership.** Confirm a bot-only Shadowguard room yields when you arrive at the entrance, and that a human-owned room is never reset by bot cleanup.

## Report a problem

Send: test ID, exact steps, expected/actual result, character and companion names, approximate time, map/coordinates, item name/serial if available, screenshot or journal text, and whether it repeats. Include the source commit from `git rev-parse --short HEAD`. If an item duplicates/disappears or the server crashes, keep the save/logs and report it before repeating that action extensively.

The automated suite verifies mechanics and persistence; it does not certify all client artwork, pacing, pathfinding or multiplayer fairness. Official-complete Shadowguard/Blackthorn loot ports, native Imbuing, moving ship/cannon AI and a private island for every multiplayer account remain future work.


## Scalis, Cora and Corgul

See [THREE-BOSSES.md](THREE-BOSSES.md) for commands, reward tables and the five in-game checks. Automated validation covers terrain routes, offerings, reward eligibility, artifact growth, persistence and forge recipes; visual combat balance still needs play-testing.
