# Corsair's Rest: settlement decoration

The island uses native artwork available in the installed 7.0.23.1 client. No client patch is required. The artwork survey indexed 28,572 named tile entries and produced contact sheets for coastal planting, furniture, supplies and construction pieces.

The authored layout adds shellstone paths between the guild compound, kitchen garden, resource areas and harbor; grouped trees with matching foliage; palms, ferns and flowers; a fenced cultivated garden; a harbor mess and signal cannon; and a roofed cargo chandlery. Interior additions furnish the galley, council room, captain's quarters, crew loft and chart room. Decorative props do not generate resources. Existing crop spawners, harvest areas, stations and storage retain their behavior.

`HavenIslandDecorationPlan.cs` contains the layout. `HavenIslandDecoration` preflights each group against the saved world, omitting occupied locations. It never clears sites or moves possessions. Existing bare trees explicitly owned by the estate receive their matching leafy artwork. The custom-house design is not rebuilt by this decoration pass.

Each estate and house fixture list receives a saved, invisible version marker. Repeating installation does not duplicate decorations or replace decorations subsequently moved by the owner. The existing local `guild-castle` release operation applies the pass after ensuring the headquarters exists. Its status response includes the installed decorative tile locations for verification and native-art rendering.

Keep the central arrival area, eight ladder approaches and landings, storage access, northwestern mini-champion footprint and boat water clear when extending the plan. The regression tests verify preservation, repeated installation, linked storage and ladder access against the real tile data.
