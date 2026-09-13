# Special items, training menus and pet exchange

Deployed to the preview on 2026-09-11 at approximately 23:29 America/New_York. Native patch 0021 applied. Server online on port 2699; authenticated login passed.

## Player changes

- Special Rewards now groups Weapons, Armor & clothing, Jewelry, Travel & storage, and Special tools. All rewards remains available. Purchases use the original catalog indices and preview item; changing categories never spends currency.
- Shared menu labels preserve apostrophes instead of displaying `&apos;` or `&#39;`. Stone buttons use gray art 5058 instead of black art 2624.
- Cyclone Axe (Swordsmanship; secondary Whirlwind, primary Double Strike) and Tempest Staff (Mace Fighting; primary Whirlwind, secondary Paralyzing Blow) level to 20 with visible equipment XP. Both cost 150 Marks. They join the existing area-weapon selection inside the legendary loot roll without increasing its overall probability. Existing elemental weapons remain; Cindermaul also has native Whirlwind.
- Pet training displays all ten stat choices, separated category/selection panels, and native Plan and Info screens. The confirmation shows property weight, requirements, current/result/limit and point balance, with adjustable increments and an explicit confirmation. Planning records selections without consuming points or scrolls. Actual purchases retain native eligibility/caps and fresh server checks.
- Animal Lore includes Chivalry, Bushido and Ninjitsu alongside existing combat/magic skills, native regeneration calculations, colored elemental values, rarity and an abilities summary. Finish-stage confirmation discards unused stage points; this is not a reset of already purchased training.

## Restored original special items

Original utility discovery probability: 0.2% on an eligible credited kill, selecting equally among Gilded Pathfinder, Mercy's Endless Bandage, resource satchel and Tidebound charter. Original clothing interval remains 2.5%.

- Mercy's Endless Bandage: 500 Marks. Native bandage healing, cures and resurrection checks; no consumption.
- Gilded Pathfinder shovel: 750 Marks. Target an owned, decoded, unfinished treasure map to travel beside the site. Normal digging and guardians remain. Companion follows the shared travel helper.
- Wayfarer's rune pouch: 10 Marks. Store ordinary blank runes, withdraw via menu or `[rune`, or cast Mark on the pouch to receive a marked rune. Personalized/marked runes are excluded.
- Resource satchel: restored 75-Mark option, existing gold option retained.
- Cartographer's map chest: 40 Marks. Place at home; search and collect actual maps, SOS messages and bottles from owned bags, withdraw originals. Other items stay put.
- House treasure-map travel library: 100 Marks. Trammel/Felucca catalog plus original terrain-checked Malas/Ter Mur sites; decoded map matching on any native supported facet. House access and safe landing checks apply.
- Tidebound charter: original discovery drop. Claims a one-slot sea horse. Mount for swimming and +10 effective Fishing; `[tide` opens cargo, SOS navigation and fishing-tool controls. Dismount restores the previous swimming state and removes the skill modifier; unsafe water dismount returns to last safe land. Navigation cancels on manual movement, combat, casting, disconnect, obstruction or invalid SOS. Fishing and SOS recovery remain native.

## Mission pet exchange

Use `[petexchange` or Missions > Pet exchange. Only never-claimed mission tickets in the owner's backpack qualify. Claimed pets stored again have a separate origin and are rejected. Exchange and redemption each require explicit confirmation.

- Ticket credits: Normal 1, Rare 3, Epic 8, Legendary 20.
- Redeem: 10 credits for Rare-or-better, 30 for Epic-or-better, 80 for Legendary.
- Choose among the six rare-pet mission species unlocked by the owner's companion's trained Taming and Animal Lore. New tickets have the chosen minimum rarity before their original rarity/stat rules are applied; no extra bonus supplies.
- Credits persist on the character's account tag, separate from Marks. Full backpack/insufficient credit/unqualified species consumes no credits. Duplicate exchange requests cannot award twice.

## Verification and limits

Isolated server tests cover native Mark casting, failed casts, unlimited native bandaging, shovel/map preservation and companion travel, mount/dismount Fishing and swimming, mounted cargo ownership, map storage ownership and collection, utility drops, exchange duplication/stored-pet exclusion, rarity guarantees, menu construction and existing companion/travel/combat regression.

Client appearance still requires in-game review; no visual acceptance is claimed. SOS routing retains the original path follower and stops when terrain blocks it. The new native server's pet training rules remain authoritative; no destructive whole-pet respec was invented from the screenshot's Reset Training label.

## Restart record

- Reason: deploy restored special-item functionality, mission pet exchange, Whirlwind leveling weapons and associated menu changes together.
- Clean save and stop succeeded; original server was not affected.
- Backup: `E:/Backups/Haven/Prototypes/servuo-before-special-pet-exchange-20260911-232810`. All 43 save files compared by SHA-256 and matched.
- 22 production source files copied; only the preview received native patch 0021 (Mark target and sea-horse rider lifecycle). Test files were not deployed.
- Isolated regression log reached COMPLETE; live build: zero warnings, zero errors.
- Startup listener and authenticated account/server-list/game-relay probe passed. No in-game visual validation claimed.
