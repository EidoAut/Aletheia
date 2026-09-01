namespace Aletheia.Validation;

/// <summary>
/// Describes the type of market-timing event being estimated.
/// </summary>
public enum MarketEventType
{
    /// <summary>An upside return barrier is reached.</summary>
    UpsideMove,

    /// <summary>A downside return barrier is reached.</summary>
    DownsideMove,

    /// <summary>A drawdown barrier is reached.</summary>
    Drawdown,

    /// <summary>A recovery barrier is reached.</summary>
    Recovery,

    /// <summary>A positive breakout above a trailing high is reached.</summary>
    PositiveBreakout,

    /// <summary>A negative breakout below a trailing low is reached.</summary>
    NegativeBreakout,

    /// <summary>A trend reversal from negative to positive is reached.</summary>
    TrendReversalUp,

    /// <summary>A trend reversal from positive to negative is reached.</summary>
    TrendReversalDown,
}

/// <summary>
/// Describes the event direction used by timing labels.
/// </summary>
public enum MarketEventDirection
{
    /// <summary>Positive/upside event.</summary>
    Up,

    /// <summary>Negative/downside event.</summary>
    Down,

    /// <summary>Neutral or no event.</summary>
    Neutral,
}

/// <summary>
/// Describes how event barriers are converted to return thresholds.
/// </summary>
public enum BarrierThresholdPolicy
{
    /// <summary>Use fixed simple-return percentages.</summary>
    FixedPercentage,

    /// <summary>Scale barriers by current causal volatility.</summary>
    VolatilityScaled,
}

/// <summary>
/// Describes the triple-barrier label outcome.
/// </summary>
public enum TripleBarrierOutcomeType
{
    /// <summary>The upper barrier was hit before the lower barrier.</summary>
    UpperHitFirst,

    /// <summary>The lower barrier was hit before the upper barrier.</summary>
    LowerHitFirst,

    /// <summary>No horizontal barrier was reached before the vertical barrier.</summary>
    NoBarrierHit,
}

/// <summary>
/// Identifies a timing model family.
/// </summary>
public enum MarketTimingModelKind
{
    /// <summary>Historical event prevalence baseline.</summary>
    HistoricalEventRateBaseline,

    /// <summary>Regime-transition adjusted event model.</summary>
    RegimeTransitionTimingModel,

    /// <summary>Historical-analogue event model.</summary>
    HistoricalAnalogueTimingModel,

    /// <summary>Regularized multinomial event classifier.</summary>
    RegularizedEventClassifier,

    /// <summary>Discrete-time competing-risk hazard model.</summary>
    CompetingRiskHazardModel,

    /// <summary>Spectral timing candidate model.</summary>
    SpectralTimingModel,
}

/// <summary>
/// Classifies evidence strength after validation.
/// </summary>
public enum EvidenceStrength
{
    /// <summary>Not enough out-of-sample evidence.</summary>
    Insufficient,

    /// <summary>Weak evidence.</summary>
    Weak,

    /// <summary>Moderate evidence.</summary>
    Moderate,

    /// <summary>Strong evidence.</summary>
    Strong,
}

/// <summary>
/// Describes whether a probability calibration model could be used.
/// </summary>
public enum ProbabilityCalibrationStatus
{
    /// <summary>No calibration was attempted because there were not enough prior OOS samples.</summary>
    InsufficientData,

    /// <summary>Raw probabilities were used because calibration fitting failed numerically.</summary>
    Failed,

    /// <summary>Raw probabilities were retained deliberately.</summary>
    Raw,

    /// <summary>Probabilities were calibrated with a fitted model trained on prior OOS samples.</summary>
    Calibrated,
}

/// <summary>
/// Describes the numerical status of a multinomial event-classifier fit.
/// </summary>
public enum MarketEventClassifierFitStatus
{
    /// <summary>The optimizer met the configured loss or gradient tolerance.</summary>
    Converged,

    /// <summary>The optimizer stopped at the configured iteration cap before convergence.</summary>
    MaxIterationsReached,

