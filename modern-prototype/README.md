# Haven modern preview for ServUO

This is a populated modern-world preview with Haven's first companion port. It is **separate from the current Haven installer and live server**. It starts a fresh world and does not import existing characters or saves.

The source targets ServUO `pub57` at `d76bf4443cf76d081ddaf8f57c87ff33749256af`. Haven's ModernUO companion served as the behavior reference. This implementation uses ServUO's native creature AI, spells, parties, containers and manual serialization.

## Working slice

- `[c` or `[companion` claims one permanent, bonded companion per character. Reopening the gump does not teleport or duplicate it.
- Follow, guard, stay and targeted attack. Guard chooses the closest eligible hostile to the owner, excluding players, controlled/summoned pets and wild tameables. Direct attacks can target wild tameables where native rules permit.
- Warrior, Caster and Archer roles use native melee, Magery and bow AI. Switching roles preserves equipment and refuses combat, active casting or insufficient pack space. New movement orders cancel queued spells and targets.
- Spoken `all follow me`, `all guard me`, `all stay`, `all stop`, `all kill`, `all attack`, and `all heal me`, plus the companion's name instead of `all`. Attack speech shares the native targeting cursor with ordinary pets.
- Native Greater Heal with casting delay, mana cost, range, poison/wound checks and a cooldown. Automatic low-health owner healing while connected; manual healing through the gump.
- Native owner-party membership and pet damage attribution, giving the owner loot rights. Corpse auto-looting and routing to the companion pack are **not** implemented here.
- Owner-only backpack and nested-item deposit/withdraw permissions. Capacity: 1,000 items, no container weight limit. Access uses the native **two-tile** inventory range; Haven's 12-tile hook is not ported yet.
- Demonstration supply missions of 5, 15 or 30 minutes: 100 gold per minute, merged stacks, pending rewards when the pack is full, a report and restart recovery. This is a timed mission proof, not physical gathering or the full mission catalog. The follower slot remains reserved while away.
- Explicit recall with ownership, life, stable, follower and combat checks. No automatic appearance beside an offline owner.
- Release, transfer, friend and drop-all orders blocked for the permanently bound companion.

## Build a fresh interactive prototype

On Windows, use Git, a .NET SDK capable of building .NET Framework 4.8, and your own modern EA Classic data. The destination must not exist. From the repository root:

```powershell
./modern-prototype/Build-Prototype.ps1 `
  -Destination 'E:/HavenTests/CompanionInteractive' `
  -ClientData 'D:/Games/Ultima Online Classic' `
  -DotnetPath 'C:/path/to/dotnet.exe' `
  -PopulateWorld
```

This builds, generates the native world in stages, saves, verifies a second startup and stops. Then run `Start-Preview.ps1` from that destination. Point a separate client profile at your modern Classic data and **127.0.0.1:2699**. Native account creation occurs on first login; choose a new test account. The listener is loopback only. The builder does not change an existing client profile, account, world or game asset.

Use `[preview` for a one-time test kit and ten travel destinations, or `[c` for Alden. The kit raises this test character's skills/caps to 120, stats to 100 each, and supplies armor, leech weapons, a shield, bow, spellbooks, a Parry III primer, bandages and a bank check. Equip items yourself; read the primer to learn its mastery. These are test conveniences, not final progression balance. Entire-kit capacity is checked before claiming.

`Stop-Preview.ps1` requests a world save and clean shutdown. It does not forcibly terminate a process or touch the original Haven. Startup and shutdown scripts were exercised against the separate populated preview.

Omit `-PopulateWorld` for a bare build. `-Test` and `-PopulateWorld` cannot be combined. Do not copy a Haven/ModernUO save into this server.

## Native world

The preview's 25 native setup stages cover towns, doors, vendors/spawns, travel links, decorations, Doom, SA/Underworld, High Seas, revamped dungeons, Blackthorn and TOL/Shadowguard. Runtime validation found 6,835 spawners, six Doom Gauntlet controllers, 17 Shadowguard instances and 14 specific Blackthorn entry/exit links. Trammel's native Blackthorn entrance is beneath the castle; Felucca uses the older stairway.

Generation and save/reload validation also passed on a second fresh checkout using `-PopulateWorld`. See [world checks](world-checks.txt). This proves setup and persistence; it does not certify every boss, quest or dungeon completion path.

## Automated verification

The current milestone passed **64 checks**, with zero failures and zero Release-build warnings/errors. Local evening instructions are in [TONIGHT.md](TONIGHT.md).

Add `-Test` with another new destination to build, run fresh-world checks, save, restart and verify recovery. Test mode creates a disposable account with a random password and no client connection, then stops the server. It writes `companion-checks.log`.

Checks cover ownership, duplicate prevention, speech, shared pet targeting, actual AI movement, melee damage, spell healing/mana, ranged role combat, party membership, owner loot credit, full-pack reward retention, stacking, mission timing, preview travel/claims and persistence. See the exact latest [recorded checks](companion-checks.txt).

The original 34-check companion slice was reproduced on a fresh checkout. The suite has since expanded with the preview controls and role switching. Ranged tests explicitly activate the fixture's AI and lock movement through native test attachments so melee cannot satisfy the damage assertion; real clients activate their local sectors normally.

Mission deadlines are accelerated by reflection in the test harness only. Production duration is unchanged. The reload test verifies offline completion, then restores the player's saved position to simulate the location part of login. It is not a network login test.

Early fixture failures used an unsuitable map patch and omitted native `Player=true`; these were corrected and their logs retained locally. The shared-target implementation was also corrected so companion speech joins the native pet cursor instead of replacing it.

## Remaining work

This is not a full replacement for live Alden. Bard masteries, Spellweaving rotations, equipment progression, resurrection, taming and dungeon assistance, full AFK missions, ledgers, companion-owned pets, remote inventory, guild/bot economy and the island are not ported here.

Next gates:

1. Real client testing: readable gump, drag/drop, two-player parties, combat/death, logout/login and an actual five-minute mission.
2. Extend the companion using native modern masteries and pet systems.
3. Dry-run explicit conversion of custom saved types and balances, preserving identities and owner relationships and reporting unsupported types.
4. Rehearse a complete migration before any live deployment.

The current live Haven server and player saves have not been modified.
