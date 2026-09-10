# Corsair settlement rebuild

## Visual revision in progress

The owner rejected the open-sided sheds and isolated ivy as unfinished-looking. The next pass begins with one enclosed harbor storehouse, before repeating its style elsewhere. `tools/plan-corsair-storehouse.py OUTPUT.json` authors the review sample: four perimeter walls with a two-tile loading doorway, windows, a continuous pitched roof with gable infill, rear cargo stacks, a shipping desk and a net-mending bench. The eastern doorway meets the existing pier path; the centre stays clear.

This sample is **not yet installed** and does not run at server startup. It is being shown for visual review before a property-preserving world migration. Its native-art placement manifest contains no client artwork. The deployed design described below remains in place until that migration is tested.

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

The finishing pass uses compact hedge-bordered flower beds and ivy attached to existing supports, informed by the [UO Home Decor landscape gallery](https://uohomedecor.com/photo-galleries/deco-landscape/) (FrontPatio, FrontPatio-2 and Roof-1). These are layout references; the build uses the installed client's native art. The supplied Reddit discussion was readable, but its browser image view required a verification challenge and was not inspected.

## Migration

`HavenIslandSettlement.cs` is a one-time revision of estate-managed outdoor statics. The old plan remains as the precise identity/position manifest for removal. Unrecognized or moved items, containers, addons and player property are not deleted. Previous objects are temporarily internalized; any placement/access failure removes new objects and restores the old objects before returning the error.

Paving is checked as a connected graph of actual placed, walkable surfaces. Eleven destination tiles must be reachable from the arrival square. The existing pier is one unit above ground and is checked at that height. A saved marker makes repeated installation idempotent. Release status exposes route results and the actual managed outdoor artwork for review.

The authoring source is `tools/plan-island-settlement.py`; it generates `HavenIslandSettlementPlan.cs`. Coastal planting is constrained to the installed map's dry, level ground. Do not remove rejected tiles silently: revise the plan or report a property conflict.

## Verification

Tests exercise a complete estate with working fixtures, preserve house components/ladder landings/storage, require all destinations to connect, preserve player possessions, verify repeat installation, and check rollback after a player-owned obstacle blocks the trial trail.
