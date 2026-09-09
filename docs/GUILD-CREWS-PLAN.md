# Haven guild crews

Design prepared while deployment is on hold. This document describes proposed behavior, not a live feature.

## What already exists

Bots can accept a real player's normal party invitation through `BotPlayerParty` and use `PlayerGroupBehavior` to follow and assist. Busy workers, outlaws, and bots already committed to a group can decline. Their ordinary guild tags come from `BotGuilds`, a fictional guild catalog; these tags are not membership in the player's real guild.

Ordinary bots have temporary play sessions and can be deleted and replaced. Making one a guild worker must first preserve its identity and progression. Adding a tag alone would lose the recruit and its work when its session ends.

## Player controls: Fellowship Board

A board at the guild house opens a compact roster. Each row shows name, role, current job, relevant skills, progress, and next delivery. Selecting a member opens Orders, Equipment, Skills, and Work History tabs. Orders remain open after ordinary commands.

Guild leaders and officers with recruitment permission can recruit a nearby willing bot, assign jobs, and dismiss recruits. Normal members can invite an available recruit into their party and inspect the roster. Treasury withdrawal follows explicit guild permissions. A player cannot recruit or redirect another guild's worker.

Start with four guild recruits and at most two recruited bots in any player-led adventuring party. The permanent companion remains separate. These limits should be settings, so they can be adjusted after playing with the system.

## Automatic work

Recruitment defaults to **Auto work**. Unpartied recruits rotate eligible training and gathering jobs. Their best gathering skill determines a specialty; combat recruits train through hunting. Manual orders take priority. After a party adventure ends, recruits resume their previous work assignment.

Jobs are explicit timed missions, using the same model as companion expeditions. The board should say “Mining expedition” and show its timer, rather than implying that simulated rewards were mined from visible world nodes. Physical roaming and harvesting can be a later mode.

Each job awards modest relevant skill and stat progress, observes the recruit's caps, and deposits resources into a guild resource ledger. Mining unlocks higher metals, lumberjacking higher woods, and hunting higher leathers according to skill. Rewards are stored as resource balances and withdrawn as commodity deeds, with no nested reward bags.

The ledger pauses jobs when its configured stock limit is reached. It records worker, job ID, resource type, amount, and completion time. It does not generate champion rewards, Haven marks, Astral shards, or pet tickets through routine guild gathering.

## Persistence and delivery

Use a serialized guild crew record separate from transient bot AI. Store guild identity, stable recruit identity, skills, stats, role, gear, job, start/due times, and monotonically increasing job sequence. Recruited bots must be exempt from normal session logout and spawner replacement. An orphaned guild record must not create a second copy of a recruit.

Completing a job and crediting its resources is one main-loop transaction. Persist the last credited sequence so reconnect, restart, or repeated completion processing cannot deliver twice. On restart, settle at most the one job that was already running; do not manufacture an unlimited backlog of offline jobs. Then schedule the next job normally.

Dismissal returns assigned player gear and ends the current job without deleting paid-for equipment. Guild disbanding first moves balances and gear to a recoverable owner claim, then releases recruits. No worker should retain another guild's permissions after transfer.

## Adventuring behavior

Use the existing party system instead of a second invitation mechanism. Show whether a recruit is available and why it cannot join. Grouped bots assist fights the party has engaged; they do not pull fresh encounters. They cure and heal party members and their pets, reserve mana for emergencies, choose targets they can reach, and stop retrying an unreachable target.

Role priorities: healer—cure, emergency heal, resurrection, support; caster—emergency support, safe area attacks, single-target damage; bard—maintain both effects of the selected mastery and support party pets; fighter—protect endangered members and attack the party target. All roles obey Hold, Follow, Attack, and Retreat. Recovery from a stuck target must not silently override Hold or the companion's explicit AFK mode.

Bot assistance must neither multiply nor dilute a human's personal mini-champion reward. Guild gathering and companion AFK missions remain separate jobs with separate delivery ownership.

## Acceptance checks before enabling

- Recruit survives save/load with the same identity, skills, and equipment; no replacement duplicates it.
- Guild permissions reject outside recruitment, orders, and withdrawals.
- Job completion cannot credit twice, including across a restart boundary.
- Party invitations pause automatic jobs; disband resumes them once.
- Deleted or disbanded guilds leave recoverable gear and resources.
- Resource variants and deed withdrawals preserve exact amounts and types.
- Party pets receive appropriate healing and buffs; neutral creatures and players are not attacked by unsolicited pulls.

Deployment remains on hold until the player returns.
