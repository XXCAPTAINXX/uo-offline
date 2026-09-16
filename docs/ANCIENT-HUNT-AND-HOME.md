# Ancient Hunt and island home

Staged only. The deployment hold remains active. No live server, world save or client map was changed.

## Island home

**[home** finds the caller's saved Corsair's Rest estate. It uses the island landing, brings following pets, searches adjacent landing tiles if blocked, and preserves normal combat, criminal, casting and travel restrictions. Other characters cannot use an owner's island home command. The existing banked island chart uses the same checks.

## Abyss status

The installed 7.0.23.1 data contains the Ter Mur/Abyss map. Abyss and Underworld spawn JSON files exist, but scripts for important actors including Stygian Dragon, Medusa and Slasher of Veils are absent from this build. Their spawn definitions do not make a complete working dungeon.

The Ancient Hunt is a separate compatible encounter. It does not enable, replace or claim completion of the full Stygian Abyss.

## Research and appearance

[UOAlive's Tamadin guide](https://uoalive.com/wiki/PlayerGuide%3ATamadin) lists Ancient Hellhound among boss-fighting pets. A reliable public stat sheet for the specific custom Abyss encounter was not found. The following mechanics and rolls are Haven's adaptation of the player's remembered Healing and high Dexterity/stamina, not verified UOAlive formulas.

Client mobtypes.txt lists body 1069 as Ancient_Hell_Hound. The new pet uses that body and its original hue. This is existing client art, not a new art installation; its rendered appearance still needs in-game visual acceptance. The current implementation is a combat pet, not a mount; the optional question about the remembered mount behavior remains unanswered.

## How the encounter works

- After deployment authorization, **[HavenAbyssSetup** places one brazier at **526, 758, -92, Ter Mur**. Setup checks actual floor and refuses duplicates.
- Players reach the staged site with **[abyss** or **Abyss hunt** in the Wayfarer's Atlas, subject to ordinary travel restrictions. Landing is 527, 758, -92, with nearby fallbacks.
- Use the brazier to see current Taming/Lore, requirements and encounter status. Starting requires **110 base Taming and Animal Lore**.
- Defeat two waves of three guardians. The hunt cancels if its starter dies, leaves the 28-tile boundary or exceeds 15 minutes. Guardians are leashed.
- Clearing the fight reveals **one wild, tamable** Ancient Hellhound. Animal Lore shows the actual rolled stats. It remains for 20 minutes.
- Rarity: **50% Rare, 35% Epic, 15% Legendary**. Existing custom-pet rarity bonuses apply; Legendary begins at **one follower slot**. Other rolls begin at three.
- After the pet is tamed, lost or the hunt expires, the brazier rests for ten minutes. Previously owned or currently controlled pets are preserved during cleanup.
- Restart interrupts the ritual and removes its unowned actors, rather than duplicating rewards or resuming a half-completed fight.

The initial candidate at 526, 766 was an isolated central platform. The final location is on connected floor. Spawn selection follows adjacent walkable cells with small height changes and checks line of sight, avoiding disconnected platforms and lava.

## Ancient Hellhound

Before rarity bonuses: Strength 450–550, Dexterity **180–210**, Intelligence 180–220, health 500–650, damage 17–23. Stamina follows Dexterity. Rarity raises the starting stats, so the final values can be higher. Dexterity and stamina remain over 150 after taming; no species stat-loss modifier is applied.

Native Healing starts at 110, Anatomy at 100, with 120 caps; the normal taming skill adjustment still applies. The tamed pet heals itself and its owner using native healing calculations, at 12-tile range and a two-second heal duration, with an eight-second interval and line-of-sight checks. Owner healing displays the Healing buff. Healing is innate and does not consume a purchased ability choice. Native hellhound fire breath and existing custom-rarity abilities remain available.

## Verification

Isolated tests cover owned island travel and following pets, travel restrictions, high stats and healing, innate ability accounting, actual Abyss floor and travel, skill requirements, both waves, duplicate completion rejection, taming preservation and wild-pet cleanup. In-client art, combat pacing and gump appearance remain visual/playtest checks.

The complete Haven regression suite passed **216 tests, zero failures and zero skips** after this pass (artifacts/ideas-abyss-full01.log). The live UOContent.dll hash remains 631B7E4B698F1C97730693C35376219D7C795DE9ABC5A0EC08F149300264C135.
