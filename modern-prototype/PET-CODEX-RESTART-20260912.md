# Pet and Codex release — September 12, 2026

Published to the Haven ServUO preview on port 2699 after the user finished taming the Legendary bear. The server saved and stopped cleanly around 18:49 EDT; the new process started at 18:50:51 EDT. Startup, authenticated account login, populated server list and game relay passed. The startup error log is empty. Live compilation completed with zero warnings and zero errors. All 23 deployed source files match their release hashes.

## What changed

- **Champion's Codex:** no item or weight capacity limit. Archived contents add no backpack weight or slots; the book itself still weighs one stone. Existing accepted scroll/skull/primer/binder types and normal withdrawal checks remain. Combining and splitting work inside the archive.
- **Pet sanctuary:** matches the dark pet-training panels with readable text and rarity-colored names. “Turn in Rare and below...” previews the pet count and credit total, then requires confirmation. It includes eligible Normal/Rare pets throughout the book, irrespective of the current page/search. Favorites, bonded pets, Epic and Legendary pets are excluded. Every pet's eligibility is checked again when confirming; newly added pets are not silently included.
- **Other pet menus:** training, upgrade confirmation, lore, mission exchange, companion pet management, and training planning/info use the same dark theme. Lore no longer displays the internal Hellhound class name or the namespace on Dragon Breath. Innate healing is listed for the bear and Hellhound.
- **Ancient Hellhound:** rideable using the native Ancient Hell Hound mount art. Existing pets retain their serial, ownership, skills and training. Native dismount and mount-prevention rules work; storing the pet safely dismounts it. Mounted state survives saving and reloading.
- **Frostbound bear:** innate Healing for itself and its owner, alongside Guardian Roar and Colossal Rage. Uses native healing/cure calculations, a two-second action and eight-second cooldown; requires a living, controlled, unmounted bear and a valid nearby recipient. Existing bears receive at least 110 Healing with a 120 cap and 90 Anatomy without reducing higher skills. This does not change their taming requirement.
- **Legendary skills:** new rolls can select any of the 24 trainable pet skills, including skills currently at zero such as Discordance. They receive 3–5 distinct rolls at 125–150 with matching caps. Existing legendary rolls are preserved. A high skill does not automatically teach its associated magical ability; native training requirements still apply.
- **Overcap bard effects:** rarity-tagged pets and companions gain longer targeted peace above 120, up to 30% longer at 150. Discordance can reach a 34% debuff at 150 before native difficult-target reductions. Actual overcapped Musicianship is used instead of replacing it with 120. Ordinary NPC behavior is unchanged. Lore describes applicable bonuses; this is not a claim that every skill gained a new custom effect.
- **Jenna's bard support:** Tame assist can maintain Resilience and Perseverance instead of disabling mastery songs. It avoids replacing an active spell/target while attempting to calm the animal. Offensive mastery selection is suppressed during Tame assist. The Roles screen reports shared buffs or missing Musicianship, party, range, or mana requirements. Shared masteries still require 90 Musicianship, sufficient bard skill, party membership and mana.

## Taming investigation

The frostbound bear's base taming requirement is 100; rarity does not directly raise it. Native roll rounding gives roughly 1% success at exactly 100, 21% at 110 and 41% at 120 for a fresh animal at that requirement. Earlier conversational figures of 0.2%, 20% and 40% described the unrounded formula and were corrected during the investigation. Prior owners can raise difficulty.

Legendary stats and skills increase native barding difficulty. Jenna waits for successful calming before beginning an assisted tame. Repeated peace failures therefore need not represent completed tame attempts. The old test snapshot showed 113.4 Taming, but it predates the user's latest gains and is not a live skill reading. No taming probability was changed in this release. The user confirmed the bear was obtained before this restart.

## Verification

- 600 scrolls accepted without increasing backpack weight or item count; combine, split and original-item withdrawal passed. A separate process reloaded all 599 remaining scrolls with unlimited capacity and zero content weight/slots.
- Bulk-exchange eligibility excluded Epic/Legendary, favorites and bonded pets.
- Native bear healing increased an unrelated fixture owner's health, independently of companion healing.
- A connected-owner test received both native Peacemaking mastery effects at 105 Peace/100 Music while following and during an active Tame assist session.
- Hellhound ownership restrictions, native dismount prevention and safe ticket storage passed. A second process retained the mounted pet's identity, owner and 150 Discordance and dismounted the same creature successfully.
- All 24 trainable skills were eligible from zero; rolls remained within 3–5 and 125–150, and existing rolls were not replaced.
- Native targeted peace duration and Discordance effects passed their overcap integration checks, including the 150 ceiling and unchanged ordinary NPC behavior.
- Pet-menu frame and label contrast checks passed for sanctuary, bulk confirmation, training, upgrade, lore, planning and info. These are server-side checks; no in-client visual review of this batch is claimed.

Early test failures were resolved before deployment: an offline owner fixture automatically cleared its songs, a tameable rabbit completed its tame before the active-taming assertion, and the mount reload assertion initially expected an offline owner's internal map instead of its logout map. The corrected tests passed without weakening the gameplay assertions.

## Backup and scope

Backup: `E:/Backups/Haven/Prototypes/servuo-before-pet-codex-20260912-184910`.

Contains cleanly stopped Saves, Scripts, Config and root files. All 43 save files were SHA256-verified; `SAVE-SHA256.txt` is in the backup. Deployment manifest and source hashes are in `verification/pet-codex-release.json` and `verification/pet-codex-live-SHA256.txt` in the workspace. No test fixtures or test Saves were copied to the live server.

This restart deployed 21 custom source files and the Peacemaking/Discordance native call sites. The island/house replacement was not deployed. The separate original ModernUO server was not restarted.

For rollback, stop the preview cleanly first and preserve its newest world separately. Restore the complete pre-release backup as a matched code/world set; old code cannot safely read saves containing the new mounted Hellhound item type. Such a rollback would lose play since this backup, so prefer a forward fix when possible.
