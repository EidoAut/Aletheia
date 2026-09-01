#pragma warning disable SA1204 // Factory method follows the distribution surface for readability.

using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Forecasting;

/// <summary>
/// Represents a return forecast summary for one horizon.
/// </summary>
public sealed class ForecastDistribution
{
    private readonly IReadOnlyDictionary<int, double> percentiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastDistribution"/> class.
    /// </summary>
    /// <param name="horizonResolution">The resolved forecast horizon.</param>
    /// <param name="expectedReturn">The expected return.</param>
    /// <param name="medianReturn">The median return.</param>
    /// <param name="percentiles">The selected return percentiles.</param>
    /// <param name="probabilityPositive">The probability of a positive return.</param>
    /// <param name="probabilityReturnGreaterThanFivePercent">The probability of a return above five percent.</param>
    /// <param name="probabilityLossGreaterThanTenPercent">The probability of a loss greater than ten percent.</param>
    /// <param name="capabilities">The forecast quantities explicitly supported.</param>
    /// <param name="pointForecastStatistic">The statistic represented by the point forecast.</param>
    /// <param name="pointForecastReturn">The optional explicit point forecast value.</param>
    public ForecastDistribution(
        ForecastHorizonResolution horizonResolution,
        double expectedReturn,
        double medianReturn,
        IReadOnlyDictionary<int, double> percentiles,
        double probabilityPositive,
        double probabilityReturnGreaterThanFivePercent,
        double probabilityLossGreaterThanTenPercent,
        ForecastCapabilities capabilities = ForecastCapabilities.PointForecast |
            ForecastCapabilities.ExpectedReturn |
            ForecastCapabilities.Median |
            ForecastCapabilities.ProbabilityPositive |
            ForecastCapabilities.Quantiles,
        PointForecastStatistic pointForecastStatistic = PointForecastStatistic.Mean,
        double? pointForecastReturn = null)
    {
        this.HorizonResolution = horizonResolution ?? throw new ArgumentNullException(nameof(horizonResolution));
        this.ExpectedReturn = expectedReturn;
        this.MedianReturn = medianReturn;
        this.percentiles = new Dictionary<int, double>(percentiles ?? throw new ArgumentNullException(nameof(percentiles)));
        this.ProbabilityPositive = probabilityPositive;
        this.ProbabilityReturnGreaterThanFivePercent = probabilityReturnGreaterThanFivePercent;
        this.ProbabilityLossGreaterThanTenPercent = probabilityLossGreaterThanTenPercent;
        this.Capabilities = capabilities;
        this.PointForecastStatistic = pointForecastStatistic;
        this.PointForecastReturn = pointForecastReturn ?? SelectPointForecast(
            expectedReturn,
            medianReturn,
            pointForecastStatistic);
    }

    /// <summary>
    /// Gets the forecast horizon.
    /// </summary>
    public ForecastHorizon RequestedHorizon => this.HorizonResolution.RequestedHorizon;

    /// <summary>
    /// Gets the resolved horizon metadata used by the forecast.
    /// </summary>
    public ForecastHorizonResolution HorizonResolution { get; }

    /// <summary>
    /// Gets the expected return.
    /// </summary>
    public double ExpectedReturn { get; }

    /// <summary>
    /// Gets the expected return when explicitly supported; otherwise <see langword="null"/>.
    /// </summary>
    public double? ExpectedReturnOrNull => this.Supports(ForecastCapabilities.ExpectedReturn)
        ? this.ExpectedReturn
        : null;

    /// <summary>
    /// Gets the principal point forecast return.
    /// </summary>
    public double PointForecastReturn { get; }

    /// <summary>
    /// Gets the point forecast when explicitly supported; otherwise <see langword="null"/>.
    /// </summary>
    public double? PointForecastReturnOrNull => this.Supports(ForecastCapabilities.PointForecast)
        ? this.PointForecastReturn
        : null;

    /// <summary>
    /// Gets the forecast quantities explicitly supported.
    /// </summary>
    public ForecastCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the statistic represented by <see cref="PointForecastReturn"/>.
    /// </summary>
    public PointForecastStatistic PointForecastStatistic { get; }

    /// <summary>
    /// Gets the median return.
    /// </summary>
    public double MedianReturn { get; }

    /// <summary>
    /// Gets the median return when explicitly supported; otherwise <see langword="null"/>.
    /// </summary>
    public double? MedianReturnOrNull => this.Supports(ForecastCapabilities.Median)
        ? this.MedianReturn
        : null;

