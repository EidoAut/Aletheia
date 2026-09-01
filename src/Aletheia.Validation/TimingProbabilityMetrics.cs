namespace Aletheia.Validation;

/// <summary>
/// Calculates probability quality metrics for timing predictions.
/// </summary>
public static class TimingProbabilityMetrics
{
    private const double Epsilon = 1e-12d;

    /// <summary>
    /// Calculates multiclass Brier score.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <returns>The Brier score.</returns>
    public static double BrierScore(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        EnsureAligned(predictions, outcomes);
        if (predictions.Count == 0)
        {
            return 0d;
        }

        var sum = 0d;
        for (var index = 0; index < predictions.Count; index++)
        {
            var actual = ToClass(outcomes[index]);
            var probabilities = predictions[index].Probabilities;
            for (var klass = 0; klass < 3; klass++)
            {
                var error = probabilities[klass] - (klass == actual ? 1d : 0d);
                sum += error * error;
            }
        }

        return sum / predictions.Count;
    }

    /// <summary>
    /// Calculates multiclass log loss.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <returns>The log loss.</returns>
    public static double LogLoss(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        EnsureAligned(predictions, outcomes);
        if (predictions.Count == 0)
        {
            return 0d;
        }

        var sum = 0d;
        for (var index = 0; index < predictions.Count; index++)
        {
            var actual = ToClass(outcomes[index]);
            sum -= Math.Log(Math.Clamp(predictions[index].Probabilities[actual], Epsilon, 1d));
        }

        return sum / predictions.Count;
    }

    /// <summary>
    /// Calculates a simple expected calibration error for the max-probability class.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <param name="binCount">The number of probability bins.</param>
    /// <returns>The expected calibration error.</returns>
    public static double ExpectedCalibrationError(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes,
        int binCount = 10)
    {
        EnsureAligned(predictions, outcomes);
        if (predictions.Count == 0)
        {
            return 0d;
        }

        var bins = new List<(double Confidence, double Correct, int Count)>();
        for (var bin = 0; bin < binCount; bin++)
        {
            bins.Add((0d, 0d, 0));
        }

        for (var index = 0; index < predictions.Count; index++)
        {
            var probabilities = predictions[index].Probabilities;
            var predicted = ArgMax(probabilities);
            var confidence = probabilities[predicted];
            var bin = Math.Min(binCount - 1, (int)Math.Floor(confidence * binCount));
            var current = bins[bin];
            bins[bin] = (
                current.Confidence + confidence,
                current.Correct + (predicted == ToClass(outcomes[index]) ? 1d : 0d),
                current.Count + 1);
        }

        var error = 0d;
        foreach (var bin in bins)
        {
            if (bin.Count == 0)
            {
                continue;
            }

            error += (bin.Count / (double)predictions.Count) *
                Math.Abs((bin.Confidence / bin.Count) - (bin.Correct / bin.Count));
        }

        return error;
    }

    /// <summary>
    /// Calculates balanced accuracy.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <returns>Balanced accuracy.</returns>
    public static double BalancedAccuracy(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        EnsureAligned(predictions, outcomes);
        if (predictions.Count == 0)
        {
            return 0d;
        }

        var correct = new int[3];
        var totals = new int[3];
        for (var index = 0; index < predictions.Count; index++)
        {
            var actual = ToClass(outcomes[index]);
            totals[actual]++;
            if (ArgMax(predictions[index].Probabilities) == actual)
            {
                correct[actual]++;
            }
        }

        var recalls = Enumerable.Range(0, 3)
            .Where(klass => totals[klass] > 0)
            .Select(klass => correct[klass] / (double)totals[klass])
            .ToArray();
        return recalls.Length == 0 ? 0d : recalls.Average();
    }

    /// <summary>
    /// Creates a calibration summary.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <returns>The calibration summary.</returns>
    public static TimingCalibrationSummary Summarize(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        var ece = ExpectedCalibrationError(predictions, outcomes);
        return new TimingCalibrationSummary(
            predictions.Count,
            BrierScore(predictions, outcomes),
            LogLoss(predictions, outcomes),
            ece,
            BalancedAccuracy(predictions, outcomes),
            ece <= 0.06d ? "Good" : ece <= 0.12d ? "Fair" : "Weak",
            PerClassCalibration(predictions, outcomes),
            ReliabilityBins(predictions, outcomes),
            DecomposeBrier(predictions, outcomes));
    }

    /// <summary>
    /// Calculates one-vs-rest ECE values for each timing class.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <param name="binCount">The number of probability bins.</param>
    /// <returns>Classwise calibration summaries.</returns>
    public static IReadOnlyList<TimingClassCalibrationSummary> PerClassCalibration(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes,
        int binCount = 10)
    {
        EnsureAligned(predictions, outcomes);
        return Enumerable.Range(0, 3)
            .Select(klass => new TimingClassCalibrationSummary(
                ClassName(klass),
                ExpectedClassCalibrationError(predictions, outcomes, klass, binCount),
                predictions.Count))
            .ToArray();
    }

