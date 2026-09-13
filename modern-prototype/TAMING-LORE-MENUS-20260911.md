# Taming supplies, lore and companion menus — 2026-09-11

Restored original taming mission supply rolls: 8% bonding potion,8% reusable shrinking leash,9% combat 105 Power Scroll per search. Added user-requested house shrinking post at 2% per roll (original post existed outside that reward pool). Durations5/15/30/60 use1/3/6/15 search rolls and retain the best custom-pet rarity. All rolls and items are fixed at dispatch, stored with the ticket, deleted if recalled early. Completed bonus bags go into the companion backpack; ticket storage version1 persists the pending bag, reads old version0. Existing trips created before deployment have no retroactive bonus bag.

Leash and house post reuse free post safety checks and exact pet tickets. House post requires owner/co-owner placement. Bonding potion consumes only on successfully bonding an eligible nearby owned pet. Public post discovery excludes portable house posts.

Animal Lore successful inspection now opens HavenPetLoreGump with Lore/Combat/Skills tabs, rarity, ownership/bonding/food, weapon skill, damage, current resistance, native ability listing and species signature. All skill rows are paged. Original native training interface stays available via its labeled button, retaining native permissions. This does not replace every vendor/training-internal native lore screen.

Flat rectangular labeled controls now on companion main, mission, role, skills, assigned-pet, offline-selection, legacy taming and mission-timer screens. Codex compact item arrows unchanged. Combat bar now uses warm parchment behind gray buttons in response to last screenshot. No screenshot-based claim of perfect visual QA; static widths corrected for header/footer and pet row controls.

Previously staged rarity tooltips, dedicated Roles screen and mission-command timer feedback are included with all production source in this release. Reported-skill repair marker not enabled. Recall-follow movement bug still outstanding.

Validation: isolated clean build and runtime suite COMPLETE. Tested bonus boundaries, duration rolls, dispatch snapshot, completion delivery, cancellation deletion, reusable leash return, consumed successful bonding potion, flat controls across companion/lore menus; existing CUB, challenge, inventory, travel and skill retention checks pass. Final combat-bar color edit compiled live after isolated tests.

Backup E:/Backups/Haven/Prototypes/servuo-before-taming-lore-menus-20260911-202140; save hashes matched. Native patch0018 plus production source deployed.
Live build zero warnings/errors; restart and login/server-list/relay checks passed.
