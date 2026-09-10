# Bot pet and dungeon economy

The Commons now has 16 trades, including Pets, Pet Supplies and Dungeon Supplies. Existing stock and sales are preserved when missing stalls are added.

## Physical production

Wild tamer bots consign actual successfully tamed animals. Tickets retain the pet and its stats across saves and transfers. Players can inspect before purchase and feed the claimed animal to bond when normally eligible. Player-owned pets and animals near connected players are excluded from bot taming.

Pet-supply workshops gather and consume materials for bonding potions, reusable leashes and house posts. Dungeon goods use a different path: available combat bots form a three-member crew, travel to an installed entrance, fight actual monsters and operate the supported encounter mechanics. They receive only the encounter's normal earned rewards. A timeout ends the run without inventing loot. Corpses require the bot or its party's participation and ordinary looting permission.

Supported dispatch destinations: Blackthorn, the six Shadowguard rooms, native Doom, installed Abyss mini-champions and the pirate boarding encounter. Existing dungeon crawlers also retain their normal routes. This extends the compatible dungeon implementations already on this build; it does not claim to port every later-era dungeon or loot table.

## Players take priority

Unassigned dungeon bots offer to join an arriving player's party or leave. The offer lasts 30 seconds. Declining or ignoring it makes them leave. Recruitment uses native parties, validates the responding player and capacity, and protects guild workers and existing human groups. A three-bot crew needs three free party slots.

Shadowguard currently reserves one physical room per group, not unlimited private copies. Bot-only rooms yield to a player at the lobby. Cleanup never resets a room that contains human participants.

While grouped, collected bot loot goes to the human party leader's nearby owned companion if space permits, then to the leader's backpack. A full companion never destroys the reward. In a bot-led guest party, the first human member receives the bot loot. Bot equipment and belongings already held before grouping are not swept out of their inventory.

## Market use

Open `[market`; select a listing to review its price and producer, or inspect the actual pet. Wallet gold pays for purchases. Double-click Minax notes to credit `[expeditions`; `[minax 10` withdraws ten earned credits into a transferable note. Doom recipes work with their matching artifacts. Pirate cargo redeems for doubloons. Supported resource deeds work with the Resource Ledger.

Each stall holds at most 24 listings. Empty shelves fill only as work succeeds; fixed prices are custom prices, not a supply-and-demand model. Local release status exposes actual listings and deployed crews' locations, health, targets and route state.

## Player checks

- Find a dungeon bot. Accept its offer and confirm native party membership, then fight and check your companion's bag. With a full or absent companion, check your own pack.
- Decline an offer or wait 30 seconds. Confirm the bot leaves and stops competing for kills.
- Start a human Shadowguard room while bots are elsewhere. Confirm their cleanup does not reset your progress.
- Inspect, buy and claim a pet; compare stats before and after and feed it to bond.
- Buy/redeem a Minax note; verify the exact balance. Try withdrawal with a full pack and check that the balance is retained.
- Visit the Commons after actual bot adventures and inspect finite new stock. Pathfinding and party-offer layout still need client play-testing.