    /// <summary>
    /// Builds winner-class reliability-diagram bins.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <param name="binCount">The number of probability bins.</param>
    /// <returns>Reliability bins.</returns>
    public static IReadOnlyList<TimingReliabilityBin> ReliabilityBins(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes,
        int binCount = 10)
    {
        EnsureAligned(predictions, outcomes);
        var bins = new List<TimingReliabilityBin>(binCount);
        for (var bin = 0; bin < binCount; bin++)
        {
            var lower = bin / (double)binCount;
            var upper = (bin + 1) / (double)binCount;
            var members = predictions
                .Select((prediction, index) => new
                {
                    Prediction = prediction,
                    Outcome = outcomes[index],
                })
                .Where(item =>
                {
                    var confidence = item.Prediction.Probabilities[ArgMax(item.Prediction.Probabilities)];
                    return IsInBin(confidence, lower, upper, bin == binCount - 1);
                })
                .ToArray();
            if (members.Length == 0)
            {
                bins.Add(new TimingReliabilityBin(lower, upper, 0, null, null));
                continue;
            }

            bins.Add(new TimingReliabilityBin(
                lower,
                upper,
                members.Length,
                members.Average(item => item.Prediction.Probabilities[ArgMax(item.Prediction.Probabilities)]),
                members.Average(item => ArgMax(item.Prediction.Probabilities) == ToClass(item.Outcome) ? 1d : 0d)));
        }

        return bins;
    }

    /// <summary>
    /// Calculates an approximate multiclass Brier decomposition by confidence bins.
    /// </summary>
    /// <param name="predictions">Predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <param name="binCount">The number of probability bins.</param>
    /// <returns>The decomposition.</returns>
    public static TimingBrierDecomposition DecomposeBrier(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes,
        int binCount = 10)
    {
        EnsureAligned(predictions, outcomes);
        if (predictions.Count == 0)
        {
            return new TimingBrierDecomposition(0d, 0d, 0d);
        }

        var baseRates = new double[3];
        foreach (var outcome in outcomes)
        {
            baseRates[ToClass(outcome)]++;
        }

        for (var klass = 0; klass < 3; klass++)
        {
            baseRates[klass] /= outcomes.Count;
        }

        var reliability = 0d;
        var resolution = 0d;
        for (var bin = 0; bin < binCount; bin++)
        {
            var lower = bin / (double)binCount;
            var upper = (bin + 1) / (double)binCount;
            var members = predictions
                .Select((prediction, index) => new
                {
                    Prediction = prediction,
                    Outcome = outcomes[index],
                })
                .Where(item =>
                {
                    var confidence = item.Prediction.Probabilities[ArgMax(item.Prediction.Probabilities)];
                    return IsInBin(confidence, lower, upper, bin == binCount - 1);
                })
                .ToArray();
            if (members.Length == 0)
            {
                continue;
            }

            var forecastMean = new double[3];
            var observed = new double[3];
            foreach (var member in members)
            {
                for (var klass = 0; klass < 3; klass++)
                {
                    forecastMean[klass] += member.Prediction.Probabilities[klass];
                }

                observed[ToClass(member.Outcome)]++;
            }

            for (var klass = 0; klass < 3; klass++)
            {
                forecastMean[klass] /= members.Length;
                observed[klass] /= members.Length;
                reliability += (members.Length / (double)predictions.Count) *
                    Math.Pow(forecastMean[klass] - observed[klass], 2d);
                resolution += (members.Length / (double)predictions.Count) *
                    Math.Pow(observed[klass] - baseRates[klass], 2d);
            }
        }

        var uncertainty = baseRates.Sum(rate => rate * (1d - rate));
        return new TimingBrierDecomposition(reliability, resolution, uncertainty);
    }

    /// <summary>
    /// Converts a triple-barrier outcome to the classifier's class index.
    /// </summary>
    /// <param name="outcome">The realized triple-barrier outcome.</param>
    /// <returns>The classifier class index.</returns>
    internal static int ToClass(TripleBarrierOutcomeType outcome)
    {
        return outcome switch
        {
            TripleBarrierOutcomeType.UpperHitFirst => 0,
            TripleBarrierOutcomeType.LowerHitFirst => 1,
            TripleBarrierOutcomeType.NoBarrierHit => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unsupported outcome."),
        };
    }

    private static int ArgMax(IReadOnlyList<double> values)
    {
        var best = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] > values[best])
            {
                best = index;
            }
        }

        return best;
    }

    private static double ExpectedClassCalibrationError(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes,
        int klass,
        int binCount)
    {
        if (predictions.Count == 0)
        {
            return 0d;
        }

        var error = 0d;
        for (var bin = 0; bin < binCount; bin++)
        {
            var lower = bin / (double)binCount;
            var upper = (bin + 1) / (double)binCount;
            var members = predictions
                .Select((prediction, index) => new
                {
                    Prediction = prediction,
                    Outcome = outcomes[index],
                })
                .Where(item => IsInBin(item.Prediction.Probabilities[klass], lower, upper, bin == binCount - 1))
                .ToArray();
            if (members.Length == 0)
            {
                continue;
            }

            var predicted = members.Average(item => item.Prediction.Probabilities[klass]);
            var observed = members.Average(item => ToClass(item.Outcome) == klass ? 1d : 0d);
            error += (members.Length / (double)predictions.Count) * Math.Abs(predicted - observed);
        }

        return error;
    }

    private static bool IsInBin(double probability, double lower, double upper, bool isLastBin)
    {
        return probability >= lower && (probability < upper || (isLastBin && probability <= upper));
    }

    private static string ClassName(int klass)
    {
        return klass switch
        {
            0 => "UP",
            1 => "DOWN",
            2 => "NONE",
            _ => throw new ArgumentOutOfRangeException(nameof(klass), klass, "Unsupported class."),
        };
    }

    private static void EnsureAligned(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        ArgumentNullException.ThrowIfNull(outcomes);
        if (predictions.Count != outcomes.Count)
        {
            throw new ArgumentException("Predictions and outcomes must be aligned.");
        }
    }
}
