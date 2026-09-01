namespace Aletheia.Application;

/// <summary>
/// Describes the investor-facing directional interpretation of a signal.
/// </summary>
public enum DirectionalSignal
{
    /// <summary>
    /// No defensible direction is available.
    /// </summary>
    None,

    /// <summary>
    /// Evidence favors buying or accumulating exposure.
    /// </summary>
    Buy,

    /// <summary>
    /// Evidence favors holding current exposure or taking no new action.
    /// </summary>
    Hold,

    /// <summary>
    /// Evidence favors selling or reducing exposure.
    /// </summary>
    Sell,
}
