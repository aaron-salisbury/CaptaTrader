# Trading Considerations

Problems that trading systems routinely solve, what CaptaTrader does
about each today, and what is left for later phases. Status values:
**Handled**, **Partial**, and **Open** (tracked in `roadmap.md`).

------------------------------------------------------------------------

## Orders and execution

| Topic | Why it matters | Status |
|-------|----------------|--------|
| **Record before send** | If the app dies between sending and recording, money moved with no record. Every attempt is written as `Pending` first, then completed in one transaction with its tax-lot changes. Leftover `Pending` rows are marked `Unknown` at the next start, with a warning to reconcile. | Handled |
| **Idempotency** | A retried request must not place a second order. Each attempt carries a client order ID (the attempt's GUID) that brokerages such as Alpaca use to de-duplicate. | Handled (ID sent); Open (automatic lookup-by-ID reconciliation) |
| **Ambiguous outcomes** | A timeout or 5xx *after* sending means the order may exist. These become `Unknown`, never `Failed`, and are never silently retried. An `Unknown` outcome halts the session, and trading on that brokerage stays blocked until the user records, order by order, what the brokerage shows (any fill goes into the tax lots). | Handled (halt, block, manual reconciliation); Open (automatic lookup) |
| **Never cancel in flight** | Stopping or Ctrl+C cancels the market data stream, not a sent order; the stop waits for it to finish and be recorded. | Handled |
| **Market vs. limit orders** | Market orders pay the taker fee and cross the spread (slippage). Limit orders can earn the lower maker fee and fix the price, but may not fill. At one-cent margins this is decisive. The engine uses market orders for now. | Open, decided by the real strategy |
| **Slippage** | Estimates use the brokerage's expected slippage. The placeholder once "profited" a cent in estimate but lost three in reality before slippage was included; a test now guards this. | Handled (estimate); Open (measure actual vs. reference price) |
| **Partial fills** | A canceled or expired order may still be partly filled; the filled part is recorded. | Handled |
| **Minimum order size and precision** | Each brokerage declares a minimum notional and BTC decimals; quantities are floored so the app never sells more than it holds. | Handled |

## Risk controls (pre-trade checks)

Applied to every strategy proposal, independent of the strategy:

-   Max USD per buy, max orders per rolling hour, and cash/BTC balance
    checks. **Handled.**
-   **App-owned lots only:** the bot never sells BTC it did not buy,
    protecting the user's existing holdings. **Handled.**
-   **Simulated never drives real:** replayed prices cannot trade a real
    account; demo mode only uses simulated Providers. **Handled.**
-   **Kill switches** such as max daily loss, max position size, and
    circuit breakers on abnormal price moves or stale data. **Open.**
    These matter before real money.
-   **Stale market data:** live feeds pause and back off on errors rather
    than ending the session, but decisions are not yet blocked when the
    last tick is old. **Open.**

## Brokerage and account constraints

-   **Trade-count rules.** The US *pattern day trader* rule applies to
    margin accounts trading securities, not spot crypto. Crypto
    constraints are mostly **API rate limits** (requests per minute),
    **fee tiers** that improve with 30-day volume, and account-level
    limits. The strategy context exposes the remaining order budget so a
    strategy can weigh "one cent now" against "three cents later" when
    attempts are scarce. **Partial:** budget exposed; no strategy uses it
    yet.
-   **Fee schedules change** and depend on volume tier. Providers declare
    a schedule; it should eventually be read from the account. **Open.**
-   **Custody and the account as the source of truth.** Balances come
    from the brokerage at session start and after every fill. Lots are
    the app's own books. A drift check (lots vs. brokerage BTC) is
    **Open**.
-   **Paper vs. live are separate Providers** with separate keys, never a
    toggle. Only paper environments ship today. **Handled** (by
    design); live Providers are **Open**.

## Strategy realities

-   **Fees dominate small trades.** A round trip at a 0.25% taker fee and
    5 bps of slippage costs about 0.6%. On $25 that is about 15 cents, so
    a one-cent net profit needs a move of roughly 0.6%.
-   **FIFO couples lots.** Under FIFO a lot bought high blocks profitable
    sales of newer lots. See `tax_considerations.md`.
-   **"Buy the dip" in a crash is catching a falling knife.** The demo day
    shows it: the placeholder would buy at $57.8k and hold all day
    without the falling-market filter.
-   **Backtests lie.** Replay fills at the replayed price; live fills do
    not. Minute candles hide intra-minute paths (the replay assumes
    open → low/high → close). Overfitting to one day is easy. A real
    strategy needs multiple days and regimes, out-of-sample testing, and
    paper trading before live. **Open.**
-   **Determinism.** The placeholder is random; seeded strategies make
    tests and replays repeatable. **Partial** (the seed constructor exists).

## Operations

-   **Clock and time zones:** stored in UTC; tax years use local time.
    Decisions use market time from the tick. **Handled.**
-   **Logs** go to a rolling file in the app folder, never the console.
    **Handled.**
-   **Running unattended** (sleep, reboots, network drops, a laptop lid):
    a terminal session is fragile. Running as a service or daemon with
    notifications is **Open**.
-   **Backups** of the database and tax folder. **Open** (documented
    only).
-   **Secrets** live in the OS credential store; the database holds only
    opaque references. Use **trade-only API keys with withdrawals
    disabled**, and IP allow-listing where the brokerage offers it.
    **Handled** (storage); user guidance only for key scopes.

## Legal and practical

-   Not financial or tax advice. The app is as-is and educational.
-   Respect each brokerage's API terms (automated trading permissions,
    rate limits, data redistribution). The bundled demo data is a small
    sample from Coinbase Exchange's public API, used for offline
    demonstration.
