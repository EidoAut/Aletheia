using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Dynamics;

/// <summary>
/// Fits a first-order autoregressive model to log returns.
/// </summary>
/// <remarks>
/// This is a deliberately simple baseline dynamic model:
/// <c>r_t = c + phi r_{t-1} + epsilon_t</c>. More complex state-space models
/// must compete against such transparent baselines.
/// </remarks>
public sealed class AutoregressiveStateModel : IDynamicModel
{
    private double intercept;
    private double phi;
    private double innovationVariance;
    private bool isStationary = true;

    /// <inheritdoc />
    public DynamicModelDescriptor Descriptor { get; } = new(
        "aletheia.dynamic.ar1-log-return",
        "AR(1) log-return state model",
        "1.2.0",
        "A first-order autoregressive baseline fitted to log returns.",
        [StandardStateDimensions.LogReturn]);

    /// <inheritdoc />
    public IReadOnlyList<StateDimension> RequiredStateDimensions => this.Descriptor.RequiredStateDimensions;

    /// <summary>
    /// Gets the fitted intercept <c>c</c>.
    /// </summary>
    public double Intercept => this.intercept;

    /// <summary>
    /// Gets the fitted AR coefficient <c>phi</c>.
    /// </summary>
    public double Phi => this.phi;

    /// <summary>
    /// Gets the fitted innovation variance.
    /// </summary>
    public double InnovationVariance => this.innovationVariance;

    /// <summary>
    /// Gets a value indicating whether <c>|phi| &lt; 1</c>.
    /// </summary>
    public bool IsStationary => this.isStationary;

    /// <summary>
    /// Creates an AR(1) model with explicit parameters for deterministic tests.
    /// </summary>
    /// <param name="intercept">The AR intercept.</param>
    /// <param name="phi">The AR coefficient.</param>
    /// <param name="innovationVariance">The innovation variance.</param>
    /// <returns>A configured AR(1) model.</returns>
    public static AutoregressiveStateModel FromParameters(double intercept, double phi, double innovationVariance)
    {
        if (innovationVariance < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(innovationVariance), innovationVariance, "Innovation variance cannot be negative.");
        }

