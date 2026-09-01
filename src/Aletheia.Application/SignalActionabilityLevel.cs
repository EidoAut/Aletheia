namespace Aletheia.Application;

/// <summary>
/// Summarizes whether a qualified signal can be used for a current decision.
/// </summary>
public enum SignalActionabilityLevel
{
    /// <summary>
    /// Strategic, tactical, and freshness checks support current use.
    /// </summary>
    Actionable,

    /// <summary>
    /// The signal can be read, but caveats prevent a fully actionable decision.
    /// </summary>
    Caution,

    /// <summary>
    /// Current actionability is blocked.
    /// </summary>
    Unavailable,
}
