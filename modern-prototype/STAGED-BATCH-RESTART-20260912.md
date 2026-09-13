# Preview deployment — September 12, 2026, 01:36 EDT

User authorized deployment of all staged work. Preview server is online on port 2699.

## Save, backup and validation

- Clean save and shutdown completed; original server untouched.
- Backup: `E:/Backups/Haven/Prototypes/servuo-before-staged-batch-20260912-013544`.
- All 43 save files matched SHA256 against backup.
- Fourteen custom production files deployed, plus native patches 0022 (tailor stock) and 0023 (Animal Lore tickets).
- Isolated regression completed, including stored-pet exchange, Legendary rejection and 3–5 Legendary skill counts.
- Corrected obstructed Blackthorn landing after native terrain checks. Final startup reported no blocked passage pair.
- Live Release build: zero warnings, zero errors. Authenticated login, server list and game relay passed.
- No manual client visual/audio or full interactive gameplay verification was performed.

## Changes deployed

- Rictor Quake's existing companion gets a one-time female appearance and name Jenna Ashford. Existing skills, items and missions remain. `[companionfemale` is also available.
- Occasional private encouragement, 8–15 minutes apart while nearby; Jenna uses lightly flirty lines.
- Double-click a nearby owned companion for the native paperdoll alongside controls. Movable worn equipment can be removed; supplied role equipment remains protected.
- Equipped advanced leveling gear receives XP from eligible kills the companion damaged. Existing role-gear progression remains separate.
- Companion Peacemaking, including taming assistance, and Provocation now check Musicianship for gains. Native Discordance already does so.
- Legendary pets receive 3–5 distinct existing eligible skills at 125–150, limited by available skills. Existing rolls are upgraded once without reducing skills.
- Pet exchange accepts stored pet tickets as well as unused mission tickets. Legendary tickets/pets are rejected at listing and confirmation. Surrender warning includes the pet's training.
- Animal Lore can target your pet tickets in your backpack, showing the exact stored creature. Tickets have consistent species colors for the twelve mission species; others use the default hue.
- Mini-champ boss corpse loot: independent 25% level 3–5 map, 50% resource deed, 10% themed Alacrity and 2% weapon/shield set rolls. Challenge has three boss rolls. Per-player completion rewards remain.
- Heartwood set: Cyclone Mace and Heartwood bulwark. Ironhook: Cyclone Scimitar and Ironhook's guard. Regent: Cyclone Kryss and Regent's ward. Shields have defense, mana regeneration and lower mana cost, plus leveling records.
- Special rewards delivered through HavenAdvancedRewards show a firework, throttled to one per three seconds, with dedicated sound ID 32766. This does not cover every independent loot system.
- Cleaned user-provided ICQ MP3 installed at `D:/Uo Offline/Haven-ServUO-Preview-Client/SoundOverrides/32766.mp3`. Restart the game client to load it. Native sound files were not replaced. File SHA256: `26CC0652F95788F4E250A6CE11096BADA5C43502AFF99C17FA5E99024741E8E1`.
- Stone service buttons use contrasting dark slate.
- Six lodge decorative crates become owner-secured usable containers; player-owned containers are not converted.
- Lodge workshop gains a Lockpicking/Remove Trap practice chest. Double-click to set/reset the lock, then use lockpicks; target with Remove Trap for harmless practice. Native skill prerequisites still apply.
- Universal dye tub and dyes in rewards; standard tailor stock includes ordinary, leather, furniture, runebook and universal tubs. Universal colors movable items in your backpack; choose color using ordinary dyes. Unlimited uses.
- Blackthorn red emblem at 6409,2679 links to 6359,2571, with return pad at 6361,2570 and return landing 6411,2679. Separate same-facet pairs in Trammel and Felucca. Native pet teleport behavior is used.
- Practice golem at Trammel 3469,2601,10: 30,000 health, no offensive AI, zero damage, health restored each think, death prevented, no kill awards.

## Still unfinished

Housing island/community-center port, surrounding training-area decoration, manual gameplay checks, and universal coverage of the special-drop alert remain unfinished. The island terrain was not deployed. Existing server saves are preserved; normal autosaves persist startup migrations.

The earlier 00:24 deployment already shipped 15-second companion/training cooldowns, Luck mission shortening (2% per 100 Luck, capped at 60 to 40 minutes), extra one-handed Whirlwind choices, highest-first personal skills and removal of ship supplies from new Haven mini-champ rewards. That earlier backup was `E:/Backups/Haven/Prototypes/servuo-before-luck-cooldowns-20260912-002449`, also with 43 matching save files and passing live build/login.
