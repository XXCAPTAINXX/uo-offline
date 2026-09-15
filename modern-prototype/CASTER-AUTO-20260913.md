# Companion caster Auto rotation — 2026-09-13

Matches the rotation in the player's installed JCS_NecroWeaverBar_Beta4.py Auto mode: self-targeted Wildfire followed by Thunderstorm whenever native cast/recovery timing allows. Word of Death does not interrupt this rotation.

Wildfire was previously absent from the companion selector. Thunderstorm had an extra six-second cooldown, fixed eight-tile selection range and the general short-range automatic-target filter.

The new field refresh is based on native skill/focus duration, with a 3.75-second refresh margin. Refresh cooldown starts after successful sequence validation, so rejected/fizzled casts remain eligible to retry. Thunderstorm uses the native radius (3 + Arcane Focus). The selector retains the bar's default 40-mana reserve and native scaled costs. Magery remains a fallback when enemies are outside the area-spell radii.

Both native area spells filter to permitted hostile creatures in companion support range; controlled/summoned pets and ordinary tameables are excluded. Champion-spawn creatures are eligible. Native damage, mana use, skill checks and spell timing remain.

Runtime CasterAutoSmoke verified actual Wildfire and Thunderstorm damage against two champion-spawn enemies, harmless animal exclusion, low-health target handling, field refresh, no artificial Thunderstorm throttle, and mana reserve.
