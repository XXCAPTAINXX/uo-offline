# Guard patrol and the Sovereign Store

Use the client's UO Store button or **[store** to open the store. **[sovereigns** is an alias; **[achievements** opens the reward history. This is Haven's compatible store, with its own catalog.

Sovereigns are a saved **character** balance, separate from gold, Haven Marks and Astral shards. They use no inventory slots. Rewards go directly to this balance; purchases arrive directly in your backpack. A failed delivery spends nothing. Confirm each purchase after reading its description.

## Earning Sovereigns

| Activity | Reward | Repeatable? |
|---|---:|---|
| Welcome | 25 | Once per character |
| Discover a named town | 25 | Once per town and facet |
| Discover a named dungeon | 50 | Once per dungeon and facet |
| Reach 50 base skill | 5 | Once per skill |
| Reach 100 base skill | 25 | Once per skill |
| Reach 100 base Strength, Dexterity or Intelligence | 10 each | Once per stat |
| Own a nearby controlled animal | 15 | Once |
| Have a bonded animal nearby | 25 | Once |
| Monster kill milestones | 10 at the first kill; then 12 / 20 / 60 / 110 / 510 / 1,010 at 25 / 100 / 500 / 1,000 / 5,000 / 10,000 kills | Once each |
| First wandering encounter clear | 25 | Once |
| Wandering encounter clear | 10 + twice the tier | Each clear, including credited helpers |
| Major boss | 50; 100 at 20,000+ maximum HP | Each credited kill |

Existing skill and stat milestones count automatically. Previously recorded discoveries receive a one-time top-up to the new 25/50 rates. Visits begin recording with this update and are checked every three seconds while alive and online; revisit places explored before the update. Town shops and dungeon subrooms do not create separate discovery rewards when a named parent town/dungeon exists. Unnamed areas and custom areas without town/dungeon regions are not discovery achievements yet.

Boss credit uses the server's kill rights, including credit routed from pets and companions. Champions, Scalis-family bosses and hostile creatures with at least 4,000 maximum HP qualify. Claimants must be within 18 tiles on the same map. Controlled, summoned, previously owned, invulnerable and training creatures cannot award Sovereigns. Repeated callbacks for the same kill cannot pay the same character twice.

The history keeps the latest 60 transactions; completed achievement IDs, earned total and balance persist independently. This initial achievement set covers the activities above; it does not yet attach rewards to every quest, craft or harvest event.

## Store catalog

| Reward | Sovereigns |
|---|---:|
| Instant pet bonding potion | 50 |
| Pet dye | 75 |
| Reusable pet shrinking leash | 100 |
| Wayfarer's rune pouch | 100 |
| Gatherer's resource satchel | 250 |
| Agapite runic hammer, 25 uses | 350 |
| Mercy's Endless Bandage | 500 |
| The Gilded Pathfinder | 750 |

The shop has four readable entries per page, descriptions, confirmation and an achievement/history tab. These are existing working shard items; their normal use restrictions still apply.

## Guard behavior

Pets and companions ordered to **Guard** defend against existing attackers first. When idle, Guard looks for the closest eligible hostile within eight tiles **of the owner**, once per second. It does not start patrol attacks in guarded towns, while the owner is hidden/dead, while mounted, or during companion taming assistance.

The search excludes players, player bots, vendors, owned pets, passive animals, hidden or pacified creatures and animals currently being tamed. Visibility, line of sight and harmful-action rules apply. Automatically chosen targets have a 12-tile leash; dead, hidden or ineligible targets return the pet to Guard. Explicit Follow, Stay and Attack commands retain their behavior. Companion target-death cleanup now preserves Guard.

## Checks and play testing

Verified 1,101 content tests, including persisted currency/achievement state, repeated discovery checks, per-player boss credit, full-backpack refunds, forged/replayed confirmations, all eight catalog purchases, and Guard target selection and leash behavior. Six store layouts were rendered with native UI art/fonts and inspected.

In game:

1. Open the UO Store button and **[store**; compare balances.
2. Visit a town and dungeon; confirm the reward occurs once and appears under **[achievements**.
3. Buy an item, then retry with a full pack; verify no currency is lost on failure.
4. Kill a major boss with another player; check each eligible player's balance.
5. Order pets/companion to Guard near hostile monsters, then use Follow or Stay to stop patrol behavior.
6. Reconnect and confirm balance, history and purchases remain intact.