        return new AutoregressiveStateModel
        {
            intercept = intercept,
            phi = phi,
            innovationVariance = innovationVariance,
            isStationary = Math.Abs(phi) < 1d,
        };
    }

    /// <inheritdoc />
    public DynamicModelResult Fit(DynamicModelInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.LogReturns.Count < 3)
        {
            this.intercept = 0d;
            this.phi = 0d;
            this.innovationVariance = 0d;
            this.isStationary = true;
            return this.CreateResult();
        }

        var values = input.LogReturns.ToValueArray();
        var lagged = new double[values.Length - 1];
        var current = new double[values.Length - 1];

        for (var index = 1; index < values.Length; index++)
        {
            lagged[index - 1] = values[index - 1];
            current[index - 1] = values[index];
        }

        var fit = LinearRegression.Fit(lagged, current);
        this.intercept = fit.Intercept;
        this.phi = fit.Slope;
        this.isStationary = Math.Abs(this.phi) < 1d;

        var residuals = new double[current.Length];
        for (var index = 0; index < current.Length; index++)
        {
            residuals[index] = current[index] - (this.intercept + (this.phi * lagged[index]));
        }

        var residualVolatility = residuals.Length < 2
            ? 0d
            : DescriptiveStatistics.SampleStandardDeviation(residuals);
        this.innovationVariance = residualVolatility * residualVolatility;

        return this.CreateResult();
    }

    /// <inheritdoc />
    public DynamicForecast Forecast(DynamicState currentState, ForecastHorizon horizon)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        if (horizon.Unit != ForecastHorizonUnit.Observations)
        {
            throw new ArgumentException("AR(1) forecasts require an observation-count horizon.", nameof(horizon));
        }

        if (!currentState.TryGetValue(StandardStateDimensions.LogReturn, out var currentLogReturn))
        {
            throw new IncompatibleDynamicStateException(
                "AR(1) log-return forecasting requires StandardStateDimensions.LogReturn in the current state.");
        }

        var cumulativeExpectedLogReturn = this.ForecastCumulativeLogReturn(currentLogReturn, horizon.Value);
        var cumulativeVariance = this.CalculateCumulativeForecastErrorVariance(horizon.Value);
        var medianSimpleReturn = Math.Exp(cumulativeExpectedLogReturn) - 1d;
        var expectedSimpleReturn = Math.Exp(cumulativeExpectedLogReturn + (0.5d * cumulativeVariance)) - 1d;
        var quantiles = this.CalculateSimpleReturnQuantiles(
            cumulativeExpectedLogReturn,
            Math.Sqrt(Math.Max(0d, cumulativeVariance)));

        return new DynamicForecast(
            horizon,
            cumulativeExpectedLogReturn,
            medianSimpleReturn,
            expectedSimpleReturn,
            cumulativeVariance,
            horizon.Value,
            this.isStationary,
            quantiles);
    }

    /// <summary>
    /// Recursively forecasts expected log returns.
    /// </summary>
    /// <param name="currentLogReturn">The current log return <c>r_t</c>.</param>
    /// <param name="steps">The number of future observations.</param>
    /// <returns>The expected future log returns.</returns>
    public double[] ForecastExpectedLogReturns(double currentLogReturn, int steps)
    {
        if (steps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), steps, "Forecast steps must be positive.");
        }

        var expected = new double[steps];
        var previous = currentLogReturn;
        for (var step = 0; step < steps; step++)
        {
            previous = this.intercept + (this.phi * previous);
            expected[step] = previous;
        }

        return expected;
    }

    /// <summary>
    /// Recursively forecasts the cumulative expected log return.
    /// </summary>
    /// <param name="currentLogReturn">The current log return <c>r_t</c>.</param>
    /// <param name="steps">The number of future observations.</param>
    /// <returns>The sum of expected future log returns.</returns>
    public double ForecastCumulativeLogReturn(double currentLogReturn, int steps)
    {
        return this.ForecastExpectedLogReturns(currentLogReturn, steps).Sum();
    }

    /// <summary>
    /// Calculates exact cumulative AR(1) forecast-error variance under homoskedastic innovations.
    /// </summary>
    /// <remarks>
    /// For <c>r_t = c + phi r_{t-1} + epsilon_t</c>, the error in future
    /// return <c>r_{t+k}</c> is a weighted sum of future innovations. The error
    /// in the cumulative return over <c>h</c> observations is therefore:
    ///
    /// <c>sum_{m=1}^{h} a_m epsilon_{t+m}</c>
    ///
    /// where <c>a_m = sum_{j=0}^{h-m} phi^j</c>. Because innovations are
    /// assumed uncorrelated with variance <c>sigma_epsilon^2</c>, cumulative
    /// variance is <c>sigma_epsilon^2 * sum(a_m^2)</c>.
    /// </remarks>
    /// <param name="steps">The number of future observations.</param>
    /// <returns>The cumulative log-return forecast-error variance.</returns>
    public double CalculateCumulativeForecastErrorVariance(int steps)
    {
        if (steps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), steps, "Forecast steps must be positive.");
        }

        var sumSquaredWeights = 0d;
        for (var innovationOffset = 1; innovationOffset <= steps; innovationOffset++)
        {
            var remainingTerms = steps - innovationOffset;
            var weight = 0d;
            var power = 1d;
            for (var term = 0; term <= remainingTerms; term++)
            {
                weight += power;
                power *= this.phi;
            }

            sumSquaredWeights += weight * weight;
        }

        return this.innovationVariance * sumSquaredWeights;
    }

    private DynamicModelResult CreateResult()
    {
        return new DynamicModelResult(
            this.Descriptor,
            new Dictionary<string, double>
            {
                ["Intercept"] = this.intercept,
                ["Phi"] = this.phi,
                ["InnovationVariance"] = this.innovationVariance,
                ["IsStationary"] = this.isStationary ? 1d : 0d,
            },
            this.innovationVariance,
            this.isStationary);
    }

    private IReadOnlyDictionary<int, double> CalculateSimpleReturnQuantiles(
        double cumulativeExpectedLogReturn,
        double cumulativeLogReturnStandardDeviation)
    {
        var zScores = new Dictionary<int, double>
        {
            [10] = -1.2815515655446004d,
            [25] = -0.6744897501960817d,
            [50] = 0d,
            [75] = 0.6744897501960817d,
            [90] = 1.2815515655446004d,
        };

        return zScores.ToDictionary(
            pair => pair.Key,
            pair => Math.Exp(cumulativeExpectedLogReturn + (cumulativeLogReturnStandardDeviation * pair.Value)) - 1d);
    }
}
