# Architecture

Aletheia is organized around a strict separation between pure mathematical calculation, application use-case orchestration, and I/O or presentation concerns.

## Dependency Direction

The center of the system is `Aletheia.Core`. Mathematical modules depend inward on domain types and reusable time-series structures. Data ingestion, application orchestration, the CLI, and the desktop shell sit at the edge.

```text
Aletheia.Cli
 └── Aletheia.Application

Aletheia.Desktop
 └── Aletheia.Application

Aletheia.Application
 ├── Data
 ├── Analytics
 ├── Dynamics
 ├── Spectral
 ├── Forecasting
 ├── Simulation
 ├── Validation
 └── Persistence

Data ───────────────► Core, TimeSeries
Analytics ──────────► Core, TimeSeries, Mathematics
Dynamics ───────────► Core, TimeSeries, Mathematics, Analytics
Spectral ───────────► Mathematics/Core only where needed
Forecasting ────────► Core, TimeSeries, Mathematics, Analytics
Simulation ─────────► Core, Mathematics, Forecasting, Analytics
Validation ─────────► Core, Forecasting, Analytics, Dynamics, Mathematics
Persistence ────────► Core, Validation
```

The probabilistic market-timing engine lives in `Aletheia.Validation` and is surfaced through `Aletheia.Application`, `Aletheia.Cli`, and `Aletheia.Desktop`. It consumes fund NAV history plus validation-gated internal evidence; it does not introduce outbound trading, broker, or portfolio-allocation dependencies.

## Current Scope

Aletheia has accumulated several scientific and product passes: unit semantics, identity,
probabilistic naming, validation, fair comparison, application orchestration, fund discovery,
provider provenance, local cache safety, native desktop hardening, periodic-investment simulation,
research-report synthesis, probabilistic market timing, causal market-timing hardening, and causal
horizon integrity. The current repository includes:

- immutable fund and NAV domain types;
- provider abstraction for fund data;
- CSV and deterministic sample providers;
- CNMV IIC fund discovery and history loading from official XML ZIP publications;
- provider-backed search by name, ISIN, partial ISIN, and management company;
- local provider payload caching with stable cache keys;
- provenance metadata for provider id, retrieval timestamp, source URI/reference, cache state, external identifier, requested/returned date range, observation frequency, observation counts, and dataset fingerprint;
- holiday-tolerant business-daily cadence detection without date repair;
- data-quality diagnostics;
- return and risk analytics with frequency-aware annualization;
- time-domain feature extraction;
- dynamic state estimation with explicit `SimpleReturn`, `LogReturn`, `LogNavVelocityPerObservation`, and `LogNavAccelerationPerObservationSquared` dimensions;
- a shared state feature pipeline used for both current and historical states;
- state schema descriptors with deterministic configuration fingerprints;
- first-component PCA infrastructure;
- FFT and observation-index power-spectrum diagnostics;
- rolling spectral stability diagnostics;
- historical analogue search without future-date matches, with strict schema compatibility and query-date normalization;
- naive probabilistic forecast distributions with horizon-resolution metadata;
- deterministic Monte Carlo simulation over explicit observation counts;
- deterministic periodic-investment value scenarios with initial/monthly capital, exact percentile trajectories, cadence scaling and path-month workload limits;
- model-agnostic walk-forward execution with expanding and rolling windows;
- forecast-model adapters for zero return, historical probability climatology, historical mean, AR(1), and historical analogues;
- explicit forecast capabilities and point-forecast statistic metadata;
- direction prediction rules separate from point/probability forecasts;
- typed model failures rather than exception-driven normal flow;
- point, probability, calibration, quantile, interval-coverage, and baseline-relative skill metrics;
- point, probability, and quantile common-support evaluation sets for direct comparison;
- coverage diagnostics and grouped failure reasons;
- deterministic non-overlapping horizon subset selection;
- Model Arena comparison without a single magic score;
- immutable SQLite prediction ledger with separate evaluation rows and content-fingerprint conflict detection;
- structured prediction, model, dataset, and state-schema metadata;
- CLI pipeline over the application layer;
- native WinForms analytical shell over the application layer;
- fund-search-centered WinForms start page with sample/CSV as secondary inputs;
- dark chart styling for title, axes, ticks, grid, and legend;
- EIDO/Aletheia header identity and graphite/amber palette;
- shell-wide busy progress, operation cancellation and recoverable-state restoration;
- lazy analytical page population and deferred chart refresh;
- periodic-investment access through both CLI and WinForms.

## Boundaries

Mathematical classes are deterministic and side-effect free where practical. I/O is isolated in `Aletheia.Data`, `Aletheia.Cli`, `Aletheia.Application`, and `Aletheia.Desktop`. The dynamic state is a named vector so dimensions can evolve without rewriting the entire engine.

The central design rule after Milestone 1.2 is that mathematical values with different units do not share ambiguous names. A horizon is either calendar days or observations. A return is either simple return or log return. A simple-return forecast is identified as a point forecast, median, or expectation. A spectral period is currently measured in observations. A derivative over NAV history is explicitly per observation.

## Data Flow

```text
Fund NAV history
    -> observation-frequency metadata
    -> transformations that preserve cadence metadata
    -> unified state feature pipeline
    -> dynamic state with schema fingerprint
    -> historical analogues and validation
```

```text
Original log-return signal
    -> mean removal / detrending / windowing
    -> FFT / one-sided amplitude and power spectrum
    -> dominant period in observations
    -> rolling stability diagnostics
```

