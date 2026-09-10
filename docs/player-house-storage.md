# Player access to R.E.C. house storage

The custom pirate headquarters uses the same company permissions as the original castle. The owner and co-owners can use guild-secured containers even before the owner creates a guild. Guild members retain their existing access; strangers are denied.

Double-click a profession chest or cabinet to retrieve its contents. Nested bags open normally, and items can be dragged to your backpack or put back. Drop items into the receiving chest to sort them into linked profession storage. Normal distance and combat restrictions still apply.

Patch 0063 extends the existing company-specific secure-container rule to HavenPirateHeadquarters. No stored items, ownership records, or chest locations are changed.

Regression verification reproduced the failure on the receiving chest before the fix. The custom-house migration test now checks owner/co-owner access to every storage container, nested contents, withdrawal and redeposit preserving quantity, and rejection of strangers. The full content suite passes 1,107 tests.
