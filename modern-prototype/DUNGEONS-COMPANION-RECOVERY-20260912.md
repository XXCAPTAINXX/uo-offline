# Dungeon, companion and pet release — September 12, 2026

## Live restart record

Each restart clean-saved first, backed up the stopped save and replaced source/assembly files, and SHA-256 verified all backup save files. All live and isolated builds completed with zero warnings and errors. Every restart passed the local account-login, populated server-list and game-relay checks on port 2699. The separate original server was not changed.

| Restart (EDT) | Change | Backup under `E:/Backups/Haven/Prototypes/` |
| --- | --- | --- |
| 20:44:45, reopened approximately 20:45 | Doom overhead status/control, solo reward rates, Gravefire weapons | `servuo-before-solo-dungeons-20260912-204445` |
| 20:47:48, reopened approximately 20:48 | Shadow Knight reveal repair | `servuo-before-shadow-reveal-20260912-204748` |
| 20:59:35, reopened 21:00:05 | Companion Recall/bard effects and expanded dungeon weapons | `servuo-before-companion-recall-buffs-20260912-205935` |
| 21:26:57, reopened 21:28:39 | Jenna recovery, support range, companion safeguards and pet training changes | `servuo-before-jenna-restoration-20260912-212657` |

The first three restarts changed code only. The last applied a targeted Jenna restoration to a copy of the latest stopped save, verified the result after reloading, then deployed that save with matching hashes. It did not replace the world with an older backup. In-client visual confirmation of new menus and buff icons remains pending.

## Jenna recovery

The current account link pointed to Silas Ironwood, a dead male Warrior with no bard skills. No Jenna record existed in the current mobile or item save. The newest intact Jenna found in the inspected backups was `servuo-before-peace-attempt-20260912-143105`.

Restored Jenna Ashford as female, Bard, 46 completed missions, Musicianship 117, plus 16 pack/equipment/progression records. The backup held Peacemaking 88.2; the user's subsequent report of 100+ was used to set a conservative 100 minimum. The exact later fractional value and any other changes after 14:31 could not be reconstructed. The recovery therefore restores this known snapshot, not a claim of complete recovery of every later gain.

Jenna's former mobile serial 38035 had been reused by Jeremy, and multiple item serials had been reused by unrelated objects. All restored objects received fresh serials, preserving those existing objects. Jenna is now serial 5669 (`0x1625`). Silas was preserved. The surviving legendary stormhorn kirin, serial 44252, was not duplicated or reassigned; the old mount item and assignment record were omitted from the import.

The recovery bundle and verification-only export/import source are retained locally at `E:/Backups/Haven/Prototypes/jenna-recovery-20260912` and `verification/HavenRecoveryLiveBuild/Scripts/JennaRecovery.cs`. The importer was never deployed as live server code.

Companion lookup now recovers an existing owned companion before creating a recruit. A stale saved link with no matching companion reports that recovery is needed rather than silently creating a replacement. Companions are protected from pet-ticket storage, are bonded on Recall, and use bonded death handling.

## Behavior changes

- `[doom` reports artifact points and calculated chance overhead in cyan. Luck still increases earned points using the native formula; the displayed chance includes the solo modifier.
- `[doomcontrol` supplies a blessed device with 1–5 bosses per room. The setting is shared, persists on the devices, and changes the next room rather than replacing the current room's bosses.
- Doom and Blackthorn artifact-roll probabilities are multiplied by 1.5, capped at 100%. Blackthorn beacon chance rises from 15% to 22.5%. Shadowguard's final-boss chance rises from 20% to 30% at zero Luck, retaining the Luck component. Loot quality and tables are unchanged.
- The Shadow Knight's half-health hiding phase ends after eight seconds. Nearby controlled pets can reveal it earlier using Detect Hidden, with normal skill checks. Restarts rearm the reveal timer for saved hidden knights; revealing also clears the frozen state.
- Recall accepts dead, distant, cross-map, stabled and combat-active companions. Dead companions return and use the existing five-second recovery. Ownership and a valid destination remain required; early mission recall still forfeits unfinished rewards.
- Qualified Bard companions prefer Peacemaking's Resilience/Perseverance. Their bound owner receives both actual effects and native buff icons without needing party membership. Regular Discordance attempts remain available. Effects are removed when support conditions fail.
- Companion command, inventory and beneficial support range is 24 tiles, including bard songs and owner/owned-pet bandaging. Line of sight remains required. Recall has no distance limit. Hostile target acquisition and taming reach were not broadly increased.
- Ancient hellhound Dragon Breath is innate rather than a stored training ability. Existing ability entries are removed and the normal breath effect, mana use and cooldown run independently. It is excluded from training choices. No speculative training-point refund was made for its native starting ability.
- Frosthunt bears can choose the native trainable magical schools, special abilities, moves and area effects, and see all native pet skill-cap options. Training costs, scroll requirements and native ability-combination limits remain.

## Dungeon slayer weapons

Each theme offers a one-handed, shield-compatible Whirlwind scimitar and mace with equipment evolution. Each costs 225 Marks. Doom bosses have a 4% combined Gravefire drop chance; other eligible bosses have a 4% themed-weapon roll when their native slayer classification matches an available theme. This is not a guaranteed drop. Mixed dungeons may require another pairing.

| Theme | Slayer pair | Suggested routes |
| --- | --- | --- |
| Gravefire | Demon / Undead | Doom |
| Abyssward | Demon / Elemental | Hythloth, Fire, Blood, Stygian Abyss |
| Cryptwarden | Undead / Repond | Deceit, Covetous, Khaldun |
| Scalebreaker | Reptile / Elemental | Destard, Ice, Fire |
| Webcleaver | Arachnid / Repond | Terathan Keep, Despise, Wrong |
| Thornbane | Fey / Elemental | Twisted Weald, Blighted Grove, Prism of Light |
| Stormguard | Elemental / Repond | Shame, Blackthorn's, Shadowguard |
| Primal Warden | Eodon / Arachnid | Eodon/Myrmidex hunting |

## Validation and remaining work

Isolated checks verified probability scaling/capping, Doom display agreement, next-room size changes, stale/invalid device response rejection, native slayer fields and shield compatibility, Shadow Knight timeout and pet reveal, real bard effect lookup and buff entries without a party, out-of-range cleanup, and Recall during death/combat/stabling/across maps. Pet checks verified innate breath no longer occupies the ability profile and all configured bear skills/magical choices validate. Reward filtering retained catalog identities. Jenna's targeted restoration survived a save and reload with its corrected account link.

The older missing-item recovery regression was not applicable to the latest save because the player had since moved recovered belongings. Its ownership assertion failed as expected against that newer arrangement; current feature checks were then run separately without saving fixtures.

Still pending: expanded dye palette; Eodon pet implementation and mounted-art verification; Sovereign booster items. No booster item or rideable Eodon creature was advertised as delivered in this release.
