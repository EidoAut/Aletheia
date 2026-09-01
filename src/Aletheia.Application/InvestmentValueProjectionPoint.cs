namespace Aletheia.Application;

/// <summary>
/// Stores one monthly portfolio-value projection across simulated paths.
/// </summary>
public sealed record InvestmentValueProjectionPoint(
    int MonthOffset,
    DateOnly Date,
    double TotalContributed,
    double MeanValue,
    double P10Value,
    double P25Value,
    double MedianValue,
    double P75Value,
    double P90Value);
