using Aletheia.Analytics;

namespace Aletheia.Application;

/// <summary>
/// Stores top-level realized performance and risk metrics.
/// </summary>
public sealed record PerformanceSummary(
    double Cagr,
    double CumulativeReturn,
    double AnnualizedVolatility,
    DrawdownResult MaximumDrawdown,
    double CurrentDrawdown,
    double SharpeRatio,
    double SortinoRatio,
    double Lag1Autocorrelation);
