# Codex, Warden, companion support and evolving equipment

- Claim one free Champion's Codex per character with `[codex` or Starter supplies -> Free Champion's Codex. Replacement copies cost 1,000 bank gold at Arcane supplies. Actual scrolls, skulls, mastery primers and binders are retained; nested collection obeys ownership, locks, and capacity. Stored items add no backpack slots; weight still applies. Withdraw by arrow; hover for item properties. Power-scroll combine ratios are 8x105 ->110, 12x110 ->115, 10x115 ->120; splitting reverses these. Failed operations preserve inputs.
- Old Haven Warden: original starter-boss stats restored at the original relocated site (3698,2595 Trammel), with a clickable marker, `[warden`, and travel page four. Respawns 120-180 seconds after defeat. Each damage participant receives 3-6 Marks, including pet/companion credit; corpse contains rich loot, 1,500-2,500 extra gold, two runic-enhanced gear items and a 15% special-bracelet roll. The original 20% matching-set ring drop is now restored.
- Companion support heals owner/self/eligible party members and owned pets, cures poison, offers player resurrection, and revives owned dead bonded pets. Ten mana per action, two seconds for healing/cures and ten seconds for resurrection. Native player resurrection confirmation remains. Mortal Strike and unreachable/blocked targets are rejected where applicable. Existing native bandages retain their two-second maximum.
- Caster role learns Spellweaving/Necromancy/Spirit Speak to a minimum 100, maintains native Wraith Form, uses Gift of Renewal when injured and Word of Death as a finisher alongside Magery. Native mana-drain rules apply; targets without mana cannot fuel it. The grimoire grants FC2/FCR6. Companion mage AI no longer chooses its frequent combat Teleport and adds only 0.1 seconds beyond native cast/recovery times.
- Existing companion role weapons/shield automatically receive evolving stats. Caster gets an evolving grimoire. Boarding shields retain their existing item serial and wear and also evolve. Twenty levels, 100 XP per level, capped at 1,900 XP; eligible hostile kills while equipped grant 1-20 XP according to monster health. Companion must have damaged the enemy. Level appears in item name; `[gearprogress` targets an item for XP. Progress is stored in persistent internal records; deleted-item records are cleaned up. Weapon mana leech starts at 60% and reaches 100%.
- Boarding shield remains 60 Marks. At level one: Parry10, DCI15, HCI10, Hits15, Stamina10, regen3 for each pool, LMC5 and resist bonuses10. Level20 raises Parry15, HCI15, Hits30, Stamina20, regen5, LMC10 and resist bonuses15. Native attribute caps apply.
- Mara's menu labels the existing action `Summon my corpse`. It requires proximity, life and no combat/criminal restrictions; only the existing last corpse is moved, with its contents retained.
- Double-clicking the wallet now deposits eligible pack/nested-bag gold and bank checks before opening. Only successful bank deposits consume items; repeated opens cannot duplicate credit.

Validation: isolated ContentSmoke fresh/reload and CasterSmoke exercise storage, conversion rollback, ownership, exact values, evolving items, Warden credit/loot/cooldown, healing/cures/resurrection, corpse recovery, native Wraith activation/drain, casting cadence, and wallet deposits. No test files are deployed to live Scripts.

## Matching jewelry restoration

Nine matching rings are available from Special rewards at 30 Marks each; Concord talisman costs 150 Marks. Rings copy their original bracelet attributes and skill bonuses. Equipping a matching pair enables the original theme bonus through the native AosAttributes calculation; adding Concord adds 250 Luck and 10% weapon/spell damage. Removing either matching item immediately removes the bonus. Native caps still apply.

Rings and Concord earn equipped hostile-kill XP, level 1-20 over 1,900 XP. Each level adds 5 Luck and each fifth level adds one Strength/Dexterity/Intelligence, matching original growth. Concord at level20 unlocks permanent follower capacity6, never stacking and never lowering a higher capacity. Equip and double-click to claim if it reached level20 elsewhere. Preview arrows show actual properties and progression.

