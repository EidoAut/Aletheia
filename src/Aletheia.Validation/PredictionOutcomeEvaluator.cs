using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Evaluates a stored prediction against a realized return.
/// </summary>
public sealed class PredictionOutcomeEvaluator
{
    /// <summary>
    /// Evaluates a prediction.
    /// </summary>
    /// <param name="prediction">The immutable prediction record.</param>
    /// <param name="realizedReturn">The realized simple return.</param>
    /// <returns>The prediction evaluation.</returns>
    public PredictionEvaluation Evaluate(PredictionRecord prediction, double realizedReturn)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        var absoluteError = prediction.Supports(ForecastCapabilities.PointForecast)
            ? Math.Abs(realizedReturn - prediction.PointForecastReturn)
            : 0d;
        var lower = 0d;
        var upper = 0d;
        var hasQuantiles = prediction.Supports(ForecastCapabilities.Quantiles);
        var hasLower = hasQuantiles && prediction.ReturnPercentiles.TryGetValue(25, out lower);
        var hasUpper = hasQuantiles && prediction.ReturnPercentiles.TryGetValue(75, out upper);
        var insideInterquartileRange = hasLower && hasUpper && realizedReturn >= lower && realizedReturn <= upper;

        return new PredictionEvaluation(prediction, realizedReturn, absoluteError, insideInterquartileRange);
    }
}
