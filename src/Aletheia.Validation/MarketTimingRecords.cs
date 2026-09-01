#pragma warning disable SA1402 // Timing DTOs are intentionally grouped as one protocol surface.
#pragma warning disable SA1649 // The file groups market-timing protocol records.

using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Defines one market-timing event in explicit horizon and threshold terms.
/// </summary>
/// <param name="Type">The event type.</param>
/// <param name="Threshold">The absolute positive threshold value.</param>
/// <param name="Horizon">The event horizon.</param>
/// <param name="ReferenceValue">The reference value used by the event.</param>
/// <param name="Direction">The event direction.</param>
public sealed record MarketEventDefinition(
    MarketEventType Type,
    double Threshold,
    ForecastHorizon Horizon,
    string ReferenceValue,
    MarketEventDirection Direction);

/// <summary>
/// Defines a triple-barrier labeling problem.
/// </summary>
/// <param name="Horizon">The vertical barrier horizon.</param>
/// <param name="UpsideThreshold">The fixed upside simple-return threshold.</param>
/// <param name="DownsideThreshold">The fixed downside simple-return threshold.</param>
/// <param name="Policy">The barrier threshold policy.</param>
/// <param name="UpsideVolatilityMultiplier">The upside volatility multiplier for volatility-scaled barriers.</param>
/// <param name="DownsideVolatilityMultiplier">The downside volatility multiplier for volatility-scaled barriers.</param>
public sealed record TripleBarrierDefinition(
    ForecastHorizon Horizon,
    double UpsideThreshold,
    double DownsideThreshold,
    BarrierThresholdPolicy Policy = BarrierThresholdPolicy.FixedPercentage,
    double UpsideVolatilityMultiplier = 1d,
    double DownsideVolatilityMultiplier = 1d);

/// <summary>
/// Stores one causal feature vector used by timing models.
/// </summary>
/// <param name="Date">The feature date.</param>
/// <param name="ObservationIndex">The observation index.</param>
/// <param name="Values">The named finite feature values. A missing key means the feature was not causally available.</param>
public sealed record MarketTimingFeatureVector(
    DateOnly Date,
    int ObservationIndex,
    IReadOnlyDictionary<string, double> Values)
{
    /// <summary>
    /// Returns whether a feature was causally available for this cutoff.
    /// </summary>
    /// <param name="name">The feature name.</param>
    /// <returns><see langword="true"/> when the feature has a finite observed value.</returns>
    public bool HasFeature(string name) =>
        this.Values.TryGetValue(name, out var value) && double.IsFinite(value);

    /// <summary>
    /// Reads a causally available feature value.
    /// </summary>
    /// <param name="name">The feature name.</param>
    /// <param name="value">The feature value.</param>
    /// <returns><see langword="true"/> when the feature has a finite observed value.</returns>
    public bool TryGetFeature(string name, out double value)
    {
        if (this.Values.TryGetValue(name, out value) && double.IsFinite(value))
        {
            return true;
        }

        value = 0d;
        return false;
    }
}

/// <summary>
/// Stores diagnostics for a generated feature path.
/// </summary>
/// <param name="Features">The causal feature vectors.</param>
/// <param name="FeatureNames">The deterministic feature order.</param>
/// <param name="GarchConverged">Whether GARCH converged on the current prefix.</param>
/// <param name="VolatilityDiagnostic">Volatility model diagnostic.</param>
/// <param name="CurrentGarchVolatility">The current GARCH volatility or fallback volatility.</param>
/// <param name="CurrentEwmaVolatility">The current EWMA volatility.</param>
public sealed record MarketTimingFeaturePipelineResult(
    IReadOnlyList<MarketTimingFeatureVector> Features,
    IReadOnlyList<string> FeatureNames,
    bool GarchConverged,
    string VolatilityDiagnostic,
    double CurrentGarchVolatility,
    double CurrentEwmaVolatility);