    /// <summary>The optimization produced non-finite values.</summary>
    NumericalFailure,

    /// <summary>The training set did not contain enough aligned or class-balanced data.</summary>
    InsufficientData,
}

/// <summary>
/// Describes whether a model may enter a horizon-specific timing ensemble.
/// </summary>
public enum ModelEligibilityStatus
{
    /// <summary>The model passed horizon-specific OOS gates.</summary>
    Eligible,

    /// <summary>The model is a baseline and is reported but not ensemble-weighted.</summary>
    BaselineOnly,

    /// <summary>There were too few OOS samples for a reliable eligibility decision.</summary>
    InsufficientEvidence,

    /// <summary>The model did not beat the baseline enough to be useful.</summary>
    NoPositiveSkill,

    /// <summary>The model's calibration quality was too weak.</summary>
    CalibrationRejected,

    /// <summary>The model's bootstrap skill interval was too weak.</summary>
    UnstableSkill,

    /// <summary>The model was rejected by an explicit model-family gate.</summary>
    ExplicitlyRejected,
}

/// <summary>
/// Interprets robust feature-space distance for out-of-distribution diagnostics.
/// </summary>
public enum OutOfDistributionLevel
{
    /// <summary>The current point is within historical feature support.</summary>
    InDistribution,

    /// <summary>The current point is unusual but not outside support.</summary>
    SlightlyUnusual,

    /// <summary>The current point is outside historical support.</summary>
    OutOfDistribution,
}

/// <summary>
/// Describes the current timing zone.
/// </summary>
public enum MarketTimingZone
{
    /// <summary>There is not enough evidence to classify a timing zone.</summary>
    InsufficientEvidence,

    /// <summary>Strong accumulation area.</summary>
    StrongAccumulation,

    /// <summary>Accumulation area.</summary>
    Accumulation,

    /// <summary>Positive watch area.</summary>
    WatchPositive,

    /// <summary>No clear timing edge.</summary>
    Neutral,

    /// <summary>Negative watch area.</summary>
    WatchNegative,

    /// <summary>Reduction area.</summary>
    Reduction,

    /// <summary>Strong reduction area.</summary>
    StrongReduction,
}

/// <summary>
/// Describes the research timing decision action.
/// </summary>
public enum TimingDecisionAction
{
    /// <summary>Validated evidence is insufficient for a timing decision.</summary>
    InsufficientEvidence,

    /// <summary>Research output favors a strong buy / accumulation action.</summary>
    StrongBuy,

    /// <summary>Research output favors a buy / accumulation action.</summary>
    Buy,

    /// <summary>Research output favors holding or waiting.</summary>
    Hold,

    /// <summary>Research output favors reducing exposure.</summary>
    Reduce,

    /// <summary>Research output favors exiting or strong reduction.</summary>
    Sell,

    /// <summary>Research output favors strong accumulation.</summary>
    StrongAccumulate,

    /// <summary>Research output favors accumulation.</summary>
    Accumulate,

    /// <summary>Research output favors waiting with a positive bias.</summary>
    WatchPositive,

    /// <summary>Research output is neutral.</summary>
    Neutral,

    /// <summary>Research output favors caution.</summary>
    WatchNegative,

    /// <summary>Research output favors strong reduction.</summary>
    StrongReduce,
}

/// <summary>
/// Describes timing alert conditions that may become external notifications later.
/// </summary>
public enum TimingAlertKind
{
    /// <summary>Upside probability is rising.</summary>
    UpsideProbabilityRising,

    /// <summary>Downside probability is rising.</summary>
    DownsideProbabilityRising,

    /// <summary>Accumulation zone was entered.</summary>
    AccumulationZoneEntered,

    /// <summary>Reduction zone was entered.</summary>
    ReductionZoneEntered,

    /// <summary>High-risk regime was entered.</summary>
    HighRiskRegimeEntered,

    /// <summary>Timing confidence improved.</summary>
    TimingConfidenceImproved,

    /// <summary>Structural change was detected.</summary>
    StructuralChangeDetected,
}
