using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Represents a model that can be trained and evaluated under walk-forward rules.
/// </summary>
public interface IForecastModel
{
    /// <summary>
    /// Gets the stable model descriptor.
    /// </summary>
    ModelDescriptor Descriptor { get; }

    /// <summary>
    /// Gets deterministic model configuration values.
    /// </summary>
    IReadOnlyDictionary<string, string> Configuration { get; }

    /// <summary>
    /// Gets the forecast quantities this model explicitly supports.
    /// </summary>
    ForecastCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the statistic represented by the model's principal point forecast.
    /// </summary>
    PointForecastStatistic PointForecastStatistic { get; }

    /// <summary>
    /// Gets the deterministic fingerprint of <see cref="Configuration"/>.
    /// </summary>
    string ConfigurationFingerprint { get; }

    /// <summary>
    /// Trains the model using only data available inside the supplied training context.
    /// </summary>
    /// <param name="context">The training context.</param>
    /// <param name="cancellationToken">A token used to cancel long-running evaluations.</param>
    /// <returns>The typed training result.</returns>
    ModelTrainingResult Train(ForecastTrainingContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Produces a forecast from a fitted model without access to post-cutoff data.
    /// </summary>
    /// <param name="trainingResult">The fitted training result.</param>
    /// <param name="context">The prediction context.</param>
    /// <param name="cancellationToken">A token used to cancel long-running evaluations.</param>
    /// <returns>The typed prediction result.</returns>
    ForecastPredictionResult Predict(
        ModelTrainingResult trainingResult,
        ForecastPredictionContext context,
        CancellationToken cancellationToken = default);
}
