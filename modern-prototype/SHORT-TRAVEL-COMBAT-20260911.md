# Haven travel combat cooldown

HavenPreview.CanTravel and mini-champion StartError use a shared 15-second window after the most recent incoming or outgoing aggression. A stale Combatant reference cannot extend this window. Each new attack refreshes the cooldown. Native aggression expiry and criminal status are unchanged; this does not change native spell travel or companion mission restrictions. Home remains unrestricted.

Regression checks verify blocking on fresh aggression, allowing travel after 16 seconds while the native record remains unexpired, and blocking again after its refresh. Full isolated runtime suite COMPLETE; isolated/live builds clean. Live saved and backed up to E:/Backups/Haven/Prototypes/servuo-before-short-combat-20260911-205805 with matching save hashes. Server restarted on port2699 and authenticated login/relay probe passed.

Separate outstanding work: companion follow-through on spell travel; leveling area weapons; clarify whether world-loot clothing should gain levels, have more special drops, or both. Candidate weapon themes: storm sword, fire war hammer, frost bow; reuse native area-hit effects and shared level/XP properties.