/// <summary>
/// Supplies optional external evidence to the timing feature pipeline.
/// </summary>
/// <param name="SpectralReliability">Validated spectral reliability, if available.</param>
/// <param name="SpectralPhase">Dominant spectral phase, if available.</param>
/// <param name="SpectralStability">Dominant spectral stability, if available.</param>
/// <param name="EnsembleExpectedReturn">Validation-gated ensemble expected return, if available.</param>
/// <param name="EnsembleProbabilityPositive">Validation-gated ensemble positive-return probability, if available.</param>
/// <param name="EnsembleDownsideProbability">Validation-gated ensemble downside probability, if available.</param>
/// <param name="EnsembleDisagreement">Validation-gated ensemble disagreement, if available.</param>
/// <param name="EnsembleReliability">Validation-gated ensemble reliability, if available.</param>
public sealed record MarketTimingExternalEvidence(
    double? SpectralReliability = null,
    double? SpectralPhase = null,
    double? SpectralStability = null,
    double? EnsembleExpectedReturn = null,
    double? EnsembleProbabilityPositive = null,
    double? EnsembleDownsideProbability = null,
    double? EnsembleDisagreement = null,
    double? EnsembleReliability = null);

/// <summary>
/// Stores an online change-point probability.
/// </summary>
/// <param name="Index">The observation index.</param>
/// <param name="ProbabilityChangePoint">The probability of a structural change now.</param>
/// <param name="RunLengthWindow">The causal comparison window length.</param>
public sealed record ChangePointProbabilityPoint(
    int Index,
    double ProbabilityChangePoint,
    int RunLengthWindow);

/// <summary>
/// Stores one triple-barrier target label.
/// </summary>
/// <param name="Date">The starting date.</param>
/// <param name="StartIndex">The starting observation index.</param>
/// <param name="Outcome">The realized outcome.</param>
/// <param name="TimeToEvent">The first event time in observations, or the horizon if no horizontal barrier was reached.</param>
/// <param name="RealizedReturn">The terminal simple return at event or vertical barrier.</param>
/// <param name="MaximumFavorableExcursion">The maximum favorable simple-return excursion before the label ends.</param>
/// <param name="MaximumAdverseExcursion">The maximum adverse simple-return excursion before the label ends.</param>
/// <param name="UpperThreshold">The effective upper barrier simple-return threshold.</param>
/// <param name="LowerThreshold">The effective lower barrier simple-return threshold as a positive magnitude.</param>
/// <param name="RequestedTargetDate">The requested calendar target date, when applicable.</param>
/// <param name="EffectiveValuationDate">The observation date actually used to value the horizon.</param>
/// <param name="IsCalendarValuationApproximation">Whether the effective valuation date approximates the requested target date.</param>
/// <param name="IsHorizonComplete">Whether the full requested horizon was present in the dataset.</param>
public sealed record TripleBarrierOutcome(
    DateOnly Date,
    int StartIndex,
    TripleBarrierOutcomeType Outcome,
    int TimeToEvent,
    double RealizedReturn,
    double MaximumFavorableExcursion,
    double MaximumAdverseExcursion,
    double UpperThreshold,
    double LowerThreshold,
    DateOnly? RequestedTargetDate = null,
    DateOnly? EffectiveValuationDate = null,
    bool IsCalendarValuationApproximation = false,
    bool IsHorizonComplete = true)
{
    /// <summary>
    /// Gets the last observation index required to know this label's realized outcome.
    /// Horizontal hits end at the first barrier touch; no-hit labels end at the vertical barrier.
    /// </summary>
    public int EndIndex => this.StartIndex + this.TimeToEvent;
}

/// <summary>
/// Stores a probability interval.
/// </summary>
/// <param name="Lower">The lower probability bound.</param>
/// <param name="Upper">The upper probability bound.</param>
public sealed record ProbabilityInterval(double Lower, double Upper);

