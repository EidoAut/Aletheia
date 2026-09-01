#pragma warning disable SA1204 // Public API stays before private helper methods in this workflow type.

using Aletheia.Core;

namespace Aletheia.Forecasting;

/// <summary>
/// Builds an evidence-weighted forecast ensemble from validated model forecasts.
/// </summary>
public sealed class ForecastEnsemble
{
    private const double QuantileProbabilityTolerance = 1e-12d;

    /// <summary>
    /// Combines eligible forecast distributions using exponentially transformed validation loss.
    /// </summary>
    /// <param name="members">The candidate members.</param>
    /// <param name="lambda">The positive loss temperature.</param>
    /// <returns>The ensemble result.</returns>
    public ForecastEnsembleResult Combine(IReadOnlyList<ForecastEnsembleMember> members, double lambda = 25d)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (!double.IsFinite(lambda) || lambda <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(lambda), lambda, "Lambda must be positive and finite.");
        }

        var eligible = members
            .Where(member => member.Eligible &&
                IsValidationHorizonAligned(member) &&
                HasRequiredEnsembleCapabilities(member.Distribution) &&
                double.IsFinite(member.ValidatedLoss) &&
                member.ValidatedLoss >= 0d &&
                double.IsFinite(member.CalibrationPenalty) &&
                member.CalibrationPenalty >= 0d)
            .ToArray();
        if (eligible.Length == 0)
        {
            return new ForecastEnsembleResult(null, Array.Empty<ForecastEnsembleComponent>(), 0d, 0d, "No model had sufficient validation evidence.");
        }

        var requestedHorizons = eligible
            .Select(member => member.Distribution.RequestedHorizon)
            .Distinct()
            .ToArray();
        if (requestedHorizons.Length > 1)
        {
            throw new ArgumentException("Forecast ensemble members must all target the same forecast horizon.", nameof(members));
        }

        var rawWeights = eligible
            .Select(member => Math.Exp(-lambda * (member.ValidatedLoss + member.CalibrationPenalty)))
            .ToArray();
        var rawSum = rawWeights.Sum();
        if (rawSum <= 0d || !double.IsFinite(rawSum))
        {
            return new ForecastEnsembleResult(null, Array.Empty<ForecastEnsembleComponent>(), 0d, 0d, "Ensemble weights were numerically degenerate.");
        }

        var weights = rawWeights.Select(value => value / rawSum).ToArray();
        var firstHorizon = eligible[0].Distribution.HorizonResolution;
        var expected = Weighted(eligible, weights, member => member.Distribution.ExpectedReturn);
        var percentiles = BuildMixturePercentiles(eligible, weights);
        var median = percentiles.GetValueOrDefault(
            50,
            Weighted(eligible, weights, member => member.Distribution.MedianReturn));
        var probabilityPositive = Weighted(eligible, weights, member => member.Distribution.ProbabilityPositive);
        var probabilityAboveFive = Weighted(eligible, weights, member => member.Distribution.ProbabilityReturnGreaterThanFivePercent);
        var probabilityLossAboveTen = Weighted(eligible, weights, member => member.Distribution.ProbabilityLossGreaterThanTenPercent);

        var pointForecasts = eligible.Select(member => member.Distribution.PointForecastReturn).ToArray();
        var pointMean = pointForecasts.Zip(weights).Sum(pair => pair.First * pair.Second);
        var disagreement = Math.Sqrt(pointForecasts.Zip(weights).Sum(pair =>
        {
            var deviation = pair.First - pointMean;
            return pair.Second * deviation * deviation;
        }));
        var effectiveModelCount = 1d / weights.Sum(weight => weight * weight);
        var suppliedSampleCounts = eligible
            .Where(member => member.EffectiveOosSampleCount > 0)
            .Select(member => member.EffectiveOosSampleCount)
            .ToArray();
        var sampleFactor = suppliedSampleCounts.Length == 0
            ? 1d
            : Math.Min(1d, suppliedSampleCounts.Min() / 30d);
        var reliability = Math.Clamp(
            sampleFactor * (effectiveModelCount / eligible.Length) * (1d / (1d + disagreement)),
            0d,
            1d);
        var capabilities = ForecastCapabilities.PointForecast |
            ForecastCapabilities.ExpectedReturn |
            ForecastCapabilities.Median |
            ForecastCapabilities.ProbabilityPositive |
            (percentiles.Count >= 3 ? ForecastCapabilities.Quantiles : ForecastCapabilities.None);
        var distribution = new ForecastDistribution(
            firstHorizon,
            expected,
            median,
            percentiles,
            probabilityPositive,
            probabilityAboveFive,
            probabilityLossAboveTen,
            capabilities,
            PointForecastStatistic.Mean,
            expected);
        var components = eligible.Select((member, index) => new ForecastEnsembleComponent(
            member.ModelId,
            weights[index],
            member.ValidatedLoss,
            member.CalibrationPenalty)).ToArray();

        return new ForecastEnsembleResult(
            distribution,
            components,
            disagreement,
            reliability,
            "Weights use exp(-lambda * (same-horizon validated loss + calibration penalty)); quantiles are obtained by deterministic inversion of the weighted mixture CDF.");
    }

    /// <summary>
    /// Determines whether a distribution can contribute to all quantities emitted by the ensemble.
    /// </summary>
    /// <param name="distribution">The candidate distribution.</param>
    /// <returns><see langword="true"/> when the distribution supports the core ensemble quantities.</returns>
    public static bool HasRequiredEnsembleCapabilities(ForecastDistribution distribution)
    {
        ArgumentNullException.ThrowIfNull(distribution);
        const ForecastCapabilities Required = ForecastCapabilities.PointForecast |
            ForecastCapabilities.ExpectedReturn |
            ForecastCapabilities.Median |
            ForecastCapabilities.ProbabilityPositive;
        return distribution.Supports(Required);
    }

    private static bool IsValidationHorizonAligned(ForecastEnsembleMember member)
    {
        return !member.ValidationHorizon.HasValue ||
            member.ValidationHorizon.Value.Equals(member.Distribution.RequestedHorizon);
    }

    private static Dictionary<int, double> BuildMixturePercentiles(
        IReadOnlyList<ForecastEnsembleMember> members,
        IReadOnlyList<double> weights)
    {
        var percentiles = new Dictionary<int, double>();
        foreach (var percentile in new[] { 10, 25, 50, 75, 90 })
        {
            percentiles[percentile] = MixtureQuantile(members, weights, percentile / 100d);
        }

        return percentiles;
    }

    private static double MixtureQuantile(
        IReadOnlyList<ForecastEnsembleMember> members,
        IReadOnlyList<double> weights,
        double probability)
    {
        var bounds = members
            .SelectMany(member => QuantileKnots(member.Distribution).Select(knot => knot.Value))
            .Where(double.IsFinite)
            .ToArray();
        if (bounds.Length == 0)
        {
            return 0d;
        }

        var lower = bounds.Min();
        var upper = bounds.Max();
        if (lower == upper || IsCdfAtOrAbove(MixtureCdf(members, weights, lower), probability))
        {
            return lower;
        }

        for (var iteration = 0; iteration < 96; iteration++)
        {
            var middle = lower + ((upper - lower) * 0.5d);
            if (IsCdfAtOrAbove(MixtureCdf(members, weights, middle), probability))
            {
                upper = middle;
            }
            else
            {
                lower = middle;
            }
        }

        return upper;
    }

    private static bool IsCdfAtOrAbove(double cdf, double probability) =>
        cdf + QuantileProbabilityTolerance >= probability;

    private static double MixtureCdf(
        IReadOnlyList<ForecastEnsembleMember> members,
        IReadOnlyList<double> weights,
        double value)
    {
        var sum = 0d;
        for (var index = 0; index < members.Count; index++)
        {
            sum += weights[index] * ApproximateCdf(members[index].Distribution, value);
        }

        return sum;
    }

    private static double ApproximateCdf(ForecastDistribution distribution, double value)
    {
        var knots = QuantileKnots(distribution);
        if (knots.Count == 0)
        {
            return value < distribution.MedianReturn ? 0d : 1d;
        }

        if (knots.All(knot => knot.Value == knots[0].Value))
        {
            return value < knots[0].Value ? 0d : 1d;
        }

        if (value < knots[0].Value)
        {
            return 0d;
        }

        for (var index = 0; index < knots.Count - 1; index++)
        {
            var left = knots[index];
            var right = knots[index + 1];
            if (value > right.Value)
            {
                continue;
            }

            if (right.Value == left.Value)
            {
                return right.Probability;
            }

            var weight = (value - left.Value) / (right.Value - left.Value);
            return left.Probability + ((right.Probability - left.Probability) * Math.Clamp(weight, 0d, 1d));
        }

        return 1d;
    }

    private static IReadOnlyList<(double Probability, double Value)> QuantileKnots(ForecastDistribution distribution)
    {
        var values = distribution.Percentiles
            .Where(pair => double.IsFinite(pair.Value) && pair.Key is > 0 and < 100)
            .Select(pair => (Probability: pair.Key / 100d, Value: pair.Value))
            .Append((Probability: 0.5d, Value: distribution.MedianReturn))
            .OrderBy(pair => pair.Probability)
            .ToArray();
        if (values.Length == 0)
        {
            return [];
        }

        var monotone = new List<(double Probability, double Value)>(values.Length);
        var previousValue = double.NegativeInfinity;
        foreach (var item in values)
        {
            var value = Math.Max(item.Value, previousValue);
            monotone.Add((item.Probability, value));
            previousValue = value;
        }

        return monotone;
    }

    private static double Weighted(
        IReadOnlyList<ForecastEnsembleMember> members,
        IReadOnlyList<double> weights,
        Func<ForecastEnsembleMember, double> selector)
    {
        var sum = 0d;
        for (var index = 0; index < members.Count; index++)
        {
            sum += selector(members[index]) * weights[index];
        }

        return sum;
    }
}
