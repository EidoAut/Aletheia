namespace Aletheia.Mathematics;

/// <summary>
/// Stores the fitted coefficients of a univariate ordinary least squares model.
/// </summary>
public sealed class LinearRegressionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinearRegressionResult"/> class.
    /// </summary>
    /// <param name="intercept">The fitted intercept.</param>
    /// <param name="slope">The fitted slope.</param>
    /// <param name="rSquared">The coefficient of determination.</param>
    public LinearRegressionResult(double intercept, double slope, double rSquared)
    {
        this.Intercept = intercept;
        this.Slope = slope;
        this.RSquared = rSquared;
    }

    /// <summary>
    /// Gets the fitted intercept.
    /// </summary>
    public double Intercept { get; }

    /// <summary>
    /// Gets the fitted slope.
    /// </summary>
    public double Slope { get; }

    /// <summary>
    /// Gets the coefficient of determination.
    /// </summary>
    public double RSquared { get; }
}