/// <summary>
/// Stores calibrated and raw probability diagnostics.
/// </summary>
/// <param name="Raw">The raw probability triple.</param>
/// <param name="Calibrated">The calibrated probability triple, or the raw triple when calibration is unavailable.</param>
/// <param name="Status">The calibration status.</param>
/// <param name="Method">The calibration method.</param>
/// <param name="TrainingSampleCount">The number of prior OOS samples used to fit the current calibrator.</param>
public sealed record ProbabilityCalibrationDiagnostic(
    MarketEventPrediction Raw,
    MarketEventPrediction Calibrated,
    ProbabilityCalibrationStatus Status,
    string Method,
    int TrainingSampleCount);

/// <summary>
/// Stores terminal-return forecast quantiles.
/// </summary>
/// <param name="P10">The 10th percentile terminal return.</param>
/// <param name="P25">The 25th percentile terminal return.</param>
/// <param name="P50">The median terminal return.</param>
/// <param name="P75">The 75th percentile terminal return.</param>
/// <param name="P90">The 90th percentile terminal return.</param>
/// <param name="SampleCount">The number of terminal returns behind the estimate.</param>
/// <param name="Method">The estimation method.</param>
public sealed record ForecastReturnQuantiles(
    double P10,
    double P25,
    double P50,
    double P75,
    double P90,
    int SampleCount,
    string Method);

/// <summary>
/// Stores the effective barriers used for the current horizon.
/// </summary>
/// <param name="Upside">The effective upside simple-return barrier.</param>
/// <param name="Downside">The effective downside simple-return barrier as a positive magnitude.</param>
/// <param name="Policy">The barrier policy.</param>
/// <param name="Diagnostic">Human-readable barrier diagnostic.</param>
public sealed record EffectiveBarrierDiagnostic(
    double Upside,
    double Downside,
    BarrierThresholdPolicy Policy,
    string Diagnostic);

/// <summary>
/// Stores timing probability calibration metrics.
/// </summary>
/// <param name="SampleCount">The out-of-sample sample count.</param>
/// <param name="BrierScore">The multiclass Brier score.</param>
/// <param name="LogLoss">The multiclass log loss.</param>
/// <param name="ExpectedCalibrationError">The expected calibration error.</param>
/// <param name="BalancedAccuracy">The balanced accuracy from maximum-probability labels.</param>
/// <param name="CalibrationLabel">A readable calibration label.</param>
/// <param name="PerClassCalibration">One-vs-rest reliability summaries for UP, DOWN and NONE.</param>
/// <param name="ReliabilityBins">Winner-class reliability diagram bins.</param>
/// <param name="BrierDecomposition">Approximate multiclass Brier decomposition by winner-confidence bins.</param>
public sealed record TimingCalibrationSummary(
    int SampleCount,
    double BrierScore,
    double LogLoss,
    double ExpectedCalibrationError,
    double BalancedAccuracy,
    string CalibrationLabel,
    IReadOnlyList<TimingClassCalibrationSummary>? PerClassCalibration = null,
    IReadOnlyList<TimingReliabilityBin>? ReliabilityBins = null,
    TimingBrierDecomposition? BrierDecomposition = null);

/// <summary>
/// Stores one-vs-rest calibration error for a timing class.
/// </summary>
/// <param name="ClassName">The class name.</param>
/// <param name="ExpectedCalibrationError">The classwise ECE.</param>
/// <param name="SampleCount">The sample count.</param>
public sealed record TimingClassCalibrationSummary(
    string ClassName,
    double ExpectedCalibrationError,
    int SampleCount);

/// <summary>
/// Stores one reliability-diagram bin for timing probabilities.
/// </summary>
/// <param name="LowerBoundInclusive">The lower probability bound.</param>
/// <param name="UpperBoundInclusive">The upper probability bound.</param>
/// <param name="SampleCount">The number of predictions in the bin.</param>
/// <param name="MeanConfidence">The mean predicted confidence.</param>
/// <param name="ObservedAccuracy">The observed winner accuracy.</param>
public sealed record TimingReliabilityBin(
    double LowerBoundInclusive,
    double UpperBoundInclusive,
    int SampleCount,
    double? MeanConfidence,
    double? ObservedAccuracy);

