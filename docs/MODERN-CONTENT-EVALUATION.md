# Modern content foundation evaluation — September 10, 2026

## Decision

Use **ServUO as the candidate foundation for the next modern-content Haven prototype**. Keep the existing playable Haven server on ModernUO until companion portability, player-data conversion, and real gameplay tests pass. This is a recommendation based on a compiled and exercised fresh-world candidate, not approval to replace the live world or a claim that migration is finished.

The current project inherited a T2A/Felucca-oriented setup. Haven expanded it to an ML-compatible ruleset and a mixture of classic geometry, selected modern dungeon blocks, modern graphics, and custom later-era content. Merely replacing map files cannot supply working expansion mechanics. Conversely, the existing ModernUO engine is not itself an old T2A engine: engine technology and implemented game content are separate questions.

Given the requested Shadowguard, Blackthorn, Abyss crafting, naval gameplay, masteries, pet training and room sets, a broader content foundation is a better candidate than continuing to recreate every missing official system. This tradeoff should have been investigated earlier.

## Candidate and reproducible evidence

- ServUO branch: `pub57`, commit `d76bf4443cf76d081ddaf8f57c87ff33749256af`.
- Clean Release build: **0 warnings, 0 errors**; executable reports 57.4.0.0, targets .NET Framework 4.8, built with .NET SDK 10.0.201.
- Candidate expansion configuration: upstream default `EJ` (Endless Journey). Its configuration describes EJ as development in progress; do not treat every feature as complete.
- Modern EA Classic data was read directly from an existing private installation. No client data was changed or published.
- Candidate listened on **127.0.0.1:2699**, separate from Haven on 127.0.0.1:2593. Candidate stopped after each test phase. No live saves or accounts were loaded.
- **38 passing checks, 0 failures** in the final fresh-world and reload run: 29 fresh-world checks and 9 reload checks. The harness source and sanitized results are in `tools/evaluation` and `docs/evaluation`.
- The checked-in runner was then exercised against a second newly downloaded checkout and fresh world: clean build and all 38 checks passed again.
- An earlier harness pass used filenames instead of class names for two bosses. `OsiredonTheScalisEnforcer.cs` declares `Osiredon`; `Charybdis.cs` declares `Charydbis`. Those were harness lookup errors, corrected before a new fresh-world run. Initial results are retained in the local evaluation archive.
- Current upstream ModernUO tree was also inspected at `865a8cf2185829042f20b51e06523172caa2d378` (9,060 entries, not truncated). Expected native Shadowguard, Imbuing, kitchen-set and High Seas paths were not found by the targeted path searches. This is a source-inventory finding, not a compiled test of that upstream revision.

## What actually ran

| Area | Evidence | What this does not establish |
|---|---|---|
| Maps | All six facets loaded and returned land data | Every entrance, bridge and treasure site is traversable |
| Kitchen set | Four counters; pie safe and china cabinet containers; cabinet gold persisted across restart; both stove orientations constructed; both three-tile basins changed water quantity and artwork correctly | Client rendering, house placement, player interaction or stove cooking end to end |
| Shadowguard | Controller allocated instances; all six encounter types ran setup and a tick; room addons created; Bar pirates and 16 Orchard puzzle trees verified; controller reloaded | Completing puzzles, party admission, roof combat, rewards or recovering an active encounter after restart |
| Blackthorn | An invasion began, created a beacon and spawned enemies in wave one | Completing all waves, earning currency, vendor purchases or both-facet playthroughs |
| Imbuing | Skill callback registered; a weapon returned property-count and intensity rules | Resource consumption, success rates or crafting balance |
| Ships | Britannian and Orcish vessels constructed; Britannian vessel found a fitting ocean site and moved | Client steering, passengers, cannons, fishing, SOS or companion following |
| Sea bosses | Osiredon, Corgul and Charydbis constructed and deleted successfully | Summoning, combat, corpse loot and rare-drop probabilities |
| Housing | Custom foundation constructed from modern multi data with ownership and components | Building UI, castle-size customization or importing the R.E.C. home |
| Persistence | Fresh isolated world saved and restarted; kitchen contents and Shadowguard controller checked | Compatibility with Haven/ModernUO save files |

These are automated server smoke and behavior checks, **not client playthroughs**. No account was created and the entire stock world was not generated. The stock `CreateWorld` setup offers SA/TOL decoration, High Seas, Doom, Blackthorn and spawner generation; those still need a staged gameplay rehearsal.

## Availability versus import effort

The candidate contains the exact `DecorativeKitchenSet`, `PieSafe`, `ChinaCabinet`, `ButcherBlock`, `Countertop`, `WoodStoveAddon` and `WashBasinAddon` implementations. The modern kitchen is not a feature we need to invent.

Source sizes below are a scope indicator, not time estimates or a count of all transitive dependencies:

