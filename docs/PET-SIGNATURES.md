# Custom pet identities and rarity appearance

Staged only. Deployment hold remains active. This pass changes custom species; ordinary tameable animals retain their native rarity and combat rules.

## Species mechanics

| Pet | Innate identity | Rarity progression |
|---|---|---|
| Emberwing ostard | Cinderwake: a fire patch at its enemy, cast within six tiles; burns engaged enemies standing in the patch for six seconds | 8/12/16/20 fire damage per two-second pulse; Epic/Legendary radius two instead of one; 18-second cooldown |
| Moonfang wolf | Moon Hunt: prey loses physical resistance for eight seconds; extra physical strike below one-third health | Resistance reduction 5/8/11/14; finishing strike 12/18/24/30 before defenses; same marks cannot stack; 12-second cooldown |
| Frostmane steed | Winter's Grasp: cold strike, stamina drain and six-second Dexterity reduction that slows weapon swings | Stronger chill; Epic catches one additional engaged enemy and Legendary two; 14-second cooldown |
| Verdant llama | Sanctuary Grove: heals owner and their nearby pets, without requiring melee attacks | Four pulses of 4/6/8/10 healing over eight seconds; Epic cures up to Regular poison, Legendary up to Greater; 18-second cooldown |
| Stormscale drake | Ranged hunter: holds four to six tiles, retreats from close enemies, and fires an energy bolt every three seconds. Chain Tempest jumps through engaged enemies | Chain hits 2/3/4/5 targets, losing 25% damage per jump; 14-second chain cooldown. Bolts use rarity and Tactics; real damage feeds normal pet training |
| Stormhorn kirin | Arcane Reservoir: spends 15 mana to support its owner even outside combat, or siphons enemy mana in melee | Restores/siphons 12/17/22/27 mana; Legendary melee siphons interrupt interruptible casting by non-bard-immune enemies; ten-second cooldown |
| Frostbound bear | Guardian Roar: draws a vulnerable attacker away from its owner and gains a six-second melee ward; retains Colossal Rage | Ward reduces incoming melee damage by 12/16/20/24%; 18-second cooldown. Rage remains +50% melee damage below half health for ten seconds, with a 30-second cooldown |
| Ancient hellhound | Ashen Wound: fire strike from up to six tiles suppresses enemy healing; retains native Healing and fire breath | Healing suppression lasts 3/4/5/6 seconds; 18-second cooldown |
| Vampiric steed | Sanguine Rescue: additional physical drain heals its owner below half health, otherwise itself; native self-leech remains | Strike rises from 12 to 27; healing never exceeds actual damage inflicted; 12-second cooldown |

Signatures do not consume learned ability choices or reset training. Higher rarity strengthens the species' own mechanics instead of giving every custom pet the same generic fire/heal/energy/mana proc. Support requires a nearby living owner. Offensive signatures exclude players, owned/released pets and bystanders; area effects only spread to engaged enemies. Normal restrictions on poison and mortal wounds still apply to healing.

Fields and debuffs are temporary. They expire on their duration, ownership/range changes, source death/removal or restart. Fields do not block walking. Signatures do not fire while the pet is frozen, paralyzed or ridden. Native special abilities and learned abilities remain available.

## Elemental defenses

Emberwing has Fire, Frostmane Cold, and Stormscale Energy defense floors of **75/80/90/100%** for Common/Rare/Epic/Legendary. Legendary means actual immunity to that element, including removal of AOS's usual one-damage minimum for a fully resisted attack. Other damage components, direct damage and armor-ignoring attacks still work. Ordinary creatures keep the native minimum-damage rule.

The bear has minimum Physical resistance of **65/70/75/80%** and Cold resistance of **75/80/85/90%**. Other defenses remain unchanged. These innate floors apply without overwriting saved training seeds. The training screen includes the innate floor, keeps its normal 80% purchase ceiling, and cannot charge for an ineffective upgrade. A useful purchase starts above the innate floor and remains subject to the normal aggregate budget.

Animal Lore's effective resistance values show these defenses. Its Details action and creature tooltips explain the species signature and innate defense.

## Rarity appearance

Every custom species has a distinct four-shade family using existing client hues. Common coats are darker, Rare more saturated, Epic brighter and Legendary lighter. No new animation files or client installation are needed.

| Species | Common | Rare | Epic | Legendary | Theme |
|---|---|---|---|---|---|
| Emberwing | 0x21E | 0x1BB | 0x02D | 0x02E | Copper and ember |
| Moonfang | 0x1FB | 0x198 | 0x00A | 0x137 | Lunar violet |
| Frostmane | 0x24B | 0x1E8 | 0x05A | 0x05B | Glacier cyan |
| Verdant | 0x237 | 0x1D4 | 0x046 | 0x047 | Forest green |
| Stormscale | 0x255 | 0x1F2 | 0x064 | 0x191 | Lightning blue |
| Stormhorn | 0x200 | 0x19D | 0x00F | 0x074 | Arcane orchid |
| Frostbound bear | 0x250 | 0x1ED | 0x05F | 0x0C4 | Winter blue |
| Ancient hellhound | 0x219 | 0x1B6 | 0x028 | 0x029 | Volcanic red |
| Vampiric steed | 0x214 | 0x1B1 | 0x023 | 0x0EC | Blood crimson |

Legendary pets have a brief, silent shimmer every 45 seconds while idle with their owner. It is suppressed during combat, while either pet or owner is hidden, and while mounted. It is visual only and does not confer another combat bonus.

Existing active pets refresh automatically. Stored tickets refresh their reserved pet when inspected/claimed; shrunken tokens refresh on inspection and restore. Pet dyes override the automatic coat. Their saved restoration target migrates to the species' current rarity shade, so **Original color** restores the rarity appearance. Chosen dye colors, stats and skills are preserved.

Brand-new creature shapes are not included: UO needs full animation sets, with compatible mount artwork for rideable species. Current body/animation IDs remain intact. The palette was checked against the installed 7.0.23.1 hue table; actual appearance on each animation still needs an in-game visual review after deployment is authorized.

## Integration

Canonical content is under `playerbots/source/CustomBots/UOOffline`. Native patch **0048** changes the fully resisted AOS minimum and evaluates `ForcedAI` only once during AI replacement, preventing a discarded duplicate custom AI/timer. The patch was applied successfully to the independent audit checkout. No Server engine code was changed.

Regression tests cover actual elemental damage, mixed/armor-ignore damage, ranged movement and training, native stop orders, spell interruption, secondary-target exclusions, support, effect cleanup, useful training purchases, rarity shades and dye preservation. See the implementation tracker for the final test result.
