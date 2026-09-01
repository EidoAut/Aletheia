#pragma warning disable SA1402 // The fitted classifier is kept with its trainer for readability.

namespace Aletheia.Validation;

/// <summary>
/// Fits a deterministic L2-regularized multinomial logistic event classifier.
/// </summary>
public sealed class MarketEventClassifier
{
    private const int ClassCount = 3;
    private readonly MarketEventClassifierOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketEventClassifier"/> class.
    /// </summary>
    /// <param name="options">Classifier options.</param>
    public MarketEventClassifier(MarketEventClassifierOptions? options = null)
    {
        this.options = options ?? new MarketEventClassifierOptions();
        if (!double.IsFinite(this.options.L2Regularization) || this.options.L2Regularization < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(options), this.options.L2Regularization, "L2 regularization must be finite and non-negative.");
        }

        if (!double.IsFinite(this.options.LearningRate) || this.options.LearningRate <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(options), this.options.LearningRate, "Learning rate must be positive and finite.");
        }

        if (this.options.MaxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), this.options.MaxIterations, "MaxIterations must be positive.");
        }

        if (!double.IsFinite(this.options.Tolerance) || this.options.Tolerance <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(options), this.options.Tolerance, "Tolerance must be positive and finite.");
        }
    }

    /// <summary>
    /// Fits the classifier.
    /// </summary>
    /// <param name="features">Feature vectors.</param>
    /// <param name="labels">Triple-barrier labels aligned to features.</param>
    /// <param name="featureNames">The deterministic feature order.</param>
    /// <returns>The fitted classifier.</returns>
    public MarketEventClassifierFit Fit(
        IReadOnlyList<MarketTimingFeatureVector> features,
        IReadOnlyList<TripleBarrierOutcome> labels,
        IReadOnlyList<string> featureNames)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(featureNames);
        var aligned = Align(features, labels);
        if (aligned.Count == 0)
        {
            return MarketEventClassifierFit.Failure(
                featureNames,
                MarketEventClassifierFitStatus.InsufficientData,
                "No aligned timing features and labels were available.");
        }

        var usableFeatureNames = featureNames
            .Where(name => aligned.All(sample => sample.Feature.HasFeature(name)))
            .ToArray();
        if (usableFeatureNames.Length == 0)
        {
            return MarketEventClassifierFit.Failure(
                featureNames,
                MarketEventClassifierFitStatus.InsufficientData,
                "No causally available feature was present for every aligned sample.");
        }

        var classCounts = new int[ClassCount];
        foreach (var sample in aligned)
        {
            classCounts[ToClass(sample.Label.Outcome)]++;
        }

        if (classCounts.Any(count => count < this.options.MinimumSamplesPerClass))
        {
            return MarketEventClassifierFit.Failure(
                usableFeatureNames,
                MarketEventClassifierFitStatus.InsufficientData,
                "At least one event class has insufficient samples.");
        }

        var dimension = usableFeatureNames.Length;
        var means = new double[dimension];
        var scales = new double[dimension];
        for (var featureIndex = 0; featureIndex < dimension; featureIndex++)
        {
            var name = usableFeatureNames[featureIndex];
            means[featureIndex] = aligned.Average(sample => Value(sample.Feature, name));
            var variance = aligned.Sum(sample =>
            {
                var deviation = Value(sample.Feature, name) - means[featureIndex];
                return deviation * deviation;
            }) / Math.Max(1, aligned.Count - 1);
            scales[featureIndex] = Math.Sqrt(Math.Max(1e-12d, variance));
        }

        var weights = new double[ClassCount, dimension + 1];
        var n = aligned.Count;
        var previousLoss = double.PositiveInfinity;
        var finalLoss = double.PositiveInfinity;
        var finalGradientNorm = double.PositiveInfinity;
        for (var iteration = 0; iteration < this.options.MaxIterations; iteration++)
        {
            var objective = EvaluateObjective(aligned, usableFeatureNames, means, scales, weights, this.options.L2Regularization);
            if (!double.IsFinite(objective.Loss) || !double.IsFinite(objective.GradientNorm))
            {
                return MarketEventClassifierFit.Failure(
                    usableFeatureNames,
                    MarketEventClassifierFitStatus.NumericalFailure,
                    "Multinomial logistic regression produced a non-finite loss or gradient.");
            }

            finalLoss = objective.Loss;
            finalGradientNorm = objective.GradientNorm;
            if (objective.GradientNorm < this.options.Tolerance ||
                (double.IsFinite(previousLoss) && Math.Abs(previousLoss - objective.Loss) < this.options.Tolerance))
            {
                return MarketEventClassifierFit.Success(
                    usableFeatureNames,
                    weights,
                    means,
                    scales,
                    MarketEventClassifierFitStatus.Converged,
                    iteration,
                    objective.Loss,
                    objective.GradientNorm);
            }

            previousLoss = objective.Loss;
            for (var klass = 0; klass < ClassCount; klass++)
            {
                weights[klass, 0] -= this.options.LearningRate * objective.Gradient[klass, 0];
                for (var featureIndex = 0; featureIndex < dimension; featureIndex++)
                {
                    weights[klass, featureIndex + 1] -= this.options.LearningRate * objective.Gradient[klass, featureIndex + 1];
                }
            }
        }

        return MarketEventClassifierFit.Fitted(
            false,
            usableFeatureNames,
            weights,
            means,
            scales,
            MarketEventClassifierFitStatus.MaxIterationsReached,
            this.options.MaxIterations,
            finalLoss,
            finalGradientNorm,
            "Multinomial logistic regression reached MaxIterations before convergence.");
    }

    private static OptimizationStep EvaluateObjective(
        IReadOnlyList<AlignedSample> aligned,
        IReadOnlyList<string> featureNames,
        IReadOnlyList<double> means,
        IReadOnlyList<double> scales,
        double[,] weights,
        double l2Regularization)
    {
        var dimension = featureNames.Count;
        var gradient = new double[ClassCount, dimension + 1];
        var loss = 0d;
        foreach (var sample in aligned)
        {
            var vector = Standardize(sample.Feature, featureNames, means, scales);
            var probabilities = PredictRaw(weights, vector);
            var actual = ToClass(sample.Label.Outcome);
            loss -= Math.Log(Math.Clamp(probabilities[actual], 1e-12d, 1d));
            for (var klass = 0; klass < ClassCount; klass++)
            {
                var error = probabilities[klass] - (klass == actual ? 1d : 0d);
                gradient[klass, 0] += error;
                for (var featureIndex = 0; featureIndex < dimension; featureIndex++)
                {
                    gradient[klass, featureIndex + 1] += error * vector[featureIndex];
                }
            }
        }

        var n = aligned.Count;
        loss /= n;
        for (var klass = 0; klass < ClassCount; klass++)
        {
            gradient[klass, 0] /= n;
            for (var featureIndex = 0; featureIndex < dimension; featureIndex++)
            {
                var parameter = weights[klass, featureIndex + 1];
                loss += 0.5d * l2Regularization * parameter * parameter;
                gradient[klass, featureIndex + 1] = (gradient[klass, featureIndex + 1] / n) + (l2Regularization * parameter);
            }
        }

        var gradientNorm = 0d;
        for (var klass = 0; klass < ClassCount; klass++)
        {
            for (var parameterIndex = 0; parameterIndex < dimension + 1; parameterIndex++)
            {
                gradientNorm += gradient[klass, parameterIndex] * gradient[klass, parameterIndex];
            }
        }

        return new OptimizationStep(loss, gradient, Math.Sqrt(gradientNorm));
    }

    private static IReadOnlyList<AlignedSample> Align(
        IReadOnlyList<MarketTimingFeatureVector> features,
        IReadOnlyList<TripleBarrierOutcome> labels)
    {
        var labelsByIndex = labels.ToDictionary(label => label.StartIndex);
        var aligned = new List<AlignedSample>();
        foreach (var feature in features)
        {
            if (labelsByIndex.TryGetValue(feature.ObservationIndex, out var label))
            {
                aligned.Add(new AlignedSample(feature, label));
            }
        }

        return aligned;
    }

    private static int ToClass(TripleBarrierOutcomeType outcome)
    {
        return outcome switch
        {
            TripleBarrierOutcomeType.UpperHitFirst => 0,
            TripleBarrierOutcomeType.LowerHitFirst => 1,
            TripleBarrierOutcomeType.NoBarrierHit => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unsupported outcome."),
        };
    }

    private static double Value(MarketTimingFeatureVector feature, string name)
    {
        return feature.TryGetFeature(name, out var value) ? value : 0d;
    }

    private static double[] Standardize(
        MarketTimingFeatureVector feature,
        IReadOnlyList<string> featureNames,
        IReadOnlyList<double> means,
        IReadOnlyList<double> scales)
    {
        var values = new double[featureNames.Count];
        for (var index = 0; index < featureNames.Count; index++)
        {
            values[index] = (Value(feature, featureNames[index]) - means[index]) / scales[index];
        }

        return values;
    }

    private static double[] PredictRaw(double[,] weights, IReadOnlyList<double> vector)
    {
        var logits = new double[ClassCount];
        var max = double.NegativeInfinity;
        for (var klass = 0; klass < ClassCount; klass++)
        {
            var value = weights[klass, 0];
            for (var index = 0; index < vector.Count; index++)
            {
                value += weights[klass, index + 1] * vector[index];
            }

            logits[klass] = value;
            max = Math.Max(max, value);
        }

        var sum = 0d;
        for (var klass = 0; klass < ClassCount; klass++)
        {
            logits[klass] = Math.Exp(logits[klass] - max);
            sum += logits[klass];
        }

        if (sum <= 0d || !double.IsFinite(sum))
        {
            return [1d / 3d, 1d / 3d, 1d / 3d];
        }

        for (var klass = 0; klass < ClassCount; klass++)
        {
            logits[klass] /= sum;
        }

        return logits;
    }

    private sealed record AlignedSample(MarketTimingFeatureVector Feature, TripleBarrierOutcome Label);

    private sealed record OptimizationStep(double Loss, double[,] Gradient, double GradientNorm);
}

