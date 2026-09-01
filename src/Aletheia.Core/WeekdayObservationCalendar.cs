namespace Aletheia.Core;

/// <summary>
/// Treats Monday through Friday as observation dates.
/// </summary>
/// <remarks>
/// This is not a full exchange or fund calendar. It is a documented default for
/// samples and tests when only weekday valuation semantics are required.
/// </remarks>
public sealed class WeekdayObservationCalendar : IObservationCalendar
{
    /// <inheritdoc />
    public string Name => nameof(WeekdayObservationCalendar);

    /// <inheritdoc />
    public int CountObservations(DateOnly start, DateOnly end)
    {
        if (end <= start)
        {
            return 0;
        }

        var count = 0;
        for (var date = start.AddDays(1); date <= end; date = date.AddDays(1))
        {
            if (IsObservationDate(date))
            {
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc />
    public DateOnly AdvanceObservations(DateOnly start, int observations)
    {
        if (observations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(observations), observations, "Observation count must be positive.");
        }

        var remaining = observations;
        var date = start;
        while (remaining > 0)
        {
            date = date.AddDays(1);
            if (IsObservationDate(date))
            {
                remaining--;
            }
        }

        return date;
    }

    private static bool IsObservationDate(DateOnly date) =>
        date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
}
