namespace Aletheia.Dynamics;

/// <summary>
/// Stores a constrained GARCH(1,1) fit.
/// </summary>
/// <param name="Omega">The positive variance intercept.</param>
/// <param name="Alpha">The lagged innovation loading.</param>
/// <param name="Beta">The lagged variance loading.</param>
/// <param name="LogLikelihood">The Gaussian log likelihood.</param>
/// <param name="Converged">A value indicating whether the constrained search found a valid optimum.</param>
/// <param name="Diagnostic">A human-readable convergence diagnostic.</param>
/// <param name="ConditionalVariances">The conditional variance path.</param>
/// <param name="Mean">The mean removed from returns before fitting.</param>
public sealed record Garch11FitResult(
    double Omega,
    double Alpha,
    double Beta,
    double LogLikelihood,
    bool Converged,
    string Diagnostic,
    IReadOnlyList<double> ConditionalVariances,
    double Mean = 0d)
{
    /// <summary>
    /// Gets persistence alpha + beta.
    /// </summary>
    public double Persistence => this.Alpha + this.Beta;

    /// <summary>
    /// Gets the last conditional volatility estimate.
    /// </summary>
    public double LastVolatility => this.ConditionalVariances.Count == 0
        ? 0d
        : Math.Sqrt(Math.Max(0d, this.ConditionalVariances[^1]));

    /// <summary>
    /// Advances the conditional variance one step with fixed fitted parameters.
    /// </summary>
    /// <param name="previousObservation">The latest observed raw return.</param>
    /// <param name="previousConditionalVariance">The previous conditional variance.</param>
    /// <returns>The next conditional variance.</returns>
    public double NextConditionalVariance(double previousObservation, double previousConditionalVariance)
    {
        var residual = previousObservation - this.Mean;
        return Math.Max(1e-12d, this.Omega + (this.Alpha * residual * residual) + (this.Beta * previousConditionalVariance));
    }
}
