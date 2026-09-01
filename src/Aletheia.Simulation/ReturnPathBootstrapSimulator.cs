using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Simulation;

/// <summary>
/// Simulates cumulative returns by resampling historical log returns.
/// </summary>
public sealed class ReturnPathBootstrapSimulator
{
    /// <summary>
    /// Simulates cumulative returns with historical bootstrap.
    /// </summary>
    /// <param name="historicalLogReturns">The finite historical log returns.</param>
    /// <param name="horizonResolution">The resolved horizon.</param>
    /// <param name="pathCount">The number of paths.</param>
    /// <param name="seed">The deterministic seed.</param>
    /// <returns>The return simulation result.</returns>
    public ReturnSimulationResult SimulateHistoricalBootstrap(
        IReadOnlyList<double> historicalLogReturns,
        ForecastHorizonResolution horizonResolution,
        int pathCount,
        int seed)
    {
        return this.Simulate(
            historicalLogReturns,
            horizonResolution,
            pathCount,
            seed,
            ReturnSimulationMethod.HistoricalBootstrap,
            blockSize: 1);
    }

    /// <summary>
    /// Simulates cumulative returns with moving-block bootstrap.
    /// </summary>
    /// <param name="historicalLogReturns">The finite historical log returns.</param>
    /// <param name="horizonResolution">The resolved horizon.</param>
    /// <param name="pathCount">The number of paths.</param>
    /// <param name="seed">The deterministic seed.</param>
    /// <param name="blockSize">The positive contiguous block size.</param>
    /// <returns>The return simulation result.</returns>
    public ReturnSimulationResult SimulateBlockBootstrap(
        IReadOnlyList<double> historicalLogReturns,
        ForecastHorizonResolution horizonResolution,
        int pathCount,
        int seed,
        int blockSize)
    {
        return this.Simulate(
            historicalLogReturns,
            horizonResolution,
            pathCount,
            seed,
            ReturnSimulationMethod.BlockBootstrap,
            blockSize);
    }

    private ReturnSimulationResult Simulate(
        IReadOnlyList<double> historicalLogReturns,
        ForecastHorizonResolution horizonResolution,
        int pathCount,
        int seed,
        ReturnSimulationMethod method,
        int blockSize)
    {
        ArgumentNullException.ThrowIfNull(historicalLogReturns);
        ArgumentNullException.ThrowIfNull(horizonResolution);
        if (pathCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pathCount), pathCount, "Path count must be positive.");
        }

        if (blockSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be positive.");
        }

        if (historicalLogReturns.Count == 0)
        {
            return new ReturnSimulationResult(
                method,
                [0d],
                ForecastDistribution.FromSamples(horizonResolution, [0d]),
                "No historical returns were available; emitted a zero-return placeholder.");
        }

        for (var index = 0; index < historicalLogReturns.Count; index++)
        {
            if (!double.IsFinite(historicalLogReturns[index]))
            {
                throw new ArgumentException("Bootstrap simulation requires finite log returns.", nameof(historicalLogReturns));
            }
        }

        var random = new Random(seed);
        var samples = new double[pathCount];
        for (var path = 0; path < pathCount; path++)
        {
            var cumulativeLogReturn = 0d;
            var step = 0;
            while (step < horizonResolution.EffectiveObservationCount)
            {
                var start = random.Next(historicalLogReturns.Count);
                var actualBlock = method == ReturnSimulationMethod.HistoricalBootstrap
                    ? 1
                    : Math.Min(blockSize, horizonResolution.EffectiveObservationCount - step);
                for (var offset = 0; offset < actualBlock && step < horizonResolution.EffectiveObservationCount; offset++)
                {
                    cumulativeLogReturn += historicalLogReturns[(start + offset) % historicalLogReturns.Count];
                    step++;
                }
            }

            samples[path] = Math.Exp(cumulativeLogReturn) - 1d;
        }

        var diagnostic = method == ReturnSimulationMethod.HistoricalBootstrap
            ? "Historical bootstrap samples individual observed log returns with replacement."
            : $"Block bootstrap samples contiguous blocks of {blockSize} observations with replacement.";

        return new ReturnSimulationResult(
            method,
            samples,
            ForecastDistribution.FromSamples(horizonResolution, samples),
            diagnostic);
    }
}
