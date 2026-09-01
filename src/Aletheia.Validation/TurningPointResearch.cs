#pragma warning disable SA1402 // Turning-point detector, estimator, and labels form one small research component.
#pragma warning disable SA1649 // The file groups turning-point research types.

using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Detects retrospectively confirmed historical turning points.
/// </summary>
public sealed class TurningPointDetector
{
    /// <summary>
    /// Detects local peaks and troughs after a minimum reversal.
    /// </summary>
    /// <param name="navSeries">The NAV series.</param>
    /// <param name="minimumReversal">The minimum reversal magnitude.</param>
    /// <returns>Confirmed turning points.</returns>
    public IReadOnlyList<TurningPointLabel> Detect(NavSeries navSeries, double minimumReversal = 0.03d)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        if (!double.IsFinite(minimumReversal) || minimumReversal <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumReversal), minimumReversal, "Minimum reversal must be positive and finite.");
        }

        if (navSeries.Count < 3)
        {
            return Array.Empty<TurningPointLabel>();
        }

        var labels = new List<TurningPointLabel>();
        var candidateHighIndex = 0;
        var candidateLowIndex = 0;
        var mode = 0;
        for (var index = 1; index < navSeries.Count; index++)
        {
            var value = (double)navSeries[index].Value;
            var high = (double)navSeries[candidateHighIndex].Value;
            var low = (double)navSeries[candidateLowIndex].Value;
            if (value > high)
            {
                candidateHighIndex = index;
            }

            if (value < low)
            {
                candidateLowIndex = index;
            }

            var dropFromHigh = (value / (double)navSeries[candidateHighIndex].Value) - 1d;
            var riseFromLow = (value / (double)navSeries[candidateLowIndex].Value) - 1d;
            if (mode >= 0 && dropFromHigh <= -minimumReversal)
            {
                labels.Add(new TurningPointLabel(
                    navSeries[candidateHighIndex].Date,
                    candidateHighIndex,
                    true,
                    Math.Abs(dropFromHigh)));
                candidateLowIndex = index;
                mode = -1;
            }
            else if (mode <= 0 && riseFromLow >= minimumReversal)
            {
                labels.Add(new TurningPointLabel(
                    navSeries[candidateLowIndex].Date,
                    candidateLowIndex,
                    false,
                    riseFromLow));
                candidateHighIndex = index;
                mode = 1;
            }
        }

        return labels;
    }
}

/// <summary>
/// Estimates whether the current area historically preceded a peak or trough.
/// </summary>
public sealed class TurningPointProbabilityEstimator
{
    /// <summary>
    /// Estimates turning-point probabilities from confirmed retrospective labels.
    /// </summary>
    /// <param name="features">Feature vectors.</param>
    /// <param name="turningPoints">Confirmed turning points.</param>
    /// <param name="current">Current feature vector.</param>
    /// <param name="horizonObservations">The lookahead horizon.</param>
    /// <returns>Turning-point probabilities.</returns>
    public TurningPointProbability Estimate(
        IReadOnlyList<MarketTimingFeatureVector> features,
        IReadOnlyList<TurningPointLabel> turningPoints,
        MarketTimingFeatureVector current,
        int horizonObservations)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(turningPoints);
        ArgumentNullException.ThrowIfNull(current);
        if (features.Count == 0 || turningPoints.Count == 0)
        {
            return new TurningPointProbability(0d, 0d, 0);
        }

        var labels = turningPoints
            .Where(point => point.Index <= current.ObservationIndex)
            .ToArray();
        var comparable = features
            .Where(feature => feature.ObservationIndex < current.ObservationIndex)
            .OrderBy(feature => Distance(feature, current))
            .Take(30)
            .ToArray();
        if (comparable.Length == 0)
        {
            return new TurningPointProbability(0d, 0d, 0);
        }

        var peak = 0;
        var trough = 0;
        foreach (var feature in comparable)
        {
            if (labels.Any(point => point.IsPeak && point.Index > feature.ObservationIndex && point.Index <= feature.ObservationIndex + horizonObservations))
            {
                peak++;
            }

            if (labels.Any(point => !point.IsPeak && point.Index > feature.ObservationIndex && point.Index <= feature.ObservationIndex + horizonObservations))
            {
                trough++;
            }
        }

        return new TurningPointProbability(
            peak / (double)comparable.Length,
            trough / (double)comparable.Length,
            comparable.Length);
    }

    private static double Distance(MarketTimingFeatureVector left, MarketTimingFeatureVector right)
    {
        var sum = 0d;
        foreach (var pair in right.Values)
        {
            left.Values.TryGetValue(pair.Key, out var leftValue);
            var deviation = leftValue - pair.Value;
            sum += deviation * deviation;
        }

        return Math.Sqrt(sum);
    }
}

/// <summary>
/// Stores one retrospectively confirmed turning point.
/// </summary>
/// <param name="Date">The turning-point date.</param>
/// <param name="Index">The observation index.</param>
/// <param name="IsPeak">Whether the point is a peak.</param>
/// <param name="ConfirmedReversal">The reversal that confirmed the point.</param>
public sealed record TurningPointLabel(DateOnly Date, int Index, bool IsPeak, double ConfirmedReversal);

/// <summary>
/// Stores experimental turning-point probabilities.
/// </summary>
/// <param name="ProbabilityPeakWithinHorizon">Probability current area precedes a peak.</param>
/// <param name="ProbabilityTroughWithinHorizon">Probability current area precedes a trough.</param>
/// <param name="ComparableSampleCount">Number of comparable historical areas.</param>
public sealed record TurningPointProbability(
    double ProbabilityPeakWithinHorizon,
    double ProbabilityTroughWithinHorizon,
    int ComparableSampleCount);
