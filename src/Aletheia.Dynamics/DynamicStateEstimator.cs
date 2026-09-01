using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Reconstructs an initial dynamic state from historical NAV observations.
/// </summary>
/// <remarks>
/// The estimator intentionally produces a flexible named vector. The dimensions
/// selected here are a first milestone, not a permanent definition of the hidden
/// state of a fund.
/// </remarks>
public sealed class DynamicStateEstimator
{
    private readonly IStateFeaturePipeline pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicStateEstimator"/> class.
    /// </summary>
    /// <param name="pipeline">The canonical state feature pipeline.</param>
    public DynamicStateEstimator(
        IStateFeaturePipeline? pipeline = null)
    {
        this.pipeline = pipeline ?? new DynamicStateFeaturePipeline();
    }

    /// <summary>
    /// Estimates the current state vector.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>The current dynamic state.</returns>
    public DynamicState Estimate(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count == 0)
        {
            return new DynamicState(DateOnly.MinValue, new Dictionary<StateDimension, double>(), 0d, this.pipeline.Schema);
        }

        return this.pipeline.Build(navSeries, navSeries.Count - 1);
    }
}
