# Scalis, Cora and Corgul

These are Haven-compatible encounters for the existing ML engine, with native map locations and working combat, loot and persistence. They are not a complete later-era expansion port.

## Finding them

* `[scalis`: current coordinates and a route to Chelonia's sea access. A roaming Scalis starts south of the pier around 4000,3670 on Trammel. Bring a boat or supported sea mount. One Scalis per facet; after death he respawns in 15 minutes.
* White **Fabled Fishing Nets** have a 25% Scalis chance at 100 base Fishing if no Scalis is alive on that facet. Otherwise they report his location without consumption. A simultaneous second summon refunds its net. Failed chance rolls retain the native Leviathan encounter. Nets cost 25,000 wallet gold at Arcane Supplies and remain obtainable from ancient SOS treasure.
* `[cora`: Cora's Covetous final chamber at 5457,1808 on Trammel, with a chamber approach travel button. Her violet rifts give two seconds to move before periodic mana drain and damage; she also teleports toward attackers. The separate endless Void Pool event is not included.
* `[corgul`: travel to Haven's sacrificial altar beside the Covetous entrance. Offer a treasure map and a world map, and sacrifice health down to 1 HP, for a three-hour chart. Recover before using it. The chart takes you and nearby following pets to the original Island of the Soulbinder (boss around 6431,1236). An island exit returns you to Covetous. World maps cost 500 wallet gold at Arcane Supplies. Corgul has replenishing soulbound guards and a damaging pull attack.

Cora and Corgul each respawn after 15 minutes. Their status windows show whether they are alive. Corgul's chart is Haven's compatible entrance; it does not implement the official moving island sailing transition. Travel requires a safe location and normal travel eligibility.

## Rewards

Deal at least 600 damage, including your pet/companion's damage, and remain on the same facet within 32 tiles when the boss dies. Each qualifying contributor receives personal rewards once. An idle bystander does not qualify. Dead participants still qualify if nearby. Eligible grouped bot damage credits its human party owner; independent bots keep earned loot for the market.

| Boss | Guaranteed personal rewards | Independent rare rolls |
|---|---|---|
| Scalis | 40,000 gold, 20 Haven marks, 10 Astral shards, SOS bottle, fishing net and fishing pole | 25% one of four evolving artifacts; separately 5% small soul forge deed |
| Cora | 30,000 gold, 20 marks, 10 shards, level 5 treasure map | 25% one of five evolving Covetous artifacts |
| Corgul | 50,000 gold, 20 marks, 10 shards, level 6 map, 0.5 Tactics Transcendence scroll | 25% one of seven evolving artifacts |

Boss corpses also contain rich normal loot and gems. Personal rewards go directly to the pack; a carried wallet absorbs supported shard currency using the existing reward path. The shared progression system takes these special artifacts to level 20. Ordinary gear does not become evolving gear.

* Scalis: Enchanted Coral Bracelet, Leviathan Hide Bracers, Illustrious Wand of Thundering Glory, Smiling Moon Blade.
* Cora: Blight of the Tundra, Bracelet of Protection, Brightblade, Hephaestus, Prismatic Lenses.
* Corgul: Enchanted Sash, Handbooks on Mysticism and the Undead, Ring of the Soulbinder, Helm of Vengeance, Rune Engraved Pegleg, Culling Blade.

The placeable/redeedable small soul forge works as a forge and opens Haven's existing **Abyss artifice** recipes. It is not full official Imbuing. Later-era artifact properties absent from this engine use compatible attributes (for example mana leech or resistance bonuses); the item's actual tooltip is authoritative. The Scalis encounter uses sea damage and eels, rather than a full ship hull/cannon or life-leech inversion system. Later-era clothing recipes are not part of this release.

## In-game checks

1. Open each status window and reach its approach. Check that Cora and Corgul are on accessible floor; reach Scalis by water.
2. Buy a world map and white net with wallet gold. An already-active Scalis must leave the net intact.
3. Offer maps at Corgul's altar, heal, enter with the chart, and test the island exit with a following companion.
4. Fight with a companion and another player. Both contributing players should receive their own completion rewards; idle spectators should not.
5. Check artifact stats/progression, and if a forge drops, place it in your own house and use a recipe with its required materials.

Reference: [official fishing encounters](https://uo.com/wiki/ultima-online-wiki/skills/fishing/leviathan-and-osiredon-the-scalis-enforcer/), [High Seas artifacts](https://uo.com/wiki/ultima-online-wiki/items/artifact-collections/artifacts-high-seas/), [Covetous](https://uo.com/wiki/ultima-online-wiki/world/dungeons/dungeon-covetous/), [Covetous artifacts](https://uo.com/wiki/ultima-online-wiki/items/artifact-collections/artifacts-covetous/), [Corgul](https://uo.com/wiki/ultima-online-wiki/combat/pvm-player-versus-monster/corgul-the-soulbinder/).
