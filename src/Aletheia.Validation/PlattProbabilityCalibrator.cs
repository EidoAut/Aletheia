namespace Aletheia.Validation;

/// <summary>
/// Applies one-vs-rest Platt scaling to timing probabilities.
/// </summary>
public sealed class PlattProbabilityCalibrator
{
    private readonly double[] intercepts = new double[3];
    private readonly double[] slopes = new double[3];

    /// <summary>
    /// Fits one-vs-rest Platt scaling parameters.
    /// </summary>
    /// <param name="predictions">Raw predictions.</param>
    /// <param name="outcomes">Realized outcomes.</param>
    /// <returns>A fitted calibrator.</returns>
    public PlattProbabilityCalibrator Fit(
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        if (predictions.Count != outcomes.Count)
        {
            throw new ArgumentException("Predictions and outcomes must be aligned.");
        }

        if (predictions.Count < 30)
        {
            for (var klass = 0; klass < 3; klass++)
            {
                this.intercepts[klass] = 0d;
                this.slopes[klass] = 1d;
            }

            return this;
        }

        for (var klass = 0; klass < 3; klass++)
        {
            var intercept = 0d;
            var slope = 1d;
            for (var iteration = 0; iteration < 120; iteration++)
            {
                var gradientIntercept = 0d;
                var gradientSlope = 0d;
                for (var index = 0; index < predictions.Count; index++)
                {
                    var p = Math.Clamp(predictions[index].Probabilities[klass], 1e-6d, 1d - 1e-6d);
                    var x = Math.Log(p / (1d - p));
                    var y = TimingProbabilityMetrics.ToClass(outcomes[index]) == klass ? 1d : 0d;
                    var fitted = 1d / (1d + Math.Exp(-(intercept + (slope * x))));
                    gradientIntercept += fitted - y;
                    gradientSlope += (fitted - y) * x;
                }

                intercept -= 0.03d * gradientIntercept / predictions.Count;
                slope -= 0.03d * gradientSlope / predictions.Count;
            }

            this.intercepts[klass] = intercept;
            this.slopes[klass] = slope;
        }

        return this;
    }

    /// <summary>
    /// Calibrates a prediction.
    /// </summary>
    /// <param name="prediction">The raw prediction.</param>
    /// <returns>The calibrated prediction.</returns>
    public MarketEventPrediction Calibrate(MarketEventPrediction prediction)
    {
        ArgumentNullException.ThrowIfNull(prediction);
        var raw = prediction.Probabilities;
        var calibrated = new double[3];
        var sum = 0d;
        for (var klass = 0; klass < 3; klass++)
        {
            var p = Math.Clamp(raw[klass], 1e-6d, 1d - 1e-6d);
            var x = Math.Log(p / (1d - p));
            calibrated[klass] = 1d / (1d + Math.Exp(-(this.intercepts[klass] + (this.slopes[klass] * x))));
            sum += calibrated[klass];
        }

        if (sum <= 0d || !double.IsFinite(sum))
        {
            return prediction;
        }

        return new MarketEventPrediction(calibrated[0] / sum, calibrated[1] / sum, calibrated[2] / sum);
    }
}
