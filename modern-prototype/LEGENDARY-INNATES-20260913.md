# Legendary rolled abilities — 2026-09-13

Legendary skill rolls now grant matching innate actions rather than only skill values. Existing legendary records are repaired on server startup; rolls are preserved. Required supporting skills receive a minimum base/cap of 100, preserving higher values. The native taming-loss exemption includes these supporting skills.

Actions use a separate innate dispatcher, leaving purchased training abilities, points, follower slots and species AI intact. At most one action is attempted successfully per six-second pulse. Dead, stored, stabled, ridden, paralyzed and distant pets do not act. Offensive actions require an engaged enemy, line of sight and native harmful-action permission. Stop/stay blocks offensive actions.

- Bard rolls: native Discordance, emergency Peacemaking, or Provocation against a second engaged enemy; Musicianship supplied.
- Musicianship alone also grants Discordance at 100.
- Evaluating Intelligence grants Magery; Spirit Speak grants Necromancy.
- Magery: Energy Bolt; Necromancy: Pain Spike; Mysticism: Eagle Strike; Spellweaving: Word of Death.
- Chivalry: Divine Fury; Bushido: Confidence; Ninjitsu: Mirror Image, with supporting Hiding/Stealth.
- Healing: native self/owner healing with Anatomy.
- Poisoning: close-range poison application with a skill check.
- Detect Hidden: native area detection. Hiding: native hiding while critically injured.
- Passive rolls retain their normal native combat/stat behavior.

Lore lists the granted innate actions. The numerical roll list stays separate from supporting skills.

Validation: release build; runtime checks for support floors and preservation, unchanged ability count, actual native Discordance effect and Magery damage; other spell-school casts checked separately. Test fixtures suppress startup autostabling.
