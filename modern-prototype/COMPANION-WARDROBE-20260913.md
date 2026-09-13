# Companion paperdoll and persistent outfit

The saved Jenna (0x1625) had boots, cloak and backpack, but no torso or leg
clothing/armor. No spare outfit was present in her backpack. Starter garments
were immovable. The existing double-click opened the paperdoll and immediately
opened the command menu over it.

Double-click now opens only the usable native paperdoll. The [c menu includes
Paperdoll / dress. Starter clothing and body armor can be removed by their
owner, while role weapons/shields keep existing rules. Worn removable items use
the same owner-only inventory reach as her pack. Other players cannot equip or
remove her belongings.

Missing coverage receives a blessed default dress (female), or shirt and pants,
at startup or resurrection. These basic garments have no durability wear.
Existing outfits are preserved, and repeating setup does not duplicate clothes.
Ordinary custom equipment keeps its normal durability.

The integration test exposed that KeepsItemsOnDeath normally moves removable
equipped items into a backpack. Companion death now explicitly retains worn
items on their original layers, keeping her dressed through resurrection.

Passed native lift at five tiles, native equip, owner/stranger permissions,
fallback outfit/idempotency, and exact custom-item retention across a role
change, death and resurrection. Isolated and production builds passed.
No test saves are deployed; release replaces the three scoped source files.
No live client visual review was performed.