JewelrySmoke exercises all nine themes through native attribute reads, unequip behavior, Concord combination, nonstacking growth/unlock, catalog prices, and save/reload identity and bonuses. Fresh and reload passed. Broader Astral, quest, Legendary and Doom progression is still pending.

Mini-champion wave enemies and bosses now disable native low-health fleeing for both melee and mage AI. Ordinary world creatures retain their own behavior.

## Codex browsing and skill-free travel

Codex now separates browsing from withdrawal: seven categories, case-insensitive skill/name search, alphabetical or quantity sorting, seven spaced rows, selected-item details, and explicit Withdraw one / Combine / Split controls. Exact recipes are displayed; insufficient quantities show how many more are needed. Selection never consumes or withdraws an item. Item properties remain on browse and withdraw arrows. Page/category/search/sort stay selected after actions and targeting returns to the menu. Different champion skull types now have separate rows. Existing stored items and serialization are unchanged.

Recall, Mark and Gate Travel require no Magery skill for players in the opted-in Haven preview. Mana, reagents or applicable equipment discounts, casting delay, spell access, and normal travel restrictions are unchanged. Other spells keep their skill requirements.

CodexMenuSmoke passed category/search/sort, distinct skulls/exact Transcendence values, selection-versus-withdrawal, combine/split quantities, and tooltip attachment checks. Native skill checks passed at zero Magery for all three travel spells; combat spell requirements and mana costs were checked. Live build and login verified separately. Client appearance still needs player feedback; automated checks do not establish visual quality.

## Skill-table Codex and combat-bar persistence

Codex opens on the requested skill matrix: one row per skill, columns for105/110/115/120, mastery volumes I/II/III, Alacrity and total Transcendence points. Eighteen rows per page, skill search and All/Stored toggle. A cell opens a compact panel with actual-item tooltip/withdrawal and explicit conversion recipe buttons. Transcendence withdrawal selects an exact existing value; it never merges or rounds the stored scrolls. The detailed category browser remains available for stat caps, skulls and binders and returns to the table.

