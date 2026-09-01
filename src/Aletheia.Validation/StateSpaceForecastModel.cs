using System.Globalization;
using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.Dynamics;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Forecasts cumulative returns with a local linear trend Kalman model fitted to log NAV.
/// </summary>
public sealed class StateSpaceForecastModel : IForecastModel
{
    /// <summary>
    /// The stable model id.
    /// </summary>
    public const string ModelId = "aletheia.forecast.state-space-local-linear";

    private const double MinimumPlausibleSimpleReturn = -0.95d;
    private const double MaximumPlausibleSimpleReturn = 4d;
    private const double MaximumCumulativeVariance = 4d;
    private const double MinimumVarianceScale = 1e-12d;

    private readonly LocalLinearTrendKalmanModel kalmanModel;
    private readonly IReadOnlyDictionary<string, string> configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateSpaceForecastModel"/> class.
    /// </summary>
    /// <param name="minimumLogReturns">The minimum log-return observations required.</param>
    /// <param name="returnCalculator">The return calculator.</param>
    /// <param name="kalmanModel">The state-space model.</param>
    public StateSpaceForecastModel(
        int minimumLogReturns = 30,
        ReturnCalculator? returnCalculator = null,
        LocalLinearTrendKalmanModel? kalmanModel = null)
    {
        if (minimumLogReturns < 5)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLogReturns), minimumLogReturns, "Minimum log returns must be at least 5.");
        }

        this.MinimumLogReturns = minimumLogReturns;
        this.kalmanModel = kalmanModel ?? new LocalLinearTrendKalmanModel();
        this.configuration = new Dictionary<string, string>
        {
            ["MinimumLogReturns"] = ModelConfigurationFingerprint.Format(minimumLogReturns),
            ["ObservationEquation"] = "LogNav = Level + noise",
            ["StateEquation"] = "LevelTrendLocalLinear",
            ["FitPolicy"] = "RefitEveryCutoff",
        };
    }

    /// <summary>
    /// Gets the minimum required log-return count.
    /// </summary>
    public int MinimumLogReturns { get; }

    /// <inheritdoc />
    public ModelDescriptor Descriptor { get; } = new(
        ModelId,
        "State Space Local Linear",
        "1.0.0");

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Configuration => this.configuration;

    /// <inheritdoc />
    public ForecastCapabilities Capabilities =>
        ForecastCapabilities.PointForecast |
        ForecastCapabilities.ExpectedReturn |
        ForecastCapabilities.Median |
        ForecastCapabilities.ProbabilityPositive |
        ForecastCapabilities.Quantiles |
        ForecastCapabilities.FullDistribution;

    /// <inheritdoc />
    public PointForecastStatistic PointForecastStatistic => PointForecastStatistic.Mean;

    /// <inheritdoc />
    public string ConfigurationFingerprint => ModelConfigurationFingerprint.Calculate(this.Descriptor, this.Configuration);

    /// <inheritdoc />
    public ModelTrainingResult Train(ForecastTrainingContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        var logNav = ToLogNav(context.TrainingSeries);
        if (logNav.Length < this.MinimumLogReturns)
        {
            return ModelTrainingResult.Failure(
                ForecastStatus.InsufficientData,
                "State-space model does not have enough log-NAV observations.",
                new Dictionary<string, string> { ["LogNavObservations"] = ModelConfigurationFingerprint.Format(logNav.Length) });
        }

        try
        {
            var logReturnVariance = EstimateLogReturnVariance(logNav);
            var filter = this.kalmanModel.Filter(
                logNav,
                logReturnVariance * 0.25d,
                logReturnVariance * 0.05d,
                logReturnVariance * 0.005d);
            return ModelTrainingResult.Success(filter, new Dictionary<string, string>
            {
                ["LogNavObservations"] = ModelConfigurationFingerprint.Format(logNav.Length),
                ["LogReturnVarianceScale"] = logReturnVariance.ToString("G17", CultureInfo.InvariantCulture),
                ["LogLikelihood"] = filter.LogLikelihood.ToString("G17", CultureInfo.InvariantCulture),
                ["ObservationVariance"] = filter.ObservationVariance.ToString("G17", CultureInfo.InvariantCulture),
                ["LevelVariance"] = filter.LevelVariance.ToString("G17", CultureInfo.InvariantCulture),
                ["TrendVariance"] = filter.TrendVariance.ToString("G17", CultureInfo.InvariantCulture),
            });
        }
        catch (ArgumentException exception)
        {
            return ModelTrainingResult.Failure(ForecastStatus.InvalidData, exception.Message);
        }
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

        if (trainingResult.FittedState is not KalmanFilterResult filter)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InvalidData, "State-space fitted filter was not available.");
        }

        var forecast = this.kalmanModel.Forecast(filter, context.HorizonResolution.EffectiveObservationCount);
        if (forecast.Count == 0)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InsufficientData, "State-space forecast did not produce points.");
        }

        var lastObservedLogNav = filter.LastEstimate?.Observation;
        if (!lastObservedLogNav.HasValue)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InvalidData, "State-space fitted filter was empty.");
        }

        var terminalForecast = forecast[^1];
        var cumulativeMean = terminalForecast.ExpectedValue - lastObservedLogNav.Value;
        var cumulativeVariance = terminalForecast.Variance;
        if (!double.IsFinite(cumulativeMean) ||
            !double.IsFinite(cumulativeVariance) ||
            cumulativeVariance < 0d ||
            cumulativeVariance > MaximumCumulativeVariance)
        {
            return ForecastPredictionResult.Failure(
                ForecastStatus.ModelRejected,
                "State-space projected distribution failed cumulative variance plausibility checks.",
                BuildProjectionDiagnostics(cumulativeMean, cumulativeVariance, null, null));
        }

        var cumulativeStandardDeviation = Math.Sqrt(Math.Max(0d, cumulativeVariance));
        var expectedSimpleReturn = Math.Exp(cumulativeMean + (0.5d * cumulativeVariance)) - 1d;
        var medianSimpleReturn = Math.Exp(cumulativeMean) - 1d;
        if (!IsPlausibleReturn(expectedSimpleReturn) || !IsPlausibleReturn(medianSimpleReturn))
        {
            return ForecastPredictionResult.Failure(
                ForecastStatus.ModelRejected,
                "State-space projected distribution exceeds plausibility bounds for fund-return forecasts.",
                BuildProjectionDiagnostics(cumulativeMean, cumulativeVariance, expectedSimpleReturn, medianSimpleReturn));
        }

        var probabilityPositive = cumulativeStandardDeviation == 0d
            ? cumulativeMean > 0d ? 1d : 0d
            : NormalDistribution.StandardCdf(cumulativeMean / cumulativeStandardDeviation);
        var percentiles = new Dictionary<int, double>
        {
            [10] = LogNormalReturnQuantile(cumulativeMean, cumulativeStandardDeviation, 0.10d),
            [25] = LogNormalReturnQuantile(cumulativeMean, cumulativeStandardDeviation, 0.25d),
            [50] = medianSimpleReturn,
            [75] = LogNormalReturnQuantile(cumulativeMean, cumulativeStandardDeviation, 0.75d),
            [90] = LogNormalReturnQuantile(cumulativeMean, cumulativeStandardDeviation, 0.90d),
        };
        var distribution = new ForecastDistribution(
            context.HorizonResolution,
            expectedSimpleReturn,
            medianSimpleReturn,
            percentiles,
            probabilityPositive,
            ProbabilityLogReturnAbove(cumulativeMean, cumulativeStandardDeviation, Math.Log(1.05d)),
            ProbabilityLogReturnBelow(cumulativeMean, cumulativeStandardDeviation, Math.Log(0.90d)),
            this.Capabilities,
            this.PointForecastStatistic,
            expectedSimpleReturn);

        return ForecastPredictionResult.Success(distribution, trainingResult.Diagnostics);
    }

    private static bool IsPlausibleReturn(double value)
    {
        return double.IsFinite(value) &&
            value >= MinimumPlausibleSimpleReturn &&
            value <= MaximumPlausibleSimpleReturn;
    }

    private static double[] ToLogNav(NavSeries series)
    {
        var result = new double[series.Count];
        for (var index = 0; index < series.Count; index++)
        {
            if (series[index].Value <= 0m)
            {
                throw new ArgumentException("State-space log-NAV model requires strictly positive NAV values.", nameof(series));
            }

            result[index] = Math.Log((double)series[index].Value);
        }

        return result;
    }

    private static double EstimateLogReturnVariance(IReadOnlyList<double> logNav)
    {
        if (logNav.Count < 3)
        {
            return MinimumVarianceScale;
        }

        var differences = new double[logNav.Count - 1];
        for (var index = 1; index < logNav.Count; index++)
        {
            differences[index - 1] = logNav[index] - logNav[index - 1];
        }

        var mean = differences.Average();
        var variance = differences.Sum(value =>
        {
            var deviation = value - mean;
            return deviation * deviation;
        }) / (differences.Length - 1d);
        return Math.Max(MinimumVarianceScale, variance);
    }

    private static IReadOnlyDictionary<string, string> BuildProjectionDiagnostics(
        double cumulativeMean,
        double cumulativeVariance,
        double? expectedSimpleReturn,
        double? medianSimpleReturn)
    {
        var diagnostics = new Dictionary<string, string>
        {
            ["CumulativeMean"] = cumulativeMean.ToString("G17", CultureInfo.InvariantCulture),
            ["CumulativeVariance"] = cumulativeVariance.ToString("G17", CultureInfo.InvariantCulture),
            ["MinimumPlausibleSimpleReturn"] = MinimumPlausibleSimpleReturn.ToString("G17", CultureInfo.InvariantCulture),
            ["MaximumPlausibleSimpleReturn"] = MaximumPlausibleSimpleReturn.ToString("G17", CultureInfo.InvariantCulture),
            ["MaximumCumulativeVariance"] = MaximumCumulativeVariance.ToString("G17", CultureInfo.InvariantCulture),
        };
        if (expectedSimpleReturn.HasValue)
        {
            diagnostics["ExpectedSimpleReturn"] = expectedSimpleReturn.Value.ToString("G17", CultureInfo.InvariantCulture);
        }

        if (medianSimpleReturn.HasValue)
        {
            diagnostics["MedianSimpleReturn"] = medianSimpleReturn.Value.ToString("G17", CultureInfo.InvariantCulture);
        }

        return diagnostics;
    }

    private static double LogNormalReturnQuantile(double mean, double standardDeviation, double probability)
    {
        if (standardDeviation == 0d)
        {
            return Math.Exp(mean) - 1d;
        }

        return Math.Exp(mean + (standardDeviation * NormalDistribution.StandardInverseCdf(probability))) - 1d;
    }

    private static double ProbabilityLogReturnAbove(double mean, double standardDeviation, double threshold)
    {
        if (standardDeviation == 0d)
        {
            return mean > threshold ? 1d : 0d;
        }

        return 1d - NormalDistribution.StandardCdf((threshold - mean) / standardDeviation);
    }

    private static double ProbabilityLogReturnBelow(double mean, double standardDeviation, double threshold)
    {
        if (standardDeviation == 0d)
        {
            return mean < threshold ? 1d : 0d;
        }

        return NormalDistribution.StandardCdf((threshold - mean) / standardDeviation);
    }
}
