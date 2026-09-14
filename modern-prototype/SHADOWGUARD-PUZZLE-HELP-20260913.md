# Shadowguard companion puzzle help — 2026-09-13

Jenna's main companion menu has a Help with puzzle button, also available as Puzzle beside her name on the compact combat bar. The combat bar stays open after using it. It requires the normal living, nearby, owner-controlled companion and an active room in which the owner is a participant.

## Orchard

Apples last 120 seconds in Haven (native 30 seconds outside preview mode). Tree trunks and foliage show their virtue pair; apples name the opposite tree. The button picks an apple when the owner is within three tiles of a tree, colors the correct tree trunk and foliage bright gold, and uses the native apple targeting action when the owner is within ten tiles and has line of sight. It handles one pair per request. An apple held by another participant is not taken or replaced. Temporary highlighting clears when the apple is gone or replaced; startup clears saved temporary gold hues.

## Fountain

Stand within three tiles of the marked unfinished spigot. Collect canal pieces dropped by this encounter's water elementals into your backpack or Jenna's backpack. The helper plans a clear cardinal route inside the room, checks that all required pieces are available, then rotates and places those existing pieces and runs the native flow checker. It reports the required count if supplies are short. Filled canals and other placed canals are avoided; pick up loose pieces if they obstruct a route. It completes one spigot per request. It does not create materials or directly award room completion.

## Verification

- Audit and production builds: zero warnings/errors.
- Eight randomized native Fountain layouts: 32 spigots completed through native flow checking, using pieces from both backpacks; insufficient supplies left progress unchanged.
- All 16 Orchard virtues agree with native opposite-pair matching; apple lifespan verified at 120 seconds.
- Highlight test: both matching trunk and foliage receive the gold hue; the source tree retains its normal hue.
- Active Orchard test: button picked an apple and invoked native targeting; both matching trees were removed after the native delayed throw.
- No live client visual inspection. Only Orchard and Fountain are supported, not every dungeon puzzle.

Native patch: patches/0048-shadowguard-puzzle-help.patch. Custom sources: HavenOrchardHelp.cs, HavenFountainHelp.cs, and the companion menu entry in HavenCompanion.cs. Test fixture: tests/ShadowguardHelpSmoke.cs; run its Fountain and asynchronous Orchard entry points in separate disposable server sessions because native canal-fill timers outlive fixture cleanup.

## Completion and looting time

Patch 0049-shadowguard-completion-loot-time.patch gives Haven's completed puzzle rooms five minutes before their normal lobby return (formerly 60 seconds). The Roof already used five minutes. A gold completion message explains the delay and the character's Exit Shadowguard option. Participating players still present receive room credit immediately, so choosing to leave before cleanup cannot lose progress. Native time limits for unfinished encounters and cleanup after everybody leaves remain in effect.

ShadowguardCompletionSmoke verifies every room's five-minute duration, immediate completion credit retained after departure, and a single delayed reset despite repeated completion calls. The timer test uses an accelerated one-second subclass; no five-minute live gameplay wait was performed.
