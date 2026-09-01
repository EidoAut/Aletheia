namespace Aletheia.Core;

/// <summary>
/// Describes the sampling semantics of an ordered observation series.
/// </summary>
public enum ObservationFrequency
{
    /// <summary>
    /// The spacing is unknown or materially irregular.
    /// </summary>
    Irregular,

    /// <summary>
    /// Observations are expected once per calendar day.
    /// </summary>
    Daily,

    /// <summary>
    /// Observations are expected on business weekdays.
    /// </summary>
    BusinessDaily,

    /// <summary>
    /// Observations are expected weekly.
    /// </summary>
    Weekly,

    /// <summary>
    /// Observations are expected monthly.
    /// </summary>
    Monthly,
}
