# Pet roster and world rarity update — September 12, 2026

## Book and workflow
Open [petbook. The roster now has a case-insensitive name search, Favorites view, and sort choices Recently added, Rarity, Name, Follower slots and Taming skill. Filters include All, Favorites, Exchangeable, Bonded and Legendary. Rows show rarity, follower slots, taming requirement, bond status and acquisition timestamp. Older tickets show unknown dates and sort behind newly acquired tickets.

Each row has Lore, Claim, Favorite/Unfavorite and, where eligible, Exchange with the credit value. Exchange opens a confirmation identifying the exact pet and its permanent surrender. Favorites, bonded pets and Legendary pets cannot be exchanged; ownership/access and protection are rechecked on confirmation. Ordinary exchange scanning still excludes book contents; explicit book exchange is required. Replay cannot award duplicate credits. Spend pet credits opens the existing higher-rarity redemption flow.

Jenna's successful assisted tames go straight to the owner's book; completed mission ticket delivery and credit redemptions also deliver there. If the book cannot accept an assisted tame, the pet remains assigned to Jenna for reclaiming. Existing owned pets are not rerolled. Player-performed taming continues normally; this automatic routing is for companion-assisted tames. Existing loose tickets can be targeted into the book.

Favorites and acquisition dates are serialized on tickets (version 2, with backward-compatible old-ticket loading). Claim returns the original pet; the consumed ticket's favorite flag does not follow it outside storage.

## New wild-spawn odds
| Habitat | Normal | Rare | Epic | Legendary |
| --- | ---: | ---: | ---: | ---: |
| Vampiric steed | 25% | 40% | 25% | 10% |
| Ancient Hunt hellhound | 0% | 30% | 45% | 25% |
| Frostbound bear | 0% | 40% | 35% | 25% |
| Chelonia tortoise | 0% | 40% | 35% | 25% |

Previously the steed spawner never assigned rarity. It now rolls once for each newly spawned animal. The hellhound previously rolled 50/35/15 Rare/Epic/Legendary. Combat waves, taming requirements and quick respawn timers remain. Existing animals are not retroactively rerolled. Mission rarity odds are unchanged.

## Restart and verification
Cleanly saved, backed up and restarted around 03:35 EDT. Eight production files deployed: HavenPetBook.cs, HavenPetExchange.cs, HavenPetMissions.cs, HavenCompanionTaming.cs, HavenPetHabitats.cs, HavenAbyssTrial.cs, HavenSnowBearDen.cs and HavenChelonia.cs. No native patch or island files deployed.

Isolated inventory run reached COMPLETE, covering search, recent/name sorts, favorite filter, favorite/bonded protection, explicit book exchange, replay rejection and exact bonded release. Native taming run reached COMPLETE, including delivery of the exact tamed animal into the owner's book and cancellation. Rarity threshold checks passed. Verification and live Release builds had zero warnings/errors. Authenticated login/server-list/game-relay probe passed after startup. Client layout was not manually inspected in-game.

Backup: E:/Backups/Haven/Prototypes/servuo-before-pet-roster-20260912-033458
All 43 save files matched backup SHA256 hashes; config, scripts, engine sources and root files were also copied. Original server untouched.
