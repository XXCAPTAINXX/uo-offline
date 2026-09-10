# Wallet and bank payments

Keep the Adventurer's Wallet anywhere inside your backpack. Guild registration, house placement and customization, insurance, vendor rentals, and other fees that use the normal bank payment service can now spend its gold automatically. The guild registration fee remains 25,000 gold.

Payments combine all carried wallets, account gold/platinum (when enabled), physical bank gold, and bank checks. Existing Haven merchants also use loose backpack gold, retaining their wallet → backpack → bank order. An insufficient total leaves every source unchanged. The wallet menu shows the funds available through the shared bank payment service; this display is capped at the maximum single bank transaction, 2,147,483,647 gold.

Gold stays in its existing saved wallet, bank items, or account. There is no copy, automatic deposit, or currency migration. Only this character's carried wallets are accessible: wallets stored in the bank, another character's backpack, or a companion's pack are not linked. Account gold keeps its existing account-wide behavior and is counted once per payment. Marks and Astral shards keep their separate balances.

Native bank withdrawals retain their normal access checks at each service. Wallet withdrawals to your backpack retain their capacity checks. Gold deposits and refunds still follow the normal bank deposit rules. Systems that insist on targeting physical coins are not changed by this integration.

## Verification

`HavenWorldTestsBankPayments` covers wallet-only guild fees without creating a bank box, nested/multiple wallet ownership, combined bank checks, insufficient-funds atomicity, merchant double-count prevention and payment priority, mixed account/physical bank payment, exact platinum change, and large balance overflow. A native player-vendor purchase test verifies item delivery, exact payment, and replay rejection when carried gold plus the wallet exceeds the signed 32-bit limit.

The native change is `patches/0067-wallet-bank-payments.patch` (after the guild name fix in 0066). It changes only UOContent's Banker, guild creation gump, and the player-vendor purchase balance accumulator (now 64-bit). No Server engine or serialization fields change. The `GetBalance` overload returning gold/check lists has no production callers in this source tree; those lists describe physical bank items while the total includes account funds and carried wallets. Payments must use `Banker.Withdraw`, not consume only the listed items. This branch has no `CombinedWithdraw` implementation or callers.

## In-game check

1. Put at least 25,000 gold in your wallet and create your guild; the confirmation should report wallet and bank funds.
2. With less than 25,000 in the wallet and the remainder in your bank, registration should still succeed.
3. Inspect `[wallet` for the combined available balance. Moving a wallet out of your backpack should remove it from that available amount.
