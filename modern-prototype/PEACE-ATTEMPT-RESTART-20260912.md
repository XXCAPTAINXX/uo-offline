# Peacemaking attempt gains — live restart, September 12, 2026

Live deployment completed at approximately 14:32 EDT. The server saved and stopped cleanly at approximately 14:31 EDT, then restarted on port 2699. Authenticated account login, populated server list and game relay passed. Startup error output was empty.

## Change and use

Jenna's valid targeted Peacemaking attempts now train even when the target is outside the normal difficulty gain window. At 87.9, an ordinary valid attempt awards 0.3 skill, including failed attempts. Native success difficulty is preserved. Already-calmed and invalid targets are rejected before the training hook. Skill locks, caps and the existing slower progression above 120 remain in effect. This does not make Jenna attempt Peacemaking more often; her existing Tame and bard behavior selects the attempts. No command or manual skill reset is needed.

Only HavenCompanionProgression.cs and the native Peacemaking call site were deployed. Recovered island house work remains unfinished and was not included.

## Verification

An isolated runtime regression passed: 87.9 -> 88.2 on an easy attempt, then 88.5 on an impossible failed attempt; a locked skill did not increase, and a capped skill did not exceed 125. Both test and live builds succeeded with zero warnings and zero errors. The native patch reverse-check passed in the test workspace. These are server-side checks; no live client gameplay or visual validation is claimed.

## Backup

Backup: E:/Backups/Haven/Prototypes/servuo-before-peace-attempt-20260912-143105

Contains the cleanly stopped Saves, Scripts, Config and root files. All 43 save files were verified against the originals with SHA256; the manifest is SAVE-SHA256.txt in the backup. No player skills or possessions were manually changed. The original ModernUO server was not restarted.
