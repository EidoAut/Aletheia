namespace Aletheia.Simulation;

/// <summary>
/// Identifies a stochastic return simulation method.
/// </summary>
public enum ReturnSimulationMethod
{
    /// <summary>
    /// Samples historical observations independently with replacement.
    /// </summary>
    HistoricalBootstrap,

    /// <summary>
    /// Samples contiguous historical blocks with replacement.
    /// </summary>
    BlockBootstrap,
}
