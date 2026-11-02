# Decisions

Decisions made while building CaptaTrader, with their reasons. Each is
a candidate to revisit; change one by editing it here with the new
reasoning rather than silently diverging.

------------------------------------------------------------------------

## D1. Rebuild from scratch on the shared solution layout

The earlier `src` was exploratory and was removed. The new solution
follows Business at the center defining capabilities, Data and
Integrations implementing them, an executable composition root, and one
test project. See `development_conventions.md`.

```text
src/
  CaptaTrader.Business       capabilities, rules, strategies, trading engine
  CaptaTrader.Data           SQLite + Dapper stores and migrations
  CaptaTrader.Integrations   brokerages, market data, credential stores, CSV writer
  CaptaTrader.CLIApp         executable "CaptaTrader": composition root + console screens
  CaptaTrader.Tests          MSTest, including architecture and composition tests
```

There is no `*.Presentation.*` project: a terminal app is its own
presentation, so its screens live with the composition root.

## D2. PrettyConsole for rendering

PrettyConsole provides the spinner (`Spinner`), the pinned live status
region (`LiveConsoleRegion`), menus (`Console.Selection`), tables, and
colored interpolated output. Only the CLIApp references it.

PrettyConsole's package also copies an AI-assistant skill into
`.agents/skills` on build. That copy is disabled in
`src/Directory.Build.props` so builds never write into the repository;
flip `CopyOnBuild` to opt in.

## D3. One local login, gate only

-   The tool is one-to-one with its user. A `CHECK (UserAccountId = 1)`
    table makes "at most one login" a data guarantee. A fresh install (or
    a missing database) offers only *Create new login*; afterwards only
    *Log in* is offered.
-   Passwords are hashed with PBKDF2-HMAC-SHA256 at 600,000 iterations
    with a random 16-byte salt (OWASP Password Storage Cheat Sheet). The
    iteration count is stored so it can rise later. Failed logins get a
    growing delay, and the app exits after five.
-   **The login gates use of the app; it is not encryption.** The
    database is a local file readable by the OS user, and API keys are
    protected by the OS credential store under that same OS user.
    Encrypting the database with a password-derived key was considered
    and deferred: it adds key management and makes a forgotten password
    fatal to tax records.
-   No password recovery. A forgotten password means removing the
    `UserAccount` row (or the database). The ledger and tax files remain
    readable either way. **Open question:** add a `--reset-login` option
    that keeps the ledger?
-   Business enforces the session: `AutoTrader.StartAsync` refuses to run
    unless a user (or demo) session is signed in.

## D4. Secrets in the OS credential store; references in settings

Ported unchanged from QmlativDevAssistant: Windows Credential Manager on
Windows, libsecret `secret-tool` on Linux, and an explicit "unsupported"
store elsewhere (macOS Keychain is a future addition). Settings hold only
an opaque `CredentialReference` per Provider field.

## D5. Providers by category; the CLI never knows Integration specifics

Business defines two capability categories, `ProviderKind.Brokerage` and
`ProviderKind.MarketData`. Each Integration supplies a `ProviderDescriptor`
(ID, name, description, whether simulated, credential fields). The CLI
lists descriptors, stores the user's choice, and prompts for the declared
credential fields, all without referencing an Integration type. Demo mode
selects "the first simulated, keyless Provider of each kind" through
Business (`SelectSimulatedProvidersAsync`) rather than by ID.

Strategies follow the same pattern through `StrategyDescriptor` and
`StrategyCatalog`, but strategies are **Business** code: deciding when to
trade is a business rule, not an external integration. Strategies are
pure decision functions over a `StrategyContext` and cannot place
orders.

Providers shipped now:

| Kind | Provider | Simulated | Keys |
|------|----------|-----------|------|
| Brokerage | Paper trading (default, $1,000 start, 0.25% taker fee, 5 bps slippage) | Yes | None |
| Brokerage | Alpaca crypto API, **paper environment** | Yes | Key ID + secret |
| Market data | Coinbase Exchange public ticker (default, live) | No | None |
| Market data | Historical replay of 2024-08-05 | Yes | None |

**No live-money brokerage ships yet.** Alpaca paper exercises a real API,
real keys, and a real order lifecycle without risk. A live Provider will
be a separate descriptor (see the conventions), added once kill switches
and reconciliation exist.

### Prior work: Binance.US and CoinAPI in the old solution

The exploratory solution removed in D1 had started a real exchange
integration. It is not carried forward, but it is recorded here so it is
not lost. The last commit containing it is `65df0b2` on `master`
(2025-08-08); view it with `git show 65df0b2:<path>`. All paths are under
`src/CaptaTrader.Business/Modules/Trading/`.

-   **`ExternalServices/Exchange/IExchangeService.cs`**: the brokerage
    abstraction. It had an exchange ID, a quote ("medium") symbol, account
    balances, spot price, and buy and sell methods that returned only a
    timestamp.
-   **`ExternalServices/Exchange/BinanceExchangeService.cs`**: Binance.US
    through the `Binance.Net` 11.4.0 library (JKorf), with
    `BinanceEnvironment.Us` because Binance.com refuses US locations.
    -   Built a `BinanceRestClient` on an `IHttpClientFactory` client, with
        the API key and secret taken from the session user's config.
    -   **Working:** account balances (available and locked per asset) and
        spot price (best ask on `{symbol}USDT`).
    -   **Not working:** buying and selling. Each was a commented-out
        `PlaceOrder` call (limit order, `ImmediateOrCancel`), and each
        returned `DateTime.Now` without placing an order.
    -   A TODO considered calling the REST API directly instead of using
        `Binance.Net`.
