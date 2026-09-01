namespace Aletheia.Mathematics;

/// <summary>
/// Controls the first-component PCA calculation.
/// </summary>
public sealed record PcaOptions
{
    /// <summary>
    /// Gets the maximum number of power-iteration steps.
    /// </summary>
    public int MaximumIterations { get; init; } = 500;

    /// <summary>
    /// Gets the convergence tolerance for the loading vector.
    /// </summary>
    public double Tolerance { get; init; } = 1e-10;

    /// <summary>
    /// Gets a value indicating whether each feature should be standardized before PCA.
    /// </summary>
    public bool Standardize { get; init; } = true;
}
