# Haven world and service repair

## New Haven recovery

`[bank` (also `[ohshit`) is now available to players and teleports living characters and ghosts
to New Haven bank. The former GM bank-inspection command is `[BankBox`.
Arrival is within two tiles of Elias Thorne, the free resurrection healer. Silas Grey recovers corpses and Mira Willow resurrects bonded pets. All three remain beside the bank and occasionally speak when a player is nearby.
Double-click the healer for free resurrection, or the summoner to confirm free
recovery of the character's most recent surviving corpse. The original corpse
and remaining loot are moved; decayed bodies and removed items are not recreated,
and another player's corpse cannot be claimed. Repeating setup does not duplicate
the NPCs. The service row has wider spacing, lamps and a planter, with the portal away from the arrival point. Every animal trainer also provides free pet resurrection: double-click them or choose Resurrect from their context menu and target your nearby dead bonded pet. No pet skills are lost. Native changes are reproduced by patches 0013 and 0014.

## Reduced bot density

Player-bot spawners now fill to two-thirds of their saved counts, rounded to the
nearest whole bot, retaining at least one at small nonempty spawners. The 1,600
base target becomes an effective target of 1,067. Daily session and party limits
use that effective target. Fixed-role bot spawners are scaled too. PK bots are
disabled (zero density): saved PK spawners cannot refill, automatic PK placement
is disabled, and a bot assigned the PK behavior is removed before its next action.
Ordinary NPC/monster spawners are unaffected. Counts are not rewritten, so
restarts do not compound the reduction. `[SetBotPopulation` edits the base
target; its messages also show the effective target.

This update targets `haven-rc4` and its existing saved items. It does not migrate
the world to the separate `modern-evolution` development line.

## What changes

- On startup, missing native vendor, wildlife and dungeon spawners are restored
  in small batches. Existing spawners and their living NPCs are preserved;
  stopped spawners restart and empty ones repopulate.
- Trammel uses the New Haven spawn layout. Other enabled facets retain the
  matching expansion data. Twenty-six Haven quest instructors and three groups
  of beginner animals, mounts and practice creatures are repaired explicitly.
- Every bank represented by a native banker spawn receives a supply stone,
  upgrade stone, reward stone, pet hitching post and dungeon portal. Placement
  searches for walkable tiles and preserves existing nearby service objects.
- `[GmPanel` is a 620 × 560 window with World, Spawn bots, Travel and Cleanup
  tabs. **Repair missing creatures, NPCs and bank services** reruns the repair;
  **Refresh progress** displays the current counts and errors.
- `[WorldStatus` displays progress. `[RepairWorld` is the GM command equivalent
  of the repair button. World repair runs automatically without either command.
- Stone purchases use four illustrated rows per page with
  Previous, Next and Close controls. Purchase menus remain open after buying.
  Upgrades display the next tier and cost before the player chooses to buy.

Allow about a minute after startup for the queued population to finish; duration
depends on machine speed and how much is missing. Errors are counted in the
status and detailed in the server log. The normal world save persists changes.

## Validation

Built against the pinned ModernUO commit
`e7f85d404d52e0def1fb342b3dc185894a57017d` with all Haven patches.
The regression suite passes locally, including native map placement using the
installed 7.0.23.1 client data. The tests check bank service coverage and repeat
placement, a live banker surviving repeat repair, living Haven instructors and
training creatures, expansion selection, tab bounds and long-menu navigation.

CI runs the same suite. Tests requiring copyrighted client map data
are skipped when that data is unavailable. Set `MODERNUO_TEST_DATA_DIR` to a
client data folder to run them locally. The suite also needs
`HAVEN_WORLD_DATA` pointing at ModernUO's Distribution directory and the
`SolutionDir` MSBuild property set to the ModernUO root so native test data copies.

In-game acceptance: wait for `[WorldStatus` to finish, visit New Haven and a bank
on another enabled facet, buy supplies, inspect the upgrade cost, browse dungeon
destinations, and verify that repeating Repair World preserves existing NPCs.

## Permanent companion

Use `[companion` to claim Alden Ashford, recall the same follower, and open the companion menu. Double-click the companion to open the menu without recalling. One companion is linked to each character and uses one follower slot. Recall preserves the companion and shared inventory, including when claimed from the owner's stable record.

The menu provides Follow, Guard, Stay, Attack, Heal/Resurrect, Shared Pack, Join/Leave Party, and Fighter/Healer/Bard roles. Standard pet speech orders also work. This is game AI with buttons and orders, not a free-text chatbot. Companions assist against monsters, never players or their controlled pets. All roles can heal and offer a resurrection confirmation; healers act faster and bards maintain temporary strength, dexterity and intelligence songs for nearby party members. Support requires proximity, mana and cooldowns. Bard songs expire and do not stack with another companion's song.

Training earns one mastery minute per elapsed minute, including offline time and server downtime; successful combat gives additional practice at most once per ten seconds. Saved timestamps prevent double credit. Mastery and level continue growing without a configured gameplay cap. Native skill storage tops out at 6,553.5; separate mastery continues improving combat and support after that display ceiling. Offline growth represents training, not unsupervised monster kills or generated loot.

