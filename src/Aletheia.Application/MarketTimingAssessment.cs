#pragma warning disable SA1402 // Market-timing presentation records are intentionally grouped.

using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Stores the market-timing assessment for one fund.
/// </summary>
/// <param name="GeneratedAt">The generation timestamp.</param>
/// <param name="TrainingCutoff">The last observation used for current features.</param>
/// <param name="CurrentState">The current dynamic state.</param>
/// <param name="Horizons">Per-horizon timing assessments.</param>
/// <param name="CurrentTimingZone">The current timing zone.</param>
/// <param name="Decision">The research timing decision.</param>
/// <param name="PrimaryHorizonSelectionReason">Why the primary horizon was selected.</param>
/// <param name="Narrative">The deterministic narrative.</param>
/// <param name="Warnings">Warnings.</param>
/// <param name="Evidence">Positive evidence.</param>
/// <param name="CounterEvidence">Counter-evidence.</param>
/// <param name="AlertConditions">Alert conditions.</param>
/// <param name="AssessmentChange">The reconstructed change, when available.</param>
/// <param name="RegimeForecasts">Regime transition forecasts.</param>
/// <param name="ModelArenaResults">Scientific timing arena results.</param>
/// <param name="VolatilityDiagnostic">Volatility model diagnostic.</param>
/// <param name="GarchIntegrated">Whether GARCH was integrated for current volatility.</param>
/// <param name="EconomicBacktest">The economic OOS timing backtest, when evaluated.</param>
public sealed record MarketTimingAssessment(
    DateTimeOffset GeneratedAt,
    DateOnly TrainingCutoff,
    DynamicState CurrentState,
    IReadOnlyList<MarketTimingHorizonAssessment> Horizons,
    MarketTimingZone CurrentTimingZone,
    TimingDecision Decision,
    string PrimaryHorizonSelectionReason,
    MarketTimingNarrative Narrative,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> CounterEvidence,
    IReadOnlyList<TimingAlertCondition> AlertConditions,
    TimingAssessmentChange? AssessmentChange,
    IReadOnlyList<RegimeTransitionForecast> RegimeForecasts,
    IReadOnlyList<MarketTimingArenaResult> ModelArenaResults,
    string VolatilityDiagnostic,
    bool GarchIntegrated,
    TimingEconomicBacktestAssessment? EconomicBacktest = null);

/// <summary>
/// Stores one horizon-level market-timing assessment.
/// </summary>
/// <param name="Horizon">The horizon.</param>
/// <param name="ProbabilityUp">Probability upper barrier is reached first.</param>
/// <param name="ProbabilityDown">Probability lower barrier is reached first.</param>
/// <param name="ProbabilityNeutral">Probability no large move occurs.</param>
/// <param name="ProbabilityUpBeforeDown">Probability of upside before downside.</param>
/// <param name="ProbabilityDownBeforeUp">Probability of downside before upside.</param>
/// <param name="ForecastExpectedReturn">Terminal return expectation from the horizon return distribution, when available.</param>
/// <param name="ExpectedBarrierPayoff">Expected payoff implied by first-hit barrier probabilities.</param>
/// <param name="DownsideProbability">Downside-event probability.</param>
/// <param name="UpsideBarrier">The effective upside barrier used by labels and current forecast.</param>
/// <param name="DownsideBarrier">The effective downside barrier used by labels and current forecast.</param>
/// <param name="ReturnQuantiles">True terminal-return quantiles, when enough samples exist.</param>
/// <param name="MedianTimeToUp">Median time to upside event.</param>
/// <param name="MedianTimeToDown">Median time to downside event.</param>
/// <param name="ExpectedTimeToFirstEvent">Expected time to first event.</param>
/// <param name="UpProbabilityInterval">Bootstrap interval for upside event frequency.</param>
/// <param name="DownProbabilityInterval">Bootstrap interval for downside event frequency.</param>
/// <param name="Reliability">Reliability score.</param>
/// <param name="ModelAgreement">Model agreement score.</param>
/// <param name="EvidenceStrength">Evidence strength.</param>
/// <param name="Zone">Timing zone.</param>
/// <param name="ExpectedNavP10">P10 NAV estimate from true terminal-return quantiles.</param>
/// <param name="ExpectedNavP50">Median NAV estimate from true terminal-return quantiles.</param>
/// <param name="ExpectedNavP90">P90 NAV estimate from true terminal-return quantiles.</param>
public sealed record MarketTimingHorizonAssessment(
    ForecastHorizon Horizon,
    double ProbabilityUp,
    double ProbabilityDown,
    double ProbabilityNeutral,
    double ProbabilityUpBeforeDown,
    double ProbabilityDownBeforeUp,
    double? ForecastExpectedReturn,
    double ExpectedBarrierPayoff,
    double DownsideProbability,
    double UpsideBarrier,
    double DownsideBarrier,
    ForecastReturnQuantiles? ReturnQuantiles,
    int? MedianTimeToUp,
    int? MedianTimeToDown,
    double ExpectedTimeToFirstEvent,
    ProbabilityInterval UpProbabilityInterval,
    ProbabilityInterval DownProbabilityInterval,
    double Reliability,
    double ModelAgreement,
    EvidenceStrength EvidenceStrength,
    MarketTimingZone Zone,
    double? ExpectedNavP10,
    double? ExpectedNavP50,
    double? ExpectedNavP90)
{
    /// <summary>
    /// Gets the heuristic reliability index. This is not a probability of being right.
    /// </summary>
    public double ReliabilityIndex => this.Reliability;
}

