namespace Aletheia.Validation;

/// <summary>
/// Combines validated market-timing models by out-of-sample evidence.
/// </summary>
public sealed class MarketTimingEnsemble
{
    /// <summary>
    /// Combines model predictions.
    /// </summary>
    /// <param name="models">The candidate model results.</param>
    /// <param name="fallback">The fallback prediction.</param>
    /// <param name="targetOosSamples">The OOS sample count at which sample evidence is treated as mature.</param>
    /// <param name="oodDistance">The robust distance between the current feature vector and the training feature cloud.</param>
    /// <param name="oodThreshold">The robust distance at which OOD penalties become material.</param>
    /// <returns>The ensemble result.</returns>
    public MarketTimingEnsembleResult Combine(
        IReadOnlyList<MarketTimingModelResult> models,
        MarketEventPrediction fallback,
        int targetOosSamples = 30,
        double oodDistance = 0d,
        double oodThreshold = 3.5d)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentNullException.ThrowIfNull(fallback);
        var eligible = models
            .Where(model => model.EligibleForEnsemble &&
                model.Kind != MarketTimingModelKind.HistoricalEventRateBaseline &&
                model.EligibilityStatus == ModelEligibilityStatus.Eligible &&
                double.IsFinite(model.BrierSkillVsBaseline) &&
                model.BrierSkillVsBaseline > 0d)
            .ToArray();
        var candidateCount = models.Count(model => model.Kind != MarketTimingModelKind.HistoricalEventRateBaseline);
        if (eligible.Length == 0)
        {
            var fallbackReason = BuildFallbackReason(models);
            return new MarketTimingEnsembleResult(
                fallback,
                Array.Empty<MarketTimingEnsembleComponent>(),
                1d,
                0d,
                0d,
                "No timing model had sufficient OOS evidence; baseline fallback used.",
                candidateCount,
                0,
                false,
                fallbackReason);
        }

        var rawWeights = eligible
            .Select(model => Math.Exp((12d * model.BrierSkillVsBaseline) - (2d * model.Calibration.ExpectedCalibrationError)))
            .ToArray();
        var rawSum = rawWeights.Sum();
        if (rawSum <= 0d || !double.IsFinite(rawSum))
        {
            return new MarketTimingEnsembleResult(
                fallback,
                Array.Empty<MarketTimingEnsembleComponent>(),
                1d,
                0d,
                0d,
                "Timing ensemble weights were numerically degenerate; baseline fallback used.",
                candidateCount,
                0,
                false,
                "Timing ensemble weights were numerically degenerate.");
        }

        var weights = rawWeights.Select(value => value / rawSum).ToArray();
        var up = Weighted(eligible, weights, model => model.CurrentPrediction.ProbabilityUpFirst);
        var down = Weighted(eligible, weights, model => model.CurrentPrediction.ProbabilityDownFirst);
        var neutral = Weighted(eligible, weights, model => model.CurrentPrediction.ProbabilityNoEvent);
        var normalized = Normalize(new MarketEventPrediction(up, down, neutral));
        var disagreement = WeightedDisagreement(eligible, weights, normalized);
        var effectiveCount = 1d / weights.Sum(weight => weight * weight);
        var minimumOosSampleCount = eligible.Min(model => model.Calibration.SampleCount);
        var averageSkill = eligible.Average(model => Math.Max(0d, model.BrierSkillVsBaseline));
        var averageSkillIntervalWidth = eligible.Average(model => Math.Max(0d, model.BrierSkillInterval.Upper - model.BrierSkillInterval.Lower));
        var averageCalibrationError = eligible.Average(model => Math.Clamp(model.Calibration.ExpectedCalibrationError, 0d, 1d));
        var reliability = ReliabilityIndexCalculator.Calculate(
            minimumOosSampleCount,
            targetOosSamples,
            averageCalibrationError,
            averageSkill,
            averageSkillIntervalWidth,
            temporalInstability: 0d,
            effectiveCount / eligible.Length,
            disagreement,
            oodDistance,
            oodThreshold);
        var components = eligible.Select((model, index) => new MarketTimingEnsembleComponent(
            model.ModelName,
            weights[index],
            model.BrierSkillVsBaseline,
            model.Calibration.ExpectedCalibrationError)).ToArray();
        return new MarketTimingEnsembleResult(
            normalized,
            components,
            disagreement,
            effectiveCount,
            reliability,
            "Weights use horizon-specific OOS Brier skill penalized by calibration error; ReliabilityIndex also penalizes OOS sample scarcity, skill uncertainty, weight concentration, disagreement, and OOD distance.",
            candidateCount,
            eligible.Length,
            true,
            string.Empty);
    }

    private static string BuildFallbackReason(IReadOnlyList<MarketTimingModelResult> models)
    {
        var candidates = models
            .Where(model => model.Kind != MarketTimingModelKind.HistoricalEventRateBaseline)
            .ToArray();
        if (candidates.Length == 0)
        {
            return "No candidate timing models were evaluated.";
        }

        var grouped = candidates
            .GroupBy(model => model.EligibilityStatus)
            .OrderByDescending(group => group.Count())
            .First();
        var sample = grouped.First();
        return grouped.Key == ModelEligibilityStatus.InsufficientEvidence
            ? sample.RejectionReason
            : $"No candidate passed ensemble gates; most common rejection: {grouped.Key}.";
    }

    private static double Weighted(
        IReadOnlyList<MarketTimingModelResult> models,
        IReadOnlyList<double> weights,
        Func<MarketTimingModelResult, double> selector)
    {
        var sum = 0d;
        for (var index = 0; index < models.Count; index++)
        {
            sum += selector(models[index]) * weights[index];
        }

        return sum;
    }

    private static double WeightedDisagreement(
        IReadOnlyList<MarketTimingModelResult> models,
        IReadOnlyList<double> weights,
        MarketEventPrediction prediction)
    {
        var sum = 0d;
        for (var index = 0; index < models.Count; index++)
        {
            var upDeviation = models[index].CurrentPrediction.ProbabilityUpFirst - prediction.ProbabilityUpFirst;
            var downDeviation = models[index].CurrentPrediction.ProbabilityDownFirst - prediction.ProbabilityDownFirst;
            sum += weights[index] * ((upDeviation * upDeviation) + (downDeviation * downDeviation));
        }

        return Math.Sqrt(sum);
    }

    private static MarketEventPrediction Normalize(MarketEventPrediction prediction)
    {
        var up = Math.Clamp(prediction.ProbabilityUpFirst, 0d, 1d);
        var down = Math.Clamp(prediction.ProbabilityDownFirst, 0d, 1d);
        var neutral = Math.Clamp(prediction.ProbabilityNoEvent, 0d, 1d);
        var sum = up + down + neutral;
        return sum <= 0d || !double.IsFinite(sum)
            ? new MarketEventPrediction(1d / 3d, 1d / 3d, 1d / 3d)
            : new MarketEventPrediction(up / sum, down / sum, neutral / sum);
    }
}
