![Capta Trader](./brand/logo_100.png)

Capta Trader
===============

Simple, as-is, Bitcoin trading bot for the terminal, written in C#.

> [!IMPORTANT]
> This app is experimental and just for fun. It is not financial or tax advice.
> Only simulated (paper) brokerages ship today.

## Running

```text
CaptaTrader            start normally (create a login on first run, then log in)
CaptaTrader --demo     replay a real, volatile day (2024-08-05) against a paper account
CaptaTrader --help     options
```

From the repository: `dotnet run --project src/CaptaTrader.CLIApp -- --demo`.

Once you're logged in, you can choose a brokerage and market data source, store API keys (kept in
your OS credential store), pick a strategy, set trading limits and tax-estimate rates,
start or stop auto-trading at any time, browse recent trades, and export per-tax-year CSVs.

## Architecture
A cross-platform terminal application with a modified three-tier architecture: **Business**
defines capabilities and rules, **Data** (SQLite + Dapper) and **Integrations** (brokerages,
market data, OS credential stores, report files) implement them, and **CLIApp** is the
composition root and console presentation, rendered with
[PrettyConsole](https://github.com/dusrdev/PrettyConsole).

## Documentation
- [Development conventions](docs/development_conventions.md)
- [Decisions](docs/decisions.md)
- [Tax considerations](docs/tax_considerations.md)
- [Trading considerations](docs/trading_considerations.md)
- [Roadmap](docs/roadmap.md)

## Versioning
Capta Trader uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

## Build Requirements
- All projects target the LTS version of the [.NET SDK](https://dotnet.microsoft.com/en-us/download).
- Build and test: `dotnet build/cake.cs`, or `dotnet test --project src/CaptaTrader.Tests`.
