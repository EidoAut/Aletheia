# Prediction Ledger

## Purpose

The prediction ledger is Aletheia's accountability mechanism. A forecast is stored as an immutable prediction before the realized future outcome is evaluated.

## Core Rule

```text
Prediction != PredictionEvaluation
HistoricalWalkForward != Live
```

Predictions are never updated with actual outcomes. Evaluations are stored in a separate table linked by `PredictionId`.

## Schema Sketch

```text
Prediction
  PredictionId
  LogicalKey
  ContentFingerprint
  Origin
  GeneratedAtUtc
  FundIdentifier
  DatasetFingerprint
  ModelId / ModelVersion / ModelConfigurationFingerprint
  StateSchemaFingerprint
  TrainingStart / TrainingEnd
  CutoffIndex / CutoffDate
  RequestedHorizon / ResolvedHorizon
  TargetIndex / TargetDate
  ForecastDistribution
  ForecastCapabilities
  PointForecastStatistic
  Diagnostics

PredictionEvaluation
  PredictionEvaluationId
  PredictionId
  EvaluationContentFingerprint
  EvaluatedAtUtc
  ActualReturn
  AbsoluteError
  SquaredError
  DirectionCorrect
  ProbabilityOutcome
  BrierContribution
  DirectionRule
```

## SQLite Persistence

`Aletheia.Persistence` implements `IPredictionLedger` using `Microsoft.Data.Sqlite` and explicit SQL. Schema initialization is automatic. Prediction rows use a unique logical key for idempotency, and a separate content fingerprint detects scientific conflicts. Rerunning the same walk-forward validation with identical content is idempotent. Reusing the same logical key with different content raises `PredictionLedgerIntegrityException`.

## Historical Versus Live

Milestone 2 stores Model Arena predictions as `HistoricalWalkForward`. These rows are useful scientific backtest evidence, but they are not a live track record. Live prediction origin is reserved for future real-time forecasting workflows.

## Limitations

The first ledger is local SQLite. It is intentionally small and replaceable through `IPredictionLedger`; it is not a multi-user production database.
