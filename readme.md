Capta Trader
===============

Simple, as-is, Bitcoin trading bot written in C#. For educational purposes only.

> [!WARNING]
> This bot is in pre-alpha development and subject to significant change.

## Architecture
This is a cross-platform CLI application with a three-tier architecture. The presentation layer follows a MVP design pattern and the business layer is a modular monolith with locality of behavior in mind.

## Versioning
Capta Trader uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

During initial development the version will be at 0.1.0. Once usable, the version will remain 0.y.z during rapid development. When the bot is ready for live use (executing real trades), version 1.0.0 will be released.

## Build Requirements
- All projects target the LTS version of the [.NET SDK](https://dotnet.microsoft.com/en-us/download).
