using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Forecasting;

/// <summary>
/// Produces a naive probabilistic forecast baseline from historical returns.
/// </summary>
/// <remarks>
/// The model first uses realized historical returns over each horizon. When a
/// series is too short for enough horizon samples, it falls back to a seeded
/// Gaussian approximation fitted to per-observation log returns. This keeps the baseline
/// probabilistic and reproducible.
/// </remarks>
public sealed class NaiveForecastBaseline
{
    private const int MinimumHistoricalSamples = 20;
    private const int ApproximationSampleCount = 2_000;
    private const int Seed = 271828;
    private readonly ReturnCalculator returnCalculator;
    private readonly ForecastHorizonResolver horizonResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="NaiveForecastBaseline"/> class.
    /// </summary>
    /// <param name="returnCalculator">The return calculator.</param>
    /// <param name="horizonResolver">The resolver used for calendar-day horizons.</param>
    public NaiveForecastBaseline(
        ReturnCalculator? returnCalculator = null,
        ForecastHorizonResolver? horizonResolver = null)
    {
        this.returnCalculator = returnCalculator ?? new ReturnCalculator();
        this.horizonResolver = horizonResolver ?? new ForecastHorizonResolver();
    }

    /// <summary>
    /// Generates probabilistic forecasts for the requested horizons.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="horizons">The forecast horizons.</param>
    /// <returns>A baseline forecast result.</returns>
    public ForecastResult Forecast(NavSeries navSeries, IReadOnlyList<ForecastHorizon> horizons)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(horizons);

        var distributions = new List<ForecastDistribution>(horizons.Count);
        foreach (var horizon in horizons)
        {
            var samples = this.CollectHistoricalHorizonReturns(navSeries, horizon);
            var resolution = this.horizonResolver.Resolve(
                horizon,
                navSeries.EndDate,
                navSeries.ObservationFrequency);
            if (samples.Count < MinimumHistoricalSamples)
            {
                samples = this.GenerateApproximateSamples(navSeries, resolution);
            }

            distributions.Add(ForecastDistribution.FromSamples(resolution, samples));
        }

        return new ForecastResult("Naive historical-distribution baseline", distributions);
    }

    private List<double> CollectHistoricalHorizonReturns(NavSeries navSeries, ForecastHorizon horizon)
    {
        var samples = new List<double>();
        for (var startIndex = 0; startIndex < navSeries.Count - 1; startIndex++)
        {
            var endIndex = this.ResolveEndIndex(navSeries, startIndex, horizon);
            if (endIndex < 0)
            {
                break;
            }

            var start = navSeries[startIndex].Value;
            var end = navSeries[endIndex].Value;
            if (start <= 0m || end <= 0m)
            {
                continue;
            }

            samples.Add(((double)end / (double)start) - 1d);
        }

        return samples;
    }

    private List<double> GenerateApproximateSamples(NavSeries navSeries, ForecastHorizonResolution resolution)
    {
        var logReturns = this.returnCalculator.CalculateLogReturns(navSeries).ToValueArray();
        if (logReturns.Length == 0)
        {
            return [0d];
        }

        var mean = DescriptiveStatistics.Mean(logReturns);
        var standardDeviation = logReturns.Length < 2
            ? 0d
            : DescriptiveStatistics.SampleStandardDeviation(logReturns);
        var random = new Random(Seed + resolution.RequestedHorizon.Value + ((int)resolution.RequestedHorizon.Unit * 10_000));
        var samples = new List<double>(ApproximationSampleCount);

        for (var sample = 0; sample < ApproximationSampleCount; sample++)
        {
            var cumulativeLogReturn = 0d;
            for (var step = 0; step < resolution.EffectiveObservationCount; step++)
            {
                cumulativeLogReturn += mean + (standardDeviation * this.NextGaussian(random));
            }

            samples.Add(Math.Exp(cumulativeLogReturn) - 1d);
        }

        return samples;
    }

    private int ResolveEndIndex(NavSeries navSeries, int startIndex, ForecastHorizon horizon)
    {
        if (horizon.Unit == ForecastHorizonUnit.Observations)
        {
            var endIndex = startIndex + horizon.Value;
            return endIndex < navSeries.Count ? endIndex : -1;
        }

        var targetDate = navSeries[startIndex].Date.AddDays(horizon.Value);
        return this.FindIndexOnOrAfter(navSeries, targetDate, startIndex + 1);
    }

    private int FindIndexOnOrAfter(NavSeries navSeries, DateOnly date, int startIndex)
    {
        for (var index = startIndex; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= date)
            {
                return index;
            }
        }

        return -1;
    }

    private double NextGaussian(Random random)
    {
        var u1 = 1d - random.NextDouble();
        var u2 = 1d - random.NextDouble();

        return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
    }
}
