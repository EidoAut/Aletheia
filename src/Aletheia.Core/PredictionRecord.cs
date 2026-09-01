namespace Aletheia.Core;

/// <summary>
/// Stores one immutable forecast issued by Aletheia for later evaluation.
/// </summary>
/// <remarks>
/// The prediction ledger is a scientific accountability mechanism. Failed
/// predictions must remain visible rather than being overwritten by newer,
/// better-looking forecasts.
/// </remarks>
public sealed class PredictionRecord
{
    private readonly IReadOnlyDictionary<int, double> returnPercentiles;
    private readonly IReadOnlyDictionary<string, string> modelParameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionRecord"/> class.
    /// </summary>
    /// <param name="predictionId">The stable prediction identifier.</param>
    /// <param name="fundIdentifier">The fund identifier.</param>
    /// <param name="generatedAtUtc">The UTC timestamp at which the prediction was generated.</param>
    /// <param name="dataCutoffDate">The last observation date available to the prediction.</param>
    /// <param name="horizonResolution">The resolved forecast horizon.</param>
    /// <param name="pointForecastReturn">The transformed point forecast return, when applicable.</param>
    /// <param name="expectedReturn">The expected return over the horizon.</param>
    /// <param name="medianReturn">The median return over the horizon.</param>
    /// <param name="probabilityPositive">The probability assigned to a positive return.</param>
    /// <param name="returnPercentiles">The forecast return percentiles.</param>
    /// <param name="model">The structured model identity.</param>
    /// <param name="modelParameters">Stable model parameter or configuration values.</param>
    /// <param name="aletheiaVersion">The Aletheia version identifier.</param>
    /// <param name="stateSchemaVersion">The state schema version used by the prediction.</param>
    /// <param name="stateSchemaFingerprint">The state schema fingerprint used by the prediction.</param>
    /// <param name="datasetIdentity">The dataset identity.</param>
    /// <param name="randomSeed">The random seed when stochastic simulation was used.</param>
    /// <param name="signal">The signal associated with the prediction, if any.</param>
    /// <param name="signalStrength">The signal strength, if any.</param>
    /// <param name="featureConfigurationId">The feature configuration identity.</param>
    /// <param name="forecastCapabilities">The forecast quantities explicitly supported by this prediction.</param>
    /// <param name="pointForecastStatistic">The statistic represented by <paramref name="pointForecastReturn"/>.</param>
    public PredictionRecord(
        Guid predictionId,
        FundIdentifier fundIdentifier,
        DateTimeOffset generatedAtUtc,
        DateOnly dataCutoffDate,
        ForecastHorizonResolution horizonResolution,
        double pointForecastReturn,
        double expectedReturn,
        double medianReturn,
        double probabilityPositive,
        IReadOnlyDictionary<int, double> returnPercentiles,
        ModelDescriptor model,
        IReadOnlyDictionary<string, string> modelParameters,
        string aletheiaVersion,
        string stateSchemaVersion,
        string stateSchemaFingerprint,
        DatasetIdentity datasetIdentity,
        int? randomSeed,
        InvestmentSignal? signal,
        double? signalStrength,
        string featureConfigurationId,
        ForecastCapabilities forecastCapabilities = ForecastCapabilities.PointForecast |
            ForecastCapabilities.ExpectedReturn |
            ForecastCapabilities.Median |
            ForecastCapabilities.ProbabilityPositive |
            ForecastCapabilities.Quantiles,
        PointForecastStatistic pointForecastStatistic = PointForecastStatistic.Median)
    {
        ArgumentNullException.ThrowIfNull(returnPercentiles);
        ArgumentNullException.ThrowIfNull(modelParameters);

        this.PredictionId = predictionId;
        this.FundIdentifier = fundIdentifier;
        this.GeneratedAtUtc = generatedAtUtc;
        this.DataCutoffDate = dataCutoffDate;
        this.HorizonResolution = horizonResolution ?? throw new ArgumentNullException(nameof(horizonResolution));
        this.PointForecastReturn = pointForecastReturn;
        this.ExpectedReturn = expectedReturn;
        this.MedianReturn = medianReturn;
        this.ProbabilityPositive = probabilityPositive;
        this.returnPercentiles = new Dictionary<int, double>(returnPercentiles);
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        this.modelParameters = new Dictionary<string, string>(modelParameters);
        this.AletheiaVersion = aletheiaVersion;
        this.StateSchemaVersion = stateSchemaVersion;
        this.StateSchemaFingerprint = stateSchemaFingerprint;
        this.DatasetIdentity = datasetIdentity ?? throw new ArgumentNullException(nameof(datasetIdentity));
        this.RandomSeed = randomSeed;
        this.Signal = signal;
        this.SignalStrength = signalStrength;
        this.FeatureConfigurationId = featureConfigurationId;
        this.ForecastCapabilities = forecastCapabilities;
        this.PointForecastStatistic = pointForecastStatistic;
    }

