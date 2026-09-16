# Menus, pet lore and bank storage

This update separates crowded controls, improves contrast, and keeps detailed pet information out of hover tooltips.

- Regional resource missions: separate rows for focus selection and the footer.
- Mission duration: wider reward descriptions and a clear Cancel footer.
- AFK missions, guild resource storage, guild roster and pet dyes: readable text on dark backgrounds.
- Companion taming choices: separate pet names, requirements and readiness; long mission status text can scroll.
- Pet training: skill-cap categories lead to the paged power-scroll controls instead of drawing an overflowing list.
- Stone shops: two-line item names, concise statistics, full hover descriptions and item previews.
- Artisan markets: readable headings, item rows, prices and inspect controls.
- Champion's Codex, sea cargo and GM panel: additional clearance around footer/close controls.
- Book of Masteries: more room for learned mastery names and shorter help text.

## Pet information

Hover text keeps compact training/ability summaries and a short species signature. Use Animal Lore, then **Abilities / lore**, for exact signature mechanics, rarity bonuses, rolled Legendary skills, learned abilities, training progress, care and innate defenses. The **Creature lore** tab adds original stories for the ten custom species families. These stories do not change combat mechanics.

## Personal banks

Personal banks expand to **2,000 items** on connection. Existing contents and bank serials are preserved. Banks already configured for more items, or unlimited items, retain those settings. Bank weight remains unlimited. Nested ordinary items still count toward the bank limit; existing virtual storage books keep their own rules. The player's carried backpack limit is unchanged.

## Rune pouch

Arcane Supplies sells the Wayfarer's rune pouch for 250 gold, and starter bundles include one. It stores up to 60,000 ordinary blank recall runes as a balance. Marked/personalized runes and travel scrolls are not absorbed. Cast **Mark** on the pouch to consume one blank and receive a normal marked rune for the current location. Failed casts, restricted locations and a full backpack do not consume a rune. `[rune` withdraws one blank.

## In-game checks

1. Open companion Tasks → Resource routes. Read the last row and try each focus control, then Close.
2. Check both normal and taming mission duration menus, AFK settings, and later skill pages.
3. Inspect shop items with long names; verify the complete hover description and preview remain available.
4. Use Animal Lore on a custom pet, a shrunken pet and a pet ticket. Open **Abilities / lore**, both tabs, and return to the overview.
5. Reconnect, open your existing bank and deposit an item after the old 125-item limit. Confirm previous contents are intact.
6. Store blank runes in the pouch, cast Mark on it, and inspect the new rune. Repeat with a full backpack and confirm the balance stays unchanged.

Automated menu captures and native-art layout previews cover representative empty, populated and later-page states. In-client rendering should still be checked with the player's preferred UI scale.