/// <summary>
/// Stores a fitted market-event classifier.
/// </summary>
public sealed class MarketEventClassifierFit
{
    private readonly IReadOnlyList<string> featureNames;
    private readonly double[,] weights;
    private readonly IReadOnlyList<double> means;
    private readonly IReadOnlyList<double> scales;

    private MarketEventClassifierFit(
        bool isSuccess,
        IReadOnlyList<string> featureNames,
        double[,] weights,
        IReadOnlyList<double> means,
        IReadOnlyList<double> scales,
        MarketEventClassifierFitStatus status,
        int iterationsCompleted,
        double finalLoss,
        double gradientNorm,
        string diagnostic)
    {
        this.IsSuccess = isSuccess;
        this.featureNames = featureNames;
        this.weights = weights;
        this.means = means;
        this.scales = scales;
        this.Status = status;
        this.IterationsCompleted = iterationsCompleted;
        this.FinalLoss = finalLoss;
        this.GradientNorm = gradientNorm;
        this.Diagnostic = diagnostic;
    }

    /// <summary>
    /// Gets a value indicating whether the fit succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the structured fit status.
    /// </summary>
    public MarketEventClassifierFitStatus Status { get; }

    /// <summary>
    /// Gets the number of completed optimization iterations.
    /// </summary>
    public int IterationsCompleted { get; }

