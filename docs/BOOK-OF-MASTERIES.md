# Book of Masteries — Haven compatibility implementation

The deployment hold was released by the player. The compatibility book and paired companion songs are installed; current status and evidence are in [the implementation tracker](IMPLEMENTATION-TRACKER.md). Client presentation and gameplay acceptance still need the [test checklist](PLAYER-TEST-CHECKLIST.md).

## Getting started after installation

Training Supplies → **Book of Masteries and primers**:

- Book of Masteries: 1,000 wallet gold.
- Skill Mastery Primer I: 2,500 gold.
- Primer II: 7,500 gold.
- Primer III: 15,000 gold.

Primers are offered for every supported mastery skill. Learn a primer by double-clicking it in your backpack with at least 90 base skill. Higher volumes can be learned directly. Open the book, select the matching mastery, then use its ability buttons. Switching mastery has a ten-minute cooldown. Weapon moves use the native special-move system; passive effects have no cast button.

This is separate from the older **Shard mastery manual**, which remains available for its existing stat focus.

The book contains the 45-entry mastery catalog, including passive entries. Learning and the selected mastery persist across saves. Active timed spells need to be cast again after restarting.

## Combat integration

Native combat routes hit, miss, parry, weapon removal, incoming damage, spell damage, and attribute queries into the mastery effects. Mana Shield consumes mana to absorb damage, Mystic Weapon can use Mysticism minus 25 for weapon accuracy, Toughness and Invigorate raise maximum health, and Tolerance can spend stamina to resist poison. Combat Training and Whispering affect pet combat/regeneration and skill-gain chance. Passive hooks cover Intuition's mana, Boarding's stable capacity, Knockout's wrestling damage, Potency's poison charges, and enchanted summons' health, stamina regeneration, and dispel resistance.

Bard party effects include controlled and summoned pets of party members. Recipients leaving the party, changing map, or moving outside the effect radius lose the benefit. Buff lookup also checks current map and range, rather than trusting a stale recipient list.

Damage reflection is guarded against recursively triggering mastery reflection again. Mastery timers are stopped when their caster or target is deleted. Temporary field items disappear on world load rather than becoming permanent saved decorations.

## Compatibility differences and limits

This is a Haven/ML compatibility port, not a full later-expansion upgrade. The book uses a server gump with mastery selection and ability buttons. Later-era client artwork, descriptions, and exact balance still need an in-game review before release.

- Conduit spreads damage from Pain Spike, Poison Strike, and Strangle to valid enemies inside its field, at most once per second. It does not clone every necromancy debuff.
- Summon Reaper uses this engine's mage AI with an immobile poison aura; the engine has no separate Spellweaving AI implementation. Doom's Skeletal Dragon is excluded from Command Undead so its event ownership cannot be broken.
- Shadow improves the Detect Hidden difficulty calculation. The engine's other reveal paths have not been changed.
- Resilience provides regeneration, a 25% chance to resist poison, and skill-scaled 10–60% reductions to new bleed, Mortal Strike and Curse effects. Bleed rounds up to the next two-second tick. Native PlayerMobile and BaseCreature poison paths are covered, so eligible party pets benefit. Existing effects are not retroactively shortened when a song starts. Other spell-specific curses remain unchanged.
- Saving Throw adds a disarm-block chance to its existing bonuses: 10/20/30% at mastery tiers I/II/III with 120 base weapon skill and Tactics. Lower training scales the chance down; overcapped skills do not exceed 30%. This is an explicit Haven balance choice, not an assertion of the official hidden formula.

These differences must remain visible in release notes. Do not present this as a verified exact reproduction of every official mastery effect.

## Companion and bot improvements

An eligible bard companion maintains both effects of its selected mastery. It selects Peacemaking for an injured owner, Discordance against a suitable tough enemy, and Provocation for ordinary support. It attempts missing effects rather than restarting active ones, waits between decisions, and avoids rapid mastery switching. Skill determines companion mastery proficiency, up to volume III. Changing role, departing on a mission, or clearing songs removes its mastery effects.

Grouped player bots stop pulling fresh encounters and recognize party-owned pets as allies. The shared behavior tick visits a construction/load registry instead of scanning every mobile in the world. Deletion unregisters the bot, and tick processing uses a snapshot so spawning or deleting bots during a behavior does not invalidate iteration.

The persistent roster and timed gathering baseline are now staged; implemented controls and remaining design work are distinguished in [GUILD-CREWS-PLAN.md](GUILD-CREWS-PLAN.md).

## Follow-up review fixes

Upkeep cannot become zero or negative from overcapped skills. Collective bard bonuses use the correct Peacemaking and Discordance skills. Fully absorbed damage stays zero through the native AOS damage path (patch 0046). Thrust tracks and cancels its target timer. Bodyguard validates the pet owner's acceptance, rejects duplicate coverage, and redirects damage using the original damage amount. Overlapping Invigorate effects preserve the strongest surviving bonus. The integration suite casts real paired bard songs and verifies overlap cleanup.

## Verification

The isolated verification tree is `verification/HavenMasteryVerify`, based on the current released patch set through 0044. Added checks cover every active spell/move registration, unlearned casting rejection, shop entries/prices, primer consumption, saved mastery levels and cooldown, real Mana Shield damage/expiry, party-pet membership/range cleanup, bard choice/proficiency, and bot registration/deletion and pull policy.

The complete local content suite is run after changes. Native defense hooks are in patch 0051; tests cast actual paired Peacemaking songs and inspect native bleed, wound and curse timers, pet range cleanup and mastery eligibility. A passing suite is not proof of every ability's balance or client presentation.

The [official bard mastery description](https://uo.com/wiki/ultima-online-wiki/skills/bardic-skills/bard-masteries/) supports the intended regeneration and detrimental-effect protection categories. Haven's formula, poison probability and compatibility boundaries above are documented separately rather than presented as an exact official port.
