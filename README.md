# Aletheia

<p align="center">
  <img src="docs/assets/eido_logo.png" alt="Aletheia by EIDO" width="220" />
</p>

**Aletheia is an open-source quantitative fund analysis and forecasting platform written in C# that models investment funds as stochastic dynamic systems. It combines time-series analysis, dynamic-state modeling, signal processing, probabilistic forecasting and rigorous out-of-sample validation to study market states, transitions and investment hypotheses.**

Aletheia is a quantitative research and analytical tool. It does not guarantee future investment performance.

## Wiki

The canonical documentation is now the [Aletheia Wiki](docs/index.md). Start there for installation,
desktop and CLI workflows, decision-signal interpretation, models, validation, limitations,
architecture, and developer documentation.

## Start Here

| Goal | Link |
| --- | --- |
| Install and run Aletheia | [Installation](docs/getting-started/installation.md) |
| Run the sample analysis | [First Analysis](docs/getting-started/first-analysis.md) |
| Understand `BUY?`, `HOLD?`, `SELL?`, and `NO CALL` | [Decision Signals](docs/concepts/decision-signals.md) |
| Inspect scientific rigor | [Validation Philosophy](docs/validation/philosophy.md) |
| Review limitations | [Validation Limitations](docs/validation/limitations.md) |
| Extend the codebase | [Repository Guide](docs/development/repository-guide.md) |

## Current Status

This repository contains product release 2.7.3 with scientific version
`2.12.0-causal-horizon-integrity`. It is a technically robust and scientifically grounded
open-source MVP with core domain types, time-series analytics, provider-backed fund discovery,
provenance, dynamic-state reconstruction, spectral diagnostics, probabilistic forecasting,
simulation, walk-forward validation, Model Arena, prediction-ledger integrity, market-timing
research, a CLI, and a native WinForms desktop shell.

The CLI intentionally reports `NO RELIABLE SIGNAL` for unvalidated decisions and `NO RELIABLE ECONOMIC BACKTEST` when historical OOS timing evidence is insufficient. Aletheia should not emit BUY/SELL signals or trading curves until prediction records, validation history, calibration, model comparison, and delayed economic execution justify them.

## Prerequisites

The preferred toolchain is a stable .NET 8 SDK. The repository accepts any installed SDK from 8.0.100 onward and only rolls to a later major when no compatible .NET 8 SDK is available. Prerelease SDKs are accepted as a fallback so Visual Studio Preview installations can still load the solution. CI installs the latest stable .NET 8 SDK explicitly.

Verify the SDK resolver before opening or building the solution:

```powershell
dotnet --list-sdks
dotnet --version
```

## Run

```powershell
dotnet run --project src/Aletheia.Cli -- sample
dotnet run --project src/Aletheia.Cli -- analyze examples/sample-fund.csv
dotnet run --project src/Aletheia.Cli -- funds search mediolanum
dotnet run --project src/Aletheia.Cli -- funds search --isin ES0000000000
dotnet run --project src/Aletheia.Cli -- analyze --provider cnmv-iic --fund ES0000000000
dotnet run --project src/Aletheia.Cli -- arena sample
dotnet run --project src/Aletheia.Cli -- arena examples/sample-fund.csv
dotnet run --project src/Aletheia.Cli -- arena --provider cnmv-iic --fund ES0000000000
dotnet run --project src/Aletheia.Cli -- timing sample
dotnet run --project src/Aletheia.Cli -- timing examples/sample-fund.csv
dotnet run --project src/Aletheia.Cli -- timing --provider cnmv-iic --fund ES0000000000
dotnet run --project src/Aletheia.Cli -- backtest sample
dotnet run --project src/Aletheia.Cli -- backtest examples/sample-fund.csv --cost 0.001 --slippage 0.0005 --delay 1
dotnet run --project src/Aletheia.Cli -- backtest --provider cnmv-iic --fund ES0000000000 --from 2024-01-01 --to 2024-12-31
dotnet run --project src/Aletheia.Cli -- simulate sample --initial 1800 --monthly 100 --years 10 --paths 5000
dotnet run --project src/Aletheia.Cli -- simulate examples/sample-fund.csv --initial 10000 --monthly 0 --years 5
dotnet run --project src/Aletheia.Cli -- simulate --provider cnmv-iic --fund ES0000000000 --initial 1800 --monthly 100 --years 10
dotnet run --project src/Aletheia.Cli -- predictions list
dotnet run --project src/Aletheia.Desktop
```

## Build, test and publish

```powershell
./scripts/build.ps1
./scripts/publish-desktop.ps1 -Configuration Release -Runtime win-x64
```

The publish script creates `artifacts/Aletheia.Desktop-win-x64.zip`. Pass `-SelfContained` to include the .NET runtime; CI uses this option so its artifact is ready to run on Windows x64. The local SDK compatibility policy is stored in `global.json`; CI deliberately installs the latest stable .NET 8 SDK.

## Documentation Site

```powershell
python -m venv .venv-docs
.venv-docs\Scripts\Activate.ps1
pip install -r requirements-docs.txt
mkdocs serve
mkdocs build --strict
```

