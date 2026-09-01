using Aletheia.Core;
using Aletheia.Dynamics;

namespace Aletheia.Validation;

/// <summary>
/// Forecasts HMM state probabilities by applying the transition matrix.
/// </summary>
public sealed class RegimeTransitionForecaster
{
    /// <summary>
    /// Forecasts regime probabilities at a future horizon.
    /// </summary>
    /// <param name="regime">The fitted Gaussian HMM result.</param>
    /// <param name="horizon">The observation horizon.</param>
    /// <returns>The regime transition forecast.</returns>
    public RegimeTransitionForecast Forecast(GaussianHmmResult regime, ForecastHorizon horizon)
    {
        ArgumentNullException.ThrowIfNull(regime);
        if (regime.States.Count == 0 || regime.LatestProbabilities.Count == 0)
        {
            return new RegimeTransitionForecast(horizon, new Dictionary<string, double>(), 0d, 0d);
        }

        var probabilities = regime.LatestProbabilities.ToArray();
        var currentState = BestIndex(probabilities);
        for (var step = 0; step < Math.Max(1, horizon.Value); step++)
        {
            probabilities = Step(probabilities, regime.TransitionMatrix);
        }

        var highVariance = regime.States.Max(state => state.Variance);
        var stateProbabilities = new Dictionary<string, double>(StringComparer.Ordinal);
        var enterHighRisk = 0d;
        for (var index = 0; index < regime.States.Count; index++)
        {
            stateProbabilities[regime.States[index].Label] = probabilities[index];
            if (regime.States[index].Label.Contains("Bear", StringComparison.OrdinalIgnoreCase) ||
                regime.States[index].Variance >= highVariance)
            {
                enterHighRisk += probabilities[index];
            }
        }

        return new RegimeTransitionForecast(
            horizon,
            stateProbabilities,
            Math.Clamp(enterHighRisk, 0d, 1d),
            Math.Clamp(1d - probabilities[currentState], 0d, 1d));
    }

    private static int BestIndex(IReadOnlyList<double> values)
    {
        var best = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] > values[best])
            {
                best = index;
            }
        }

        return best;
    }

    private static double[] Step(IReadOnlyList<double> probabilities, double[,] transition)
    {
        var stateCount = probabilities.Count;
        var result = new double[stateCount];
        for (var to = 0; to < stateCount; to++)
        {
            for (var from = 0; from < stateCount; from++)
            {
                result[to] += probabilities[from] * transition[from, to];
            }
        }

        var sum = result.Sum();
        if (sum <= 0d || !double.IsFinite(sum))
        {
            return Enumerable.Repeat(1d / stateCount, stateCount).ToArray();
        }

        for (var index = 0; index < result.Length; index++)
        {
            result[index] /= sum;
        }

        return result;
    }
}
