# Custom courtyard island deployment — September 12, 2026

## Live changes

The stock island castle was replaced with the previously designed 31x31 R.E.C. courtyard compound: captain's house, workshop loft, barn/crew loft, galleries, bridges and courtyard. It uses 2,555 building components, 70 fixtures and eight ladder links. Cargo containers are usable, with native house security. [island arrives outside the estate.

The original private storage vault (serial 1073962057) was transferred intact, with its nested contents. Three linked storage stations serve the owner's account. Friends/co-owners retain house and ladder access; storage remains account-private. Public/private status and access lists are copied.

The southeast cove now has its visible Blackwake expedition board and connected trail. Double-click the nearby board to start the existing three-wave encounter or collect its rewards. Three pirate sites use native closed waypoint patrol loops. This release does not increase loot rates or create a duplicate mini-champ.

## Restart and backup

Clean shutdown before backup at 22:10 EDT. Backup: E:/Backups/Haven/Prototypes/servuo-before-custom-courtyard-20260912-221048. Includes Saves with SHA-256 manifest, old Scripts.dll, replaced sources and both original server/client multi asset sets.

The client was reopened during installation and locked MultiCollection.uop. Installation paused; the user closed it again. All six installed server/client multi files were then verified against the staged manifest. No client profile or credentials changed.

Live build: zero warnings/errors. Migration saved at 22:12:01 EDT: new house 1073955408, owner 37901, original vault 1073962057, 70 fixtures. No test or historical Saves were imported. A subsequent clean restart checks live save loading.

Sources installed: HavenIslandUpgrade, HavenRecoveredHouseMigration, HavenCoveApproach, HavenRecoveredHeadquarters, HavenRecoveredCourtyard, HavenRecoveredHouseLayout, HavenIslandEstate, HavenIslandPatrols, HavenIslandEncounters and HavenIslandInstall. Native multi.idx, multi.mul and MultiCollection.uop installed in both Haven-Island-Server-Data and Haven-Island-Client-Data.

## Verification

Rehearsal used the recent 21:49 stopped backup. Migration and separate-process reload passed: original owner and every nested vault serial/parent preserved, 70 fixtures, eight ladders and walking access, three linked stores, three closed native patrol loops and linked cove board. Earlier forced-rollback and compressed house-packet checks remain applicable. Native-art exterior render reviewed; no live in-client visual/playthrough claim is made.

The operator migration is opt-in, checks exact asset hashes and estate serial/location, rejects unaccounted-for placed possessions or occupants, and stops startup on failure rather than permitting subsequent autosave. A save failure requires restoration of the stopped backup.

Still pending: hands-on roof hiding, ladders, boat and combat-boundary playthrough; Eodon mount artwork validation and additional Sovereign reward designs. The old automatic profession-sorting furniture system is not included.

Both post-start account login/server-list/game-relay probes passed. Live reload completed around 22:13 EDT; server online at 127.0.0.1:2699.
