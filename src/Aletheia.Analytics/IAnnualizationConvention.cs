using Aletheia.Core;

namespace Aletheia.Analytics;

/// <summary>
/// Resolves observation-frequency metadata into annualization periods.
/// </summary>
/// <remarks>
/// Annualization is a unit conversion. A per-observation volatility is not an
/// annualized volatility until the number of observations per year has been
/// defined for the series cadence.
/// </remarks>
public interface IAnnualizationConvention
{
    /// <summary>
    /// Resolves the number of return periods per year.
    /// </summary>
    /// <param name="frequency">The observation frequency.</param>
    /// <returns>The annualization period count.</returns>
    double ResolvePeriodsPerYear(ObservationFrequency frequency);
}
