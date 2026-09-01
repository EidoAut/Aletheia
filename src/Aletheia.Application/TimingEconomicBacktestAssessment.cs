#pragma warning disable SA1402 // The signal trace is part of the economic backtest DTO contract.

using Aletheia.Core;
using Aletheia.Simulation;

namespace Aletheia.Application;

/// <summary>
/// Stores the economic timing backtest derived only from historical OOS timing decisions.
/// </summary>
/// <param name="Status">The reliability status.</param>
/// <param name="IsReliable">Whether a validated economic backtest was produced.</param>
/// <param name="Diagnostic">A deterministic explanation of the evidence gate.</param>
/// <param name="Horizon">The timing horizon used to build decisions.</param>
/// <param name="SignalCount">The number of OOS timing decisions converted to target exposure.</param>
/// <param name="FirstSignalDate">The first OOS signal date.</param>
/// <param name="LastSignalDate">The last OOS signal date.</param>
/// <param name="ExecutionDelayObservations">The execution delay.</param>
/// <param name="TransactionCostRate">The proportional transaction cost.</param>
/// <param name="SlippageRate">The proportional slippage cost.</param>
/// <param name="SignalTrace">The OOS signal trace.</param>
/// <param name="Results">Comparable strategy results.</param>
public sealed record TimingEconomicBacktestAssessment(
    string Status,
    bool IsReliable,
    string Diagnostic,
    ForecastHorizon? Horizon,
    int SignalCount,
    DateOnly? FirstSignalDate,
    DateOnly? LastSignalDate,
    int ExecutionDelayObservations,
    double TransactionCostRate,
    double SlippageRate,
    IReadOnlyList<TimingEconomicSignalTrace> SignalTrace,
    IReadOnlyList<TimingBacktestResult> Results);

/// <summary>
/// Stores one historical OOS timing decision and its delayed execution date.
/// </summary>
/// <param name="CalculationDate">The OOS prediction calculation date.</param>
/// <param name="DecisionDate">The date on which the decision was available.</param>
/// <param name="ExecutionDate">The NAV date on which the exposure can first be executed.</param>
/// <param name="Zone">The reconstructed timing zone.</param>
/// <param name="Reliability">The reconstructed reliability index.</param>
/// <param name="TargetExposure">The target gross exposure.</param>
public sealed record TimingEconomicSignalTrace(
    DateOnly CalculationDate,
    DateOnly DecisionDate,
    DateOnly ExecutionDate,
    string Zone,
    double Reliability,
    double TargetExposure);
