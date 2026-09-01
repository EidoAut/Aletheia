using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Validates probability and quantile semantics emitted by forecast models.
/// </summary>
public static class ForecastOutputValidator
{
    /// <summary>
    /// Validates a forecast distribution.
    /// </summary>
    /// <param name="distribution">The distribution to validate.</param>
    /// <returns>A failure reason, or <see langword="null"/> when the distribution is valid.</returns>
    public static string? Validate(ForecastDistribution distribution)
    {
        ArgumentNullException.ThrowIfNull(distribution);

        if (distribution.Capabilities == ForecastCapabilities.None)
        {
            return "A successful forecast must expose at least one forecast capability.";
        }

        if (Supports(distribution, ForecastCapabilities.PointForecast) &&
            !IsFinite(distribution.PointForecastReturn))
        {
            return "Forecast point estimate must be finite.";
        }

        if (Supports(distribution, ForecastCapabilities.ExpectedReturn) &&
            !IsFinite(distribution.ExpectedReturn))
        {
            return "Forecast expected return must be finite.";
        }

        if (Supports(distribution, ForecastCapabilities.Median) &&
            !IsFinite(distribution.MedianReturn))
        {
            return "Forecast median return must be finite.";
        }

        if (Supports(distribution, ForecastCapabilities.ProbabilityPositive))
        {
            if (!IsProbability(distribution.ProbabilityPositive) ||
                !IsProbability(distribution.ProbabilityReturnGreaterThanFivePercent) ||
                !IsProbability(distribution.ProbabilityLossGreaterThanTenPercent))
            {
                return "Forecast probabilities must be finite values in [0, 1].";
            }
        }
        else if (distribution.ProbabilityPositive != 0d ||
            distribution.ProbabilityReturnGreaterThanFivePercent != 0d ||
            distribution.ProbabilityLossGreaterThanTenPercent != 0d)
        {
            return "Forecast probabilities require probability forecast capability.";
        }

        if (!Supports(distribution, ForecastCapabilities.Quantiles) && distribution.Percentiles.Count > 0)
        {
            return "Forecast quantiles require quantile forecast capability.";
        }

        var previousPercentile = -1;
        double? previousValue = null;
        foreach (var pair in distribution.Percentiles.OrderBy(pair => pair.Key))
        {
            if (pair.Key <= previousPercentile || pair.Key < 0 || pair.Key > 100)
            {
                return "Forecast percentile keys must be unique values in [0, 100].";
            }

            if (!IsFinite(pair.Value))
            {
                return "Forecast quantiles must be finite.";
            }

            if (previousValue.HasValue && pair.Value < previousValue.Value)
            {
                return "Forecast quantiles must be monotonically nondecreasing.";
            }

            previousPercentile = pair.Key;
            previousValue = pair.Value;
        }

        return null;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool IsProbability(double value) => IsFinite(value) && value >= 0d && value <= 1d;

    private static bool Supports(ForecastDistribution distribution, ForecastCapabilities capability) =>
        (distribution.Capabilities & capability) == capability;
}
