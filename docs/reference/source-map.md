# Source Map

This page lists stable source and test locations. Paths are relative to the repository root.

## Core and Data

| Topic | Source | Tests |
| --- | --- | --- |
| Release identifiers | `src/Aletheia.Core/AletheiaRelease.cs` | `tests/Aletheia.Core.Tests` |
| Forecast horizons | `src/Aletheia.Core/ForecastHorizon.cs`, `src/Aletheia.Core/ForecastHorizonResolver.cs` | `tests/Aletheia.Core.Tests` |
| NAV series | `src/Aletheia.Core/NavSeries.cs` | `tests/Aletheia.Core.Tests` |
| CSV data | `src/Aletheia.Data/CsvFundDataReader.cs` | `tests/Aletheia.Data.Tests/CsvFundDataReaderTests.cs` |
| CNMV provider | `src/Aletheia.Data/CnmvIicProvider.cs` | `tests/Aletheia.Data.Tests/CnmvIicProviderTests.cs` |
| Provider cache | `src/Aletheia.Data/LocalProviderCache.cs` | `tests/Aletheia.Data.Tests/ProviderInfrastructureTests.cs` |

## Models and Validation

| Topic | Source | Tests |
| --- | --- | --- |
| AR(1) | `src/Aletheia.Validation/AutoregressiveForecastModel.cs` | `tests/Aletheia.Validation.Tests` |
| Kalman forecast | `src/Aletheia.Validation/StateSpaceForecastModel.cs` | `tests/Aletheia.Validation.Tests/StateSpaceForecastModelTests.cs` |
| GARCH | `src/Aletheia.Dynamics/Garch11Estimator.cs` | `tests/Aletheia.Dynamics.Tests/DynamicVolatilityAndRegimeTests.cs` |
| HMM | `src/Aletheia.Dynamics/GaussianHiddenMarkovModel.cs` | `tests/Aletheia.Dynamics.Tests/DynamicVolatilityAndRegimeTests.cs` |
| Forecast ensemble | `src/Aletheia.Forecasting/ForecastEnsemble.cs` | `tests/Aletheia.Forecasting.Tests/ForecastEnsembleTests.cs` |
| Model Arena | `src/Aletheia.Validation/ModelArena.cs` | `tests/Aletheia.Validation.Tests/ModelArenaTests.cs` |
| Market timing | `src/Aletheia.Validation/MarketTimingModelArena.cs` | `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs` |
| Prediction ledger | `src/Aletheia.Persistence/SqlitePredictionLedger.cs` | `tests/Aletheia.Persistence.Tests/SqlitePredictionLedgerTests.cs` |

## Application and Surfaces

| Topic | Source | Tests |
| --- | --- | --- |
| Application orchestration | `src/Aletheia.Application/AletheiaApplicationService.cs` | `tests/Aletheia.Application.Tests` |
| Research reports | `src/Aletheia.Application/FundResearchReportBuilder.cs` | `tests/Aletheia.Application.Tests` |
| CLI | `src/Aletheia.Cli/Program.cs` | exercised through application and integration-level command validation |
| Desktop shell | `src/Aletheia.Desktop/MainForm.cs` | `tests/Aletheia.Desktop.Tests` |
| Desktop pages | `src/Aletheia.Desktop/Pages` | `tests/Aletheia.Desktop.Tests` |
