using Aletheia.Core;
using Aletheia.Forecasting;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Stores the current forecast result for one model and horizon.
/// </summary>
public sealed record ForecastModelRun(
    ModelDescriptor Model,
    ForecastCapabilities Capabilities,
    PointForecastStatistic PointForecastStatistic,
    string ConfigurationFingerprint,
    ForecastHorizon RequestedHorizon,
    ForecastStatus Status,
    string? FailureReason,
    ForecastDistribution? Distribution);
