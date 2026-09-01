using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Builds simple historical state observations for analogue search.
/// </summary>
public sealed class HistoricalAnalogueFeatureBuilder
{
    private readonly IStateFeaturePipeline pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoricalAnalogueFeatureBuilder"/> class.
    /// </summary>
    /// <param name="pipeline">The canonical state feature pipeline.</param>
    public HistoricalAnalogueFeatureBuilder(IStateFeaturePipeline? pipeline = null)
    {
        this.pipeline = pipeline ?? new DynamicStateFeaturePipeline();
    }

    /// <summary>
    /// Creates a compact historical state vector at each eligible date.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="lookback">The trailing observation lookback.</param>
    /// <returns>Historical state observations.</returns>
    public IReadOnlyList<StateObservation> Build(NavSeries navSeries, int lookback = 30)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (lookback <= 1 || navSeries.Count <= lookback)
        {
            return Array.Empty<StateObservation>();
        }

        var observations = new List<StateObservation>();
        for (var index = lookback; index < navSeries.Count; index++)
        {
            var state = this.pipeline.Build(navSeries, index);

            observations.Add(new StateObservation(
                state.Date,
                state.Dimensions,
                state.Schema));
        }

        return observations;
    }
}
