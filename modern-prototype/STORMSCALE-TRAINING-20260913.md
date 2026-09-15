# Stormscale training

Stormscale inherited the native Drake definition with no magical, special, weapon or area-effect training choices. It now uses the same broad training catalog as Haven's snow bear. Its zero-base trainable skills are eligible for skill-cap options. Existing stage completion, power-scroll, point and ability-slot checks remain in force.

Its forced ranged-melee AI now applies only while its AI type is Melee. Learned spell schools can select their native AI, including Mysticism, Spellweaving and Necromancy. The independent Chain Tempest signature remains available; the default three-second ranged bolt is part of its untrained melee AI.

Apply native patch 0044 plus HavenMissionPets.cs and HavenPetTrainingBridge.cs. The test checks all skill/ability categories, zero-base Discordance eligibility, native learned Mysticism AI selection on a controlled Stormscale, and unchanged ordinary drake skill eligibility. Existing Stormscales use the revised definition without rerolling stats or rarity.