    /// <summary>
    /// Gets selected return percentiles.
    /// </summary>
    public IReadOnlyDictionary<int, double> Percentiles => this.percentiles;

    /// <summary>
    /// Gets the probability of a positive return.
    /// </summary>
    public double ProbabilityPositive { get; }

    /// <summary>
    /// Gets the positive-return probability when explicitly supported; otherwise <see langword="null"/>.
    /// </summary>
    public double? ProbabilityPositiveOrNull => this.Supports(ForecastCapabilities.ProbabilityPositive)
        ? this.ProbabilityPositive
        : null;

    /// <summary>
    /// Gets the probability of a return above five percent.
    /// </summary>
    public double ProbabilityReturnGreaterThanFivePercent { get; }

    /// <summary>
    /// Gets the probability of a return above five percent when explicitly supported; otherwise <see langword="null"/>.
    /// </summary>
    public double? ProbabilityReturnGreaterThanFivePercentOrNull => this.Supports(ForecastCapabilities.ProbabilityPositive)
        ? this.ProbabilityReturnGreaterThanFivePercent
        : null;

    /// <summary>
    /// Gets the probability of a loss greater than ten percent.
    /// </summary>
    public double ProbabilityLossGreaterThanTenPercent { get; }

    /// <summary>
    /// Gets the probability of a loss greater than ten percent when explicitly supported; otherwise <see langword="null"/>.
    /// </summary>
    public double? ProbabilityLossGreaterThanTenPercentOrNull => this.Supports(ForecastCapabilities.ProbabilityPositive)
        ? this.ProbabilityLossGreaterThanTenPercent
        : null;

    /// <summary>
    /// Determines whether the distribution supports a required capability.
    /// </summary>
    /// <param name="capability">The required capability.</param>
    /// <returns><see langword="true"/> when the capability is supported.</returns>
    public bool Supports(ForecastCapabilities capability) =>
        (this.Capabilities & capability) == capability;

    /// <summary>
    /// Creates a distribution summary from simulated or historical samples.
    /// </summary>
    /// <param name="horizonResolution">The resolved forecast horizon.</param>
    /// <param name="samples">The return samples.</param>
    /// <returns>A forecast distribution.</returns>
    public static ForecastDistribution FromSamples(
        ForecastHorizonResolution horizonResolution,
        IReadOnlyList<double> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Count == 0)
        {
            return new ForecastDistribution(
                horizonResolution,
                0d,
                0d,
                new Dictionary<int, double>(),
                0d,
                0d,
                0d,
                ForecastCapabilities.None,
                PointForecastStatistic.None,
                0d);
        }

        var percentiles = new Dictionary<int, double>
        {
            [10] = DescriptiveStatistics.Percentile(samples, 10d),
            [25] = DescriptiveStatistics.Percentile(samples, 25d),
            [50] = DescriptiveStatistics.Percentile(samples, 50d),
            [75] = DescriptiveStatistics.Percentile(samples, 75d),
            [90] = DescriptiveStatistics.Percentile(samples, 90d),
        };

        var mean = DescriptiveStatistics.Mean(samples);
        var capabilities = ForecastCapabilities.PointForecast |
            ForecastCapabilities.ExpectedReturn |
            ForecastCapabilities.Median |
            ForecastCapabilities.ProbabilityPositive |
            ForecastCapabilities.Quantiles;

        return new ForecastDistribution(
            horizonResolution,
            mean,
            percentiles[50],
            percentiles,
            samples.Count(value => value > 0d) / (double)samples.Count,
            samples.Count(value => value > 0.05d) / (double)samples.Count,
            samples.Count(value => value < -0.10d) / (double)samples.Count,
            capabilities,
            PointForecastStatistic.Mean,
            mean);
    }

    private static double SelectPointForecast(
        double expectedReturn,
        double medianReturn,
        PointForecastStatistic statistic)
    {
        return statistic switch
        {
            PointForecastStatistic.None => 0d,
            PointForecastStatistic.ExplicitModelPoint => medianReturn,
            PointForecastStatistic.Mean => expectedReturn,
            PointForecastStatistic.Median => medianReturn,
            _ => throw new ArgumentOutOfRangeException(nameof(statistic), statistic, "Unsupported point forecast statistic."),
        };
    }
}
