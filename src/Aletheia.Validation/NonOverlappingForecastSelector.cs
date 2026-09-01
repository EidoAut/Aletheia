namespace Aletheia.Validation;

/// <summary>
/// Selects a deterministic subset of forecasts whose target windows do not overlap.
/// </summary>
public sealed class NonOverlappingForecastSelector
{
    /// <summary>
    /// Selects the earliest eligible forecast and then skips overlapping target windows.
    /// </summary>
    /// <param name="samples">The evaluated samples.</param>
    /// <returns>A non-overlapping subset.</returns>
    public IReadOnlyList<ForecastEvaluationSample> Select(IReadOnlyList<ForecastEvaluationSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var selected = new List<ForecastEvaluationSample>();
        var lastTargetIndex = -1;
        foreach (var sample in samples.OrderBy(item => item.Prediction.PredictionCutoffIndex))
        {
            if (!sample.Prediction.TargetIndex.HasValue)
            {
                continue;
            }

            var windowStart = sample.Prediction.PredictionCutoffIndex + 1;
            if (windowStart <= lastTargetIndex)
            {
                continue;
            }

            selected.Add(sample);
            lastTargetIndex = sample.Prediction.TargetIndex.Value;
        }

        return selected;
    }
}
