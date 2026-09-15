# Enemy combat rule

User rule: enemies should not flee. New enemy content should honor this by default.

HavenEnemyRules.StandGround covers uncontrolled, unsummoned hostile creatures, champion spawns, closest-target attackers and creatures actively fighting. Vendors are excluded. Native BaseCreature low-health/timed flee checks and BaseAI's Flee transition enforce the rule. No freezing or movement-speed reduction is used; chasing, ranged positioning and encounter boundaries remain functional.

Apply 0047-enemies-stand-ground.patch with HavenEnemyRules.cs. Native test fixtures cover Ogre, Lich, Orc, Barracoon and HavenEncounterMob: low health, timed flee, explicit AI flee transition and movement capability.
