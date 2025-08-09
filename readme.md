<p align="left">
  <img src="logo.png" width="175" alt="Capta Trader Logo">
</p>

Capta Trader
===============

Simple, as-is, Bitcoin trading bot written in C#.

> [!IMPORTANT]  
> This app is experimental and just for fun.

## Architecture
This is a cross-platform CLI application with a three-tier architecture. The presentation layer follows a MVP design pattern and the business layer is a modular monolith with locality of behavior in mind.

## Versioning
Capta Trader uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

## Build Requirements
- All projects target the LTS version of the [.NET SDK](https://dotnet.microsoft.com/en-us/download).