GitHub Pages deployment is configured in `.github/workflows/docs.yml`. In GitHub repository
settings, select GitHub Actions as the Pages deployment source. Private repositories also require a
GitHub plan that supports Pages for private repos; otherwise the workflow builds the docs and skips
deployment with a notice.

## Project Shape

- `Aletheia.Core`: domain identifiers, NAV observations, explicit forecast horizons, observation calendars, dynamic state vectors, model descriptors, dataset identities, and prediction records.
- `Aletheia.TimeSeries`: immutable ordered time-series abstraction with observation-frequency metadata.
- `Aletheia.Mathematics`: descriptive statistics, regression, and first-component PCA infrastructure.
- `Aletheia.Analytics`: returns, risk metrics, rolling metrics, autocorrelation, trend, momentum, and numerical derivatives.
- `Aletheia.Data`: CSV ingestion, deterministic sample provider, CNMV IIC discovery/history provider, provider cache, provenance metadata, normalization, and data-quality diagnostics.
- `Aletheia.Dynamics`: unified dynamic-state feature construction, schema fingerprints, AR(1) log-return baseline model, and strict-schema historical analogue search.
- `Aletheia.Spectral`: FFT, inverse FFT, observation-index power spectrum, one-sided amplitude normalization, dominant frequency diagnostics, and rolling spectral stability.
- `Aletheia.Forecasting`: forecast distribution abstraction with horizon-resolution metadata, mixture-CDF ensemble quantiles, and a naive probabilistic baseline.
- `Aletheia.Simulation`: deterministic Monte Carlo return simulation, periodic-investment value scenarios with explicit cadence scaling and workload guards, and economic timing backtests with delayed execution, costs, slippage, turnover, drawdown, Sharpe, Sortino and Calmar metrics.
- `Aletheia.Validation`: model-agnostic walk-forward engine, nested walk-forward selection helpers, final holdout splitting, forecast-model adapters, metric calculators, non-overlapping horizon subset selection, Model Arena result models, and probabilistic market-timing validation.
- `Aletheia.Persistence`: SQLite prediction ledger implementation behind the validation-layer ledger abstraction.
- `Aletheia.Application`: shared use-case orchestration and presentation-ready analysis models for CLI and desktop surfaces.
- `Aletheia.Cli`: console composition interface over the application layer.
- `Aletheia.Desktop`: native WinForms analytical laboratory over the application layer.

## Scientific Rules

Aletheia treats historical evidence carefully:

- Correlation is not causation.
- A Fourier peak is not automatically a tradeable cycle.
- Calendar days, observation counts, and fund valuation days are different units.
- Annualized metrics require an observation-frequency convention; irregular histories use an explicit elapsed-time effective cadence derived from their actual timestamps.
- Observation-index spectral periods are not calendar-day periods.
- Heuristic adequacy/strength diagnostics are not statistical confidence.
- In-sample fit is insufficient.
- Historical walk-forward performance is not a live track record.
- Prediction records are immutable; realized outcomes belong to separate evaluations.
- Duplicate ledger writes must be identical or fail as integrity errors.
- Future information must not leak into historical feature generation.
- Historical market-timing features must remain causal.
- Current external spectral/ensemble evidence must never be backfilled into historical cutoffs; unavailable features remain absent rather than neutral-filled.
- Calendar-day market-timing labels require an actual valuation observation on or after the requested target date before `NoBarrierHit` can be recorded.
- A triple-barrier label can enter training only after its `EndIndex` is known; `StartIndex` alone is not sufficient.
- Validation evidence for one forecast horizon must not be reused to weight another horizon.
- Ensemble quantiles must represent a combined distribution approximation, not weighted averages of member percentile values.
- Complex models must compete against simple baselines.
- Direct model comparisons should use metric-family common-support events when enough exist.
- Unsupported forecast capabilities must show `N/A`, not fabricated metrics.
- A model with few evaluated samples must not be treated as equally reliable as one with many samples.
- Forecasts should expose uncertainty, disagreement, ReliabilityIndex, and failure.
- ReliabilityIndex is a validation-quality index, not a probability that a timing call will be correct.
- Spectral timing and unconditional hazard diagnostics remain descriptive until they have causal OOS evidence as independent predictors.
- Final holdout data should remain frozen while model design and threshold selection are performed on development data.
- A good Brier score, calibration summary, or ReliabilityIndex is not evidence of economic profitability; the delayed economic backtest is reported separately.
- External provider observations must retain their actual reported dates; Aletheia does not interpolate or forward-fill missing provider values.
- Dataset provenance should identify provider, source reference, retrieval/cache state, external identifier, and observation counts.
- The system must be allowed to return `UNKNOWN` or no reliable signal.

## Documentation

The MkDocs source lives under `docs/` and is declared explicitly in `mkdocs.yml`. The Wiki keeps
GitHub-readable Markdown while providing a searchable Material for MkDocs site with navigation,
tables of contents, code-copy buttons, Mermaid diagrams, math rendering, dark/light modes, and
GitHub Pages publication.