    /// <summary>
    /// Gets the stable prediction identifier.
    /// </summary>
    public Guid PredictionId { get; }

    /// <summary>
    /// Gets the fund identifier.
    /// </summary>
    public FundIdentifier FundIdentifier { get; }

    /// <summary>
    /// Gets the UTC timestamp at which the prediction was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>
    /// Gets the last observation date available to the prediction.
    /// </summary>
    public DateOnly DataCutoffDate { get; }

    /// <summary>
    /// Gets the resolved forecast horizon.
    /// </summary>
    public ForecastHorizonResolution HorizonResolution { get; }

    /// <summary>
    /// Gets the requested prediction horizon.
    /// </summary>
    public ForecastHorizon RequestedHorizon => this.HorizonResolution.RequestedHorizon;

    /// <summary>
    /// Gets the number of observation steps used internally.
    /// </summary>
    public int EffectiveObservationCount => this.HorizonResolution.EffectiveObservationCount;

    /// <summary>
    /// Gets the target date, when known.
    /// </summary>
    public DateOnly? TargetDate => this.HorizonResolution.TargetDate;

    /// <summary>
    /// Gets the observation frequency used to resolve the prediction horizon.
    /// </summary>
    public ObservationFrequency ObservationFrequency => this.HorizonResolution.ObservationFrequency;

    /// <summary>
    /// Gets the transformed point forecast return, when applicable.
    /// </summary>
    public double PointForecastReturn { get; }

    /// <summary>
    /// Gets the explicit forecast quantities supported by this prediction.
    /// </summary>
    public ForecastCapabilities ForecastCapabilities { get; }

    /// <summary>
    /// Gets the statistic represented by <see cref="PointForecastReturn"/>.
    /// </summary>
    public PointForecastStatistic PointForecastStatistic { get; }

    /// <summary>
    /// Gets the expected return over the horizon.
    /// </summary>
    public double ExpectedReturn { get; }

    /// <summary>
    /// Gets the median return over the horizon.
    /// </summary>
    public double MedianReturn { get; }

    /// <summary>
    /// Gets the probability assigned to a positive return.
    /// </summary>
    public double ProbabilityPositive { get; }

    /// <summary>
    /// Gets the forecast return percentiles.
    /// </summary>
    public IReadOnlyDictionary<int, double> ReturnPercentiles => this.returnPercentiles;

    /// <summary>
    /// Gets the structured model identity.
    /// </summary>
    public ModelDescriptor Model { get; }

    /// <summary>
    /// Gets stable model parameter or configuration values.
    /// </summary>
    public IReadOnlyDictionary<string, string> ModelParameters => this.modelParameters;

    /// <summary>
    /// Gets the Aletheia version identifier.
    /// </summary>
    public string AletheiaVersion { get; }

    /// <summary>
    /// Gets the state schema version used by the prediction.
    /// </summary>
    public string StateSchemaVersion { get; }

    /// <summary>
    /// Gets the state schema fingerprint used by the prediction.
    /// </summary>
    public string StateSchemaFingerprint { get; }

    /// <summary>
    /// Gets the dataset identity.
    /// </summary>
    public DatasetIdentity DatasetIdentity { get; }

    /// <summary>
    /// Gets the random seed when stochastic simulation was used.
    /// </summary>
    public int? RandomSeed { get; }

    /// <summary>
    /// Gets the signal associated with the prediction, if any.
    /// </summary>
    public InvestmentSignal? Signal { get; }

    /// <summary>
    /// Gets the signal strength, if any.
    /// </summary>
    public double? SignalStrength { get; }

    /// <summary>
    /// Gets the feature configuration identity.
    /// </summary>
    public string FeatureConfigurationId { get; }

    /// <summary>
    /// Determines whether the prediction supports a required capability.
    /// </summary>
    /// <param name="capability">The required capability.</param>
    /// <returns><see langword="true"/> when the capability is supported.</returns>
    public bool Supports(ForecastCapabilities capability) =>
        (this.ForecastCapabilities & capability) == capability;
}