/// <summary>
/// Stores an approximate multiclass Brier decomposition.
/// </summary>
/// <param name="Reliability">Calibration component; lower is better.</param>
/// <param name="Resolution">Resolution component; higher is better.</param>
/// <param name="Uncertainty">Base-rate uncertainty component.</param>
public sealed record TimingBrierDecomposition(
    double Reliability,
    double Resolution,
    double Uncertainty);

/// <summary>
/// Stores one point on a competing-risk hazard forecast.
/// </summary>
/// <param name="Step">The future observation step.</param>
/// <param name="HazardUp">The conditional upside hazard at this step.</param>
/// <param name="HazardDown">The conditional downside hazard at this step.</param>
/// <param name="Survival">The probability no event has occurred before this step.</param>
/// <param name="CumulativeIncidenceUp">The cumulative upside incidence through this step.</param>
/// <param name="CumulativeIncidenceDown">The cumulative downside incidence through this step.</param>
public sealed record EventHazardPoint(
    int Step,
    double HazardUp,
    double HazardDown,
    double Survival,
    double CumulativeIncidenceUp,
    double CumulativeIncidenceDown);

/// <summary>
/// Stores a competing-risk forecast summary.
/// </summary>
/// <param name="Horizon">The forecast horizon.</param>
/// <param name="HazardPoints">The hazard curve.</param>
/// <param name="ProbabilityUpByHorizon">The cumulative upside incidence at the horizon.</param>
/// <param name="ProbabilityDownByHorizon">The cumulative downside incidence at the horizon.</param>
/// <param name="ProbabilityNoEventByHorizon">The survival probability at the horizon.</param>
/// <param name="MedianTimeToUp">The median time to upside event, when available.</param>
/// <param name="MedianTimeToDown">The median time to downside event, when available.</param>
/// <param name="ExpectedTimeToFirstEvent">The expected time to the first event.</param>
public sealed record CompetingRiskForecast(
    ForecastHorizon Horizon,
    IReadOnlyList<EventHazardPoint> HazardPoints,
    double ProbabilityUpByHorizon,
    double ProbabilityDownByHorizon,
    double ProbabilityNoEventByHorizon,
    int? MedianTimeToUp,
    int? MedianTimeToDown,
    double ExpectedTimeToFirstEvent);

/// <summary>
/// Stores a three-class event prediction.
/// </summary>
/// <param name="ProbabilityUpFirst">The probability that the upper barrier is first.</param>
/// <param name="ProbabilityDownFirst">The probability that the lower barrier is first.</param>
/// <param name="ProbabilityNoEvent">The probability that neither barrier is reached.</param>
public sealed record MarketEventPrediction(
    double ProbabilityUpFirst,
    double ProbabilityDownFirst,
    double ProbabilityNoEvent)
{
    /// <summary>
    /// Gets the probability vector in up/down/no-event order.
    /// </summary>
    public IReadOnlyList<double> Probabilities => [this.ProbabilityUpFirst, this.ProbabilityDownFirst, this.ProbabilityNoEvent];
}

/// <summary>
/// Stores a regime transition forecast derived from HMM probabilities and transition matrix.
/// </summary>
/// <param name="Horizon">The transition horizon in observations.</param>
/// <param name="StateProbabilities">The state probabilities at the horizon.</param>
/// <param name="ProbabilityEnterHighRisk">Probability of being in a high-risk state at the horizon.</param>
/// <param name="ProbabilityLeaveCurrentState">Probability of leaving the current most-probable state.</param>
public sealed record RegimeTransitionForecast(
    ForecastHorizon Horizon,
    IReadOnlyDictionary<string, double> StateProbabilities,
    double ProbabilityEnterHighRisk,
    double ProbabilityLeaveCurrentState);

