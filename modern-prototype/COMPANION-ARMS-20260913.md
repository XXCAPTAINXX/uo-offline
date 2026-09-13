# Stronger companion weapons and female caster appearance

Existing evolution records retain XP and receive new minimum attributes on load.

Level 1 to 20 grimoire: spell damage 75–150%, bonus mana 40–100, mana regeneration 6–12, lower mana cost 15–20%, intelligence 15–30. Existing casting speed/recovery remain 2/6.

Level 1 to 20 blade/bow: weapon damage 60–100%, hit chance 15–25%, swing speed 15–40%, mana leech 75–100%, life leech 50–100%. Native aggregate caps still apply.

Jenna's wardrobe maintenance enforces her female human body and removes facial hair. WraithForm's native implementation preserves a Haven companion's human body and current hue; the spell remains the original WraithFormSpell type, retaining native mana-leech and resistance behavior. Other casters retain their normal wraith graphics. Apply patch 0043 alongside the custom sources.

CompanionArmsSmoke checks initial and maximum attributes, native equipped SDI, preservation of stronger existing attributes/XP, and Jenna's repaired body and WraithForm body selection. Client paperdoll appearance has not been visually inspected in this release.
