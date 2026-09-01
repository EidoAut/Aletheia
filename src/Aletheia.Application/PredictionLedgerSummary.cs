using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Stores one row in the prediction-ledger list view.
/// </summary>
public sealed record PredictionLedgerSummary(
    Guid PredictionId,
    PredictionOrigin Origin,
    FundIdentifier FundIdentifier,
    DateTimeOffset GeneratedAtUtc,
    DateOnly CutoffDate,
    string ModelName,
    ForecastHorizon Horizon,
    double? PointForecast,
    double? ExpectedReturn,
    double? ProbabilityPositive,
    DateOnly? TargetDate,
    string DatasetFingerprint);
