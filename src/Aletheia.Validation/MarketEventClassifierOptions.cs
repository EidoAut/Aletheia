namespace Aletheia.Validation;

/// <summary>
/// Configures the regularized multinomial event classifier.
/// </summary>
public sealed record MarketEventClassifierOptions
{
    /// <summary>
    /// Gets the L2 regularization strength.
    /// </summary>
    public double L2Regularization { get; init; } = 0.05d;

    /// <summary>
    /// Gets the learning rate.
    /// </summary>
    public double LearningRate { get; init; } = 0.08d;

    /// <summary>
    /// Gets the maximum gradient iterations.
    /// </summary>
    public int MaxIterations { get; init; } = 180;

    /// <summary>
    /// Gets the legacy alias for <see cref="MaxIterations"/>.
    /// </summary>
    public int Iterations
    {
        get => this.MaxIterations;
        init => this.MaxIterations = value;
    }

    /// <summary>
    /// Gets the convergence tolerance applied to loss improvement and gradient norm.
    /// </summary>
    public double Tolerance { get; init; } = 1e-5d;

    /// <summary>
    /// Gets the minimum samples per class required to fit.
    /// </summary>
    public int MinimumSamplesPerClass { get; init; } = 3;
}
