namespace Aletheia.Dynamics;

/// <summary>
/// Stores exponentially weighted volatility diagnostics.
/// </summary>
/// <param name="Lambda">The EWMA decay parameter.</param>
/// <param name="VariancePath">The conditional variance path.</param>
/// <param name="LastVariance">The final conditional variance estimate.</param>
/// <param name="LastVolatility">The square root of the final conditional variance.</param>
public sealed record EwmaVolatilityResult(
    double Lambda,
    IReadOnlyList<double> VariancePath,
    double LastVariance,
    double LastVolatility);
