namespace Aletheia.Analytics;

/// <summary>
/// Estimates an effective annual observation cadence from actual timestamps.
/// </summary>
public interface IIrregularAnnualizationEstimator
{
    /// <summary>
    /// Estimates the effective number of observation intervals per year.
    /// </summary>
    /// <param name="observationDates">Chronologically ordered observation dates.</param>
    /// <returns>The positive finite number of effective intervals per year.</returns>
    double EstimatePeriodsPerYear(IReadOnlyList<DateOnly> observationDates);
}
