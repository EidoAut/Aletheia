# Project Structure

This page summarizes the role of every source project in product version `2.7.3`.

| Project | Role |
| --- | --- |
| `Aletheia.Core` | Domain identifiers, NAV observations, observation frequency, forecast horizons, state schemas, dataset identities, release identifiers, and prediction records. |
| `Aletheia.TimeSeries` | Immutable ordered time-series abstraction with propagated observation-frequency metadata. |
| `Aletheia.Mathematics` | Descriptive statistics, regression primitives, causal normalization, and PCA. |
| `Aletheia.Analytics` | Return calculation, risk metrics, rolling metrics, trend, momentum, and numerical derivatives. |
| `Aletheia.Data` | CSV loading, sample provider, CNMV IIC provider, provider cache, provenance, normalization, and data quality. |
| `Aletheia.Dynamics` | Dynamic-state reconstruction, state schema fingerprints, AR(1), GARCH, Kalman filtering, HMM regimes, and analogue support. |
| `Aletheia.Spectral` | FFT, inverse FFT, power spectrum, dominant-frequency, and rolling spectral-stability diagnostics. |
| `Aletheia.Forecasting` | Forecast distributions, capability metadata, naive probabilistic baseline, and forecast ensembles. |
| `Aletheia.Simulation` | Monte Carlo paths, investment-plan simulation, stress scenarios, and timing economic backtests. |
| `Aletheia.Validation` | Forecast models, walk-forward validation, Model Arena, metrics, prediction-ledger abstraction, timing labels, timing models, calibration, nested selection, and final holdout helpers. |
| `Aletheia.Persistence` | SQLite implementation of the prediction ledger. |
| `Aletheia.Application` | Shared use-case orchestration and presentation-ready models for CLI and desktop. |
| `Aletheia.Cli` | Console composition surface over the application layer. |
| `Aletheia.Desktop` | Native WinForms analytical shell over the application layer. |

## Test Projects

Each major source project has a corresponding xUnit test project under `tests/`. Desktop tests target
`net8.0-windows`; other test projects target `net8.0`.

## Dependency Notes

The desktop project references ScottPlot.WinForms for charting. Persistence references
Microsoft.Data.Sqlite. Test projects use xUnit, Microsoft.NET.Test.Sdk, and coverlet.collector.
