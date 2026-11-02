# Tax Considerations

> **Not tax advice.** This is the research behind CaptaTrader's design,
> written for a US individual taxpayer and current as of November 2026.
> Tax law changes, and this area is changing fast. Have a tax
> professional review your situation, especially before trading real
> money at volume.

------------------------------------------------------------------------

## Short answers to the original questions

**If I buy BTC, sell it for a profit, and immediately rebuy with all the
proceeds, is that profit taxable right away?**

Yes. The IRS treats digital assets as property (Notice 2014-21). Every
sale (and every crypto-to-crypto swap, or using BTC to buy something) is
a *disposal* that **realizes** a gain or loss at that moment:
`proceeds − cost basis`. Rebuying does not defer or roll over the gain;
there is no "reinvestment" exception for property held for investment.
The rebuy simply starts a new lot with a new cost basis.

"Taxable right away" means *it counts toward that tax year*. You do not
pay per trade. You report the year's net result on your return (due the
following April). However, if you owe a meaningful amount, the IRS
expects **quarterly estimated tax payments** (Form 1040-ES) during the
year, or penalties can apply. Safe-harbor rules (paying 100%, or 110%
for higher incomes, of last year's tax through withholding/estimates)
avoid the penalty.

**If I have losses in the same year, do they offset the gains?**

Yes. On Schedule D:

1.  Short-term gains and losses (assets held one year or less) net
    against each other.
2.  Long-term gains and losses (held more than one year) net against
    each other.
3.  The two results then net against each other.
4.  If the overall result is a **net loss**, up to **$3,000** per year
    ($1,500 married filing separately) offsets ordinary income, and the
    rest **carries forward** to future years indefinitely.

So you are taxed on the *net* result of the year's trades, not on each
winning trade. The number of profitable transactions does not matter;
the dollar total does.

**What rate applies?** Almost everything a volatility bot does will be
**short-term**, taxed at your *ordinary income* rate (10%–37% federal),
plus state income tax in most states, plus possibly the 3.8% Net
Investment Income Tax at higher incomes. Long-term rates (0/15/20%)
only apply to lots held more than a year.

------------------------------------------------------------------------

## What this means for "profit after fees and taxes"

-   **Tax never changes whether a sale is profitable, only by how much.**
    Tax is a fraction of a positive gain, so if a sale clears its fees,
    it is still profitable after tax. Fees and slippage are what decide
    whether a one-cent sale is a profit. CaptaTrader's pre-trade estimate
    (`TradeEconomics.EstimateSell`) subtracts fees, expected slippage,
    and estimated tax at the user's configured marginal rate, and the
    placeholder strategy sells only when that clears the minimum
    (default $0.01).
-   **The estimate is conservative.** A loss is not credited with the tax
    it may save against other gains. In reality a loss *is* worth
    something (it offsets gains), which a future strategy may weigh.
-   **Fees are part of basis, not a separate deduction.** Buy fees add to
    the lot's cost basis; sell fees reduce proceeds. The ledger records
    them that way, so every fee reduces taxable gain.

------------------------------------------------------------------------

## Cost basis method: why the app uses FIFO

When you sell part of your BTC, *which* lots you sold determines the
gain. Since **January 1, 2025**, cost basis must be tracked **per
wallet/account** (Treas. Reg. §1.1012-1(j); Rev. Proc. 2024-28), not
pooled across everything you own.

-   **Default is FIFO** (first in, first out).
-   **Specific identification** (choosing the lots, for example the
    highest-cost lot to minimize gain) is allowed, but for assets held
    at a broker the identification must be given *to the broker* no later
    than the time of sale, and typically the broker must support it.
    Most brokerage APIs cannot receive lot instructions with an order.

CaptaTrader therefore matches sales to lots FIFO, per brokerage account,
so its records match what the brokerage will report.

**Consequence worth knowing:** under FIFO, one lot bought high blocks
profitable sales of newer, cheaper lots, because any sale consumes the
oldest lot first. The placeholder strategy runs into exactly this on the
demo day. A future strategy should either avoid stacking lots or use a
brokerage that supports specific identification.

**Pre-existing holdings:** the bot only sells BTC that *it* bought
(enforced by pre-trade checks). If you hold BTC in the same account from
before, a FIFO broker will still treat the bot's sales as sales of your
*oldest* coins first, which could turn your long-term holdings into
realized gains. **Use a dedicated account for the bot.**

------------------------------------------------------------------------

## Wash sales

The wash-sale rule (IRC §1091) disallows a loss when you rebuy a
"substantially identical" *stock or security* within 30 days. **As of
late 2026 it does not apply to crypto held as property**, because crypto
is not a "security" under §1091. Bills to extend it to digital assets
have been introduced repeatedly (most recently H.R. 9172, June 2026) but
none has been enacted.

If such a bill passes, a bot that sells at a loss and rebuys within 30
days would have losses disallowed and added to the new lot's basis. The
app's lot model can absorb that (an adjustment on Form 8949, code W), but
the strategy and reports would need updating. This is tracked in the
roadmap.

------------------------------------------------------------------------

## What the brokerage reports: Form 1099-DA

-   For **2025** sales, custodial brokers report **gross proceeds** on Form
    1099-DA.
-   For **2026 onward**, brokers also report **cost basis and holding
    period** for *covered* digital assets: those acquired on or after
    January 1, 2026, in the same custodial account. Forms for 2026
    arrive in early 2027.
-   Assets transferred in from elsewhere, or acquired earlier, are
    *non-covered*; their basis is your responsibility.

The IRS matches your return against 1099-DA, so **the brokerage's form is
authoritative for what the IRS expects**. CaptaTrader's files exist to
reconcile against it and to fill gaps (for example, non-covered lots or
paper/demo inspection), not to replace it.

------------------------------------------------------------------------

## Forms involved

-   **Form 8949** lists each disposal: description, date acquired, date
    sold, proceeds, cost basis, adjustment code and amount, and gain or
    loss. Since tax year 2025 digital assets use their own boxes:
    -   Short-term: **G** (1099-DA with basis reported), **H** (1099-DA,
        basis not reported), **I** (no 1099-DA).
    -   Long-term: **J**, **K**, **L** with the same meanings.
    -   Do not use boxes C or F for digital assets.
-   **Schedule D** totals Form 8949 and applies the netting above. When a
    1099-DA reports basis and no adjustments are needed, totals may be
    reported directly on Schedule D instead of line-by-line.
-   **Form 1040** asks every filer the digital-asset question ("did you
    receive, sell, exchange, or otherwise dispose of a digital asset?").
    Answer "yes" in any year the bot traded.

The app does not choose a box for you, because the right box depends on
what your brokerage's 1099-DA shows for each line.

------------------------------------------------------------------------

## What CaptaTrader writes, and when

Per tax year, under `%LOCALAPPDATA%\CaptaTrader\tax-reports` (Windows) or
the equivalent local app-data folder:

| File | Contents | Use |
|------|----------|-----|
| `captatrader-YYYY-form8949.csv` | One row per lot disposal, in Form 8949 column order, with term (short/long), brokerage, and lot ID | Import into tax software, give to a preparer, or check against 1099-DA |
| `captatrader-YYYY-transactions.csv` | Every filled order: time (UTC), side, quantity, price, gross value, fee, whether the fee was estimated, brokerage, broker order ID | Reconcile against brokerage statements |

-   **The SQLite ledger is the source of truth**; CSVs are regenerated
    whole from it after every filled sell (and on demand from the Tax
    reports screen), written atomically via a temporary file.
-   **CSV** was chosen over Excel: tax software and preparers import CSV,
    Excel opens it directly (UTF-8 with BOM), and it needs no library.
-   **Tax year** is the calendar year in the user's local time zone,
    which is how an individual's transaction dates are determined.
-   **Paper and demo trades are excluded** from real tax reports. Demo
    mode writes its own files in a throwaway folder.
-   Money is rounded to cents per row; totals computed from unrounded
    values can differ by a cent.

### Recordkeeping

Keep records that support your return for **at least three years** after
filing (six if income is substantially understated, and indefinitely for
the basis of anything you still hold). Back up the app folder,
including `captatrader.db` and `tax-reports`. The login cannot be
recovered, but the files are readable without it.

------------------------------------------------------------------------

## Things this app does not handle (yet)

-   **Fee precision.** Some brokerages (Alpaca crypto) take fees in the
    asset received and report them separately. The app records an
    *estimated* fee from the fee schedule and flags it; reconcile with
    the brokerage's fee activity records.
-   **State tax specifics**, NIIT, and AMT: the app's estimate is one
    user-entered marginal rate.
-   **Trader tax status / mark-to-market (§475(f))**: very active traders
    may qualify to treat gains as ordinary business income and deduct
    expenses. It requires an election by the original due date of the
    prior year's return and has significant consequences. Ask a
    professional before considering it; whether §475 even covers crypto
    is itself unsettled.
-   **Transfers between your own accounts** (not taxable, but basis must
    move with the coins).
-   **Non-US taxpayers.** Everything here is US-specific.

------------------------------------------------------------------------

## References

-   IRS, *Digital assets*: <https://www.irs.gov/digital-assets>
-   IRS Notice 2014-21 (virtual currency is property)
-   IRS Rev. Proc. 2024-28 (per-account basis transition)
-   Instructions for Form 8949: <https://www.irs.gov/instructions/i8949>
-   Instructions for Form 1099-DA: <https://www.irs.gov/instructions/i1099da>
-   IRS Topic 409, *Capital gains and losses*:
    <https://www.irs.gov/taxtopics/tc409>
-   IRS Publication 505, *Tax Withholding and Estimated Tax*
-   H.R. 9172 (119th Congress), proposed extension of wash-sale rules to
    digital assets
