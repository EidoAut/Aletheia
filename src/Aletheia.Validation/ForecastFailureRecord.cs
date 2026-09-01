using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Records a typed model failure at one historical cutoff.
/// </summary>
public sealed record ForecastFailureRecord(
    ModelDescriptor Model,
    DateOnly CutoffDate,
    int CutoffIndex,
    ForecastStatus Status,
    string Reason);
