# Original gear and stone restoration queue

Status: audited from original source; not yet ported. Modern starter supplies currently give ordinary native gear. Existing modern Marks rewards are a small separate selection.

1. Restore Apprentice Blade/Fencer/Mace/Bow with binding, combat XP, level20 progression, mana sustain and persisted state. Restore New Haven Adventurer's Robe, Apprentice Grimoire, Starter Fortune Earrings, leveling cape and evolving sash alongside their real progression hooks. Update starter stone, including a separately tracked migration claim for characters who already claimed the ordinary preview kit.
2. Restore robe upgrade service with explicit tier/price preview, stale-gump rejection and atomic payment through modern Marks/gold. Preserve bound-owner checks and cap progression. Source: HavenContent.HavenUpgradeStone, StarterItems.
3. Restore special rewards: nine bracelet themes and matching rings/sets, Champion pendant, field equipment and Astral gear. Port set/growth/economy dependencies before exposing purchases. Source: HavenContent.SpecialRewardStone, HavenMarkRewards, HavenAstralRewards, HavenGearExperience and associated classes.
4. Restore shield-warrior early/mid/late gear (shield, sword or mace, gorget and ring), quest equipment growth, Legendary/Doom artifact progression. Verify native ServUO equivalents before defining duplicate item classes.
5. Only expose entries whose item behavior, XP, binding, prices, full-pack handling and reload persistence are verified. Give each item a readable preview. Keep utility items (wallet, codex, key vault) tracked as separate dependencies rather than pretending ordinary bags/books provide their original behavior.

Player progression prerequisite: original Haven-area +1000Luck and5x natural gain chance/amount below100.0 are missing. Restore exact original area bounds and counted/free-skill rules; do not replace earned progression with instant120 skills.
