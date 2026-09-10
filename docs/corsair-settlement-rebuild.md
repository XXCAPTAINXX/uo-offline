# Corsair settlement rebuild

The first decoration pass scattered furniture and foliage and silently omitted blocked paving tiles. This revision replaces that outdoor pass with a connected settlement:

- Pier → arrival square and covered patrol board → main street → house entrance.
- Fenced kitchen garden with paths aligned to its entrances; open stable northwest of the old corsair camp.
- Covered shipwright work yard east of the house.
- Branches to the orchard, ore bank and grove.
- Narrow trail terminating at the edge of the northern mini-champion clearing.
- Kitchen courtyard around the existing working oven and furniture.
- Covered cargo store reached from the pier, with an open eastern entrance.
- Coastal palm clusters and low street borders that preserve sight lines.

Native `LargeTable` artwork replaces the incorrect nightstands used as a new outdoor table. The custom house design, indoor storage, functional addons, harvest spawners, patrol board and mini-champion remain in place.

## Migration

`HavenIslandSettlement.cs` is a one-time revision of estate-managed outdoor statics. The old plan remains as the precise identity/position manifest for removal. Unrecognized or moved items, containers, addons and player property are not deleted. Previous objects are temporarily internalized; any placement/access failure removes new objects and restores the old objects before returning the error.

Paving is checked as a connected graph of actual placed, walkable surfaces. Eleven destination tiles must be reachable from the arrival square. The existing pier is one unit above ground and is checked at that height. A saved marker makes repeated installation idempotent. Release status exposes route results and the actual managed outdoor artwork for review.

The authoring source is `tools/plan-island-settlement.py`; it generates `HavenIslandSettlementPlan.cs`. Coastal planting is constrained to the installed map's dry, level ground. Do not remove rejected tiles silently: revise the plan or report a property conflict.

## Verification

Tests exercise a complete estate with working fixtures, preserve house components/ladder landings/storage, require all destinations to connect, preserve player possessions, verify repeat installation, and check rollback after a player-owned obstacle blocks the trial trail.
