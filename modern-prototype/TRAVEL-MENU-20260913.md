# Travel menu revision (test build)

Five categories preserve all 38 fixed stops and add saved champion spawners and both mini-champ camps. Rows show facet, Felucca PvP status, and active/dormant state. Menu snapshots preserve destination identity across world sorting changes. Travel revalidates deleted objects, combat and clear approaches; champion landings check both source-floor and terrain elevations, line of sight and house exclusion. No encounter activation is performed.

Critic review identified unbounded text, terrain-only landings and misleading errors; bounded rows, floor candidates and separate combat/landing messages address those issues. Native test world: all 29 champion/mini-camp approaches passed; all 38 fixed stops occur exactly once; category/page construction and deleted-target rejection pass. Build passes with zero warnings/errors. Actual client visual review remains pending.

User clarified plain rectangular buttons, not ornate borders or arrow-labelled controls. HavenStoneGump now uses a plain rectangular surface with smaller underlying native click targets and dark text; applies to reward shops and travel. No live deployment or restart in this turn.
