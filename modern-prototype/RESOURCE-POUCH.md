# Gathering resources and companion progression

The Arcane stone sells the gatherer's resource pouch for 15,000 bank gold. It keeps actual items with 90% reduced contents weight and supports mining, lumber, fishing, skinning, leather, gems, and rare gathering materials. Target an item or nested bag, or collect the carrier's pack. Equipment is excluded. Unique catches stay actual items; no conversion to a generic ledger balance.

Companion missions still deposit into the existing ledger. Supported loose resources dropped directly into the companion pack are absorbed into that ledger automatically. Its catalog now includes hides, scales, feathers, wool, meats, fur, sand, saltpeter, pearls, and rare gathering ingredients. Existing catalog IDs are preserved; Abyss missions remain limited to their original essence IDs.

Ledger controls: Give to me transfers balances to a personal ledger (creating one if needed); Give to guild transfers to a persistent shared guild ledger; Guild ledger opens that shared inventory. All current guild members can withdraw. Nonmembers cannot access it. Existing physical ledger transfers remain available. Balances are checked before transfer, including overflow and self-transfer rejection.

Companions can train to 120 per skill without power scrolls. Existing higher caps and trained values are retained, and the total cap accommodates all skills at 120. Raising a cap does not add trained points or teach unused combat abilities.

Verification: standalone SatchelSmoke checks raw stacks, nested collection, capacity failure, weight changes and reload; companion cap migration; skinning-resource balances; personal/guild transfers, nonmember rejection, and guild persistence. It is enabled only by SATCHEL-TEST-ONLY in an isolated verification server.
