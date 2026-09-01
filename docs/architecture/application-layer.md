# Application Layer

Milestone 2.2 introduced `Aletheia.Application` as the shared orchestration layer between presentation surfaces and the mathematical engine. Milestone 2.3 extended it with fund discovery and provider-backed history loading. Milestone 2.4 adds a presentation-neutral periodic-investment simulation use case.

## Purpose

The application layer owns use-case composition:

- load deterministic sample fund data;
- load CSV fund data;
- search configured fund catalogs;
- load provider-backed fund/share-class histories;
- run the standard fund analysis;
- run current forecast models;
- run periodic-investment scenarios;
- run Model Arena;
- build validation-aware research reports;
- build probabilistic market-timing assessments;
- run delayed economic timing backtests;
- read prediction-ledger summaries and details;
- expose chart-ready presentation series.

The layer does not replace `Aletheia.Analytics`, `Aletheia.Dynamics`, `Aletheia.Spectral`, `Aletheia.Forecasting`, or `Aletheia.Validation`. It calls those projects and packages their results into stable application result models.

## Dependency Direction

```text
Aletheia.Cli
       |
       v
Aletheia.Application
       |
       v
Core / Data / Analytics / Dynamics / Spectral / Forecasting / Validation / Persistence

Aletheia.Desktop
       |
       v
Aletheia.Application
```

The CLI and WinForms shell now consume the same use cases. This reduces drift between console output and desktop output.

## Principal Use Cases

- `LoadSampleWorkspaceAsync`
- `LoadCsvWorkspaceAsync`
- `SearchFundsAsync`
- `LoadProviderWorkspaceAsync`
- `AnalyzeFund`
- `RunInvestmentSimulation`
- `RunModelArenaAsync`
- `BuildResearchReport`
- `BuildMarketTimingAssessment`
- `RunTimingEconomicBacktest`
- `GetPredictionListAsync`
- `GetPredictionDetailsAsync`

`FundWorkspace` is the current in-memory analytical workspace. It contains the loaded history, dataset identity, standard analysis result, and optional Model Arena result.

`FundDiscoveryService` aggregates `IFundCatalogProvider` instances and maps selected results to `IProvenanceAwareFundDataProvider` history loaders. Presentation layers receive `FundSearchResultSummary` and `DatasetProvenanceSummary` instead of provider-specific XML or HTTP details.

## Presentation Models

The application layer exposes lightweight models such as `DatedValue`, `DistributionSummary`, `StateProjectionPoint`, `AnaloguePath`, `ForecastModelRun`, `InvestmentSimulationSummary`, `FundSearchResultSummary`, `DatasetProvenanceSummary`, and `PredictionLedgerSummary`.

These are not mathematical engine replacements. They are stable shapes for UI and CLI presentation.
