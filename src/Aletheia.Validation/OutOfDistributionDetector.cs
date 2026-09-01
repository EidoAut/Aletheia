namespace Aletheia.Validation;

/// <summary>
/// Detects whether the current timing state is outside historical feature support.
/// </summary>
public sealed class OutOfDistributionDetector
{
    /// <summary>
    /// Evaluates robust feature-space distance.
    /// </summary>
    /// <param name="trainingFeatures">Training features.</param>
    /// <param name="current">Current feature.</param>
    /// <param name="featureNames">Feature names.</param>
    /// <param name="threshold">The OOD threshold.</param>
    /// <param name="slightlyUnusualThreshold">The threshold for unusual but not OOD states.</param>
    /// <returns>OOD diagnostic.</returns>
    public OutOfDistributionDiagnostic Evaluate(
        IReadOnlyList<MarketTimingFeatureVector> trainingFeatures,
        MarketTimingFeatureVector current,
        IReadOnlyList<string> featureNames,
        double threshold = 3.5d,
        double slightlyUnusualThreshold = 2.0d)
    {
        ArgumentNullException.ThrowIfNull(trainingFeatures);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(featureNames);
        if (trainingFeatures.Count < 20)
        {
            return new OutOfDistributionDiagnostic(
                true,
                double.PositiveInfinity,
                threshold,
                OutOfDistributionLevel.OutOfDistribution);
        }

        var sum = 0d;
        var used = 0;
        foreach (var name in featureNames)
        {
            var values = trainingFeatures
                .Where(feature => feature.HasFeature(name))
                .Select(feature => feature.Values[name])
                .Order()
                .ToArray();
            if (values.Length < 20 || !current.TryGetFeature(name, out var currentValue))
            {
                continue;
            }

            var median = Quantile(values, 0.5d);
            var deviations = values.Select(value => Math.Abs(value - median)).Order().ToArray();
            var mad = Math.Max(1e-8d, Quantile(deviations, 0.5d) * 1.4826d);

            var z = (currentValue - median) / mad;
            sum += z * z;
            used++;
        }

        if (used == 0)
        {
            return new OutOfDistributionDiagnostic(
                true,
                double.PositiveInfinity,
                threshold,
                OutOfDistributionLevel.OutOfDistribution);
        }

        var distance = Math.Sqrt(sum / used);
        var level = distance >= threshold
            ? OutOfDistributionLevel.OutOfDistribution
            : distance >= slightlyUnusualThreshold
                ? OutOfDistributionLevel.SlightlyUnusual
                : OutOfDistributionLevel.InDistribution;
        return new OutOfDistributionDiagnostic(
            level == OutOfDistributionLevel.OutOfDistribution,
            distance,
            threshold,
            level);
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

        var position = probability * (sorted.Count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        var weight = position - lower;
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * weight);
    }
}
