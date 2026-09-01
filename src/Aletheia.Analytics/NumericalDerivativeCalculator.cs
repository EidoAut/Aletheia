using Aletheia.TimeSeries;

namespace Aletheia.Analytics;

/// <summary>
/// Calculates observation-index numerical derivatives from smoothed financial signals.
/// </summary>
/// <remarks>
/// These values are signal features, not literal physical velocity or
/// acceleration. Milestone 1.1 uses observation-index derivatives: each adjacent
/// sample is one valuation observation, not one calendar day. Smoothing is
/// available because numerical differentiation amplifies high-frequency noise
/// in NAV observations.
/// </remarks>
public sealed class NumericalDerivativeCalculator
{
    private readonly TimeDomainFeatureCalculator featureCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="NumericalDerivativeCalculator"/> class.
    /// </summary>
    /// <param name="featureCalculator">The feature calculator used for smoothing.</param>
    public NumericalDerivativeCalculator(TimeDomainFeatureCalculator? featureCalculator = null)
    {
        this.featureCalculator = featureCalculator ?? new TimeDomainFeatureCalculator();
    }

    /// <summary>
    /// Calculates the first derivative per observation of a smoothed series.
    /// </summary>
    /// <param name="series">The input signal.</param>
    /// <param name="smoothingWindow">The moving-average smoothing window.</param>
    /// <returns>The first derivative per observation dated at each differenced observation.</returns>
    public TimeSeries<double> CalculateFirstDerivativePerObservation(TimeSeries<double> series, int smoothingWindow = 5)
    {
        ArgumentNullException.ThrowIfNull(series);

        var smoothed = smoothingWindow <= 1
            ? series
            : this.featureCalculator.CalculateMovingAverage(series, smoothingWindow);

        if (smoothed.Count < 2)
        {
            return new TimeSeries<double>(Array.Empty<TimeSeriesPoint<double>>(), series.ObservationFrequency);
        }

        var points = new List<TimeSeriesPoint<double>>(smoothed.Count - 1);
        for (var index = 1; index < smoothed.Count; index++)
        {
            points.Add(new TimeSeriesPoint<double>(
                smoothed[index].Date,
                smoothed[index].Value - smoothed[index - 1].Value));
        }

        return new TimeSeries<double>(points, smoothed.ObservationFrequency);
    }

    /// <summary>
    /// Calculates the second derivative per observation squared of a smoothed series.
    /// </summary>
    /// <param name="series">The input signal.</param>
    /// <param name="smoothingWindow">The moving-average smoothing window.</param>
    /// <returns>The second derivative per observation squared dated at each second-differenced observation.</returns>
    public TimeSeries<double> CalculateSecondDerivativePerObservationSquared(TimeSeries<double> series, int smoothingWindow = 5)
    {
        var firstDerivative = this.CalculateFirstDerivativePerObservation(series, smoothingWindow);
        return this.CalculateFirstDerivativePerObservation(firstDerivative, 1);
    }

    /// <summary>
    /// Calculates the first derivative per observation.
    /// </summary>
    /// <param name="series">The input signal.</param>
    /// <param name="smoothingWindow">The moving-average smoothing window.</param>
    /// <returns>The first derivative per observation.</returns>
    public TimeSeries<double> CalculateFirstDerivative(TimeSeries<double> series, int smoothingWindow = 5) =>
        this.CalculateFirstDerivativePerObservation(series, smoothingWindow);

    /// <summary>
    /// Calculates the second derivative per observation squared.
    /// </summary>
    /// <param name="series">The input signal.</param>
    /// <param name="smoothingWindow">The moving-average smoothing window.</param>
    /// <returns>The second derivative per observation squared.</returns>
    public TimeSeries<double> CalculateSecondDerivative(TimeSeries<double> series, int smoothingWindow = 5) =>
        this.CalculateSecondDerivativePerObservationSquared(series, smoothingWindow);
}
