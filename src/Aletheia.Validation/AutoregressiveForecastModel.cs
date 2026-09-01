using System.Globalization;
using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.Dynamics;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Adapts the corrected AR(1) log-return model to the common forecast interface.
/// </summary>
public sealed class AutoregressiveForecastModel : IForecastModel
{
    /// <summary>
    /// The stable model id.
    /// </summary>
    public const string ModelId = "aletheia.forecast.ar1-log-return";

    private readonly ReturnCalculator returnCalculator;
    private readonly IStateFeaturePipeline statePipeline;
    private readonly IReadOnlyDictionary<string, string> configuration =
        new Dictionary<string, string> { ["FitPolicy"] = "RefitEveryCutoff" };

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoregressiveForecastModel"/> class.
    /// </summary>
    /// <param name="returnCalculator">The return calculator.</param>
    /// <param name="statePipeline">The state feature pipeline.</param>
    public AutoregressiveForecastModel(
        ReturnCalculator? returnCalculator = null,
        IStateFeaturePipeline? statePipeline = null)
    {
        this.returnCalculator = returnCalculator ?? new ReturnCalculator();
        this.statePipeline = statePipeline ?? new DynamicStateFeaturePipeline();
    }

    /// <inheritdoc />
    public ModelDescriptor Descriptor { get; } = new(
        ModelId,
        "AR(1)",
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

        if (context.TrainingSeries.Count < 4)
        {
            return ModelTrainingResult.Failure(ForecastStatus.InsufficientData, "AR(1) requires at least four NAV observations.");
        }

        var logReturns = this.returnCalculator.CalculateLogReturns(context.TrainingSeries);
        if (logReturns.Count < 3)
        {
            return ModelTrainingResult.Failure(ForecastStatus.InsufficientData, "AR(1) requires at least three log returns.");
        }

        var model = new AutoregressiveStateModel();
        var fit = model.Fit(new DynamicModelInput(logReturns));
        if (!fit.IsStationary)
        {
            return ModelTrainingResult.Failure(
                ForecastStatus.ModelRejected,
                "Fitted AR(1) process is not stationary because |phi| is greater than or equal to one.",
                CreateDiagnostics(fit));
        }

        var state = this.statePipeline.Build(context.TrainingSeries, context.TrainingSeries.Count - 1);
        return ModelTrainingResult.Success(new ArFittedState(model, fit, state), CreateDiagnostics(fit));
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

        if (trainingResult.FittedState is not ArFittedState fitted)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InvalidData, "AR(1) fitted state was not available.");
        }

        try
        {
            var horizon = ForecastHorizon.Observations(context.HorizonResolution.EffectiveObservationCount);
            var forecast = fitted.Model.Forecast(fitted.State, horizon);
            var standardDeviation = forecast.CumulativeLogReturnStandardDeviation;
            var probabilityPositive = standardDeviation == 0d
                ? forecast.CumulativeExpectedLogReturn > 0d ? 1d : 0d
                : NormalDistribution.StandardCdf(forecast.CumulativeExpectedLogReturn / standardDeviation);
            var distribution = new ForecastDistribution(
                context.HorizonResolution,
                forecast.ExpectedSimpleReturn,
                forecast.MedianSimpleReturn,
                forecast.SimpleReturnQuantiles,
                probabilityPositive,
                ProbabilityLogReturnAbove(forecast.CumulativeExpectedLogReturn, standardDeviation, Math.Log(1.05d)),
                ProbabilityLogReturnBelow(forecast.CumulativeExpectedLogReturn, standardDeviation, Math.Log(0.90d)),
                this.Capabilities,
                this.PointForecastStatistic,
                forecast.ExpectedSimpleReturn);

            return ForecastPredictionResult.Success(distribution, trainingResult.Diagnostics);
        }
        catch (IncompatibleDynamicStateException exception)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.IncompatibleState, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ForecastPredictionResult.Failure(ForecastStatus.InvalidData, exception.Message);
        }
    }

    private static IReadOnlyDictionary<string, string> CreateDiagnostics(DynamicModelResult fit)
    {
        return new Dictionary<string, string>
        {
            ["Intercept"] = fit.Parameters["Intercept"].ToString("G17", CultureInfo.InvariantCulture),
            ["Phi"] = fit.Parameters["Phi"].ToString("G17", CultureInfo.InvariantCulture),
            ["InnovationVariance"] = fit.InnovationVariance.ToString("G17", CultureInfo.InvariantCulture),
            ["IsStationary"] = fit.IsStationary ? "true" : "false",
        };
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

    private sealed record ArFittedState(
        AutoregressiveStateModel Model,
        DynamicModelResult Fit,
        DynamicState State);
}
