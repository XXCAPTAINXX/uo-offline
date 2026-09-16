# Roaming encounters: habitat recovery and spoils collection

Saved-world inspection found the running tiger spawner 0x40034727 at
(636,2236,80), still tracking living wild tiger 0xA80C at (699,2239,40).
The tiger occupied its single slot while 63 tiles away; the spawner was not
stopped. The encounter chest 0x40036403 remained at (637,2229,80).
The owner was logged out in the snapshot, so its precise reach failure could
not be reproduced from the owner's saved position.

Eodon spawners now return the same idle, unowned wild creature when it strays
more than 12 tiles, using a spawnable tile within three tiles of its habitat.
Active combat, taming and unexpired peace defer the return. Controlled or
previously owned pets remain detached rather than moved. Existing rarity and
identity are preserved. Invaders cannot harm wild tamable creatures.

Opening a spoils chest now shows its contents and a Collect spoils button.
The encounter journal also offers Collect nearby spoils. Collection requires
a living participant on the same map within 28 tiles and uses backpack capacity
checks. This avoids the ordinary two-tile/height restriction without changing
reach rules for other containers. Full packs retain the actual remaining items;
the existing 20-minute chest expiry and shared participant rights remain.

EncounterHabitatSmoke passed against a fresh copy of the live save: same tiger
returned without duplication, wild-tamable attack rejection, raised chest
collection, stranger/range rejection, full-pack retention and repeat collection.
Isolated and production builds passed. No test saves were copied into live.
