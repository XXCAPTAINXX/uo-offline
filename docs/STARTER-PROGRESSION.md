# New Haven starter and progression pass

This is the implementation checklist for the September 8, 2026 starter/QOL redesign.

## Starter-kit verification (September 8 follow-up)

The current kit contains a character-bound blessed robe, a full evolving Magery
grimoire, a bound weapon selected from combat skills, a wallet, cleanup bag,
small brick house deed, and 50 bandages. Archer kits also include 100 arrows.
Owner/GM characters now receive the kit at creation. Existing staff characters
who were skipped can use `[StarterKit`; the command refuses when starter robes
or grimoires are already equipped or in the backpack. It is not a general item
recovery system and does not replace progressed equipment.

Successful spell checks advance one owned grimoire, including books in the
backpack. Foreign-owned books gain no experience. Tests cover kit contents,
combat-skill weapon selection, repeated provisioning, robe upgrade limits,
and backpack spellbook progression.

The checklist below also contains broader design work: milestone choices,
specialization paths, permanent lost-item recovery and comprehensive anti-farming
protections are not completed by this starter-kit fix.

## Completed

- [x] Five veteran years of starting credit (`patches/0008-veteran-rewards-five-year-headstart.patch`)
- [x] Exactly five starting veteran reward selections
- [x] One additional veteran year/selection per real month
- [x] Mark requires no Magery skill (`patches/0009-travel-spells-no-skill-required.patch`)
- [x] Recall requires no Magery skill
- [x] Gate Travel requires no Magery skill

The travel-spell change deliberately preserves mana/reagent costs and normal world restrictions such as criminal/combat checks, blocked destinations and explicit region travel rules. It removes the skill-success requirement only.

## Starter character package

- [ ] Helpful-stat starter robe
- [ ] Starter robe upgrade/progression path
- [ ] Full Magery spellbook
- [ ] Profession-appropriate starter weapon
- [ ] Starter weapon evolution/leveling
- [ ] Caster spellbook evolution/leveling so casters get equivalent progression
- [ ] Britannia Cleanup / trash bag in every starter pack
- [ ] Cheap, readily available replacement trash bags
- [ ] Small starter-house deed
- [ ] Anti-farming/account-bound or zero-resale protections where appropriate

## New Haven south-bank convenience hub

The south exterior wall of New Haven Bank should become the obvious starter/convenience strip. Keep objects tight to the wall and leave normal foot traffic clear.

- [ ] Starter gear purchase stone/vendor
- [ ] Early resources/supplies purchase stone/vendor
- [ ] Trash bag replacements
- [ ] Scroll/champion archive access
- [ ] Peerless Key Vault access
- [ ] Veteran reward access if useful in layout
- [ ] Move/fix the existing Dungeon Portal
- [ ] Functional Dungeon Portal destination gump
- [ ] Destination facet/version selection rather than silently forcing one facet
- [ ] Functional hitching post that shrinks eligible pets for free

## Travel book

- [ ] When the same logical destination is available in multiple applicable facets/versions, show both choices.
- [ ] Do not silently force the bot-free facet/version.
- [ ] Remember the last chosen facet where practical without removing the alternative.

## Wallet

- [ ] Double-clicking the wallet sweeps eligible gold from the backpack into it.
- [ ] Include normal nested backpack containers where safe.
- [ ] Give a concise confirmation with the amount deposited.
- [ ] Do not silently sweep bank-box gold.

## Scroll / champion archive

Create a compact collection item inspired by the InsaneUO archive, but implemented for this shard.

Suggested categories:

- Power Scrolls
- Stat Scrolls
- Scrolls of Transcendence
- Scrolls of Alacrity
- Mastery Primers
- Scroll Binders
- Champion progression/reward items that benefit from indexed storage

Required operations:

- [ ] Collect all compatible items from backpack
- [ ] Store counts without loose-item clutter
- [ ] Withdraw one or a selected quantity
- [ ] Bind eligible stored Power Scrolls when a binder is available
- [ ] Compact searchable/list UI

## Peerless Key Vault

- [ ] Peerless keys never expire.
- [ ] Collect compatible keys from backpack.
- [ ] Show key counts by Peerless encounter.
- [ ] Show complete/incomplete sets.
- [ ] Show exactly which keys are missing.
- [ ] Withdraw individual keys or complete sets.

## Evolving starter equipment

Progression should belong to the character/item record and survive normal use. Do not make a months-old evolved starter item permanently disposable because of one accidental loss.

Weapons and caster books should gain experience from appropriate active use and unlock milestone choices rather than only receiving automatic linear stat increases.

Starter robe paths can specialize toward warrior, caster, tamer, bard or crafter play while remaining modest enough that endgame loot still matters.

## Persistence / installation rules

- Stock ModernUO changes go in numbered unified diffs under `patches/`; the installers already apply all `*.patch` files idempotently.
- New shard systems should live in deployable custom UOContent source and be copied by both Windows and Linux installers.
- Existing saves must deserialize safely after updates.
- Avoid systems that depend on manually editing a live install, because fresh installs and updates must reproduce the same shard.