    /// <summary>
    /// Gets the final objective value, when available.
    /// </summary>
    public double FinalLoss { get; }

    /// <summary>
    /// Gets the final gradient norm, when available.
    /// </summary>
    public double GradientNorm { get; }

    /// <summary>
    /// Gets the fit diagnostic.
    /// </summary>
    public string Diagnostic { get; }

    /// <summary>
    /// Creates a successful classifier fit.
    /// </summary>
    /// <param name="featureNames">The feature names.</param>
    /// <param name="weights">The model weights.</param>
    /// <param name="means">The feature means.</param>
    /// <param name="scales">The feature scales.</param>
    /// <param name="status">The structured fit status.</param>
    /// <param name="iterationsCompleted">The number of completed optimization iterations.</param>
    /// <param name="finalLoss">The final objective value.</param>
    /// <param name="gradientNorm">The final gradient norm.</param>
    /// <returns>The fit.</returns>
    public static MarketEventClassifierFit Success(
        IReadOnlyList<string> featureNames,
        double[,] weights,
        IReadOnlyList<double> means,
        IReadOnlyList<double> scales,
        MarketEventClassifierFitStatus status = MarketEventClassifierFitStatus.Converged,
        int iterationsCompleted = 0,
        double finalLoss = 0d,
        double gradientNorm = 0d) =>
        Fitted(
            true,
            featureNames,
            weights,
            means,
            scales,
            status,
            iterationsCompleted,
            finalLoss,
            gradientNorm,
            "Regularized multinomial logistic regression converged.");