```text
Requested forecast horizon
    -> horizon resolution using observation frequency
    -> effective observation count and optional target date
    -> baseline forecast or Monte Carlo simulation
```

```text
Fund NAV history
    -> walk-forward cutoffs
    -> model training on past observations only
    -> frozen prediction ledger record
    -> realized future outcome
    -> separate prediction evaluation
    -> all-sample, metric-family common-support, and non-overlapping Model Arena metrics
```

```text
Presentation request
    -> Aletheia.Application use case
    -> domain/math/validation services
    -> presentation-ready result models
    -> CLI table or WinForms chart/page
```

```text
Fund discovery query
    -> application FundDiscoveryService
    -> provider catalog abstraction
    -> official provider search result
    -> provider history load
    -> reported NAV observations and provenance
    -> standard analysis without interpolation or forward-fill
```

```text
Research report request
    -> standard fund analysis
    -> descriptive, risk, rolling, spectral, dynamic, and stress evidence
    -> optional Model Arena validation
    -> validation-gated forecast ensemble
    -> separate long-run fund score and current-opportunity assessment
    -> decision signal with direction, qualification, evidence, counter-evidence, warnings, and actionability
```

```text
Market timing request
    -> triple-barrier event definitions
    -> causal feature pipeline
    -> walk-forward timing model arena
    -> probability calibration and baseline-relative skill checks
    -> evidence-weighted timing ensemble
    -> timing zones, warnings, and narrative
```

```text
Economic timing backtest request
    -> historical OOS timing predictions only
    -> decision dates and target exposure reconstruction
    -> delayed execution on a later NAV observation
    -> Aletheia timing path with costs and slippage
    -> buy-and-hold path invested from inception
    -> neutral/no-action path
    -> turnover, time-in-market, drawdown, Sharpe, Sortino, Calmar and return metrics
    -> NO RELIABLE ECONOMIC BACKTEST when OOS evidence is insufficient
```

## Quantitative Research Engine

The 2.8 research engine keeps the original dependency direction and adds a synthesis layer in `Aletheia.Application`. The lower projects still expose deterministic, typed calculations:

- `Aletheia.Mathematics` owns descriptive statistics and causal normalization.
- `Aletheia.Analytics` owns rolling metrics and risk measures.
- `Aletheia.Dynamics` owns EWMA volatility, constrained GARCH(1,1), local-linear Kalman filtering, and Gaussian HMM regimes.
- `Aletheia.Spectral` owns spectral diagnostics and conservative component evidence.
- `Aletheia.Forecasting` owns evidence-weighted forecast ensembling.
- `Aletheia.Simulation` owns investor-cost-aware investment plans, bootstrap return paths, and deterministic stress scenarios.
- `Aletheia.Validation` owns walk-forward validation and the state-space forecast adapter.

`FundResearchReportBuilder` is intentionally an application-layer coordinator. It does not make a standalone trading claim. It combines already-computed evidence, attaches optional Model Arena results when available, and keeps fund quality, current attractiveness, directional estimate, validation qualification, confidence, and actionability as separate concepts.

## Causal Market Timing

The 2.12 causal horizon-integrity engine extends `Aletheia.Validation` with explicit market-event definitions, triple-barrier labels whose training availability is based on `EndIndex`, causal timing features, competing-risk hazards reported outside independent ensemble diversity, calibrated event classification, robust historical analogue timing, regime-transition timing, experimental spectral timing candidates, same-horizon conservative ensemble weighting, nested walk-forward selection helpers, final holdout splitting, and an integrated economic backtest over historical OOS decisions.

The engine uses the same scientific contract as Model Arena:

- historical features are built without future information;
- current predictions are separated from reconstructed historical predictions;
- every non-baseline timing model is compared against a simple historical event-rate baseline;
- calibration and Brier skill are reported separately from directional edge;
- out-of-distribution current states and model disagreement reduce confidence.

Application surfaces convert the scientific result into a `MarketTimingAssessment`. The assessment contains per-horizon upside/downside/no-barrier probabilities, timing zones, decision strength, evidence labels, warnings, model diagnostics, hazard summaries, and a deterministic explanation. The CLI exposes this through `aletheia timing`; the desktop exposes it through the `Market Timing` page.

Economic backtesting remains separate from probability validation. The application converts only historical OOS predictions into target exposures, preserves calculation/decision/execution dates, executes after the configured observation delay, and compares against buy-and-hold and neutral/no-action baselines. A good Brier score, calibration result, or ReliabilityIndex is therefore not reported as economic profit evidence. When there are fewer than the configured usable historical OOS decisions, the application reports `NO RELIABLE ECONOMIC BACKTEST` instead of fabricating an in-sample curve.

## Limitations

The current engine includes regime switching diagnostics, local-linear Kalman filtering, validation-gated forecast ensembling, scoring, decision-signal generation, probabilistic market timing, nested walk-forward selection helpers, final holdout splitting, and delayed economic timing backtests. It still does not implement STFT, wavelets, multivariate factor models, portfolio optimization, tax-aware execution, liquidity-aware execution, or broker integration. It also does not claim that any model has demonstrated profitable forecasting; it provides the machinery to test such claims under equivalent scientific conditions and marks unvalidated directional estimates as tentative or unavailable. CNMV is the first official provider, not an exhaustive global fund universe.
