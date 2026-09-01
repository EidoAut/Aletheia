using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Coordinates deterministic validation metric calculators.
/// </summary>
public sealed class ValidationMetricCalculator
{
    private static readonly int[] StandardPercentiles = [10, 25, 50, 75, 90];

    private readonly MeanAbsoluteErrorCalculator maeCalculator = new();
    private readonly MeanSquaredErrorCalculator mseCalculator = new();
    private readonly RootMeanSquaredErrorCalculator rmseCalculator = new();
    private readonly DirectionalAccuracyCalculator directionalAccuracyCalculator = new();
    private readonly CalibrationCalculator calibrationCalculator = new();
    private readonly PinballLossCalculator pinballLossCalculator = new();
    private readonly IntervalCoverageCalculator intervalCoverageCalculator = new();

    /// <summary>
    /// Calculates the full metric summary for evaluated forecasts.
    /// </summary>
    /// <param name="samples">The evaluated prediction samples.</param>
    /// <param name="calibrationOptions">Calibration options.</param>
    /// <returns>The metric summary.</returns>
    public MetricSummary Calculate(
        IReadOnlyList<ForecastEvaluationSample> samples,
        CalibrationOptions calibrationOptions)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var pointSamples = samples
            .Where(sample => sample.Prediction.Prediction.Supports(ForecastCapabilities.PointForecast))
            .ToArray();
        var pointEvaluations = pointSamples.Select(sample => sample.Evaluation).ToArray();
        var point = new PointForecastMetrics(
            ResolveStatus(samples.Count, pointSamples.Length),
            pointEvaluations.Length,
            this.maeCalculator.Calculate(pointEvaluations),
            this.mseCalculator.Calculate(pointEvaluations),
            this.rmseCalculator.Calculate(pointEvaluations),
            this.directionalAccuracyCalculator.Calculate(pointEvaluations));
        var probability = this.calibrationCalculator.Calculate(samples, calibrationOptions);
        var losses = new Dictionary<int, double>();
        var counts = new Dictionary<int, int>();
        foreach (var percentile in StandardPercentiles)
        {
            var (meanLoss, count) = this.pinballLossCalculator.Calculate(samples, percentile);
            if (meanLoss.HasValue)
            {
                losses[percentile] = meanLoss.Value;
                counts[percentile] = count;
            }
        }

        return new MetricSummary(
            point,
            probability,
            new QuantileForecastMetrics(
                ResolveStatus(samples.Count, counts.Values.DefaultIfEmpty(0).Max()),
                losses,
                counts),
            this.intervalCoverageCalculator.Calculate(samples));
    }

    private static MetricStatus ResolveStatus(int totalSamples, int supportedSamples)
    {
        if (supportedSamples > 0)
        {
            return MetricStatus.Available;
        }

        return totalSamples == 0 ? MetricStatus.NoSamples : MetricStatus.NotSupported;
    }
}