/// <summary>
/// Stores an interpretable timing decision.
/// </summary>
/// <param name="Action">The research action.</param>
/// <param name="Strength">The signal strength.</param>
/// <param name="Probability">The primary directional probability.</param>
/// <param name="Confidence">The confidence level.</param>
/// <param name="PrimaryHorizon">The primary horizon.</param>
/// <param name="ExpectedUpside">Expected upside threshold/payoff proxy.</param>
/// <param name="ExpectedDownside">Expected downside threshold/payoff proxy.</param>
/// <param name="ExpectedPayoff">Expected barrier payoff, not a full terminal expected return.</param>
/// <param name="RiskAdjustedUtility">Risk-adjusted utility proxy.</param>
/// <param name="Evidence">Evidence.</param>
/// <param name="CounterEvidence">Counter-evidence.</param>
public sealed record TimingDecision(
    TimingDecisionAction Action,
    double Strength,
    double Probability,
    ConfidenceLevel Confidence,
    ForecastHorizon? PrimaryHorizon,
    double ExpectedUpside,
    double ExpectedDownside,
    double ExpectedPayoff,
    double RiskAdjustedUtility,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> CounterEvidence)
{
    /// <summary>
    /// Gets the investor-facing direction.
    /// </summary>
    public DirectionalSignal Direction { get; init; } = DirectionalSignal.None;

    /// <summary>
    /// Gets the validation qualification attached to the direction.
    /// </summary>
    public SignalQualification Qualification { get; init; } = SignalQualification.Unavailable;

    /// <summary>
    /// Gets directional or no-action support in [0, 1].
    /// </summary>
    public double DirectionalStrength { get; init; }

    /// <summary>
    /// Gets validation support in [0, 1].
    /// </summary>
    public double ValidationStrength { get; init; }

    /// <summary>
    /// Gets deterministic reasons for the label and qualification.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the visible investor label.
    /// </summary>
    public string DisplayLabel => DecisionSignalLabels.ToDisplayLabel(this.Direction, this.Qualification);

    /// <summary>
    /// Gets a value indicating whether the signal is qualified with a question mark.
    /// </summary>
    public bool IsTentative => this.Qualification == SignalQualification.Tentative;
}

/// <summary>
/// Stores a deterministic human-readable market-timing narrative.
/// </summary>
/// <param name="Summary">The summary.</param>
/// <param name="DirectionExplanation">The direction explanation.</param>
/// <param name="TimingExplanation">The timing explanation.</param>
/// <param name="RiskExplanation">The risk explanation.</param>
/// <param name="ConfidenceExplanation">The confidence explanation.</param>
/// <param name="ActionExplanation">The action explanation.</param>
public sealed record MarketTimingNarrative(
    string Summary,
    string DirectionExplanation,
    string TimingExplanation,
    string RiskExplanation,
    string ConfidenceExplanation,
    string ActionExplanation);
