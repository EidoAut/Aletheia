namespace Aletheia.Application;

/// <summary>
/// Stores presentation-ready periodic-investment scenario results.
/// </summary>
public sealed record InvestmentSimulationSummary(
    DatasetSummary Dataset,
    InvestmentSimulationRequest Request,
    DateOnly StartDate,
    DateOnly TargetDate,
    double TotalContributed,
    double MeanTerminalValue,
    double MedianTerminalValue,
    double P10TerminalValue,
    double P25TerminalValue,
    double P75TerminalValue,
    double P90TerminalValue,
    double ProbabilityTerminalBelowContributions,
    double ObservationPeriodsPerMonth,
    double HistoricalMeanLogReturnPerObservation,
    double HistoricalStandardDeviationPerObservation,
    double MonthlyMeanLogReturn,
    double MonthlyStandardDeviation,
    IReadOnlyList<InvestmentValueProjectionPoint> Trajectory,
    string Methodology);
