# Haven Field Guide deployment — September 9, 2026

Added a searchable 18-chapter guide through `[guide`, `[havenhelp` and `[wiki`, a
free blessed guidebook, starter delivery and a login reminder. Existing characters
can claim the book through the guide without reclaiming their starter bundle.

`docs/player-guide.json` is the shared source. Run `tools/build_player_guide.py`
after changing it to regenerate both the compiled in-game chapters and
`docs/PLAYER-GUIDE.md`. The guide covers player controls and implemented systems,
with a chapter describing unfinished content rather than presenting it as complete.

Validation: Release build and all 1,022 UOContent tests passed using the prepared
matching map data. New checks cover duplicate pack/bank book claims, lost-book
replacement and a full backpack. In-game readability and scrolling are listed as
S00 in the player checklist; the layout has not been visually checked in the live
client, to avoid ending the owner's offline companion assignment by logging in.

Deployment: confirmed world save, backed up 18 saved-world files plus configuration,
CustomBots and assemblies to `E:\Backups\Haven\Deployments\field-guide-20260909-230928`,
installed and hash-checked 322 payload files, restarted and confirmed another save.
Source implementation commit: `5383ea8`. Listener remained local at port 2593.
The companion's running assignment, 21 completed runs and next deadline matched
before and after restart. The existing E: automatic backup paths were preserved.
