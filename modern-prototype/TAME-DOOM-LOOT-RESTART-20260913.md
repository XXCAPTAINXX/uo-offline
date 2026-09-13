# Reliable assisted taming and protected Doom returns

## Taming

The companion's delayed Animal Taming cursor could compete with bard mastery
spell targeting. Assisted taming now invokes the native target synchronously
through a companion-only entry point. Beginning assist cancels unfinished
spell/cursor state; new bard mastery casts wait until assist ends. Existing
active Peacemaking songs are retained. Native player taming is unchanged.

Creatures with CanAngerOnTame previously rejected 95 percent of starts even
after Jenna had successfully calmed them. That separate anger gate is skipped
only for the active companion assist target with unexpired native peace.
Native taming skill checks, elapsed tame timer, damage interruptions, pathing,
follower capacity, subduing and species restrictions still apply.

The assistant approaches within three tiles, attempts peace, then tames. It
does not repeatedly peace an already pacified animal. Expired peace flags no
longer strand the cycle. Failed/interrupted attempts retry with a three-second
minimum between starts; native active-tame tracking prevents overlapping attempts.
Automatic harmful actions against the active tame target are blocked.

Start validation uses CurrentTameSkill and reports incompatible tamer/owner
restrictions. A creature requiring subduing waits until 10 percent health and
reports why; assist resumes automatically when the requirement is met. Range
and cycle state messages go to the owner. Follow order/target assignment keeps
the animal as the actual movement target. Successful native taming transfers
the same pet into the owner's book, retaining the assigned-pet fallback if the
book cannot accept it.

Native integration: patches/0038-reliable-companion-taming.patch modifies
Scripts/Skills/AnimalTaming.cs after the existing assisted-taming patch.

## Doom

Requirement is now 100 in Tactics, Magery OR Archery, with no Magic Resistance
requirement. Both UI and shared online/offline eligibility use this rule.
Points remain 500 per nominal mission minute, with normal luck travel reduction.

On completion, points are awarded and one artifact roll uses the resulting
real Doom chance. A successful roll creates a native item from DoomArtifact,
stores it in an internal owner-bound return parcel, then resets the owner's
Doom points. Failed rolls retain the points. Collection never rerolls or
resets points. Existing active Doom trips receive this behavior on completion.

The Missions screen has a Doom loot button with the pending item count.
Artifacts remain protected in serializable world containers until the owner
collects them into their backpack. Full packs retain the actual item, and
other owners cannot collect it. A successful collection uses the existing
celebration effect/sound. This does not change native boss artifact rolls.
The current/projected chance display remains on the Doom mission selection.

## Validation and release

Passed real timed integration: native peace, native tame timer, deliberate
damage interruption, automatic retry, and the original animal reaching its
owner's pet book. Also checked no repeated peace, conflicting bard cast,
automatic attack against the tame target, or dangling target/tame state.
Fixture uses an anger-on-tame horse and high skills to isolate orchestration;
rare species and lower skills retain their native success probabilities.

Doom checks passed four durations; low combat skill rejection and zero-Resist
acceptance; actual menu clicks; completion/collection deduplication; early
recall forfeiture; point accumulation and artifact reset; native artifact
creation; owner-only collection and full-pack retention preserving identity.
No in-game visual review was performed for this batch.

Isolated and production Release x64 builds passed with zero warnings/errors.
Preview saved and stopped before replacing eight scoped custom sources and
the patched native AnimalTaming.cs. Backup (Saves, DLL, replaced sources and
SHA256 manifest): E:/Backups/Haven/Prototypes/servuo-before-tame-doom-loot-20260913-101025.
Test saves were never copied into live. Server restarted after the build.

Post-restart account login, populated server list and game relay probe passed.