-   **`ExternalServices/Exchange/MockExchangeService.cs`**: an in-memory
    stand-in, the predecessor of today's Paper brokerage.
-   **`ExternalServices/MarketData/CoinApiDataService.cs`** with
    **`DTOs/CoinApiOhlcv.cs`**: CoinAPI (`https://rest.coinapi.io/`),
    which needs an API key. It fetched the latest 30 daily OHLCV candles
    for `{exchange}_SPOT_{asset}_USDT`, with compression turned on as
    CoinAPI requests.

Things to keep in mind if Binance.US returns as a live Provider:

-   Its pairs are quoted in USDT, not USD. Cost basis and proceeds for
    tax purposes are in dollars, so a USDT price is only an approximation
    unless it is converted.
-   Its limit IOC orders are the order type that
    `trading_considerations.md` recommends evaluating.
-   Check that Binance.US is available in the user's state.

## D6. SQLite + Dapper, decimal as TEXT, migrations

-   One database, `captatrader.db`, in the per-user app folder
    (`%LOCALAPPDATA%\CaptaTrader` on Windows), with WAL enabled so the
    menu can read while a session writes.
-   Versioned migrations.
-   Money and quantities are stored as invariant-culture decimal TEXT and
    mapped explicitly; nothing goes through SQLite REAL.
-   Tables: `ApplicationSetting`, `UserAccount`, `OrderAttempt` (every
    attempt), `TaxLot`, `LotDisposal`, and `PaperAccount`.

## D7. Logging split

-   **Console:** live trading events and status (PrettyConsole).
-   **Database ledger:** every order attempt and its outcome, plus tax
    lots and disposals. This is the source of truth.
-   **Tax files:** per-year CSVs regenerated from the ledger (see
    `tax_considerations.md`).
-   **Diagnostics:** Serilog rolling file under `logs/` in the app
    folder, kept 31 days. Nothing diagnostic is written to the console.

## D8. Trading engine behavior

-   One session at a time (`AutoTrader`). Start and stop are available
    from the main menu at any time, and the session keeps running while
    the user is in other menus.
-   **Constant active cue:** the terminal title animates a spinner with
    the BTC price whenever trading is active, so it stays visible even
    while a menu prompt blocks. Every screen header shows a green
    `● AUTO-TRADING ACTIVE` badge, and the live monitor pins an animated
    status region.
-   **Attempts are written as they happen:** each attempt prints when it
    is recorded, the live region shows `SUBMITTING …` with a spinner while
    it is in flight, and the outcome prints when it completes.
-   Events reach the CLI through a bounded channel that drops the oldest
    entries; the ledger, not the console, is the durable record.
-   Stop and Ctrl+C let an in-flight order finish. Stopping never sells
    open positions.
-   An `Unknown` outcome halts the session, and a session cannot start on
    a brokerage that still has `Unknown` orders. When starting, the app
    walks the user through each one, oldest first: they look it up at the
    brokerage and record either *ended with nothing filled* (`Reconciled`;
    an order still open must be canceled at the brokerage first) or the fill
    itself, which is applied to the tax lots in the same transaction as a
    live fill would be. A brokerage fill without a positive quantity and
    price, or one that cannot be matched to lots, is treated as `Unknown`.
-   A balance refresh that fails after a fill halts the session rather
    than letting it decide on stale balances.
-   One copy of the app runs per user, enforced by an exclusive lock on
    `captatrader.lock` in the app folder. Two copies sharing a ledger
    could otherwise mark each other's in-flight orders as abandoned.
-   The hourly order limit spans sessions: on its first tick a session
    counts the attempts already on the ledger in the past hour.

## D9. Demo mode

`CaptaTrader --demo`:

-   Uses a separate `demo` subfolder that is deleted and recreated every
    run, so it can never touch real records. There is no login, the
    session is marked demo, and Business refuses non-simulated brokerages.
-   Replays **2024-08-05**, the global risk-off selloff when BTC fell from
    about $58.3k to $49.1k and rebounded to about $54k, as 1,440 real
    one-minute Coinbase candles bundled in the Integrations assembly
    (offline and deterministic). Each candle yields four ticks at 25 ms
    intervals, so the whole day plays in about 2.5 minutes, producing
    roughly 30–45 order attempts with the placeholder strategy.
-   Ledger and lot times use market time, so the demo's records are dated
    2024 and its tax CSV is a realistic 2024 example.

## D10. Placeholder strategy

`RandomVolatilityStrategy` exists to exercise the pipeline, not to make
money. It randomly buys short dips (unless the short average is below the
hour-long average), keeps at most 10 open buys of $25 each, and sells
everything it holds **as soon as** the estimated after-fee, after-slippage,
after-tax profit is at least the configured minimum (default $0.01). It
never sells at a loss. The real BTC volatility strategy is a later phase;
the design inputs for it are in `trading_considerations.md`.

## D11. US tax assumptions

The app assumes a US individual taxpayer: FIFO per account, calendar
year in local time, short/long-term split at more than one year, and
CSVs shaped like Form 8949. See `tax_considerations.md`.

## D12. Attributions

-   Demo market data: Coinbase Exchange public candles endpoint, BTC-USD,
    2024-08-05.
-   Password hashing parameters: OWASP Password Storage Cheat Sheet.
