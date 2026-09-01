namespace Aletheia.Validation;

/// <summary>
/// Fits robust feature scaling from a causal training prefix for timing analogues.
/// </summary>
public sealed class MarketTimingRobustFeatureScaler
{
    private const double MinimumScale = 1e-8d;

    private readonly IReadOnlyDictionary<string, FeatureScale> scales;

    private MarketTimingRobustFeatureScaler(IReadOnlyDictionary<string, FeatureScale> scales)
    {
        this.scales = scales;
    }

    /// <summary>
    /// Gets the feature scales estimated from the training prefix.
    /// </summary>
    public IReadOnlyDictionary<string, (double Location, double Scale)> Scales =>
        this.scales.ToDictionary(
            pair => pair.Key,
            pair => (pair.Value.Location, pair.Value.Scale),
            StringComparer.Ordinal);

    /// <summary>
    /// Fits robust z-score parameters from training features.
    /// </summary>
    /// <param name="trainingFeatures">The causal training features.</param>
    /// <param name="featureNames">The candidate feature names.</param>
    /// <returns>The fitted scaler.</returns>
    public static MarketTimingRobustFeatureScaler Fit(
        IReadOnlyList<MarketTimingFeatureVector> trainingFeatures,
        IEnumerable<string> featureNames)
    {
        ArgumentNullException.ThrowIfNull(trainingFeatures);
        ArgumentNullException.ThrowIfNull(featureNames);
        var scales = new Dictionary<string, FeatureScale>(StringComparer.Ordinal);
        foreach (var name in featureNames.Distinct(StringComparer.Ordinal))
        {
            var values = trainingFeatures
                .Where(feature => feature.HasFeature(name))
                .Select(feature => feature.Values[name])
                .Order()
                .ToArray();
            if (values.Length == 0)
            {
                continue;
            }

            var median = Quantile(values, 0.5d);
            var deviations = values
                .Select(value => Math.Abs(value - median))
                .Order()
                .ToArray();
            var scale = 1.4826d * Quantile(deviations, 0.5d);
            if (!double.IsFinite(scale) || scale < MinimumScale)
            {
                scale = StandardDeviation(values);
            }

            if (!double.IsFinite(scale) || scale < MinimumScale)
            {
                scale = 1d;
            }

            scales[name] = new FeatureScale(median, scale);
        }

        return new MarketTimingRobustFeatureScaler(scales);
    }

    /// <summary>
    /// Calculates robust standardized Euclidean distance using fitted training-prefix scales.
    /// </summary>
    /// <param name="left">The left feature vector.</param>
    /// <param name="right">The right feature vector.</param>
    /// <returns>The distance, or positive infinity when no shared scaled feature exists.</returns>
    public double Distance(MarketTimingFeatureVector left, MarketTimingFeatureVector right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var sum = 0d;
        var used = 0;
        foreach (var pair in this.scales)
        {
            if (!left.TryGetFeature(pair.Key, out var leftValue) ||
                !right.TryGetFeature(pair.Key, out var rightValue))
            {
                continue;
            }

            var leftZ = (leftValue - pair.Value.Location) / pair.Value.Scale;
            var rightZ = (rightValue - pair.Value.Location) / pair.Value.Scale;
            var deviation = leftZ - rightZ;
            sum += deviation * deviation;
            used++;
        }

        return used == 0 ? double.PositiveInfinity : Math.Sqrt(sum / used);
    }

    private static double StandardDeviation(IReadOnlyList<double> sortedValues)
    {
        if (sortedValues.Count < 2)
        {
            return 0d;
        }

        var mean = sortedValues.Average();
        var sum = 0d;
        foreach (var value in sortedValues)
        {
            var deviation = value - mean;
            sum += deviation * deviation;
        }

        return Math.Sqrt(sum / (sortedValues.Count - 1d));
    }

    private static double Quantile(IReadOnlyList<double> sorted, double probability)
    {
        if (sorted.Count == 0)
        {
            return 0d;
        }

        if (sorted.Count == 1)
        {
            return sorted[0];
        }

        var position = Math.Clamp(probability, 0d, 1d) * (sorted.Count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        var weight = position - lower;
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * weight);
    }

    private sealed record FeatureScale(double Location, double Scale);
}
