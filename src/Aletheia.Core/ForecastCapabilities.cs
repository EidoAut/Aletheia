namespace Aletheia.Core;

/// <summary>
/// Describes which mathematical forecast quantities a model explicitly supports.
/// </summary>
/// <remarks>
/// Capabilities are not inferred from zero, empty, or nullable-looking values.
/// They are part of the scientific contract for deciding which validation
/// metrics may be calculated for a forecast.
/// </remarks>
[Flags]
public enum ForecastCapabilities
{
    /// <summary>
    /// No forecast quantity is supported.
    /// </summary>
    None = 0,

    /// <summary>
    /// The model exposes a principal point forecast.
    /// </summary>
    PointForecast = 1,

    /// <summary>
    /// The model exposes the conditional expected return estimate.
    /// </summary>
    ExpectedReturn = 2,

    /// <summary>
    /// The model exposes the conditional median return estimate.
    /// </summary>
    Median = 4,

    /// <summary>
    /// The model exposes a probability for a strictly positive return.
    /// </summary>
    ProbabilityPositive = 8,

    /// <summary>
    /// The model exposes finite return quantiles.
    /// </summary>
    Quantiles = 16,

    /// <summary>
    /// The model exposes a parametric or empirical distribution rich enough to
    /// justify distribution-level diagnostics beyond sparse quantiles.
    /// </summary>
    FullDistribution = 32,
}
