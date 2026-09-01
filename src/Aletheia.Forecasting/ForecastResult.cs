namespace Aletheia.Forecasting;

/// <summary>
/// Contains a set of forecast distributions from one model.
/// </summary>
public sealed class ForecastResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastResult"/> class.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    /// <param name="distributions">The horizon distributions.</param>
    public ForecastResult(string modelName, IReadOnlyList<ForecastDistribution> distributions)
    {
        this.ModelName = string.IsNullOrWhiteSpace(modelName)
            ? throw new ArgumentException("Model name cannot be empty.", nameof(modelName))
            : modelName;
        this.Distributions = distributions ?? throw new ArgumentNullException(nameof(distributions));
    }

    /// <summary>
    /// Gets the model name.
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Gets the horizon distributions.
    /// </summary>
    public IReadOnlyList<ForecastDistribution> Distributions { get; }
}
