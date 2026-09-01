using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Detects observation-frequency semantics from dated observations without filling missing dates.
/// </summary>
public static class ObservationFrequencyDetector
{
    private const double MinimumDenseCoverage = 0.80d;
    private const double MinimumRegularGapShare = 0.75d;
    private const double MinimumWeekendCoverageForDaily = 0.60d;

    /// <summary>
    /// Detects frequency from NAV observations.
    /// </summary>
    /// <param name="points">The observations.</param>
    /// <returns>The detected frequency.</returns>
    public static ObservationFrequency Detect(IReadOnlyList<NavPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            return ObservationFrequency.Irregular;
        }

        var ordered = points
            .OrderBy(point => point.Date)
            .GroupBy(point => point.Date)
            .Select(group => group.Last())
            .ToArray();
        if (ordered.Length < 2)
        {
            return ObservationFrequency.Irregular;
        }

        if (HasCalendarDailyCoverage(ordered))
        {
            return ObservationFrequency.Daily;
        }

        if (HasBusinessDailyCoverage(ordered))
        {
            return ObservationFrequency.BusinessDaily;
        }

        if (HasWeeklyCadence(ordered))
        {
            return ObservationFrequency.Weekly;
        }

        return HasMonthlyCadence(ordered)
            ? ObservationFrequency.Monthly
            : ObservationFrequency.Irregular;
    }

    private static bool HasCalendarDailyCoverage(IReadOnlyList<NavPoint> ordered)
    {
        var start = ordered[0].Date;
        var end = ordered[^1].Date;
        var expectedCalendarDays = end.DayNumber - start.DayNumber + 1;
        if (expectedCalendarDays <= 0 || ordered.Count / (double)expectedCalendarDays < MinimumDenseCoverage)
        {
            return false;
        }

        var expectedWeekendDays = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (IsWeekend(date))
            {
                expectedWeekendDays++;
            }
        }

        if (expectedWeekendDays == 0)
        {
            return true;
        }

        var observedWeekendDays = ordered.Count(point => IsWeekend(point.Date));
        return observedWeekendDays / (double)expectedWeekendDays >= MinimumWeekendCoverageForDaily;
    }

    private static bool HasBusinessDailyCoverage(IReadOnlyList<NavPoint> ordered)
    {
        if (ordered.Count < 3)
        {
            return false;
        }

        var expectedBusinessDays = 0;
        for (var date = ordered[0].Date; date <= ordered[^1].Date; date = date.AddDays(1))
        {
            if (!IsWeekend(date))
            {
                expectedBusinessDays++;
            }
        }

        var observedBusinessDays = ordered.Count(point => !IsWeekend(point.Date));
        return expectedBusinessDays > 0 &&
            observedBusinessDays / (double)expectedBusinessDays >= MinimumDenseCoverage;
    }

    private static bool HasWeeklyCadence(IReadOnlyList<NavPoint> ordered)
    {
        if (ordered.Count < 3)
        {
            return false;
        }

        var gaps = GetDayGaps(ordered);
        var regularGapCount = gaps.Count(gap => gap is >= 4 and <= 10);
        return Median(gaps) is >= 5d and <= 9d &&
            regularGapCount / (double)gaps.Length >= MinimumRegularGapShare;
    }

    private static bool HasMonthlyCadence(IReadOnlyList<NavPoint> ordered)
    {
        if (ordered.Select(point => (point.Date.Year, point.Date.Month)).Distinct().Count() != ordered.Count)
        {
            return false;
        }

        var monthGaps = new int[ordered.Count - 1];
        for (var index = 1; index < ordered.Count; index++)
        {
            monthGaps[index - 1] = GetMonthIndex(ordered[index].Date) - GetMonthIndex(ordered[index - 1].Date);
        }

        if (ordered.Count == 2)
        {
            return monthGaps[0] == 1;
        }

        var nearMonthlyGapCount = monthGaps.Count(gap => gap is 1 or 2);
        if (Median(monthGaps) is < 1d or > 1.5d ||
            nearMonthlyGapCount / (double)monthGaps.Length < MinimumRegularGapShare)
        {
            return false;
        }

        var expectedMonths = GetMonthIndex(ordered[^1].Date) - GetMonthIndex(ordered[0].Date) + 1;
        return expectedMonths > 0 && ordered.Count / (double)expectedMonths >= MinimumRegularGapShare;
    }

    private static int[] GetDayGaps(IReadOnlyList<NavPoint> ordered)
    {
        var gaps = new int[ordered.Count - 1];
        for (var index = 1; index < ordered.Count; index++)
        {
            gaps[index - 1] = ordered[index].Date.DayNumber - ordered[index - 1].Date.DayNumber;
        }

        return gaps;
    }

    private static double Median(IReadOnlyList<int> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        var midpoint = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[midpoint - 1] + sorted[midpoint]) / 2d
            : sorted[midpoint];
    }

    private static int GetMonthIndex(DateOnly date)
    {
        return (date.Year * 12) + date.Month;
    }

    private static bool IsWeekend(DateOnly date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
}
