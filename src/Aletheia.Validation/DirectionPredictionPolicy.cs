using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Applies explicit directional classification rules to forecast records.
/// </summary>
public static class DirectionPredictionPolicy
{
    /// <summary>
    /// Classifies a prediction's direction according to a configured rule.
    /// </summary>
    /// <param name="prediction">The prediction record.</param>
    /// <param name="rule">The configured rule.</param>
    /// <param name="flatReturnTolerance">The nonnegative flat-return tolerance.</param>
    /// <returns>The predicted direction.</returns>
    public static ForecastDirection Classify(
        PredictionLedgerRecord prediction,
        DirectionPredictionRule rule,
        double flatReturnTolerance)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        var record = prediction.Prediction;
        var resolvedRule = ResolveRule(record, rule);
        return resolvedRule switch
        {
            DirectionPredictionRule.PointForecastSign => DirectionClassifier.Classify(record.PointForecastReturn, flatReturnTolerance),
            DirectionPredictionRule.MedianSign => DirectionClassifier.Classify(record.MedianReturn, flatReturnTolerance),
            DirectionPredictionRule.ProbabilityPositiveThreshold => ClassifyProbability(record.ProbabilityPositive),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unsupported direction rule."),
        };
    }

    /// <summary>
    /// Resolves <see cref="DirectionPredictionRule.Automatic"/> to the concrete rule used.
    /// </summary>
    /// <param name="prediction">The prediction record.</param>
    /// <param name="rule">The configured rule.</param>
    /// <returns>The concrete direction rule.</returns>
    public static DirectionPredictionRule ResolveRule(
        PredictionRecord prediction,
        DirectionPredictionRule rule)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        if (rule == DirectionPredictionRule.Automatic)
        {
            if (prediction.Supports(ForecastCapabilities.ProbabilityPositive))
            {
                return DirectionPredictionRule.ProbabilityPositiveThreshold;
            }

            if (prediction.Supports(ForecastCapabilities.PointForecast))
            {
                return DirectionPredictionRule.PointForecastSign;
            }

            throw new InvalidOperationException("Automatic direction classification requires probability or point forecast capability.");
        }

        if (rule == DirectionPredictionRule.PointForecastSign &&
            !prediction.Supports(ForecastCapabilities.PointForecast))
        {
            throw new InvalidOperationException("Point-forecast direction classification requires point forecast capability.");
        }

        if (rule == DirectionPredictionRule.MedianSign &&
            !prediction.Supports(ForecastCapabilities.Median))
        {
            throw new InvalidOperationException("Median direction classification requires median forecast capability.");
        }

        if (rule == DirectionPredictionRule.ProbabilityPositiveThreshold &&
            !prediction.Supports(ForecastCapabilities.ProbabilityPositive))
        {
            throw new InvalidOperationException("Probability direction classification requires probability-positive capability.");
        }

        return rule;
    }

    private static ForecastDirection ClassifyProbability(double probabilityPositive)
    {
        if (double.IsNaN(probabilityPositive) || double.IsInfinity(probabilityPositive) ||
            probabilityPositive < 0d || probabilityPositive > 1d)
        {
            throw new ArgumentException("Probability-positive direction classification requires a probability in [0, 1].", nameof(probabilityPositive));
        }

        if (probabilityPositive > 0.5d)
        {
            return ForecastDirection.Positive;
        }

        return probabilityPositive < 0.5d ? ForecastDirection.Negative : ForecastDirection.Flat;
    }
}
