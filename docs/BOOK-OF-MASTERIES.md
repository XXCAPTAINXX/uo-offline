# Book of Masteries — staged Haven implementation

**Deployment hold:** prepared locally while the player is AFK. Do not install or restart the live server until the player returns. This has not been tested in their live client.

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
- Resilience's regeneration works. Its later-era bleed/mortal/curse duration interactions are not all ported.
- Saving Throw provides the port's stat/accuracy bonuses; a later-era disarm-blocking implementation is not included.

These differences must remain visible in release notes. Do not present this as a verified exact reproduction of every official mastery effect.

## Companion and bot improvements

An eligible bard companion maintains both effects of its selected mastery. It selects Peacemaking for an injured owner, Discordance against a suitable tough enemy, and Provocation for ordinary support. It attempts missing effects rather than restarting active ones, waits between decisions, and avoids rapid mastery switching. Skill determines companion mastery proficiency, up to volume III. Changing role, departing on a mission, or clearing songs removes its mastery effects.

Grouped player bots stop pulling fresh encounters and recognize party-owned pets as allies. The shared behavior tick visits a construction/load registry instead of scanning every mobile in the world. Deletion unregisters the bot, and tick processing uses a snapshot so spawning or deleting bots during a behavior does not invalidate iteration.

The guild-worker proposal is in [GUILD-CREWS-PLAN.md](GUILD-CREWS-PLAN.md). The persistent guild roster and automatic guild jobs are designed, not implemented by this patch.

## Verification

The isolated verification tree is `verification/HavenMasteryVerify`, based on the current released patch set through 0044. Added checks cover every active spell/move registration, unlearned casting rejection, shop entries/prices, primer consumption, saved mastery levels and cooldown, real Mana Shield damage/expiry, party-pet membership/range cleanup, bard choice/proficiency, and bot registration/deletion and pull policy.

The complete local Haven suite is run after changes. A passing suite is not proof of every ability's balance or client presentation; live installation and play testing remain on hold.
