# World clothing discoveries and pirate ledger supplies

Ported clothing generation from playerbots/source/CustomBots/UOOffline/HavenWorldDiscoveries.cs: ten original clothing styles, seven original roles, original base stats/skill bonuses/hues, and Common/Uncommon/Rare/Epic/Legendary distribution 60/25/10/4/1 percent. Original 2.5 percent interval [.002,.027) retained per eligible wild kill; no boss, negative-karma, or 100-HP threshold for the clothing roll. Controlled, summoned, bonded, previously owned, invulnerable, vendor and no-kill-award creatures excluded. Each kill/player gets one attempt. Companion/pet killers resolve through GetDamageMaster. The original .2 percent utility-item branch is not included in this clothing-only port.

Only Legendary clothing levels, matching the original legendary marker rule. Current HavenAdvancedGear kind2 replaces the old virtual marker and gear-XP record, preserving growth bonuses and standard level/XP tooltip. Uses current equipped-item kill-XP eligibility and requested durability floor. Lower rarities retain their original fixed stats. Existing unrelated ordinary clothing is unchanged.

Appended Cannonball, Grapeshot, PowderCharge and FuseCord to HavenResources; existing serialized resource IDs are untouched. The shared catalog serves personal/companion/guild ledgers, resource deeds and resource satchels. Ship deeds, cannons and other nonfungible equipment remain physical items.

Validation: all350 rarity/role/style combinations, Legendary level20/idempotent Apply, ordinary-world eligibility and duplicate-award prevention, native ammunition deposit/transfer/withdrawal for all4 resources, full isolated regression suite COMPLETE. Isolated build clean. Deployment performs clean save, full backup with save hash comparisons, live build and authenticated login/relay probe.

Area weapon design candidates: Stormblade (energy sword), Cindermaul (fire war hammer), Frostwake Bow (cold bow). Intended native area-hit mechanics with growing proc chance and standard level/XP. Not implemented in this deployment.

Live deployment complete. Backup: E:/Backups/Haven/Prototypes/servuo-before-discovery-ledger-20260911-210404. Live build/startup and authenticated login/relay probe passed.