/// <summary>
/// Stores one reconstructed historical timing prediction.
/// </summary>
/// <param name="Date">The prediction date.</param>
/// <param name="ProbabilityUp">The predicted upside probability.</param>
/// <param name="ProbabilityDown">The predicted downside probability.</param>
/// <param name="ProbabilityNeutral">The predicted no-event probability.</param>
/// <param name="Zone">The reconstructed timing zone.</param>
/// <param name="Reliability">The reconstructed reliability at the prediction date.</param>
/// <param name="Evidence">The evidence strength at the prediction date.</param>
/// <param name="RealizedOutcome">The realized triple-barrier outcome.</param>
public sealed record HistoricalTimingPrediction(
    DateOnly Date,
    double ProbabilityUp,
    double ProbabilityDown,
    double ProbabilityNeutral,
    MarketTimingZone Zone,
    double Reliability,
    EvidenceStrength Evidence,
    TripleBarrierOutcomeType RealizedOutcome);

/// <summary>
/// Stores a timing alert condition that can be surfaced later.
/// </summary>
/// <param name="Kind">The alert kind.</param>
/// <param name="Active">Whether the condition is active.</param>
/// <param name="Message">A deterministic explanation.</param>
public sealed record TimingAlertCondition(TimingAlertKind Kind, bool Active, string Message);

/// <summary>
/// Stores a timing-zone change between reconstructed assessments.
/// </summary>
/// <param name="PreviousZone">The previous zone.</param>
/// <param name="CurrentZone">The current zone.</param>
/// <param name="ChangedObservationsAgo">The number of observations since the change.</param>
/// <param name="Reasons">Deterministic reasons for the change.</param>
public sealed record TimingAssessmentChange(
    MarketTimingZone PreviousZone,
    MarketTimingZone CurrentZone,
    int ChangedObservationsAgo,
    IReadOnlyList<string> Reasons);

/// <summary>
/// Stores out-of-distribution diagnostics for the current timing state.
/// </summary>
/// <param name="OutOfDistribution">Whether the current state is outside historical support.</param>
/// <param name="RobustDistance">The robust feature-space distance.</param>
/// <param name="Threshold">The OOD threshold.</param>
/// <param name="Level">The interpretable OOD level.</param>
public sealed record OutOfDistributionDiagnostic(
    bool OutOfDistribution,
    double RobustDistance,
    double Threshold,
    OutOfDistributionLevel Level = OutOfDistributionLevel.InDistribution);

/// <summary>
/// Stores one timing model's validation and current prediction.
/// </summary>
/// <param name="Kind">The model kind.</param>
/// <param name="ModelName">The readable model name.</param>
/// <param name="RawCurrentPrediction">The uncalibrated current event prediction.</param>
/// <param name="CurrentPrediction">The current event prediction.</param>
/// <param name="OutOfSamplePredictions">The calibrated OOS predictions aligned to arena evaluations.</param>
/// <param name="CalibrationDiagnostic">Current raw/calibrated probability diagnostic.</param>
/// <param name="Calibration">OOS calibration metrics.</param>
/// <param name="RawCalibration">OOS calibration metrics before probability calibration.</param>
/// <param name="BrierSkillVsBaseline">Brier improvement versus baseline.</param>
/// <param name="BrierSkillInterval">Block-bootstrap interval for Brier improvement.</param>
/// <param name="EligibleForEnsemble">Whether this model can enter the timing ensemble.</param>
/// <param name="EligibilityStatus">The structured eligibility status.</param>
/// <param name="RejectionReason">The reason the model was rejected, when not eligible.</param>
/// <param name="Evidence">The evidence strength.</param>
/// <param name="Diagnostic">Diagnostic text.</param>
public sealed record MarketTimingModelResult(
    MarketTimingModelKind Kind,
    string ModelName,
    MarketEventPrediction RawCurrentPrediction,
    MarketEventPrediction CurrentPrediction,
    IReadOnlyList<MarketEventPrediction> OutOfSamplePredictions,
    ProbabilityCalibrationDiagnostic CalibrationDiagnostic,
    TimingCalibrationSummary Calibration,
    TimingCalibrationSummary RawCalibration,
    double BrierSkillVsBaseline,
    ProbabilityInterval BrierSkillInterval,
    bool EligibleForEnsemble,
    ModelEligibilityStatus EligibilityStatus,
    string RejectionReason,
    EvidenceStrength Evidence,
    string Diagnostic);

