# Eodon pets — September 12, 2026

## Published

- Stonehorn triceratops: rideable, three normal follower slots, 102 taming. Physical tank with Horn Guard: drains 10–22 enemy stamina and reduces incoming melee damage by 12–24% for six seconds, on an 18-second cooldown. Strength varies with rarity.
- Sunfang saber-toothed tiger: rideable, two normal slots, 102 taming. Higher dexterity, with Sunfang Pounce: 12–27 bonus physical damage before resistance, on a 12-second cooldown.
- Both use native species training definitions, extended to 1–5 slots; normal scroll/cost/ability restrictions still apply. Legendary rolls use the existing custom-pet skill system and one starting slot. Stats are not halved on tame.
- Pet-book storage/claim and rarity colors work for both. Signatures act only for an active controlled pet against eligible wild enemies, and cannot run while mounted.
- One custom spawn per species, 10–15 second replacement after tame/removal/death. Tamed creatures are removed from the spawner's accounting without being deleted. Rarity distribution matches the steed: 25% Normal, 40% Rare, 25% Epic, 10% Legendary.
- Travel stone / [preview entries: Eodon - Stonehorn triceratops (652,2104,40), and Eodon - Sunfang tiger (636,2236,80), Ter Mur.

## Restart and backup

Clean save and shutdown, followed by backup E:/Backups/Haven/Prototypes/servuo-before-eodon-pets-20260912-224520. Saves, SHA-256 manifest, old Scripts.dll and replaced sources preserved. No test Saves imported. No client files or login settings changed.

Six production sources: HavenEodonPets, HavenEodonHabitats, HavenPetSignatures, HavenPetAppearance, HavenPetTrainingBridge and HavenPreview. Habitat activation uses the explicit HAVEN-EODON-PETS marker.

Release builds in isolated and live directories passed with zero warnings/errors. Server restarted around 22:46 EDT; authenticated account login, populated server list and game relay passed on port 2699. Original ModernUO remains unchanged.

## Validation and limits

Runtime tests passed both mount/dismount relationships, original creature body IDs, Legendary rarity, training definitions, real signature effects/cooldowns, pet-book store/claim, and replacement after taming without deletion of the tamed pet. Terrain checks require legal spawn ground, no house and connected paths to surrounding points. No native Spawner habitats were present in the saved world, so surveyed Eodon positions were used.

Separate-process save/reload passed an owner riding the triceratops, the exact tiger stored in the sanctuary book, rarity/training and both habitats. The initial claim-after-reload test needed its offline test player placed back into the world, simulating login; no production claim rules changed.

Mount graphics use native bodies 0x587 and 0x588 with same-ID mount items. The installed tiledata has zero animation overrides at these entries. TazUO's [mount resolution code](https://github.com/PlayTazUO/TazUO/blob/main/src/ClassicUO.Client/Game/GameObjects/Item.cs) supports this fallback. Rider alignment and animation quality have not been visually verified in the live client; this is a functional mount implementation, not a claim of completed visual QA.

Live habitat log confirmed both surveyed spawn sites installed at 22:46:10 EDT.
