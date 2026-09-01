namespace Aletheia.Application;

/// <summary>
/// Describes qualitative confidence in a deterministic analytical conclusion.
/// </summary>
public enum ConfidenceLevel
{
    /// <summary>
    /// Evidence is weak or incomplete.
    /// </summary>
    Low,

    /// <summary>
    /// Evidence is usable but contains important uncertainty.
    /// </summary>
    Medium,

    /// <summary>
    /// Evidence is broad and internally consistent.
    /// </summary>
    High,
}