The companion combat bar no longer gets closed by the shared companion menu-opening method. Its Menu button leaves it available alongside the full menu. Right-click dismissal is disabled; an explicit Close button remains. Open with [cc.

CodexMenuSmoke passed matrix totals, exact-value withdrawals, retained browser regressions, and combat-bar close-control checks. Native build passed. Visual appearance has not been rated from a live client screenshot.

## Advanced gear restoration

The nine special bracelets and Champion pendant now evolve through equipped eligible kills (100 XP per level, level20 at1900 XP). Each gained level adds5 Luck and each fifth level adds1 Str/Dex/Int. Existing items attach automatically at startup, preserving their original item identity and stats. Matching-set behavior is unchanged. [gearprogress reports progress.

Astral shards drop from eligible hostile kills:5% below1000 HP,15% at1000+, guaranteed three at4000+. The wallet absorbs actual pack/nested-bag shards on double-click and persists a separate shard balance. Special Rewards offers Weaver ring20, Guardian mantle40, Fortune earrings60 shards, with original attributes and level20 growth. Failed purchases preserve balances. Gold and Marks cannot substitute for shards.

Legendary random gear drops use original HP-based probabilities and a Luck multiplier up to2x at5000 Luck. Warden base chance2%; other base chances0.1%/0.3%/1%/5% at100/300/1000/5000 HP. These native equipment items receive high-intensity attributes and persistent evolution. The old island-trial-specific2% branch awaits that encounter's restoration.

Doom bosses can drop a matching-artifact reforging recipe (10%). A matching native Doom artifact,100 relevant crafting skill,100 iron ingots and20 diamonds are required. Reforging retains the item, adds original damage/regen bonuses and unlocks evolution; failed or duplicate reforging consumes nothing. Native Doom artifact rewards remain separate.

Recruit/Warden/Dreadnought shield-warrior tiers restored: shield, sword, mace, gorget and ring. Recruit first claim per piece is free, replacements250 gold; Warden40 Marks per piece; Dreadnought80 shards per piece. Hover previews show native properties. Original supported Haven quest gear gains evolution when equipped; retroactive training-quest claims and broader boss-artifact/custom currency systems are not completed by this batch.

AdvancedGearSmoke passed exact currency deposits/purchase/capacity rollback, first-claim protection,15 tier/piece factories, nine bracelet growth cases, actual kill XP/shard awards, duplicate reforge rejection, native item preservation, and fresh/reload balances and progression. Ordinary gear does not automatically evolve.

## Companion Arcane Focus
Caster mode supplies a real, immovable/nontransferable strength-6 Arcane Focus. Native Spellweaving uses this gem; no general spell-damage multiplier is added. Maintenance renews it before expiry, does not duplicate it, and removes the supplied gem when leaving caster mode. Existing caster companions receive it at startup through equipment maintenance. FocusSmoke verified native lookup, renewal, uniqueness, role switching and save/reload.


### Pet release — live 2026-09-11
Ten custom species, signature abilities and tier defenses; twelve taming mission routes; persistent rarity/legendary skill rolls; native pet training with original triple-damage pace and per-enemy quotas. Companion Pets supports assignment, riding toggle, mission parking and exact-pet reclaim tickets. Bard Tame assist uses native peace/taming and returns a ticket. Assigned bonded pets can be resurrected. Travel stone: bear den, Ancient Hunt and Chelonia sanctuary; two wild vampiric steed sites.

Validated native tame completion/cancellation, player ticket claim, pet resurrection, mission parking/return, habitat waves/spawns and native login reclaim/training persistence. Original island, complete role/mastery behavior, saved offline mission rotations and custom training ability pools are not complete. See RESTORATION-QUEUE.md.


### Regional and offline missions - live 2026-09-11
Missions includes Magery reagents, Malas necromantic reagents, Doom bones, Abyss essences and rare ingredients. The restored routes use original skill gates and material completion bonuses; 60-minute trips are selectable. Native reagents and rare ingredients are supported by the ledger.

Select a route and duration, then **Save as offline default**. Open **Offline setup** to enable repeat after logout, choose the saved route or an eligible-route cycle, and choose finish/recall on login. This is off until the owner enables it. Existing manual missions finish first. Pausing repeat does not destroy an active trip. Server downtime awards no additional invented trips.

Fresh/reload tests cover exact material credits, ledger withdrawal, ownership/skills, manual-trip preservation, repeated timer ticks, both login choices, rotation and persistent settings. Complete old role/mastery and gear/training/taming bonus parity remains separate.


### Companion roles and follower slots - live 2026-09-11
Companions use zero follower slots, including existing recruits. Ordinary pets keep their normal costs. Recruiting a companion is allowed when all ordinary pet slots are occupied.

Choose **Healer** in Missions > Roles for stronger direct heals (+15), triage, cures/resurrection and support positioning. Emergency recovery heals allied players/owned pets within six tiles for 40 + Healing/5, costs 30 mana and has a 20-second cooldown. It triggers for multiple injured allies or an owner under35% health; poisoned/mortally wounded targets need their conditions handled first. Other roles keep basic healing.

Bard restores encouragement/stat songs and native adaptive masteries. Join the owner's party for native mastery sharing. Role/mission changes and invalid owner state clean up songs and casts; assisted taming pauses mastery casting. Caster adds owner Renewal/Gift of Life and filtered native Thunderstorm while retaining Wraith Form, Arcane Focus and native ranged combat. Gift of Life's companion-owner exception does not allow unrelated player targets.

Passed fresh/reload slot migration and healer tests, native party song/cleanup tests, ranged combat and two-second bandages, actual caster buffs and connected-owner taming/cancellation regression. Broader original gear/stat growth and reward parity, plus the finished island, remain separate work.