/// <summary>
/// Stores one validated timing ensemble component.
/// </summary>
/// <param name="ModelName">The model name.</param>
/// <param name="Weight">The normalized weight.</param>
/// <param name="BrierSkill">The OOS Brier skill.</param>
/// <param name="CalibrationPenalty">The calibration penalty.</param>
public sealed record MarketTimingEnsembleComponent(
    string ModelName,
    double Weight,
    double BrierSkill,
    double CalibrationPenalty);

/// <summary>
/// Stores the market-timing ensemble output.
/// </summary>
/// <param name="Prediction">The combined current event prediction.</param>
/// <param name="Components">The model components.</param>
/// <param name="ModelDisagreement">The probability disagreement across models.</param>
/// <param name="EffectiveModelCount">The effective model count.</param>
/// <param name="Reliability">A normalized reliability score.</param>
/// <param name="Diagnostic">Diagnostic text.</param>
/// <param name="CandidateModelCount">The non-baseline candidate model count.</param>
/// <param name="EligibleModelCount">The number of models that entered the ensemble.</param>
/// <param name="IsActive">Whether the ensemble is active instead of using fallback.</param>
/// <param name="FallbackReason">Why fallback was used, when inactive.</param>
public sealed record MarketTimingEnsembleResult(
    MarketEventPrediction Prediction,
    IReadOnlyList<MarketTimingEnsembleComponent> Components,
    double ModelDisagreement,
    double EffectiveModelCount,
    double Reliability,
    string Diagnostic,
    int CandidateModelCount = 0,
    int EligibleModelCount = 0,
    bool IsActive = false,
    string FallbackReason = "");

/// <summary>
/// Stores the timing arena result for one horizon.
/// </summary>
/// <param name="Definition">The triple-barrier definition.</param>
/// <param name="CurrentBarriers">The effective current barriers.</param>
/// <param name="CurrentFeature">The current causal feature vector.</param>
/// <param name="Models">The model results.</param>
/// <param name="Ensemble">The validated ensemble result.</param>
/// <param name="HazardForecast">The competing-risk hazard forecast.</param>
/// <param name="TerminalReturnQuantiles">True terminal-return quantiles, when available.</param>
/// <param name="ForecastExpectedReturn">Expected terminal return, when available.</param>
/// <param name="HistoricalPredictions">Causal reconstructed historical predictions.</param>
/// <param name="OutOfDistribution">Current state OOD diagnostics.</param>
/// <param name="Warnings">Warnings.</param>
/// <param name="TrainingLabels">Current labels admitted by EndIndex purging.</param>
/// <param name="TrainingLabelEndIndexCutoff">Maximum label EndIndex admitted into the current training set.</param>
public sealed record MarketTimingArenaResult(
    TripleBarrierDefinition Definition,
    EffectiveBarrierDiagnostic CurrentBarriers,
    MarketTimingFeatureVector CurrentFeature,
    IReadOnlyList<MarketTimingModelResult> Models,
    MarketTimingEnsembleResult Ensemble,
    CompetingRiskForecast HazardForecast,
    ForecastReturnQuantiles? TerminalReturnQuantiles,
    double? ForecastExpectedReturn,
    IReadOnlyList<HistoricalTimingPrediction> HistoricalPredictions,
    OutOfDistributionDiagnostic OutOfDistribution,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<TripleBarrierOutcome>? TrainingLabels = null,
    int TrainingLabelEndIndexCutoff = -1);
