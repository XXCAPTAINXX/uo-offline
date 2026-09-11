# Accepted restoration order

Restore the original server's behavior in this order: gear, pets, full companion roles and missions, island. This is an implementation queue, not a claim that these features are live.

1. Gear: Concord talisman, nine matching rings and actual set bonuses, equipment progression; then Astral, quest, Legendary and Doom custom gear. Preserve original prices and requirements and validate purchases, equip/unequip, growth and reload.
2. Pets: custom species, rarity, signatures, defenses, training and recovery; companion taming assistance, tickets, pet ownership and mounts. Preserve ownership and test failed taming, transfer, death and persistence.
3. Companion parity: original Fighter, Healer, Bard, Caster and Archer behavior. Restore Grind, Ore, Wood, Leather, Reagents, twelve taming routes, Malas Reagents, Doom Bones, Abyss Essences and Abyss Ingredients, with duration selection, skill gates, early recall, automatic return, mission journal and AFK routing. Compare original source rather than translating only menu names.
4. Island: irregular coast, cove, beach, docks, purposeful buildings and natural paths; master receiving chest, connected storage and working stations. Verify all entrances and floor access. Use the user-requested independent visual critic and iterate toward 8/10.

Keep the old server intact. Changes are first built and exercised in the isolated test world; live activation and remaining limitations must be reported separately.

Offline mission requirement (user addition): persistent per-companion default mission/rotation and duration, opt-in repeat while owner is logged out, skill/ownership gates, restart-safe reward completion, and owner control on return (recall or allow active trip to finish). Preserve active manual missions when logout occurs; apply default to the next eligible trip.

Gear progress: Concord/rings/set bonuses, bracelet/pendant leveling, Astral wallet/rewards, Legendary drops, Doom reforging and shield-warrior tiers implemented and tested. Remaining gear work includes retroactive training-quest claims and custom boss-artifact systems. No pet/role/mission/island parity completion is implied.

UI follow-up from player screenshot: compact companion mission timer is cramped; Open and Recall now controls/text overlap the frame and each other. Rework spacing, button hit areas and label widths, keeping the timer compact and checking long mission names. User explicitly requested this be queued without interrupting the gear/pets/roles/missions/island restoration priorities. Reference: codex-clipboard-a42ba74d-7b8e-4d54-8427-5cdc028e3ff6.png.

## Staged recovery, mission UI and pet work (not deployed)
- Unified Gathering/Taming/Roles mission screen and larger timer compile. Selecting a mission is separate from dispatch.
- Mission dispatch now reports its specific failed precondition. Thirty-minute duration is supported.
- Healer travel now distinguishes criminal status, combat target and aggression records. This diagnoses the actual blocking predicate; it does not clear legitimate flags.
- Companion owner-beneficial action override prevents native BaseCreature criminal propagation from refreshing its own owner's flag through automatic healing. Actual harmful criminal actions retain native propagation. Runtime regression passed, including unrelated criminal aid and actual companion crimes; the player's original flag source is still unconfirmed.
- Pet ticket fresh tests and overdue reload tests pass: exact reserved pet retained, one ticket delivered, collection does not duplicate.
- Legendary rolls now use the original 24 trainable skills. Persistent rarity identity is being added to pets rather than relying on the deleted claim ticket.
- Pet release implementation and verification are recorded below. Full role/mission parity remains queued.

Runtime mission regression passed: eligible 30-minute Mining dispatch, duplicate-dispatch rejection with exact reason, early recall, invalid duration, and idempotent rarity application. Pet mission overdue reload and duplicate collection passed. These are isolated test-world results, not live activation.

Durability fresh and reload tests passed after placing the fixture in the world: 255-cap migration preserves existing missing points, repeated application does not repair wear, higher caps are preserved, and post-upgrade wear remains unchanged after reload. Final staged build passed with zero warnings/errors. Live server remains unchanged.

Elemental pet defense port: Emberwing fire, Frostmane cold and Stormscale energy now match original 75/80/90/100 tier floors without modifying training seeds. Native AOS patch 0009 removes the one-damage minimum only for legendary pure matching-element attacks; armor-ignore, direct, physical, poison and mixed elements retain native damage. Chaos damage is excluded until resolved, avoiding accidental full immunity. Runtime checks passed for all three species through AOS.Damage, including armor-ignore bypass. Build clean. Full signature, species and training parity still pending; not deployed.

## Pet release checkpoint (2026-09-11)

Implemented all ten custom species and their combat signatures, tier defenses, natural hues, persistent rarity and legendary skill-roll records. Native training definitions support five follower slots and the original triple-damage training pace with persistent per-enemy quotas. Temporary combat fields expire on reload.

Twelve taming mission routes deliver the exact reserved animal through claim tickets. Companion pets support assignment, automatic mounting, riding toggle, mission parking/return and reclaiming the original pet. Assigned bonded pets can be resurrected by their companion and Haven recovery.

Bard role includes native Peacemaking-assisted taming, Provocation/Discordance combat support and a lute. Native tame completion creates a claim ticket; cancelling assist cancels the native timer. Explicit attack orders stop taming assistance. Pets menu opens from the companion or [companionpets.

Bear den, Ancient Hunt, Chelonia and two vampiric steed spawners are installed idempotently. Ancient Hunt's two guardian waves reveal a rare-or-better hellhound. Chelonia moved to a verified modern-map floor at Trammel 4095,3473 because the old offshore coordinates had no floor. Travel menu includes the three habitats.

Passed isolated runtime checks: ten-species signature/defense/training checks; native training/rarity/legendary roll persistence; assigned mounts, mission parking, early return and reclaim persistence; habitat spawning and Ancient Hunt waves; actual native peace-to-tame-to-ticket-to-player claim, cancellation of an active native tame, no owner criminal flag, and assigned bonded-pet resurrection. Final deployment record belongs in WORK-LOG.md.

Still separate work: old custom trained elemental ability pool (this release uses native ServUO training catalogs), pet dyes/shrinking consumables, full Bard songs/masteries and role parity, remaining gathering/advanced mission routes and saved offline rotations, remaining gear systems, and the finished home island. This checkpoint is a usable pet release, not full one-for-one migration completion.

Pet checkpoint deployed 2026-09-11 after the above checks, including native auto-stable/login reclaim and active training quota reload. Existing characters preserved. Remaining scope listed above is still unfinished.
