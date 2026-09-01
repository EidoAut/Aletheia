using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Trivial baseline that forecasts zero cumulative simple return.
/// </summary>
/// <remarks>
/// The model intentionally carries no market insight. It establishes the
/// minimum point-forecast benchmark that nontrivial models must beat out of
/// sample. It does not advertise a probability forecast: a deterministic
/// zero-return statement would imply P(R &gt; 0) = 0, while a neutral Brier
/// baseline requires a separate probabilistic model.
/// </remarks>
public sealed class ZeroReturnForecastModel : IForecastModel
{
    /// <summary>
    /// The stable model id.
    /// </summary>
    public const string ModelId = "aletheia.forecast.zero-return";

    private readonly IReadOnlyDictionary<string, string> configuration =
        new Dictionary<string, string> { ["PointForecast"] = "ZeroSimpleReturn" };

    /// <inheritdoc />
    public ModelDescriptor Descriptor { get; } = new(
        ModelId,
        "Zero Return",
        "2.1.0");

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Configuration => this.configuration;

    /// <inheritdoc />
    public ForecastCapabilities Capabilities => ForecastCapabilities.PointForecast;

    /// <inheritdoc />
    public PointForecastStatistic PointForecastStatistic => PointForecastStatistic.ExplicitModelPoint;

    /// <inheritdoc />
    public string ConfigurationFingerprint => ModelConfigurationFingerprint.Calculate(this.Descriptor, this.Configuration);

    /// <inheritdoc />
    public ModelTrainingResult Train(ForecastTrainingContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        return ModelTrainingResult.Success(null);
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

        var distribution = new ForecastDistribution(
            context.HorizonResolution,
            0d,
            0d,
            new Dictionary<int, double>(),
            0d,
            0d,
            0d,
            this.Capabilities,
            this.PointForecastStatistic,
            0d);

        return ForecastPredictionResult.Success(distribution);
    }
}
