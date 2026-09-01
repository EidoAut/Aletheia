namespace Aletheia.Application;

/// <summary>
/// Describes whether a direction is validated strongly enough for a full call.
/// </summary>
public enum SignalQualification
{
    /// <summary>
    /// The direction passed the configured validation and evidence gates.
    /// </summary>
    Confirmed,

    /// <summary>
    /// A direction exists, but validation, freshness, timing, or OOD evidence keeps it qualified.
    /// </summary>
    Tentative,

    /// <summary>
    /// No defensible current conclusion can be made.
    /// </summary>
    Unavailable,
}
