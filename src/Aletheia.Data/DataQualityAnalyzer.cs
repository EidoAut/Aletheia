using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Detects basic quality issues in dated NAV observations.
/// </summary>
public sealed class DataQualityAnalyzer
{
    private readonly DataQualityOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataQualityAnalyzer"/> class.
    /// </summary>
    /// <param name="options">The diagnostic options.</param>
    public DataQualityAnalyzer(DataQualityOptions? options = null)
    {
        this.options = options ?? new DataQualityOptions();
    }

    /// <summary>
    /// Evaluates raw NAV observations.
    /// </summary>
    /// <param name="points">The raw observations.</param>
    /// <returns>The data-quality report.</returns>
    public DataQualityReport Evaluate(IEnumerable<NavPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var materialized = points.ToArray();
        if (materialized.Length == 0)
        {
            return new DataQualityReport(0, 0, 0, 0, 0, 0, 0, 0d, 0, false);
        }

        var duplicateCount = materialized
            .GroupBy(point => point.Date)
            .Sum(group => Math.Max(0, group.Count() - 1));
        var nonPositiveCount = materialized.Count(point => point.Value <= 0m);
        var distinctPositive = materialized
            .Where(point => point.Value > 0m)
            .GroupBy(point => point.Date)
            .Select(group => group.First())
            .OrderBy(point => point.Date)
            .ToArray();

        var largeGapCount = CountLargeGaps(distinctPositive);
        var missingBusinessDays = this.CountMissingBusinessDays(distinctPositive);
        var coverageRatio = this.CalculateCoverageRatio(distinctPositive, missingBusinessDays);
        var suspiciousJumpCount = CountSuspiciousJumps(distinctPositive);
        var staleObservationCount = CountStaleObservations(distinctPositive);
        var hasSufficientHistory = distinctPositive.Length >= this.options.MinimumObservationCount;
        var qualityScore = this.CalculateQualityScore(
            materialized.Length,
            duplicateCount,
            nonPositiveCount,
            largeGapCount,
            missingBusinessDays,
            suspiciousJumpCount,
            staleObservationCount,
            hasSufficientHistory);

        return new DataQualityReport(
            materialized.Length,
            duplicateCount,
            nonPositiveCount,
            largeGapCount,
            missingBusinessDays,
            suspiciousJumpCount,
            staleObservationCount,
            coverageRatio,
            qualityScore,
            hasSufficientHistory);
    }

    private int CountLargeGaps(IReadOnlyList<NavPoint> points)
    {
        var count = 0;
        for (var index = 1; index < points.Count; index++)
        {
            var gap = points[index].Date.DayNumber - points[index - 1].Date.DayNumber;
            if (gap > this.options.LargeGapThresholdDays)
            {
                count++;
            }
        }

        return count;
    }

    private int CountMissingBusinessDays(IReadOnlyList<NavPoint> points)
    {
        if (points.Count < 2)
        {
            return 0;
        }

        var observedWeekdays = points
            .Where(point => this.IsBusinessDay(point.Date))
            .Select(point => point.Date)
            .ToHashSet();
        var missing = 0;

        for (var date = points[0].Date; date <= points[points.Count - 1].Date; date = date.AddDays(1))
        {
            if (this.IsBusinessDay(date) && !observedWeekdays.Contains(date))
            {
                missing++;
            }
        }

        return missing;
    }

    private double CalculateCoverageRatio(IReadOnlyList<NavPoint> points, int missingBusinessDays)
    {
        var observedBusinessDays = points.Count(point => this.IsBusinessDay(point.Date));
        var denominator = observedBusinessDays + missingBusinessDays;

        return denominator == 0 ? 0d : (double)observedBusinessDays / denominator;
    }

    private int CountSuspiciousJumps(IReadOnlyList<NavPoint> points)
    {
        var count = 0;
        for (var index = 1; index < points.Count; index++)
        {
            var logReturn = Math.Log((double)points[index].Value / (double)points[index - 1].Value);
            if (Math.Abs(logReturn) > this.options.SuspiciousJumpLogReturnThreshold)
            {
                count++;
            }
        }

        return count;
    }

    private int CountStaleObservations(IReadOnlyList<NavPoint> points)
    {
        var staleObservations = 0;
        var currentRun = 1;

        for (var index = 1; index < points.Count; index++)
        {
            if (points[index].Value == points[index - 1].Value)
            {
                currentRun++;
                if (currentRun == this.options.StaleRunThreshold)
                {
                    staleObservations += currentRun;
                }
                else if (currentRun > this.options.StaleRunThreshold)
                {
                    staleObservations++;
                }
            }
            else
            {
                currentRun = 1;
            }
        }

        return staleObservations;
    }

    private int CalculateQualityScore(
        int observations,
        int duplicates,
        int nonPositive,
        int largeGaps,
        int missingBusinessDays,
        int suspiciousJumps,
        int staleObservations,
        bool hasSufficientHistory)
    {
        var score = 100d;
        var denominator = Math.Max(1, observations);

        score -= 25d * nonPositive / denominator;
        score -= 15d * duplicates / denominator;
        score -= Math.Min(20d, largeGaps * 2d);
        score -= Math.Min(20d, missingBusinessDays * 0.1d);
        score -= Math.Min(15d, suspiciousJumps * 3d);
        score -= Math.Min(10d, staleObservations * 0.5d);

        if (!hasSufficientHistory)
        {
            score -= 15d;
        }

        return (int)Math.Round(Math.Clamp(score, 0d, 100d), MidpointRounding.AwayFromZero);
    }

    private bool IsBusinessDay(DateOnly date)
    {
        return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
    }
}
