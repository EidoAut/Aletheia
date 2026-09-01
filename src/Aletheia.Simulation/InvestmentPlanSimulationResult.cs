using Aletheia.Core;

namespace Aletheia.Simulation;

/// <summary>
/// Contains a periodic-investment Monte Carlo scenario and terminal diagnostics.
/// </summary>
public sealed record InvestmentPlanSimulationResult(
    InvestmentPlanOptions Options,
    ObservationFrequency ObservationFrequency,
    DateOnly StartDate,
    DateOnly TargetDate,
    double ObservationPeriodsPerMonth,
    double HistoricalMeanLogReturnPerObservation,
    double HistoricalStandardDeviationPerObservation,
    double MonthlyMeanLogReturn,
    double MonthlyStandardDeviation,
    double TotalContributed,
    double MeanTerminalValue,
    double MedianTerminalValue,
    double P10TerminalValue,
    double P25TerminalValue,
    double P75TerminalValue,
    double P90TerminalValue,
    double ProbabilityTerminalBelowContributions,
    IReadOnlyList<InvestmentPlanSimulationPoint> Trajectory,
    double MeanRealTerminalValue = 0d,
    double MedianRealTerminalValue = 0d,
    double P10RealTerminalValue = 0d,
    double P90RealTerminalValue = 0d,
    double ProbabilityLoss = 0d)
{
    /// <summary>
    /// Gets a concise description of the baseline scenario assumptions.
    /// </summary>
    public string Methodology => this.ObservationFrequency == ObservationFrequency.Irregular
        ? "IID Gaussian log-return baseline; irregular cadence is annualized from actual elapsed timestamps; NAV history is treated as fund-net performance; only explicitly configured external investor costs and inflation are applied."
        : "IID Gaussian log-return baseline; historical per-observation moments are scaled to months; NAV history is treated as fund-net performance; only explicitly configured external investor costs and inflation are applied.";
}
