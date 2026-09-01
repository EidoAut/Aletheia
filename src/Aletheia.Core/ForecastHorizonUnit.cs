namespace Aletheia.Core;

/// <summary>
/// Defines the unit used by a forecast horizon.
/// </summary>
/// <remarks>
/// Aletheia distinguishes calendar time from observation-index time because
/// fund NAV series commonly skip weekends, holidays, and provider-specific
/// non-valuation dates. A horizon of thirty calendar days is therefore not the
/// same mathematical object as thirty fund observations.
/// </remarks>
public enum ForecastHorizonUnit
{
    /// <summary>
    /// The horizon is measured in elapsed calendar days.
    /// </summary>
    CalendarDays,

    /// <summary>
    /// The horizon is measured in future fund observations.
    /// </summary>
    Observations,
}
