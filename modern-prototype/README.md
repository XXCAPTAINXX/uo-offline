# Haven companion prototype for ServUO

This is the first working companion slice on the modern-content candidate. It is **separate from the current Haven installer and live server**. It starts a fresh world and does not import existing characters or saves.

The source targets ServUO `pub57` at `d76bf4443cf76d081ddaf8f57c87ff33749256af`. Haven's ModernUO companion served as the behavior reference. This implementation uses ServUO's native creature AI, spells, parties, containers and manual serialization.

## Working slice

- `[c` or `[companion` claims one permanent, bonded companion per character. Reopening the gump does not teleport or duplicate it.
- Follow, guard, stay and targeted attack. Guard chooses the closest eligible hostile to the owner, excluding players, controlled/summoned pets and wild tameables. Direct attacks can target wild tameables where native rules permit.
- Spoken `all follow me`, `all guard me`, `all stay`, `all stop`, `all kill`, `all attack`, and `all heal me`, plus the companion's name instead of `all`. Attack speech shares the native targeting cursor with ordinary pets.
- Native Greater Heal with casting delay, mana cost, range, poison/wound checks and a cooldown. Automatic low-health owner healing while connected; manual healing through the gump.
- Native owner-party membership and pet damage attribution, giving the owner loot rights. Corpse auto-looting and routing to the companion pack are **not** implemented here.
- Owner-only backpack and nested-item deposit/withdraw permissions. Capacity: 1,000 items, no container weight limit. Access uses the native **two-tile** inventory range; Haven's 12-tile hook is not ported yet.
- Five-minute demonstration supply mission: 500 gold, merged stacks, pending rewards when the pack is full, a report and restart recovery. This is a timed mission proof, not physical gathering or the full mission catalog. The follower slot remains reserved while away.
- Explicit recall with ownership, life, stable, follower and combat checks. No automatic appearance beside an offline owner.
- Release, transfer, friend and drop-all orders blocked for the permanently bound companion.

## Build a fresh interactive prototype

On Windows, use Git, a .NET SDK capable of building .NET Framework 4.8, and your own modern EA Classic data. The destination must not exist. From the repository root:

```powershell
./modern-prototype/Build-Prototype.ps1 `
  -Destination 'E:/HavenTests/CompanionInteractive' `
  -ClientData 'D:/Games/Ultima Online Classic' `
  -DotnetPath 'C:/path/to/dotnet.exe'
```

This builds but does not start the server. Run `ServUO.exe` from that destination, follow native first-account setup and use a separate client profile pointed at the same modern data and **127.0.0.1:2699**. Create a test character and use `[c`. The listener is loopback only. The builder does not change an existing client profile, account, world or game asset.

The whole stock world is not populated by the builder. Use native world-creation tools in this disposable world for broader testing. Do not copy a Haven/ModernUO save into it.

## Automated verification

Add `-Test` with another new destination to build, run fresh-world checks, save, restart and verify recovery. Test mode creates a disposable account with a random password and no client connection, then stops the server. It writes `companion-checks.log`.

The final run passed **34 checks**, with zero failures and a clean Release build (zero warnings/errors). Checks cover ownership, duplicate prevention, speech, shared pet targeting, actual AI movement, melee damage, spell healing/mana, party membership, owner loot credit, full-pack reward retention, stacking, mission timing and persistence. See [recorded checks](companion-checks.txt).

The packaged builder was then run against another newly downloaded checkout and fresh world. Its build and all 34 checks passed again.

Mission deadlines are accelerated by reflection in the test harness only. Production duration is unchanged. The reload test verifies offline completion, then restores the player's saved position to simulate the location part of login. It is not a network login test.

Early fixture failures used an unsuitable map patch and omitted native `Player=true`; these were corrected and their logs retained locally. The shared-target implementation was also corrected so companion speech joins the native pet cursor instead of replacing it.

## Remaining work

This is not a full replacement for live Alden. Role switching, bard masteries, caster rotations, equipment progression, resurrection, taming and dungeon assistance, full AFK missions, ledgers, companion-owned pets, remote inventory, guild/bot economy and the island are not ported here.

Next gates:

1. Real client testing: readable gump, drag/drop, two-player parties, combat/death, logout/login and an actual five-minute mission.
2. Extend the companion using native modern masteries and pet systems.
3. Dry-run explicit conversion of custom saved types and balances, preserving identities and owner relationships and reporting unsupported types.
4. Rehearse a complete migration before any live deployment.

The current live Haven server and player saves have not been modified.
