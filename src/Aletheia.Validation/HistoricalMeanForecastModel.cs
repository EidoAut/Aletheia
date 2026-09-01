using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Forecasts from trailing historical horizon-return samples.
/// </summary>
/// <remarks>
/// The model estimates the future horizon distribution from realized horizon
/// returns whose entry and exit dates both lie inside the training window.
/// This avoids future leakage and keeps units in cumulative simple-return
/// space, matching the evaluated target.
/// </remarks>
public sealed class HistoricalMeanForecastModel : IForecastModel
{
    /// <summary>
    /// The stable model id.
    /// </summary>
    public const string ModelId = "aletheia.forecast.historical-mean";

    private readonly IReadOnlyDictionary<string, string> configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoricalMeanForecastModel"/> class.
    /// </summary>
    /// <param name="lookbackObservations">The trailing observation lookback used to collect samples.</param>
    /// <param name="minimumSamples">The minimum number of horizon samples required.</param>
    public HistoricalMeanForecastModel(int lookbackObservations = 252, int minimumSamples = 20)
    {
        if (lookbackObservations <= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lookbackObservations),
                lookbackObservations,
                "Lookback must be greater than one observation.");
        }

        if (minimumSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSamples), minimumSamples, "Minimum samples must be positive.");
        }

        this.LookbackObservations = lookbackObservations;
        this.MinimumSamples = minimumSamples;
        this.configuration = new Dictionary<string, string>
        {
            ["LookbackObservations"] = ModelConfigurationFingerprint.Format(lookbackObservations),
            ["MinimumSamples"] = ModelConfigurationFingerprint.Format(minimumSamples),
            ["ReturnUnit"] = "CumulativeSimpleReturn",
        };
    }

    /// <summary>
    /// Gets the trailing lookback.
    /// </summary>
    public int LookbackObservations { get; }

    /// <summary>
    /// Gets the minimum horizon-sample count.
    /// </summary>
    public int MinimumSamples { get; }

    /// <inheritdoc />
    public ModelDescriptor Descriptor { get; } = new(
        ModelId,
        "Historical Mean",
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

        var samples = CollectHorizonReturns(
            context.TrainingSeries,
            context.HorizonResolution.RequestedHorizon,
            this.LookbackObservations);
        if (samples.Count < this.MinimumSamples)
        {
            return ModelTrainingResult.Failure(
                ForecastStatus.InsufficientData,
                "Historical mean baseline does not have enough completed trailing horizon samples.",
                new Dictionary<string, string> { ["Samples"] = ModelConfigurationFingerprint.Format(samples.Count) });
        }

        return ModelTrainingResult.Success(samples, new Dictionary<string, string>
        {
            ["Samples"] = ModelConfigurationFingerprint.Format(samples.Count),
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

        if (trainingResult.FittedState is not IReadOnlyList<double> samples)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InvalidData, "Historical mean fitted state was not available.");
        }

        return ForecastPredictionResult.Success(
            ForecastDistribution.FromSamples(context.HorizonResolution, samples),
            trainingResult.Diagnostics);
    }

    private static List<double> CollectHorizonReturns(
        NavSeries navSeries,
        ForecastHorizon horizon,
        int lookbackObservations)
    {
        var samples = new List<double>();
        var firstStartIndex = Math.Max(0, navSeries.Count - lookbackObservations - 1);
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
}
