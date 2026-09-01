using Aletheia.Core;
using Aletheia.Dynamics;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Converts historical analogue state matching into a leakage-controlled forecast model.
/// </summary>
public sealed class HistoricalAnalogueForecastModel : IForecastModel
{
    /// <summary>
    /// The stable model id.
    /// </summary>
    public const string ModelId = "aletheia.forecast.historical-analogues";

    private readonly IStateFeaturePipeline statePipeline;
    private readonly HistoricalAnalogueFeatureBuilder featureBuilder;
    private readonly HistoricalAnalogueFinder finder;
    private readonly IReadOnlyDictionary<string, string> configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoricalAnalogueForecastModel"/> class.
    /// </summary>
    /// <param name="maximumAnalogues">The maximum nearest neighbours used for the forecast distribution.</param>
    /// <param name="minimumAnalogues">The minimum usable analogue outcomes required.</param>
    /// <param name="stateLookback">The state-builder lookback.</param>
    /// <param name="exclusionWindowObservations">The embargo around the query cutoff.</param>
    /// <param name="candidateLookbackObservations">The trailing training-history limit used for candidate search.</param>
    /// <param name="statePipeline">The state feature pipeline.</param>
    public HistoricalAnalogueForecastModel(
        int maximumAnalogues = 50,
        int minimumAnalogues = 10,
        int stateLookback = 30,
        int exclusionWindowObservations = 20,
        int? candidateLookbackObservations = 750,
        IStateFeaturePipeline? statePipeline = null)
    {
        if (maximumAnalogues <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAnalogues), maximumAnalogues, "Maximum analogues must be positive.");
        }

        if (minimumAnalogues <= 0 || minimumAnalogues > maximumAnalogues)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumAnalogues),
                minimumAnalogues,
                "Minimum analogues must be positive and no larger than maximum analogues.");
        }

        if (stateLookback <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(stateLookback), stateLookback, "State lookback must be greater than one.");
        }

        if (exclusionWindowObservations < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exclusionWindowObservations),
                exclusionWindowObservations,
                "Exclusion window cannot be negative.");
        }

        if (candidateLookbackObservations.HasValue && candidateLookbackObservations.Value <= stateLookback)
        {
            throw new ArgumentOutOfRangeException(
                nameof(candidateLookbackObservations),
                candidateLookbackObservations,
                "Candidate lookback must be larger than the state lookback.");
        }

        this.MaximumAnalogues = maximumAnalogues;
        this.MinimumAnalogues = minimumAnalogues;
        this.StateLookback = stateLookback;
        this.ExclusionWindowObservations = exclusionWindowObservations;
        this.CandidateLookbackObservations = candidateLookbackObservations;
        this.statePipeline = statePipeline ?? new DynamicStateFeaturePipeline();
        this.featureBuilder = new HistoricalAnalogueFeatureBuilder(this.statePipeline);
        this.finder = new HistoricalAnalogueFinder();
        this.configuration = new Dictionary<string, string>
        {
            ["MaximumAnalogues"] = ModelConfigurationFingerprint.Format(maximumAnalogues),
            ["MinimumAnalogues"] = ModelConfigurationFingerprint.Format(minimumAnalogues),
            ["StateLookbackObservations"] = ModelConfigurationFingerprint.Format(stateLookback),
            ["ExclusionWindowObservations"] = ModelConfigurationFingerprint.Format(exclusionWindowObservations),
            ["CandidateLookbackObservations"] = candidateLookbackObservations.HasValue
                ? ModelConfigurationFingerprint.Format(candidateLookbackObservations.Value)
                : "AllTrainingHistory",
            ["Metric"] = "SchemaStandardizedEuclidean",
        };
    }

    /// <summary>
    /// Gets the maximum nearest neighbours used for the distribution.
    /// </summary>
    public int MaximumAnalogues { get; }

    /// <summary>
    /// Gets the minimum analogue outcomes required.
    /// </summary>
    public int MinimumAnalogues { get; }

    /// <summary>
    /// Gets the state-builder lookback.
    /// </summary>
    public int StateLookback { get; }

    /// <summary>
    /// Gets the exclusion window around the query cutoff.
    /// </summary>
    public int ExclusionWindowObservations { get; }

    /// <summary>
    /// Gets the trailing training-history limit used for candidate search.
    /// </summary>
    public int? CandidateLookbackObservations { get; }

    /// <inheritdoc />
    public ModelDescriptor Descriptor { get; } = new(
        ModelId,
        "Historical Analogues",
        "2.1.0");

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Configuration => this.configuration;

    /// <inheritdoc />
    public ForecastCapabilities Capabilities =>
        ForecastCapabilities.PointForecast |
        ForecastCapabilities.ExpectedReturn |
        ForecastCapabilities.Median |
        ForecastCapabilities.ProbabilityPositive |
        ForecastCapabilities.Quantiles;

    /// <inheritdoc />
    public PointForecastStatistic PointForecastStatistic => PointForecastStatistic.Mean;

    /// <inheritdoc />
    public string ConfigurationFingerprint => ModelConfigurationFingerprint.Calculate(this.Descriptor, this.Configuration);

    /// <inheritdoc />
    public ModelTrainingResult Train(ForecastTrainingContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        var horizonSteps = context.HorizonResolution.EffectiveObservationCount;
        if (horizonSteps <= 0)
        {
            return ModelTrainingResult.Failure(ForecastStatus.InvalidData, "Historical analogue forecast requires a positive effective observation horizon.");
        }

        var trainingSeries = this.ResolveCandidateSeries(context.TrainingSeries);
        if (trainingSeries.Count <= this.StateLookback + horizonSteps + this.ExclusionWindowObservations)
        {
            return ModelTrainingResult.Failure(ForecastStatus.InsufficientData, "Not enough training observations for analogue states, exclusion, and realized analogue outcomes.");
        }

        var queryState = this.statePipeline.Build(trainingSeries, trainingSeries.Count - 1);
        var stateHistory = this.featureBuilder.Build(trainingSeries, this.StateLookback);
        var indexByDate = CreateIndexByDate(trainingSeries);
        var latestAllowedCandidateIndex = trainingSeries.Count - 1 - this.ExclusionWindowObservations;
        var eligibleStates = stateHistory
            .Where(observation =>
                indexByDate.TryGetValue(observation.Date, out var index) &&
                index < latestAllowedCandidateIndex &&
                index + horizonSteps < trainingSeries.Count)
            .ToArray();

        if (eligibleStates.Length == 0)
        {
            return ModelTrainingResult.Failure(ForecastStatus.InsufficientData, "No analogue candidates survive temporal exclusion and target-history checks.");
        }

        HistoricalAnalogueSearchResult search;
        try
        {
            search = this.finder.FindNearestWithDiagnostics(eligibleStates, queryState, this.MaximumAnalogues);
        }
        catch (IncompatibleDynamicStateException exception)
        {
            return ModelTrainingResult.Failure(ForecastStatus.IncompatibleState, exception.Message);
        }

        var samples = CollectAnalogueReturns(trainingSeries, search.Matches, indexByDate, context.HorizonResolution.RequestedHorizon);
        if (samples.Count < this.MinimumAnalogues)
        {
            return ModelTrainingResult.Failure(
                ForecastStatus.InsufficientData,
                "Not enough historical analogue outcomes were available after temporal exclusion.",
                CreateDiagnostics(search, samples.Count));
        }

        var distribution = ForecastDistribution.FromSamples(context.HorizonResolution, samples);
        return ModelTrainingResult.Success(new AnalogueFittedState(distribution), CreateDiagnostics(search, samples.Count));
    }

    /// <inheritdoc />
    public ForecastPredictionResult Predict(
        ModelTrainingResult trainingResult,
        ForecastPredictionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(trainingResult);
        ArgumentNullException.ThrowIfNull(context);

        if (!trainingResult.IsSuccess)
        {
            return ForecastPredictionResult.Failure(trainingResult.Status, trainingResult.FailureReason ?? "Training failed.");
        }

        if (trainingResult.FittedState is not AnalogueFittedState fitted)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InvalidData, "Historical analogue fitted distribution was not available.");
        }

        return ForecastPredictionResult.Success(fitted.Distribution, trainingResult.Diagnostics);
    }

    private static IReadOnlyDictionary<DateOnly, int> CreateIndexByDate(NavSeries navSeries)
    {
        var indexes = new Dictionary<DateOnly, int>();
        for (var index = 0; index < navSeries.Count; index++)
        {
            indexes[navSeries[index].Date] = index;
        }

        return indexes;
    }

    private static IReadOnlyDictionary<string, string> CreateDiagnostics(
        HistoricalAnalogueSearchResult search,
        int outcomeSamples)
    {
        return new Dictionary<string, string>
        {
            ["CandidateStates"] = ModelConfigurationFingerprint.Format(search.CandidateCount),
            ["SchemaCompatibleStates"] = ModelConfigurationFingerprint.Format(search.SchemaCompatibleCount),
            ["RejectedSchemaIncompatible"] = ModelConfigurationFingerprint.Format(search.RejectedSchemaIncompatibleCount),
            ["RejectedMissingDimensions"] = ModelConfigurationFingerprint.Format(search.RejectedMissingDimensionCount),
            ["AnalogueMatches"] = ModelConfigurationFingerprint.Format(search.Matches.Count),
            ["OutcomeSamples"] = ModelConfigurationFingerprint.Format(outcomeSamples),
        };
    }

    private static List<double> CollectAnalogueReturns(
        NavSeries navSeries,
        IReadOnlyList<HistoricalAnalogueResult> matches,
        IReadOnlyDictionary<DateOnly, int> indexByDate,
        ForecastHorizon horizon)
    {
        var samples = new List<double>();
        foreach (var match in matches)
        {
            if (!indexByDate.TryGetValue(match.Observation.Date, out var startIndex))
            {
                continue;
            }

            var endIndex = ResolveEndIndex(navSeries, startIndex, horizon);
            if (endIndex < 0 || endIndex <= startIndex)
            {
                continue;
            }

            var start = navSeries[startIndex].Value;
            var end = navSeries[endIndex].Value;
            if (start <= 0m || end <= 0m)
            {
                continue;
            }

            samples.Add(((double)end / (double)start) - 1d);
        }

        return samples;
    }

    private static int ResolveEndIndex(NavSeries navSeries, int startIndex, ForecastHorizon horizon)
    {
        if (horizon.Unit == ForecastHorizonUnit.Observations)
        {
            var endIndex = startIndex + horizon.Value;
            return endIndex < navSeries.Count ? endIndex : -1;
        }

        var targetDate = navSeries[startIndex].Date.AddDays(horizon.Value);
        for (var index = startIndex + 1; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= targetDate)
            {
                return index;
            }
        }

        return -1;
    }

    private NavSeries ResolveCandidateSeries(NavSeries trainingSeries)
    {
        if (!this.CandidateLookbackObservations.HasValue ||
            trainingSeries.Count <= this.CandidateLookbackObservations.Value)
        {
            return trainingSeries;
        }

        return new NavSeries(
            trainingSeries.Points.Skip(trainingSeries.Count - this.CandidateLookbackObservations.Value),
            trainingSeries.ObservationFrequency);
    }

    private sealed record AnalogueFittedState(ForecastDistribution Distribution);
}
