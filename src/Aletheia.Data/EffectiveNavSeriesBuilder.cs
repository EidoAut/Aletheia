using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Builds the NAV series used by analytics without inventing missing observations.
/// </summary>
public sealed class EffectiveNavSeriesBuilder
{
    private const string SourcePolicy = "Source observations; no calendar carry-forward rows detected.";
    private const string CalendarCarryForwardPolicy = "Excluded weekend calendar carry-forward rows whose NAV equals the previous source row.";

    /// <summary>
    /// Removes source rows that are likely non-valuation calendar carry-forwards.
    /// </summary>
    /// <param name="source">The source NAV series.</param>
    /// <returns>The effective NAV series and audit counts.</returns>
    public EffectiveNavSeriesResult Build(NavSeries source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Count == 0)
        {
            var empty = new NavSeries(Array.Empty<NavPoint>(), source.ObservationFrequency);
            return new EffectiveNavSeriesResult(
                empty,
                0,
                0,
                0,
                DateOnly.MinValue,
                DateOnly.MinValue,
                DateOnly.MinValue,
                SourcePolicy);
        }

        var retained = new List<NavPoint>(source.Count) { source[0] };
        var synthetic = 0;
        for (var index = 1; index < source.Count; index++)
        {
            var point = source[index];
            var previous = source[index - 1];
            if (IsWeekendCalendarCarryForward(source, previous, point))
            {
                synthetic++;
                continue;
            }

            retained.Add(point);
        }

        var frequency = ObservationFrequencyDetector.Detect(retained);
        var effective = new NavSeries(retained, frequency);
        return new EffectiveNavSeriesResult(
            effective,
            source.Count,
            effective.Count,
            synthetic,
            source.StartDate,
            source.EndDate,
            effective.EndDate,
            synthetic == 0 ? SourcePolicy : CalendarCarryForwardPolicy);
    }

    private static bool IsWeekendCalendarCarryForward(
        NavSeries source,
        NavPoint previous,
        NavPoint current)
    {
        return source.ObservationFrequency == ObservationFrequency.Daily &&
            current.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday &&
            current.Value == previous.Value;
    }
}
