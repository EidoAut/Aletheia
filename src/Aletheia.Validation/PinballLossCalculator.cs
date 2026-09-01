using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Calculates quantile pinball loss for realized returns.
/// </summary>
public sealed class PinballLossCalculator
{
    /// <summary>
    /// Calculates mean pinball loss for one percentile where forecasts expose that quantile.
    /// </summary>
    /// <param name="samples">The evaluated predictions.</param>
    /// <param name="percentile">The percentile in [0, 100].</param>
    /// <returns>The mean loss and sample count.</returns>
    public (double? MeanLoss, int SampleCount) Calculate(
        IReadOnlyList<ForecastEvaluationSample> samples,
        int percentile)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (percentile < 0 || percentile > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Percentile must be in [0, 100].");
        }

        var tau = percentile / 100d;
        var losses = new List<double>();
        foreach (var sample in samples)
        {
            if (!sample.Prediction.Prediction.Supports(ForecastCapabilities.Quantiles))
            {
                continue;
            }

            if (!sample.Prediction.Prediction.ReturnPercentiles.TryGetValue(percentile, out var quantile))
            {
                continue;
            }

            var actual = sample.Evaluation.ActualReturn;
            losses.Add(actual >= quantile
                ? tau * (actual - quantile)
                : (1d - tau) * (quantile - actual));
        }

        return losses.Count == 0 ? (null, 0) : (losses.Average(), losses.Count);
    }
}
