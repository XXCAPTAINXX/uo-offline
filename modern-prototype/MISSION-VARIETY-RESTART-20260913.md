# Hook's shield and mission variety

Hook's boarding shield now has these level 1 to 20 minimums:

- Soul Charge: 25 to 44 percent.
- Defense chance: 15 to 19 percent.
- Lower mana cost: 6 to 9 percent.
- Mana regeneration: 3 to 6.
- Bonus stamina: 5 to 14.

Existing items receive the new floors through the existing startup/evolution
apply path; higher existing attributes, experience and item identity remain.
Other shields retain their own progression. Native aggregate caps still apply.

Three appended companion routes appear under Gathering and in offline/AFK
selection and rotation. They require both Magic Resistance and one combat
skill (Tactics, Magery or Archery):

- Ruins: jewel recovery, rating 70. Two distinct rare gem types and diamonds.
- Heartwood: enchanted timber, rating 85. Heartwood/Bloodwood/Frostwood logs
  and a rare wood ingredient.
- Eodon: dragon salvage, rating 95. Colored scales, dragon blood and delicate scales.

All four existing durations and luck reductions work with these routes. Gold
and Marks use existing completion payouts. Material rolls are fixed at dispatch,
persist in the existing scheduled resource dictionary and use the existing ledger
overflow handling. Early recall retains the existing forfeiture rules. Existing
serialized mission IDs 0 through 22 are unchanged; new IDs are 23 through 25.
Offline route pagination now includes the fourth page.

Island completion parcels gain independent crew-specific bonus rolls:

| Crew | 25 percent adventure roll | 5 percent equipment roll |
| --- | --- | --- |
| Stormsail | Level 3-5 Trammel treasure map | Evolving Stormguard basher shield |
| Blackwake | SOS bottle, special net or random ruined ship-plan fragment | Evolving Hook's boarding shield |
| Drowned Fleet | Magery or Spellweaving Alacrity | Evolving Tidecaller spellbook or Drowned grimoire |

Challenge retains four parcels (all three crews plus a random fourth), so each
parcel has its own rolls. Existing pending parcels are not rerolled. Bonuses use
the persistent owner-only parcel and existing full-backpack delivery checks.
Mainland mini-champ completion rewards and existing boss corpse rolls are unchanged.
This pass expands mission choices and rewards; encounter wave mechanics are unchanged.

## Validation and release

Isolated test passed: Hook levels 1/20 and repeated apply; old mission IDs;
all 12 new route/duration combinations; skill gating; every generated material
is native and stackable; serialized scheduled rewards retain exact values;
drop probability boundaries; all themed equipment evolves; new menu construction.
Existing random mini-resource checks also passed.

Isolated and production Release x64 builds: zero warnings and errors.
Live preview was saved and stopped before replacing eight scoped sources.
Backup: E:/Backups/Haven/Prototypes/servuo-before-mission-variety-20260913-093615
(Saves, old Scripts.dll, replaced sources and SHA256 manifest).
No test saves copied into production. In-game visual review remains outstanding.
