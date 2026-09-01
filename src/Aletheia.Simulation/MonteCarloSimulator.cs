using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Simulation;

/// <summary>
/// Simulates future return distributions from fitted per-observation log-return moments.
/// </summary>
/// <remarks>
/// The Milestone 1 simulator assumes independent draws from a Gaussian
/// approximation. This is a baseline scenario generator, not a complete market
/// model and not a validated forecast engine.
/// </remarks>
public sealed class MonteCarloSimulator
{
    private readonly MonteCarloOptions options;
    private readonly ForecastHorizonResolver horizonResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonteCarloSimulator"/> class.
    /// </summary>
    /// <param name="options">The simulation options.</param>
    /// <param name="horizonResolver">The resolver used when a cutoff date is supplied.</param>
    public MonteCarloSimulator(MonteCarloOptions? options = null, ForecastHorizonResolver? horizonResolver = null)
    {
        this.options = options ?? new MonteCarloOptions();
        this.horizonResolver = horizonResolver ?? new ForecastHorizonResolver();
    }

    /// <summary>
    /// Simulates simple returns over a forecast horizon.
    /// </summary>
    /// <param name="historicalLogReturns">The historical observation log returns.</param>
    /// <param name="horizonResolution">The explicit resolved forecast horizon.</param>
    /// <param name="cancellationToken">A token used to cancel simulation.</param>
    /// <returns>The Monte Carlo result.</returns>
    public MonteCarloResult Simulate(
        IReadOnlyList<double> historicalLogReturns,
        ForecastHorizonResolution horizonResolution,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(historicalLogReturns);

        if (this.options.PathCount <= 0)
        {
            throw new InvalidOperationException("Monte Carlo path count must be positive.");
        }

        if (historicalLogReturns.Count == 0)
        {
            return new MonteCarloResult(horizonResolution, [0d]);
        }

        var mean = DescriptiveStatistics.Mean(historicalLogReturns);
        var standardDeviation = historicalLogReturns.Count < 2
            ? 0d
            : DescriptiveStatistics.SampleStandardDeviation(historicalLogReturns);
        var random = new Random(this.options.Seed + horizonResolution.RequestedHorizon.Value + ((int)horizonResolution.RequestedHorizon.Unit * 10_000));
        var samples = new double[this.options.PathCount];

        for (var path = 0; path < samples.Length; path++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cumulativeLogReturn = 0d;
            for (var step = 0; step < horizonResolution.EffectiveObservationCount; step++)
            {
                cumulativeLogReturn += mean + (standardDeviation * NextGaussian(random));
            }

            samples[path] = Math.Exp(cumulativeLogReturn) - 1d;
        }

        return new MonteCarloResult(horizonResolution, samples);
    }

    /// <summary>
    /// Resolves a requested horizon from a data cutoff date and simulates returns.
    /// </summary>
    /// <param name="historicalLogReturns">The historical observation log returns.</param>
    /// <param name="horizon">The requested horizon.</param>
    /// <param name="observationFrequency">The observation frequency used to resolve calendar horizons.</param>
    /// <param name="dataCutoffDate">The last available observation date.</param>
    /// <param name="cancellationToken">A token used to cancel simulation.</param>
    /// <returns>The Monte Carlo result.</returns>
    public MonteCarloResult Simulate(
        IReadOnlyList<double> historicalLogReturns,
        ForecastHorizon horizon,
        ObservationFrequency observationFrequency,
        DateOnly dataCutoffDate,
        CancellationToken cancellationToken = default)
    {
        var resolution = this.horizonResolver.Resolve(horizon, dataCutoffDate, observationFrequency);
        return this.Simulate(historicalLogReturns, resolution, cancellationToken);
    }

    private static double NextGaussian(Random random)
    {
        var u1 = 1d - random.NextDouble();
        var u2 = 1d - random.NextDouble();

        return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
    }
}
