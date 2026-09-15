# Original-world migration: required gates

The modern preview is a separate ServUO world. No original characters, houses, pets or currencies have been converted. Directly copying ModernUO save files into ServUO is not a migration method. The original server remains the source of existing progress.

## Before writing a converter

Take a consistent private backup of the original world and its exact code/configuration. Work from a disposable copy. Export an inventory of saved custom types and their versions, counts and ownership links; do not infer compatibility from matching class names.

Map each source type to a supported destination type or an explicitly reported unsupported category. Keep an old-to-new identity table for characters, pets, containers, guilds and houses. Do not silently discard unknown items or substitute currency values.

## What the rehearsal must account for

| Area | Required reconciliation |
| --- | --- |
| Character | Skills, caps, stats, quest/mastery progress, equipment and account association |
| Pets and companion | Owner, bond, rarity, training, learned abilities, equipment, inventories and pending missions/rewards |
| Currency and storage | Wallet, bank, loose gold, marks, shards, sovereigns, scroll books, resource ledgers and deeds; compare totals before and after |
| House and island | Ownership, plot/foundation, custom design, fixtures, secure/locked-down storage, travel destinations and usable floor access |
| Guild and market | Membership, leader, shared storage, vendor stock, prices and ownership; unsupported bot behavior must be listed |
| Timed systems | Outstanding jobs and rewards must complete at most once; preserve or explicitly resolve remaining time |

## Acceptance checks

1. Produce a conversion report with matched entities, missing types, broken references and balance differences. Any unexplained loss blocks replacement of the original world.
2. Load, save and reload the converted disposable world. Verify identities and totals remain stable, and rerunning the import cannot duplicate items or rewards.
3. Log in with a designated test copy, retrieve possessions, command pets, open house storage and visit the island. Check terrain and house placement against the destination map.
4. Rehearse rollback using the untouched original backup and original build. Keep test and production endpoints distinct.
5. Present the resulting report and remaining compromises before switching regular play to the converted world.

This is a migration plan, not evidence that conversion is implemented or that every old custom feature has a modern equivalent. Tonight's supported preview features and playtest steps are in [TONIGHT.md](TONIGHT.md).
