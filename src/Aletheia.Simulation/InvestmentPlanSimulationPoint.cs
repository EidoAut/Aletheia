namespace Aletheia.Simulation;

/// <summary>
/// Stores one monthly cross-path portfolio-value summary.
/// </summary>
public sealed record InvestmentPlanSimulationPoint(
    int MonthOffset,
    DateOnly Date,
    double TotalContributed,
    double MeanValue,
    double P10Value,
    double P25Value,
    double MedianValue,
    double P75Value,
    double P90Value);
