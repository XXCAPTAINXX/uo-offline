# Easier house foundations

House foundations can now bridge shallow uneven natural ground: at most four Z units below the placement height or two above it. The ground must be dry and passable. Collision relief applies only to foundation walls at Z=0; floors, stairs and other house components retain their terrain collision checks.

Roads, protected regions, existing buildings/houses, static obstacles, water and yard clearances retain their normal checks. This does not flatten the map or change existing houses, prices, or refunds.

Validation: 155 real-map tests pass, including a successful non-flat Haven house footprint, depth/height limits, wet/impassable terrain rejection, and existing bank/protected-region/solid-obstruction checks. Native patch0044 applies cleanly over patches0001–0043.