    /// <summary>
    /// Creates a fitted classifier record with explicit status.
    /// </summary>
    /// <param name="isSuccess">Whether the fitted model is usable for prediction.</param>
    /// <param name="featureNames">The feature names.</param>
    /// <param name="weights">The model weights.</param>
    /// <param name="means">The feature means.</param>
    /// <param name="scales">The feature scales.</param>
    /// <param name="status">The structured fit status.</param>
    /// <param name="iterationsCompleted">The iteration count.</param>
    /// <param name="finalLoss">The final loss.</param>
    /// <param name="gradientNorm">The final gradient norm.</param>
    /// <param name="diagnostic">The diagnostic.</param>
    /// <returns>The fit.</returns>
    public static MarketEventClassifierFit Fitted(
        bool isSuccess,
        IReadOnlyList<string> featureNames,
        double[,] weights,
        IReadOnlyList<double> means,
        IReadOnlyList<double> scales,
        MarketEventClassifierFitStatus status,
        int iterationsCompleted,
        double finalLoss,
        double gradientNorm,
        string diagnostic) =>
        new(
            isSuccess,
            featureNames.ToArray(),
            Copy(weights),
            means.ToArray(),
            scales.ToArray(),
            status,
            iterationsCompleted,
            finalLoss,
            gradientNorm,
            diagnostic);

    /// <summary>
    /// Creates a failed classifier fit.
    /// </summary>
    /// <param name="featureNames">The feature names.</param>
    /// <param name="status">The structured failure status.</param>
    /// <param name="diagnostic">The failure diagnostic.</param>
    /// <returns>The fit.</returns>
    public static MarketEventClassifierFit Failure(
        IReadOnlyList<string> featureNames,
        MarketEventClassifierFitStatus status,
        string diagnostic) =>
        new(
            false,
            featureNames.ToArray(),
            new double[3, featureNames.Count + 1],
            Array.Empty<double>(),
            Array.Empty<double>(),
            status,
            0,
            double.NaN,
            double.NaN,
            diagnostic);

    /// <summary>
    /// Predicts event probabilities for one feature vector.
    /// </summary>
    /// <param name="feature">The feature vector.</param>
    /// <returns>Event probabilities.</returns>
    public MarketEventPrediction Predict(MarketTimingFeatureVector feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (!this.IsSuccess)
        {
            return new MarketEventPrediction(1d / 3d, 1d / 3d, 1d / 3d);
        }

        var vector = new double[this.featureNames.Count];
        for (var index = 0; index < this.featureNames.Count; index++)
        {
            if (!feature.TryGetFeature(this.featureNames[index], out var raw))
            {
                return new MarketEventPrediction(1d / 3d, 1d / 3d, 1d / 3d);
            }

            vector[index] = (raw - this.means[index]) / this.scales[index];
        }

        var probabilities = PredictRaw(this.weights, vector);
        return new MarketEventPrediction(probabilities[0], probabilities[1], probabilities[2]);
    }

    private static double[,] Copy(double[,] source)
    {
        var result = new double[source.GetLength(0), source.GetLength(1)];
        Array.Copy(source, result, source.Length);
        return result;
    }

    private static double[] PredictRaw(double[,] weights, IReadOnlyList<double> vector)
    {
        var logits = new double[3];
        var max = double.NegativeInfinity;
        for (var klass = 0; klass < 3; klass++)
        {
            var value = weights[klass, 0];
            for (var index = 0; index < vector.Count; index++)
            {
                value += weights[klass, index + 1] * vector[index];
            }

            logits[klass] = value;
            max = Math.Max(max, value);
        }

        var sum = 0d;
        for (var klass = 0; klass < 3; klass++)
        {
            logits[klass] = Math.Exp(logits[klass] - max);
            sum += logits[klass];
        }

        if (sum <= 0d || !double.IsFinite(sum))
        {
            return [1d / 3d, 1d / 3d, 1d / 3d];
        }

        for (var klass = 0; klass < 3; klass++)
        {
            logits[klass] /= sum;
        }

        return logits;
    }
}
