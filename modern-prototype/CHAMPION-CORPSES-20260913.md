# Champion wave corpse cleanup

Ordinary wild IsChampionSpawn corpses are deleted 30 seconds after death, including unlooted contents. BaseChampion bosses, controlled, summoned and bonded creatures are excluded. Player corpses are excluded. A separate timer prevents ChampionSpawn's later one-minute BeginDecay call from extending cleanup. Eligible corpses remaining at startup receive a fresh 30-second timer when their creature reference is still available.

ChampionCorpseSmoke kills real wave, ordinary and boss fixtures through native death events. It waits 32 seconds and checks only the wave corpse was removed, even after simulating the controller's longer decay timer.
