# Roadmap

## Phase 1: Foundation

-   [x] New solution on the shared tier layout, with architecture and
    composition tests
-   [x] Single local login (create on first run, log in thereafter)
-   [x] OS credential store for API keys; Provider selection by category
-   [x] SQLite ledger: every attempt, tax lots, and FIFO disposals,
    written atomically
-   [x] Auto-trading engine: start/stop anytime, pre-trade checks,
    record-before-send, graceful stop
-   [x] PrettyConsole UI: live monitor, spinners, constant active cue
-   [x] Per-tax-year CSVs (Form 8949 lines plus transactions)
-   [x] Demo mode replaying a real volatile day
-   [x] Placeholder random strategy; Paper and Alpaca (paper) brokerages;
    Coinbase and replay market data

## Phase 2: Real strategy

-   [ ] Collect more historical days across regimes (trending, ranging,
    crash, quiet); let replay choose among them
-   [ ] Backtest harness reporting P&L after fees, slippage, and estimated
    tax, plus drawdown and trade count
-   [ ] Volatility strategy: confident-low entry, net-of-everything exit,
    weighing scarce order budget ("one cent now vs. three cents later")
-   [ ] Evaluate limit orders (maker fee, price certainty) vs. market
    orders
-   [ ] Decide lot stacking vs. one-position-at-a-time given FIFO

## Phase 3: Before real money

-   [ ] Kill switches: max daily loss, max position, stale-data and
    abnormal-move circuit breakers
-   [ ] Reconciliation: lots vs. brokerage balance drift check; automatic
    lookup of `Unknown` orders by client order ID
-   [ ] Paper brokerage order records: persist each paper fill (client
    order ID, quantity, price, fee) in the same transaction as the balance
    change, so a crash between the paper fill and the ledger update can be
    reconciled like a real brokerage's
-   [ ] Read fee tier and fee activity from the brokerage instead of
    estimating
-   [ ] Live-money brokerage Provider(s), separate from their paper
    counterparts, with an explicit confirmation on first use
-   [ ] macOS Keychain credential store

## Phase 4: Operations

-   [ ] Run unattended (service or daemon) with notifications
-   [ ] Database and tax folder backup/export
-   [ ] Optional `--reset-login` that preserves the ledger
-   [ ] Track tax-law changes (wash-sale extension to digital assets,
    Form 8949 boxes) and adjust reports
