# Adjustable companion missions

Staged only. The deployment hold remains active.

Open **[c**, choose Tasks, choose a mission, then choose 5, 15, 30 or 60 minutes. The same duration becomes the preference for subsequent AFK trips. AFK settings also offer all four lengths. Changing the preference while a companion is away affects the next trip, never the active trip.

| Duration | Completion bonus | Loot compared with a 5-minute trip | Custom-pet rarity searches |
|---|---:|---:|---:|
| 5 minutes | 0% | 1x | 1 |
| 15 minutes | 10% | 3.3x | 3 |
| 30 minutes | 15% | 6.9x | 6 |
| 60 minutes | 25% | 15x | 15 |

Gold, marks and gathered quantities use elapsed full minutes multiplied by the completion bonus; integer quantities round down. Equipment counts use one item per five rewarded minutes, with the existing first item available at three minutes. Resource rewards remain commodity deeds and rewards are unpacked directly into the shared inventory. The bonus also increases expedition skill training, stat growth and equipment experience; fractional training minutes round down. Existing passive time-based companion training remains separate.

Taming trips return **one selected species**. Completed longer trips make the listed number of rarity searches and keep the best result; ordinary species never acquire custom rarities. Each search also has the existing 25% chance to find a useful taming supply. Skill requirements remain in force. The baseline custom rarity roll remains 40% Common, 35% Rare, 20% Epic and 5% Legendary. Fifteen independent searches give about a 54% chance of at least one Legendary result.

Early returns earn ordinary rewards for full minutes worked, without the completion bonus. A taming trip must finish its entire selected duration to return a pet. Logging back in late cannot earn beyond the booked duration. Completion is claimed once. Explicit AFK remains enabled until disabled; player activity does not cancel it.

New duration contracts and preferences are separate serialized items, preserving the existing expedition and AFK record layouts. Old trips without a duration contract remain five minutes. Contract durations stay fixed across saves and preference changes.

Validation: new tests cover frozen deadlines, invalid durations, early return, delayed login, one-time payouts, ordinary/custom pet results, serialized records and explicit AFK behavior. The complete 228-test Haven suite passed in artifacts/abyss-snow-missions-full01.log.
