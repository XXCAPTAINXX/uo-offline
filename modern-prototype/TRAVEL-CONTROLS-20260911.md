# Travel and compact controls — 2026-09-11

Live preview 2699: home travel permits criminal status and active/recent combat. Ownership and clear landing remain required. Other Haven travel retains combat checks but no longer requires criminal status to clear. Criminal status itself is not cleared or disabled.

Codex uses compact arrows including item-property buttons. Combat bar uses a lighter gray background and aligned header buttons.

Deployed only this change's source diff onto the live source; previously staged challenge/roles/timer/skill-repair work was not bundled. Backup: E:/Backups/Haven/Prototypes/servuo-before-travel-combat-20260911-193637. Save hashes matched. Live build passed with zero warnings/errors; account login, server list and game relay passed.

Regression coverage: criminal-only travel allowed without clearing status; active and recent incoming/outgoing combat block ordinary travel; clearing combat permits travel. Existing companion inventory, taming protection, challenge and care tests passed in the isolated source build. Home-specific runtime test recorded separately in verification/HavenPlazaRefine/inventory-checks.log.
