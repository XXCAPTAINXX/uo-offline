# Tonight's separate modern preview

Your original Haven remains on port **2593**, with your existing character and island. The ServUO preview uses **2699**, a fresh world and a separate test character. It is not a save migration.

## On this PC

1. Server folder: `D:/Uo Offline/Haven-ServUO-Preview`. Run `Start-Preview.ps1` if the preview is stopped. It detects an already running preview.
2. Client: `D:/Uo Offline/Haven-ServUO-Preview-Client/Play Haven Preview.lnk`. Use this shortcut: it sets the correct working folder. This separate copy uses modern EA Classic data and the local preview. The prepared test account contains **Haven Explorer**. Your normal TazUO profile was not changed.
3. Client login and New Haven rendering now work. The earlier Windows prompt was approved by the user; a launch-folder problem and a password truncated by the classic 16-character login field were corrected. The test-kit claim and backpack item properties were checked in the real client. Full combat/dungeon playthrough remains pending.
4. In game, type `[preview`. Select **Prepare test character** once, then equip the supplied armor, robe, weapon and shield. Read the Parry III primer to learn the mastery. The kit is deliberately for testing, not progression balance.
5. Type `[c` to claim Alden. **Roles / missions** offers Warrior, Caster, Archer and 5/15/30-minute supply runs. **Resource missions** offers Mining, Lumber, Leather, Malas and Abyss jobs; **Resource ledger** opens his book when nearby.

Use `Stop-Preview.ps1` to save and close this server. Do not copy the original ModernUO saves into it.

## Short test list

- Open `[preview` and `[c`; check readability and button placement.
- Have Alden follow, guard and obey `all stay`. Try `all kill` on a hostile. Check that wild tameables are left alone while guarding.
- Change roles outside combat. Check sword attacks, Magery casting and bow attacks. Tell a casting companion to stay and check that he stops.
- Put an item in his pack and take it out while beside him. This version still uses a two-tile range.
- Send a five-minute supply mission. Log out and back in after it finishes, then use **Recall**. Expect 500 gold and a report, without duplicate stacks or payment.
- Open the resource ledger from the test kit. Deposit loose resources or commodity deeds, including nested bags; withdraw a chosen amount as a deed or loose items. Transfer balances to another ledger. Stored balances consume no extra inventory slots.
- Visit Luna, Royal City, the Underworld, Doom, Blackthorn and Shadowguard from `[preview`. Check doors, stairs, NPCs, ordinary combat and loot. Dungeon completion remains a playtest task.
- Close and reopen the client. Confirm the same companion and possessions remain.

## What is verified

- 90 automated companion/preview checks passed with a clean Release build.
- Native world generation, all 25 setup stages and save/reload validation also passed from a second fresh checkout.
- 6,835 spawners, six Doom Gauntlet controllers, 17 Shadowguard instances and 14 specific Blackthorn travel links were validated.
- A socket test authenticated the prepared account and received the correct server list and game relay. This is not a graphical-client login/playthrough.
- Start, save and clean shutdown scripts were exercised against the separate preview.

## Still outside this port

The old island/house, wallet, custom pets, gear progression, guild bots, market and full AFK systems have not been migrated. Alden's bard masteries, Spellweaving rotations, dungeon assistance and resurrection are also not in this milestone. The original server retains those existing systems. Check the README for subsequent additions before testing.

For a friend's independent installation, use the repository's `modern-prototype/Build-Prototype.ps1 -PopulateWorld` instructions with their own modern Classic client data. Do not share this PC's client settings, accounts or saves.
