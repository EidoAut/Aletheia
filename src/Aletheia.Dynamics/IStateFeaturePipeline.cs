using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Builds dynamic states from one canonical feature-definition pipeline.
/// </summary>
public interface IStateFeaturePipeline
{
    /// <summary>
    /// Gets the schema produced by the pipeline.
    /// </summary>
    StateSchemaDescriptor Schema { get; }

    /// <summary>
    /// Builds a state for the target observation using only data through that index.
    /// </summary>
    /// <param name="navSeries">The ordered NAV series.</param>
    /// <param name="targetIndex">The zero-based target observation index.</param>
    /// <returns>The reconstructed state.</returns>
    DynamicState Build(NavSeries navSeries, int targetIndex);
}
