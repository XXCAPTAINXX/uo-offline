# Combat and casting equipment release — September 13, 2026

Clean save and restart on Haven port 2699; build zero warnings/errors; account login/server-list/game-relay probe passed. Original server untouched. Backup: E:/Backups/Haven/Prototypes/servuo-before-combat-casting-20260913-005216 (Saves, SHA256 manifest, previous Scripts.dll, original source). No test Saves imported.

Hook's boarding shield now participates in explicit native Soul Charge progression:20% at level1 to39% at20. Existing copies refresh at startup without replacement or XP reset. Existing Haven basher bulwarks get the same Soul Charge progression. New Stormguard basher shield (offensive/stamina) and Ironwake bulwark (health/regen, SoulCharge10-29%) cost200Marks each.

Eight new caster pieces at150Marks each: Tidecaller robe, Navigator sage hat, Channeler gloves, Manawalk boots, Tidecasting ring, Deepcasting bracelet, Tidecaller spellbook and Drowned grimoire. Different profiles favor spell damage, mana regeneration, intelligence, recovery, cost reduction or mana pool. All level1-20 using existing equipped combat XP records. Native caps and Shield Bash mastery requirements remain.

Tests:12 equipment variants reach20; Soul Charge values and repeat-apply stability pass; all ten catalog entries present. Native SoulChargeContext consumes ArmorAttributes.SoulCharge for damage-to-mana effect. Runtime visual review and long-session balancing remain pending.
