using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Forecasts P(return &gt; 0) from historical completed horizon outcomes.
/// </summary>
/// <remarks>
/// "Climatology" here means the empirical base rate of positive returns for
/// the same forecast horizon inside the training window only. It is a genuine
/// probability baseline for Brier score, but it does not claim point forecasts
/// or quantiles.
/// </remarks>
public sealed class HistoricalProbabilityBaselineForecastModel : IForecastModel
{
    /// <summary>
    /// The stable model id.
    /// </summary>
    public const string ModelId = "aletheia.forecast.historical-probability-climatology";

    private readonly IReadOnlyDictionary<string, string> configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoricalProbabilityBaselineForecastModel"/> class.
    /// </summary>
    /// <param name="lookbackObservations">The trailing observation lookback used to collect horizon outcomes.</param>
    /// <param name="minimumSamples">The minimum number of completed horizon outcomes required.</param>
    public HistoricalProbabilityBaselineForecastModel(int? lookbackObservations = null, int minimumSamples = 20)
    {
        if (lookbackObservations.HasValue && lookbackObservations.Value <= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lookbackObservations),
                lookbackObservations,
                "Lookback must be greater than one observation when supplied.");
        }

        if (minimumSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSamples), minimumSamples, "Minimum samples must be positive.");
        }

        this.LookbackObservations = lookbackObservations;
        this.MinimumSamples = minimumSamples;
        this.configuration = new Dictionary<string, string>
        {
            ["LookbackObservations"] = lookbackObservations.HasValue
                ? ModelConfigurationFingerprint.Format(lookbackObservations.Value)
                : "AllTrainingHistory",
            ["MinimumSamples"] = ModelConfigurationFingerprint.Format(minimumSamples),
            ["Probability"] = "EmpiricalPositiveCompletedHorizonReturn",
            ["ReturnUnit"] = "CumulativeSimpleReturn",
        };
    }

    /// <summary>
    /// Gets the optional trailing observation lookback.
    /// </summary>
    public int? LookbackObservations { get; }

    /// <summary>
    /// Gets the minimum number of completed horizon outcomes required.
    /// </summary>
    public int MinimumSamples { get; }

    /// <inheritdoc />
    public ModelDescriptor Descriptor { get; } = new(
        ModelId,
        "Historical Probability Climatology",
        "2.1.0");

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Configuration => this.configuration;

    /// <inheritdoc />
    public ForecastCapabilities Capabilities => ForecastCapabilities.ProbabilityPositive;

    /// <inheritdoc />
    public PointForecastStatistic PointForecastStatistic => PointForecastStatistic.None;

    /// <inheritdoc />
    public string ConfigurationFingerprint => ModelConfigurationFingerprint.Calculate(this.Descriptor, this.Configuration);

    /// <inheritdoc />
    public ModelTrainingResult Train(ForecastTrainingContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        var samples = CollectHorizonReturns(
            context.TrainingSeries,
            context.HorizonResolution.RequestedHorizon,
            this.LookbackObservations);
        if (samples.Count < this.MinimumSamples)
        {
            return ModelTrainingResult.Failure(
                ForecastStatus.InsufficientData,
                "Historical probability climatology does not have enough completed training horizon outcomes.",
                new Dictionary<string, string> { ["Samples"] = ModelConfigurationFingerprint.Format(samples.Count) });
        }

        var probabilityPositive = samples.Count(value => value > 0d) / (double)samples.Count;
        return ModelTrainingResult.Success(
            new ProbabilityFittedState(probabilityPositive, samples.Count),
            new Dictionary<string, string>
            {
                ["Samples"] = ModelConfigurationFingerprint.Format(samples.Count),
                ["PositiveSamples"] = ModelConfigurationFingerprint.Format(samples.Count(value => value > 0d)),
            });
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

        if (trainingResult.FittedState is not ProbabilityFittedState state)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InvalidData, "Historical probability fitted state was not available.");
        }

        var distribution = new ForecastDistribution(
            context.HorizonResolution,
            0d,
            0d,
            new Dictionary<int, double>(),
            state.ProbabilityPositive,
            0d,
            0d,
            this.Capabilities,
            this.PointForecastStatistic,
            0d);

        return ForecastPredictionResult.Success(
            distribution,
            trainingResult.Diagnostics);
    }

    private static List<double> CollectHorizonReturns(
        NavSeries navSeries,
        ForecastHorizon horizon,
        int? lookbackObservations)
    {
        var samples = new List<double>();
        var firstStartIndex = lookbackObservations.HasValue
            ? Math.Max(0, navSeries.Count - lookbackObservations.Value - 1)
            : 0;
        for (var startIndex = firstStartIndex; startIndex < navSeries.Count - 1; startIndex++)
        {
            var endIndex = ResolveEndIndex(navSeries, startIndex, horizon);
            if (endIndex < 0 || endIndex >= navSeries.Count)
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

    private sealed record ProbabilityFittedState(double ProbabilityPositive, int SampleCount);
}
