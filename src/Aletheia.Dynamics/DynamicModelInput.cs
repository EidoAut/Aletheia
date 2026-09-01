using Aletheia.TimeSeries;

namespace Aletheia.Dynamics;

/// <summary>
/// Contains the input series needed by a dynamic model.
/// </summary>
public sealed class DynamicModelInput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicModelInput"/> class.
    /// </summary>
    /// <param name="logReturns">The log-return series used for model fitting.</param>
    public DynamicModelInput(TimeSeries<double> logReturns)
    {
        this.LogReturns = logReturns ?? throw new ArgumentNullException(nameof(logReturns));
    }

    /// <summary>
    /// Gets the log-return series used for model fitting.
    /// </summary>
    public TimeSeries<double> LogReturns { get; }
}