| Candidate content directory | C# files | Nonblank lines |
|---|---:|---:|
| Shadowguard | 14 | 10,168 |
| Blackthorn dungeon | 147 | 8,368 |
| Imbuing | 7 | 3,583 |
| High Seas | 134 | 25,672 |
| Pet training | 11 | 7,179 |
| Skill masteries | 45 | 7,007 |

These systems also depend on shared creatures, combat, crafting, loot, boats, packets, regions and persistence. Copying their directories into ModernUO would not be a finished import.

The committed Haven tree has **373 CustomBots C# files and 66 native patches**. The working tree also contains unfinished changes; the broader working source has 380 C# files, with 122 matching at least one of the audited ModernUO-specific serialization, timer or collection API patterns. Those extra edits must be reconciled explicitly rather than silently lost in migration.

Haven uses generated serialization, partial classes, ModernUO reader/writer interfaces and engine-specific hooks. ServUO uses its own engine contracts and legacy serialization. **Do not copy Haven saves into ServUO or assume a drop-in bot import.** A conversion must recreate the correct types and references, not merely copy gold and skill values.

| Approach | Advantage | Cost and conclusion |
|---|---|---|
| Continue current Haven and import small items | Keeps existing behavior and saved characters; kitchen set is a bounded candidate | Reasonable for isolated items, but does not resolve the large modern-content backlog |
| Import whole later-era systems into ModernUO | Keeps bot architecture and live data format | Large dependency-heavy integration across shared systems; continues our present compatibility burden |
| Port Haven-specific systems onto ServUO | Reuses the candidate's working modern content and related shared systems | Significant bot/API and save conversion work; **preferred prototype direction for this user's goals**, subject to a successful companion slice |

## Next implementation sequence

Progress: a first companion prototype now has commands, native combat/healing, owner-only inventory, parties and persistent timed missions. See [the buildable prototype](../modern-prototype/README.md) for scope, tests and remaining client/data-conversion gates.

1. **Companion vertical slice in the isolated candidate.** Port one companion's ownership, follow/guard/attack/heal, inventory and mission receipt. Save and reload it. Exercise party loot routing with a real player. Reuse candidate combat APIs rather than copying the whole Haven combat stack. Failure here can change the foundation recommendation.
2. **Explicit data-conversion contract.** Inventory all custom serialized types in a copied save. Export stable identifiers, ownership and relationships plus accounts/characters, skills, equipment, pets, bonded state, currencies, ledgers, scroll balances, houses, storage, guilds and quest state. Dry-run imports must report every unsupported type and reconcile totals and references. No silent dropping of items.
3. **One complete modern dungeon and one naval loop.** Generate the necessary stock world content in the candidate. Complete Shadowguard with two players/companions and verify rewards and re-entry. Complete fishing/net/boss/loot with a ship. Test real client art and movement. Only then broaden the world setup.
4. **Preserve Haven additions deliberately.** Port custom pet progression, AFK missions, market economy and conveniences in small batches. Avoid duplicating native masteries, pet training, expansion loot and crafting. Reconcile custom overcaps and power levels explicitly.
5. **Move custom island geometry and housing last.** Survey the island coordinates on the modern maps; merge changes into matching private server/client data. Rebuild furnishings with actual modern sets and verify stairs, collision, doors and storage. Existing design previews remain undeployed.
6. **Rehearse a full upgrade and rollback on copies.** Compare balances, item counts/identities and ownership before/after, then run the friend checklist. A live cutover is a separate deployment after these gates, not part of this evaluation.

No reliable completion estimate for a full migration is justified until steps 1 and 2 expose the API and data-conversion work. The result of this evaluation is a working candidate and a grounded direction, not a ready-to-play replacement for Haven.

## Sources

- [Pinned ServUO source](https://github.com/ServUO/ServUO/tree/d76bf4443cf76d081ddaf8f57c87ff33749256af)
- [ModernUO source inspected](https://github.com/modernuo/ModernUO/tree/865a8cf2185829042f20b51e06523172caa2d378)
- [Current Haven install and limitations](FRIENDS-TESTING.md)
- [Final runtime checks](evaluation/modern-content-runtime-checks.txt)

The evaluation package contains original harness code, instructions and results, not EA game assets, live accounts or player saves. Upstream source stays in its separately cloned repository with its license notices.

## Reproduce on Windows

Use a new destination, an installed .NET SDK with .NET Framework 4.8 build support, Git, and your own modern Classic client data. Run from this repository:

```powershell
./tools/evaluation/Run-ModernContentEvaluation.ps1 `
  -Destination 'E:/HavenTests/ModernContentFresh' `
  -ClientData 'D:/Games/Ultima Online Classic' `
  -DotnetPath 'C:/path/to/dotnet.exe'
```

The runner refuses an existing destination or occupied test port, fetches the pinned source, compiles it, runs the fresh-world and reload phases on loopback, and stops the candidate. It does not log into or reconfigure Haven. `HavenEvaluation.cs` belongs only in a disposable test server; it creates fixtures, saves and exits automatically.