The companion is bonded, takes no wages, retains equipment on death, and can be resurrected by Mira or a stable master. The owner can deposit and retrieve items through the shared pack (1,000 items and 50,000 stones). Storage access requires being within three tiles. Ownership, role, training and inventory persist in world saves.

## Wallet cash withdrawals

Double-click the wallet in your backpack. Choose Deposit to store carried gold, or enter an amount and choose Withdraw to receive physical gold. Withdrawals accept 1–60,000 coins per transaction and respect backpack capacity. Invalid amounts, insufficient funds, a foreign wallet or a full backpack leave the balance unchanged. Stone purchases continue to use wallet funds first.

## Repair benches

A labeled repair bench is placed beside the New Haven stones and supplied at other town banks. Double-click it and choose Repair or Restore, then select your carried or worn weapon, armor, shield or clothing. Repairs cost 50 gold per damaged item, paid from wallet/backpack/bank funds, and restore current durability to the item's existing maximum; attributes and maximum durability remain unchanged. The bench checks ownership and distance again when the target is chosen.

Restore costs 250 gold and raises maximum durability to the item type's normal maximum, including existing durability bonuses, then fully repairs it. It never reduces a higher custom maximum, changes bonuses, or repeatedly increases maximum durability. Items already at their type maximum are not charged.

Starter progression equipment (including upgraded starter weapons and the Haven robe) is exempt from both repair and restoration fees. Its progression and bonuses are preserved.

## Companion control and bard update

The companion panel is now 370 x 370 with Orders, Stats and Role tabs. Attack, Pack and out-of-range commands redisplay the controls; Recall brings the existing companion back. Stats show health, mana, stamina, attributes, damage, resistances, relevant skills, mastery and pack usage.

Companions use a 0.1-second decision and movement interval to keep up while following. Existing companions initialize the new speed and bard/support skills on their next AI tick. Worn equipment is repaired during setup and does not lose combat durability while worn by a companion (patch 0015). Player equipment retains normal wear.

Bards attempt the native Discordance skill against their current hostile monster every 12 seconds, requiring 60 Musicianship and Discordance. Attempts respect native difficulty, immunity, range, visibility and existing Discordance effects; the companion carries its own instrument. Stat songs require 80 Musicianship and Peacemaking. Higher skill and mastery strengthen buffs; songs expire after 20 seconds and refresh without stacking.

Every role automatically heals or cures the owner before supporting party members, within three tiles and line of sight, with 10 mana available. Healers act every 8 seconds; Fighters and Bards every 20 seconds. Native Discordance also expires after the companion dies or leaves the Bard role (patch 0016).

Vampiric steeds now spawn in the northern island wilderness at 3675,2410. Existing wild Old Haven steeds and their spawner migrate there; owned pets stay with their players. Throughout Haven Island on Trammel, wild steeds have a 120 HP ceiling, melee-only AI and 60% reduced melee damage capped at 8. Taming or leaving the island removes those limits without overwriting their underlying health or damage stats. Tamed steeds receive 180–210 Dexterity and maximum Stamina, including existing pets; repeated checks preserve the roll and do not refill stamina. The Warden remains at 3670,2587 with the same boss rewards and respawn timing.

New Haven services now surround the existing contribution monument at 3506,2576: stones along the northwest edge, recovery NPCs to the east, and repairs and pet storage near the southern edges. The existing monument, benches and lamps remain; two small flower pots replace the old added lamp row. Existing service objects and NPCs move rather than being duplicated. Bank and emergency travel still land immediately beside Elias, now in the square.

Wallet and companion expansion:
- Wallet double-click deposits backpack gold and sweeps visible, movable loose gold within eight tiles, respecting line of sight and excluding secured items. It opens no menu. Say `withdraw 1000` or use `[withdraw 1000`; `[wallet` opens balances, withdrawals and Astral treasures. Spoken withdrawals use wallet funds and do not also charge a nearby banker.
- Ordinary NPC merchant purchases use wallet, backpack and bank gold together. Gear upgrades already use this payment path; the upgrade screen now says so.
- Astral shards are personal kill rewards for nearby eligible players with native looting credit against untamed hostile monsters with at least 100 HP. Chance: 5%, rising to 15% at 1,000 HP; monsters with 4,000+ HP grant three. Wallets store shards separately. The treasure shop offers a Weaver ring (20), Guardian mantle (40) and Fortune earrings (60), with stats and purchase previews. Existing wallet gold migrates intact.
- Companion roles now include Caster and Archer. Casters use native Magery damage spells, Gift of Renewal, and Word of Death/Gift of Life at 80 Spellweaving, with automatic safe targeting. Archers use native ranged AI and do not consume ammunition. Native skills, offline training and role persistence continue.
- All roles regain at least 4 health, 12 stamina and 8 mana every three seconds, increasing with training. Poison suppresses the extra health regeneration. Healing consumes no bandages.
- Gear tab equips owner-supplied weapons, armor, clothing and jewelry. Swapped equipment goes into the shared pack. Equipped gear remains protected against wear; companions do not independently acquire upgrades.
- Shop buttons, icons and names show stat tooltips, with full click-through previews retained.
