using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Simulation;

/// <summary>
/// Contains Monte Carlo return samples and their distribution summary.
/// </summary>
public sealed class MonteCarloResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonteCarloResult"/> class.
    /// </summary>
    /// <param name="horizonResolution">The resolved simulation horizon.</param>
    /// <param name="samples">The simulated simple-return samples.</param>
    public MonteCarloResult(ForecastHorizonResolution horizonResolution, IReadOnlyList<double> samples)
    {
        this.HorizonResolution = horizonResolution ?? throw new ArgumentNullException(nameof(horizonResolution));
        this.Samples = samples ?? throw new ArgumentNullException(nameof(samples));
        this.Distribution = ForecastDistribution.FromSamples(horizonResolution, samples);
    }

    /// <summary>
    /// Gets the simulation horizon.
    /// </summary>
    public ForecastHorizon RequestedHorizon => this.HorizonResolution.RequestedHorizon;

    /// <summary>
    /// Gets the resolved simulation horizon.
    /// </summary>
    public ForecastHorizonResolution HorizonResolution { get; }

    /// <summary>
    /// Gets the number of simulated observation steps.
    /// </summary>
    public int SimulationStepCount => this.HorizonResolution.EffectiveObservationCount;

    /// <summary>
    /// Gets the simulated simple-return samples.
    /// </summary>
    public IReadOnlyList<double> Samples { get; }

    /// <summary>
    /// Gets the distribution summary calculated from the samples.
    /// </summary>
    public ForecastDistribution Distribution { get; }
}
