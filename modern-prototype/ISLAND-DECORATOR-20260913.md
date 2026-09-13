# Owner-managed island furnishings

The expanded Interior Decorator now recognizes registered foundation, commons,
recovered-house and cove-approach fixtures before the ordinary house check.
The recovered estate owner (or another character on that account) can move
individual visible fixtures within 24 tiles on the island. No staff access or
global item-movement privilege is granted.

Cardinal movement and height changes retain the actual item and its contents,
security and fixture registration. Native item serialization saves the position
and art, and these fixture lists do not reset positions on ordinary restarts.
House fixtures remain in their original house; moves stay within island bounds
and cardinal moves check collision. Turn uses native Flipable metadata, including
matching alternate art for Static furnishings. Art without alternate facings
reports that limitation. Individual addon components, doors, ladders and service
or encounter controls are excluded from this furnishing operation.

Outdoor managed fixtures remain fixed in the world: they need no additional
lockdown. Secure/Release still delegate to native house rules inside a house.
This change does not enable taking public island fixtures into a backpack.

IslandDecoratingSmoke passed owner-only outdoor movement, alternate Static
facings, Up/Down, stranger denial and exact container/contents preservation.
Isolated and production builds passed. A clean save, shutdown and backup
preceded deployment of two source files; no test saves were deployed.
