using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Represents a simple dynamic-model forecast summary.
/// </summary>
public sealed class DynamicForecast
{
    private readonly IReadOnlyDictionary<int, double> simpleReturnQuantiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicForecast"/> class.
    /// </summary>
    /// <param name="horizon">The forecast horizon.</param>
    /// <param name="cumulativeExpectedLogReturn">The expected cumulative log return over the horizon.</param>
    /// <param name="medianSimpleReturn">The median simple cumulative return under Gaussian log-return assumptions.</param>
    /// <param name="expectedSimpleReturn">The expected simple cumulative return under Gaussian log-return assumptions.</param>
    /// <param name="cumulativeLogReturnVariance">The cumulative log-return forecast-error variance.</param>
    /// <param name="effectiveObservationCount">The number of recursive AR steps.</param>
    /// <param name="isModelStationary">A value indicating whether the fitted AR process is stationary.</param>
    /// <param name="simpleReturnQuantiles">The simple-return quantiles under Gaussian log-return assumptions.</param>
    public DynamicForecast(
        ForecastHorizon horizon,
        double cumulativeExpectedLogReturn,
        double medianSimpleReturn,
        double expectedSimpleReturn,
        double cumulativeLogReturnVariance,
        int effectiveObservationCount,
        bool isModelStationary,
        IReadOnlyDictionary<int, double> simpleReturnQuantiles)
    {
        this.Horizon = horizon;
        this.CumulativeExpectedLogReturn = cumulativeExpectedLogReturn;
        this.MedianSimpleReturn = medianSimpleReturn;
        this.PointForecastSimpleReturn = medianSimpleReturn;
        this.ExpectedSimpleReturn = expectedSimpleReturn;
        this.CumulativeLogReturnVariance = cumulativeLogReturnVariance;
        this.EffectiveObservationCount = effectiveObservationCount;
        this.IsModelStationary = isModelStationary;
        this.simpleReturnQuantiles = new Dictionary<int, double>(
            simpleReturnQuantiles ?? throw new ArgumentNullException(nameof(simpleReturnQuantiles)));
    }

    /// <summary>
    /// Gets the forecast horizon.
    /// </summary>
    public ForecastHorizon Horizon { get; }

    /// <summary>
    /// Gets the transformed point forecast <c>exp(E[X]) - 1</c>.
    /// </summary>
    public double PointForecastSimpleReturn { get; }

    /// <summary>
    /// Gets the median simple return under Gaussian cumulative log-return assumptions.
    /// </summary>
    public double MedianSimpleReturn { get; }

    /// <summary>
    /// Gets the expected simple return under Gaussian cumulative log-return assumptions.
    /// </summary>
    public double ExpectedSimpleReturn { get; }

    /// <summary>
    /// Gets the expected cumulative log return over the horizon.
    /// </summary>
    public double CumulativeExpectedLogReturn { get; }

    /// <summary>
    /// Gets the cumulative log-return forecast-error variance.
    /// </summary>
    public double CumulativeLogReturnVariance { get; }

    /// <summary>
    /// Gets the cumulative log-return forecast-error standard deviation.
    /// </summary>
    public double CumulativeLogReturnStandardDeviation => Math.Sqrt(Math.Max(0d, this.CumulativeLogReturnVariance));

    /// <summary>
    /// Gets the number of recursive AR steps.
    /// </summary>
    public int EffectiveObservationCount { get; }

    /// <summary>
    /// Gets a value indicating whether the fitted AR process is stationary.
    /// </summary>
    public bool IsModelStationary { get; }

    /// <summary>
    /// Gets simple-return quantiles under Gaussian cumulative log-return assumptions.
    /// </summary>
    public IReadOnlyDictionary<int, double> SimpleReturnQuantiles => this.simpleReturnQuantiles;
}
