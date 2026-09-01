namespace Aletheia.Core;

/// <summary>
/// Converts between calendar dates and observation-count time.
/// </summary>
/// <remarks>
/// Implementations intentionally remain small in Milestone 1.1. The abstraction
/// exists so future fund-specific calendars can replace the default weekday
/// policy without rewriting forecasting and validation code.
/// </remarks>
public interface IObservationCalendar
{
    /// <summary>
    /// Gets the stable calendar name used in reproducibility metadata.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Counts observations strictly after <paramref name="start"/> and on or before <paramref name="end"/>.
    /// </summary>
    /// <param name="start">The exclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <returns>The number of expected observations in the interval.</returns>
    int CountObservations(DateOnly start, DateOnly end);

    /// <summary>
    /// Advances by a number of future observations.
    /// </summary>
    /// <param name="start">The date before the first advanced observation.</param>
    /// <param name="observations">The positive number of observations to advance.</param>
    /// <returns>The date of the advanced observation.</returns>
    DateOnly AdvanceObservations(DateOnly start, int observations);
}
