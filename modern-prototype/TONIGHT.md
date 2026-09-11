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

Use `[cc` for compact companion combat controls. Use `[havenmarks` to preview and buy six progression rewards. Completed missions earn2Marks per minute; a5minute mission earns10. Early recalls earn none. Rewards cost60-100Marks. Old-server Marks are not imported.

Player progression: 300 combined stat cap, 1,200 counted skill points. Animal Taming, Animal Lore, Focus and Snooping do not count toward that total. Individual caps still use normal powerscrolls. Use [skillbudget to check your budget. Existing trained values are preserved.

Expanded free skills: now25 total, combining InsaneUO/UOAlive lists with original Haven exemptions. See FREE-SKILLS.md or [skillbudget; this supersedes the four-skill list above.

Companion travel: Follow and idle Guard now use a faster travel pace in all three roles. Please walk/run around Haven and check he keeps up, including after a mission.

To test reward purchases without grinding: open [havenmarks, click Claim 100 test Marks (once per account), select a reward, then Buy selected reward. The allowance is optional and separate from mission earnings.

New combat loop: [minichamp -> Travel to camp -> Start a theme. Three waves and boss;20Marks,10,000gold check,250resource deed per damage participant. [haventest page4 has the new checks. See MINI-CHAMP.md.

Recovery: approach Ava at Haven plaza as a ghost for player resurrection. While alive/out of combat, stand beside Mara and double-click her (or use [recovery) to resurrect nearby owned pets, recover a dead companion from another facet, or bring your surviving last corpse to your feet. Services are free; corpse recall cannot recreate a decayed body.
Companions also automatically resurrect after about five seconds while you are online and within18tiles on the same facet, restoring health/mana/stamina and following you. Mara can recover a stranded dead companion instead.
Mara's nearby-pet option heals living pets/companions as well as resurrecting them. Automatic companion self-healing starts below80%health; critically hurt owners get first attempt below65%. Self-heals use native Greater Heal, mana and a4second cooldown. Poison or mortal wounds still block healing.
Healing and Veterinary bandage application takes at most2seconds, including self-bandaging and resurrection attempts. Success requirements and normal skill checks still apply. This is separate from the companion's four-second healing-spell cooldown.

Companion baseline regeneration restored: every3seconds,4health (unless poisoned),12stamina and8mana, capped at maximum values. This is in addition to native/equipment regeneration. Original training-level scaling is not yet migrated.

Mini champion placement now rejects native buildings across the full encounter footprint and relocates the old camp while idle. Reopen [minichamp and choose Travel to camp after the update. Active runs finish before relocation.
Companions now use bandages you put in their backpack (including sub-bags) to treat you within2tiles or themselves. One bandage is consumed per started attempt; normal Healing/Anatomy checks determine healing, curing and resurrection. All roles can use them, alongside existing spells and regeneration.
Companion access: stand within12tiles and line of sight to open his pack, use nested bags/ledger, and drag items in or out. Bandages between you and your own companion use12tiles at application and completion. Bandaging unrelated targets keeps its ordinary range. Automatic companion bandaging uses the same extended reach.
Mini-champ landmark: wooden expedition signpost, supply crate and lit lantern beside the camp. Double-click the marker to open the encounter menu.

Evolving starter gear: double-click the green Evolving starter gear and upgrades stone in Haven (3500,2572), or [startergear while beside it. Claim nine free bound pieces even if you previously claimed ordinary supplies. Equip the appropriate items to gain XP from hostile monster kills; weapons also gain hit XP, grimoire successful-cast XP. Robe upgrades cost Marks or wallet/bank gold.

Wallet: [wallet gives you an empty wallet if needed. Double-click it in your pack, Deposit pack gold/checks, enter an amount and Tithe to buy Chivalry points1:1(up to100,000). Coin withdrawals/bank transfers accept1-60,000. [tithe1000 uses wallet funds. Haven Marks are displayed from your existing character balance; no duplicate mark currency.

Haven area bonuses: +1,000real Luck and5xnatural skill-gain chance/amount below100.0. [havenluck reports these. Outside the original Trammel Haven area, normal rates/Luck apply. This does not grant untrained skills or remove powerscroll requirements.
The new [haventest checks are Evolving starter equipment (page4), Wallet and tithing and Haven Luck and training (page5). Gear/Luck checks offer Go to Haven.

Wallet update: [wallet now uses bank gold directly. Deposit pack gold/checks, withdraw coins, or tithe without wallet-to-bank transfers. Existing wallet gold merges into the bank automatically; failed migrations retain gold and retry when reopened.

Trash: [trashbag or Starter supplies > Get a free trash bag. Public trash chest near Haven3502,2570. Trash empties three minutes after the last deposit; retrieve mistakes before then. Protected items are rejected, including inside nested bags.

Emergency healer travel: [ohshit or [healer, also [recovery > Travel to healer. Ghosts can travel directly beside Ava; normal healer resurrection rules apply. Living players must be out of combat; criminal travel is blocked.
