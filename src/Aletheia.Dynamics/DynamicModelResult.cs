namespace Aletheia.Dynamics;

/// <summary>
/// Stores fitted dynamic-model parameters and diagnostics.
/// </summary>
public sealed class DynamicModelResult
{
    private readonly IReadOnlyDictionary<string, double> parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicModelResult"/> class.
    /// </summary>
    /// <param name="descriptor">The fitted model descriptor.</param>
    /// <param name="parameters">The fitted parameter values.</param>
    /// <param name="innovationVariance">The fitted innovation variance.</param>
    /// <param name="isStationary">A value indicating whether the fitted AR process is stationary.</param>
    public DynamicModelResult(
        DynamicModelDescriptor descriptor,
        IReadOnlyDictionary<string, double> parameters,
        double innovationVariance,
        bool isStationary)
    {
        this.Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        this.parameters = new Dictionary<string, double>(parameters ?? throw new ArgumentNullException(nameof(parameters)));
        this.InnovationVariance = innovationVariance;
        this.IsStationary = isStationary;
    }

    /// <summary>
    /// Gets the fitted model descriptor.
    /// </summary>
    public DynamicModelDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the fitted parameter values.
    /// </summary>
    public IReadOnlyDictionary<string, double> Parameters => this.parameters;

    /// <summary>
    /// Gets the fitted innovation variance.
    /// </summary>
    public double InnovationVariance { get; }

    /// <summary>
    /// Gets the fitted innovation volatility.
    /// </summary>
    public double InnovationVolatility => Math.Sqrt(Math.Max(0d, this.InnovationVariance));

    /// <summary>
    /// Gets a value indicating whether the fitted AR(1) process is stationary.
    /// </summary>
    public bool IsStationary { get; }
}
