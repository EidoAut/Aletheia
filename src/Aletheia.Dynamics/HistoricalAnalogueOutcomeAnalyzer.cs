using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Dynamics;

/// <summary>
/// Calculates subsequent returns after historical analogue states.
/// </summary>
public sealed class HistoricalAnalogueOutcomeAnalyzer
{
    /// <summary>
    /// Evaluates future returns after analogue dates.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="analogues">The historical analogue matches.</param>
    /// <param name="horizon">The outcome horizon.</param>
    /// <returns>The outcome summary.</returns>
    public AnalogueOutcomeSummary Analyze(
        NavSeries navSeries,
        IReadOnlyList<HistoricalAnalogueResult> analogues,
        ForecastHorizon horizon)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(analogues);

        var returns = new List<double>();
        foreach (var analogue in analogues)
        {
            var startIndex = FindIndexOnOrAfter(navSeries, analogue.Observation.Date);
            if (startIndex < 0)
            {
                continue;
            }

            var endIndex = ResolveEndIndex(navSeries, startIndex, horizon);
            if (endIndex < 0 || endIndex <= startIndex)
            {
                continue;
            }

            var start = navSeries[startIndex].Value;
            var end = navSeries[endIndex].Value;
            if (start <= 0m || end <= 0m)
            {
                continue;
            }

            returns.Add(((double)end / (double)start) - 1d);
        }

        if (returns.Count == 0)
        {
            return new AnalogueOutcomeSummary(0, 0d, 0d, 0d, 0d, 0d, 0d, 0d);
        }

        return new AnalogueOutcomeSummary(
            returns.Count,
            returns.Count(value => value > 0d) / (double)returns.Count,
            DescriptiveStatistics.Mean(returns),
            DescriptiveStatistics.Median(returns),
            DescriptiveStatistics.Percentile(returns, 25d),
            DescriptiveStatistics.Percentile(returns, 75d),
            returns.Min(),
            returns.Max());
    }

    private static int ResolveEndIndex(NavSeries navSeries, int startIndex, ForecastHorizon horizon)
    {
        if (horizon.Unit == ForecastHorizonUnit.Observations)
        {
            var endIndex = startIndex + horizon.Value;
            return endIndex < navSeries.Count ? endIndex : -1;
        }

        var targetDate = navSeries[startIndex].Date.AddDays(horizon.Value);
        return FindIndexOnOrAfter(navSeries, targetDate);
    }

    private static int FindIndexOnOrAfter(NavSeries navSeries, DateOnly date)
    {
        for (var index = 0; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= date)
            {
                return index;
            }
        }

        return -1;
    }
}
