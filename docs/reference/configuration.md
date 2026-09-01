# Configuration

Aletheia currently exposes configuration through CLI options, application options, desktop settings,
provider options, environment variables, and build files.

## User-Facing Configuration

| Area | Configuration |
| --- | --- |
| Desktop Model Arena | Calendar-day horizon selector in the header. |
| Simulation CLI | `--initial`, `--monthly`, `--years`, `--paths`, `--seed`. |
| Backtest CLI | `--cost`, `--slippage`, `--delay`, `--max-exposure`, `--periods-per-year`, `--no-initial-cost`. |
| Ledger path | `ALETHEIA_LEDGER_PATH`. |

## Application Defaults

`AletheiaApplicationOptions` includes:

- `RollingWindowObservations = 63`;
- `MaximumAnaloguePaths = 25`;
- `AnaloguePathHorizonObservations = 180`;
- `FreshDataMaxAgeDays = 45`;
- `ActionableDataMaxAgeDays = 75`;
- optional deterministic report timestamp;
- optional configured catalog and history providers.

## Provider Defaults

CNMV provider options include cache enablement, cache directory, cache TTL, timeouts, redirect limits,
payload byte limits, ZIP limits, XML limits, and user agent.

## Build Configuration

The shared .NET version prefix is set in `Directory.Build.props`. The central release constants live
in `src/Aletheia.Core/AletheiaRelease.cs`.
