# Guard feedback

Selecting Guard gives one confirmation. Repeating the same order or automatically returning to Guard after combat does not repeat the message or attack sound. Selecting Follow or Stay and then selecting Guard gives a new confirmation.

The companion also ignores hidden or unseen enemies when defending its owner. Previously a hidden combat target could trigger repeated Attack → Guard transitions because the pet combat handler immediately rejected that target.

The fix preserves guard patrols, defensive targeting, and manual commands. Native changes are in `patches/0068-quiet-guard-feedback.patch`; the companion change is in `HavenCompanion.DefendOwner`. No saved fields change.
