# Tonight's separate modern preview

Your original Haven remains on port **2593**, with your existing character and island. The ServUO preview uses **2699**, a fresh world and a separate test character. It is not a save migration.

## On this PC

For a normal fresh character, visit the **Starter supplies** and **Arcane supplies** stones in the New Haven plaza near3501,2574 and3504,2583. These leave skills and stats unchanged. The starter kit includes30%mana-leech sword/mace, armor, shield, bow/arrows, bandages,5,000gold and a ledger; the arcane kit provides spellbooks and a reagent-saving robe. Each kit is once per character.

The plaza also has companion recruitment, travel, free carried-equipment repair, an explicitly optional test-training stone and a services guide. The old evolving-gear upgrade and custom-currency reward stones are not ported. Native town services/quests remain available.

Type **`[?`** for commands and short descriptions. Type **`[haventest`** for the two-page checklist. Click Pass, Fail or Blocked; an optional note is saved with the next result clicked. Results go to the private local `haven-playtest-results.tsv` in the preview server directory and status is stored per character. The assistant reads results during checks; this is not instant notification. This is a server gump, not a Legion script.

**Skip “Prepare test character” if you want normal progression.** That separate convenience raises all skills to120 and stats to100 each. Ordinary starter supplies do not.

1. Server folder: `D:/Uo Offline/Haven-ServUO-Preview`. Run `Start-Preview.ps1` if the preview is stopped. It detects an already running preview.
2. Client: `D:/Uo Offline/Haven-ServUO-Preview-Client/Play Haven Preview.lnk`. Use this shortcut: it sets the correct working folder. This separate copy uses modern EA Classic data and the local preview. The prepared test account contains **Haven Explorer**. Your normal TazUO profile was not changed.
3. Client login and New Haven rendering now work. The earlier Windows prompt was approved by the user; a launch-folder problem and a password truncated by the classic 16-character login field were corrected. The test-kit claim and backpack item properties were checked in the real client. Full combat/dungeon playthrough remains pending.
4. In game, type `[preview`. Select **Prepare test character** once, then equip the supplied armor, robe, weapon and shield. Read the Parry III primer to learn the mastery. The kit is deliberately for testing, not progression balance.
5. Type `[c` to claim Alden. **Roles / missions** offers Warrior, Caster, Archer and 5/15/30-minute supply runs. **Resource missions** offers Mining, Lumber, Leather, Malas and Abyss jobs; **Resource ledger** opens his book when nearby.

Use `Stop-Preview.ps1` to save and close this server. Do not copy the original ModernUO saves into it.

## Short test list

Start in New Haven with the test gear equipped. The kit has already been claimed on Haven Explorer, so check his backpack before trying to claim it again. Spend your first few minutes testing follow/stay, opening Alden's pack and changing roles outside combat. Then try an ordinary hostile before attempting a dungeon boss.

For a quick resource check, send Alden on a **five-minute Mining** mission. After it finishes, use Recall and open his Resource ledger while nearby. Expect **100 ingots** of the tier selected from his Mining skill when dispatched, plus **500 gold**. Transfer the ledger balance into your own ledger, withdraw a chosen amount, and absorb it again. Report any missing resources, duplicate rewards or failed transfer. The full-pack cases are covered by automated checks, but the menu flow needs player feedback.

For any problem, note which server you were using, your location, the command/button and what happened. A screenshot is helpful. Keep original-world problems separate from preview problems: they run different code and saves.

- Open `[preview` and `[c`; check readability and button placement.
- Have Alden follow, guard and obey `all stay`. Try `all kill` on a hostile. Check that wild tameables are left alone while guarding.
- Change roles outside combat. Check sword attacks, Magery casting and bow attacks. Tell a casting companion to stay and check that he stops.
- Put an item in his pack and take it out while beside him. This version still uses a two-tile range.
- Send a five-minute supply mission. Log out and back in after it finishes, then use **Recall**. Expect 500 gold and a report, without duplicate stacks or payment.
- Open the resource ledger from the test kit. Deposit loose resources or commodity deeds, including nested bags; withdraw a chosen amount as a deed or loose items. Transfer balances to another ledger. Stored balances consume no extra inventory slots.
- Visit Luna, Royal City, the Underworld, Doom, Blackthorn and Shadowguard from `[preview`. Check doors, stairs, NPCs, ordinary combat and loot. Dungeon completion remains a playtest task.
- Close and reopen the client. Confirm the same companion and possessions remain.

## What is verified

- 103 automated companion/preview checks passed with a clean Release build.
- Native world generation, all 25 setup stages and save/reload validation also passed from a second fresh checkout.
- 6,835 spawners, six Doom Gauntlet controllers, 17 Shadowguard instances and 14 specific Blackthorn travel links were validated.
- A socket test authenticated the prepared account and received the correct server list and game relay. This is not a graphical-client login/playthrough.
- Start, save and clean shutdown scripts were exercised against the separate preview.
- The Shadowguard patch is installed in the interactive preview; its existing world passed two starts and an intervening clean save/reload, with successful account connection probes.
- Native Doom six-stage progression/cycle reset and Britannian ship movement/cargo/owner persistence passed in a disposable test world. Fishing, naval combat and real-player dungeon combat still need playtesting.

## Still outside this port

The old island/house, wallet, custom pets, gear progression, guild bots, market and full AFK systems have not been migrated. Alden's bard masteries, Spellweaving rotations, dungeon assistance and resurrection are also not in this milestone. The original server retains those existing systems. Check the README for subsequent additions before testing.

For a friend's independent installation, use the repository's `modern-prototype/Build-Prototype.ps1 -PopulateWorld` instructions with their own modern Classic client data. Do not share this PC's client settings, accounts or saves.

## Claim your starter home
Use `[home` and confirm the free lodge claim on your chosen character. Later `[home` returns you there outside combat/travel restrictions. Four secure chests hold up to 1,000 items each. Characters on the same account share ownership. Try depositing and retrieving an item, then walking onto the spiral stairs to reach the upper quarters. This is a new 18x18 starter lodge on native land, not your old island or its automatic sorting system.

The `[haventest` checklist now spans three pages. Location-dependent service checks have **Go to Haven**, and every page has **Return to Haven**. These use the existing safe travel rules and carry nearby eligible pets. Travel and page changes preserve your draft note and never change your result. Dungeon travel starts at the Haven travel stone so you can test that service itself; choose your destination there.
- Missions now open a compact countdown with Open; expanded companion menu has Minimize. When ready, Recall from timer. His ledger can be used directly in his nearby pack. Please confirm actual following after Recall and timer readability.

Mission return update: completed missions return automatically when you are online and alive. **Recall now** brings him back early and cancels the unfinished run without its completion rewards. Manual Recall still requires being out of combat. Returned companions follow you.
