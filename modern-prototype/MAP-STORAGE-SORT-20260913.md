# Map-storage sorting

The Cartographer's treasure-map chest now has Level, Facet and Type sort
buttons plus an Ascending/Descending toggle. Level sorting is numeric.
Type groups treasure maps, SOS messages and bottles. Existing search works
with each order. Paging, withdrawal and collecting preserve the selected sort.
Changing sort resets to the first page. Stable serial tie-breaking keeps equal
entries predictable. Withdrawal still uses the displayed item snapshot and
revalidates chest access, parent and backpack capacity.

The menu is 50 pixels taller to accommodate the controls without overlapping
the ten item rows. Item storage and serialization are unchanged.

Validation: isolated and production Release x64 builds passed with zero
warnings/errors. Saved and stopped the live preview, backed up Saves,
Scripts.dll and the replaced source under servuo-before-map-sort, then
rebuilt and restarted. In-game visual confirmation remains outstanding.
