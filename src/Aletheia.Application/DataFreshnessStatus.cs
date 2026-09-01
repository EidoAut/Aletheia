namespace Aletheia.Application;

/// <summary>
/// Classifies whether the latest effective observation can support current language.
/// </summary>
public enum DataFreshnessStatus
{
    /// <summary>
    /// The latest effective observation is recent enough for current wording.
    /// </summary>
    Fresh,

    /// <summary>
    /// The latest effective observation is usable but no longer current enough for high actionability.
    /// </summary>
    Aging,

    /// <summary>
    /// The latest effective observation is too old for an unqualified current decision.
    /// </summary>
    Stale,
}
