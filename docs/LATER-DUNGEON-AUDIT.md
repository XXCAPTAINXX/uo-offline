# Later dungeon audit — 9 September 2026

The installed ruleset is ML-compatible and uses client 7.0.23.1 data. Local searches find the native Doom encounter and several ML dungeon monsters/items, but no Shadowguard controller/instances/room encounters. ML set dressing or a named key item does not establish a complete peerless encounter. Stygian Abyss and later boss systems also need individual verification before being advertised as supported.

The [official Publish 90 description](https://uo.com/wiki/ultima-online-wiki/publish-notes/publish-90/) establishes five prerequisite rooms—Bar, Orchard, Armory, Fountain and Belfry—before the Roof. It also specifies party-leader entry, failure after a full party death/logout, room replay and a luck-influenced artifact reward. Those lifecycle and reward rules must accompany the combat, not just a portal and boss spawn.

The [ServUO pub57 Shadowguard source](https://github.com/ServUO/ServUO/tree/pub57/Scripts/Services/Expansions/Time%20Of%20Legends/Shadowguard) was inspected at tree `d76bf4443cf76d081ddaf8f57c87ff33749256af`. Fourteen C# files total roughly 520 KB, including room addons, controller, instance records, encounters, items, creatures, bosses and artifacts. Its controller uses Ter Mur entrance 501,2192,50 and multiple instanced room centers. Local GreaterDragon and FeralTreefellow base creatures exist, but that alone does not supply their encounter subclasses or event mechanics. The reference was downloaded into ignored audit artifacts only; it has not been copied into the build as an untested port.

## Work sequence

1. **Client/map compatibility:** inspect the actual entrance and all room footprints, tile art and packet support against the supplied 2012-era data. Choose an isolated newer-data migration or deliberately designed compatible room maps. Keep the current shard and saves intact.
2. **Encounter lifecycle:** implement serialized per-party progress, bounded instances, leader-only entry, queues, logout/death/abandon cleanup, restart recovery, summon/pet ownership and safe exits. Test duplicate/replayed events before rewards.
3. **Rooms:** port and test Bar bottle mechanics, Orchard matching, Armory phylacteries, Fountain waterworks and Belfry access/dragon mechanics separately. Do not substitute ordinary kill waves while calling them the official encounter.
4. **Roof:** implement all four boss behaviors, assistance rules for companions/party bots, and proper participant/luck reward attribution. Test wipe, disconnect, restart, and final-kill races.
5. **Artifacts:** bring in supported types and attributes, then deliberately choose evolving upgrades. Ensure earned bot drops flow through market consignment without duplicating participant rewards.
6. **Other major dungeons:** audit entrance, keys, instance/controller, boss AI, loot and reset behavior for each ML peerless site, then SA/Ter Mur encounters. Add travel destinations only after the whole route works.

Doom is the first staged working encounter because its native implementation already exists and can be exercised now. Shadowguard remains a substantial outstanding port. The staged mastery compatibility layer has documented differences and should not be treated as proof that every Time of Legends dependency exists.
