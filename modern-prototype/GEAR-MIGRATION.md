# Original gear and stone restoration queue

Status: the nine-piece evolving starter set, robe upgrades, wallet/tithing and Haven Luck/training boosts are implemented. The original ordinary preview kit remains available separately. Broader special reward and artifact sets below remain pending.

1. Restore Apprentice Blade/Fencer/Mace/Bow with binding, combat XP, level20 progression, mana sustain and persisted state. Restore New Haven Adventurer's Robe, Apprentice Grimoire, Starter Fortune Earrings, leveling cape and evolving sash alongside their real progression hooks. Update starter stone, including a separately tracked migration claim for characters who already claimed the ordinary preview kit.
2. Restore robe upgrade service with explicit tier/price preview, stale-gump rejection and atomic payment through modern Marks/gold. Preserve bound-owner checks and cap progression. Source: HavenContent.HavenUpgradeStone, StarterItems.
3. Restore special rewards: nine bracelet themes and matching rings/sets, Champion pendant, field equipment and Astral gear. Port set/growth/economy dependencies before exposing purchases. Source: HavenContent.SpecialRewardStone, HavenMarkRewards, HavenAstralRewards, HavenGearExperience and associated classes.
4. Restore shield-warrior early/mid/late gear (shield, sword or mace, gorget and ring), quest equipment growth, Legendary/Doom artifact progression. Verify native ServUO equivalents before defining duplicate item classes.
5. Only expose entries whose item behavior, XP, binding, prices, full-pack handling and reload persistence are verified. Give each item a readable preview. Keep utility items (wallet, codex, key vault) tracked as separate dependencies rather than pretending ordinary bags/books provide their original behavior.

Player progression prerequisite restored: original Haven-area +1000Luck and5x natural gain chance/amount below100.0. Native gain tests cover the100.0cutoff, and existing free-skill/cap tests still pass.

Restored next: nine original special bracelet classes with original native attributes and15Mark/25,000gold pricing. Matching rings, set bonuses, Champion pendant, Concord talisman and Astral progression remain pending. All five original service categories are now reachable from Supplies sign; Arcane and Training use native items where available. Custom codex/key vault/rune pouch/resource satchel and pet utilities are not falsely listed as restored.

Champion pendant is now restored at250Marks with its original attributes. Concord talisman and matching ring/set progression remain pending.
