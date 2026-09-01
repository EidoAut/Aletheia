using Aletheia.Mathematics;

namespace Aletheia.Dynamics;

/// <summary>
/// Fits a univariate Gaussian Hidden Markov Model with scaled Baum-Welch updates.
/// </summary>
public sealed class GaussianHiddenMarkovModel
{
    private const double MinimumVariance = 1e-10d;
    private const double ProbabilityFloor = 1e-12d;

    /// <summary>
    /// Fits the model to finite observations.
    /// </summary>
    /// <param name="observations">The observations in chronological order.</param>
    /// <param name="stateCount">The number of hidden states, from 2 through 4.</param>
    /// <param name="maximumIterations">The maximum EM iterations.</param>
    /// <param name="tolerance">The log-likelihood convergence tolerance.</param>
    /// <returns>The fitted HMM result.</returns>
    public GaussianHmmResult Fit(
        IReadOnlyList<double> observations,
        int stateCount = 3,
        int maximumIterations = 100,
        double tolerance = 1e-5d)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (stateCount is < 2 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(stateCount), stateCount, "State count must be between 2 and 4.");
        }

        if (maximumIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "Maximum iterations must be positive.");
        }

        if (observations.Count < stateCount * 10)
        {
            return Failure(stateCount, observations.Count, "HMM requires at least ten observations per state.");
        }

        ValidateFinite(observations);
        var means = InitializeMeans(observations, stateCount);
        var variances = InitializeVariances(observations, means);
        var initial = Enumerable.Repeat(1d / stateCount, stateCount).ToArray();
        var transition = InitializeTransition(stateCount);
        var previousLogLikelihood = double.NegativeInfinity;
        var converged = false;
        ForwardBackwardOutput? output = null;

        for (var iteration = 0; iteration < maximumIterations; iteration++)
        {
            output = ForwardBackward(observations, initial, transition, means, variances);
            if (double.IsFinite(previousLogLikelihood) &&
                Math.Abs(output.LogLikelihood - previousLogLikelihood) < tolerance)
            {
                converged = true;
                break;
            }

            previousLogLikelihood = output.LogLikelihood;
            UpdateInitial(initial, output.Gamma);
            UpdateTransitions(transition, output.XiSums);
            UpdateEmissions(observations, output.Gamma, means, variances);
        }

        output ??= ForwardBackward(observations, initial, transition, means, variances);
        var states = BuildStates(means, variances);
        return new GaussianHmmResult(
            states,
            initial,
            transition,
            output.Gamma,
            output.LogLikelihood,
            converged,
            converged ? "Scaled Baum-Welch converged." : "Reached maximum EM iterations before tolerance.",
            output.Alpha);
    }

    /// <summary>
    /// Advances filtered probabilities one observation using fixed HMM parameters.
    /// </summary>
    /// <param name="fit">The fitted HMM parameters.</param>
    /// <param name="previousFilteredProbabilities">The previous forward-filtered probabilities.</param>
    /// <param name="observation">The new observation.</param>
    /// <returns>The updated filtered probabilities.</returns>
    public IReadOnlyList<double> FilterNext(
        GaussianHmmResult fit,
        IReadOnlyList<double> previousFilteredProbabilities,
        double observation)
    {
        ArgumentNullException.ThrowIfNull(fit);
        ArgumentNullException.ThrowIfNull(previousFilteredProbabilities);
        if (!double.IsFinite(observation))
        {
            throw new ArgumentException("HMM filtering requires finite observations.", nameof(observation));
        }

        var stateCount = fit.States.Count;
        if (stateCount == 0 || previousFilteredProbabilities.Count != stateCount)
        {
            return Array.Empty<double>();
        }

        var updated = new double[stateCount];
        for (var state = 0; state < stateCount; state++)
        {
            var predicted = 0d;
            for (var previous = 0; previous < stateCount; previous++)
            {
                predicted += previousFilteredProbabilities[previous] * fit.TransitionMatrix[previous, state];
            }

            updated[state] = predicted * GaussianDensity(
                observation,
                fit.States[state].Mean,
                fit.States[state].Variance);
        }

        Normalize(updated);
        return updated;
    }

    private static GaussianHmmResult Failure(int stateCount, int observationCount, string diagnostic)
    {
        var probabilities = new double[observationCount, stateCount];
        return new GaussianHmmResult(
            Array.Empty<GaussianHmmState>(),
            Enumerable.Repeat(1d / stateCount, stateCount).ToArray(),
            new double[stateCount, stateCount],
            probabilities,
            double.NegativeInfinity,
            false,
            diagnostic,
            probabilities);
    }

    private static ForwardBackwardOutput ForwardBackward(
        IReadOnlyList<double> observations,
        IReadOnlyList<double> initial,
        double[,] transition,
        IReadOnlyList<double> means,
        IReadOnlyList<double> variances)
    {
        var timeCount = observations.Count;
        var stateCount = means.Count;
        var alpha = new double[timeCount, stateCount];
        var beta = new double[timeCount, stateCount];
        var scales = new double[timeCount];

        for (var state = 0; state < stateCount; state++)
        {
            alpha[0, state] = initial[state] * GaussianDensity(observations[0], means[state], variances[state]);
        }

        scales[0] = NormalizeRow(alpha, 0);
        for (var time = 1; time < timeCount; time++)
        {
            for (var state = 0; state < stateCount; state++)
            {
                var probability = 0d;
                for (var previous = 0; previous < stateCount; previous++)
                {
                    probability += alpha[time - 1, previous] * transition[previous, state];
                }

                alpha[time, state] = probability * GaussianDensity(observations[time], means[state], variances[state]);
            }

            scales[time] = NormalizeRow(alpha, time);
        }

        for (var state = 0; state < stateCount; state++)
        {
            beta[timeCount - 1, state] = 1d;
        }

        for (var time = timeCount - 2; time >= 0; time--)
        {
            for (var state = 0; state < stateCount; state++)
            {
                var probability = 0d;
                for (var next = 0; next < stateCount; next++)
                {
                    probability += transition[state, next] *
                        GaussianDensity(observations[time + 1], means[next], variances[next]) *
                        beta[time + 1, next];
                }

                beta[time, state] = probability / scales[time + 1];
            }
        }

        var gamma = new double[timeCount, stateCount];
        for (var time = 0; time < timeCount; time++)
        {
            var rowSum = 0d;
            for (var state = 0; state < stateCount; state++)
            {
                gamma[time, state] = alpha[time, state] * beta[time, state];
                rowSum += gamma[time, state];
            }

            if (rowSum <= 0d)
            {
                for (var state = 0; state < stateCount; state++)
                {
                    gamma[time, state] = 1d / stateCount;
                }

                continue;
            }

            for (var state = 0; state < stateCount; state++)
            {
                gamma[time, state] /= rowSum;
            }
        }

        var xiSums = new double[stateCount, stateCount];
        for (var time = 0; time < timeCount - 1; time++)
        {
            var denominator = 0d;
            for (var from = 0; from < stateCount; from++)
            {
                for (var to = 0; to < stateCount; to++)
                {
                    denominator += alpha[time, from] *
                        transition[from, to] *
                        GaussianDensity(observations[time + 1], means[to], variances[to]) *
                        beta[time + 1, to];
                }
            }

            denominator = Math.Max(ProbabilityFloor, denominator);
            for (var from = 0; from < stateCount; from++)
            {
                for (var to = 0; to < stateCount; to++)
                {
                    xiSums[from, to] += alpha[time, from] *
                        transition[from, to] *
                        GaussianDensity(observations[time + 1], means[to], variances[to]) *
                        beta[time + 1, to] / denominator;
                }
            }
        }

        var logLikelihood = scales.Sum(scale => Math.Log(Math.Max(ProbabilityFloor, scale)));
        return new ForwardBackwardOutput(alpha, gamma, xiSums, logLikelihood);
    }

    private static void UpdateInitial(double[] initial, double[,] gamma)
    {
        for (var state = 0; state < initial.Length; state++)
        {
            initial[state] = Math.Max(ProbabilityFloor, gamma[0, state]);
        }

        Normalize(initial);
    }

    private static void UpdateTransitions(double[,] transition, double[,] xiSums)
    {
        var stateCount = transition.GetLength(0);
        for (var from = 0; from < stateCount; from++)
        {
            var rowSum = 0d;
            for (var to = 0; to < stateCount; to++)
            {
                rowSum += xiSums[from, to];
            }

            for (var to = 0; to < stateCount; to++)
            {
                transition[from, to] = rowSum <= 0d
                    ? 1d / stateCount
                    : Math.Max(ProbabilityFloor, xiSums[from, to] / rowSum);
            }

            NormalizeRow(transition, from);
        }
    }

    private static void UpdateEmissions(
        IReadOnlyList<double> observations,
        double[,] gamma,
        double[] means,
        double[] variances)
    {
        for (var state = 0; state < means.Length; state++)
        {
            var weight = 0d;
            var weightedSum = 0d;
            for (var time = 0; time < observations.Count; time++)
            {
                weight += gamma[time, state];
                weightedSum += gamma[time, state] * observations[time];
            }

            if (weight <= ProbabilityFloor)
            {
                continue;
            }

            means[state] = weightedSum / weight;
            var variance = 0d;
            for (var time = 0; time < observations.Count; time++)
            {
                var residual = observations[time] - means[state];
                variance += gamma[time, state] * residual * residual;
            }

            variances[state] = Math.Max(MinimumVariance, variance / weight);
        }
    }

    private static IReadOnlyList<GaussianHmmState> BuildStates(double[] means, double[] variances)
    {
        var lowMean = means.Min();
        var highMean = means.Max();
        var highVariance = variances.Max();
        var states = new List<GaussianHmmState>(means.Length);
        for (var state = 0; state < means.Length; state++)
        {
            states.Add(new GaussianHmmState(
                state,
                means[state],
                variances[state],
                LabelState(means[state], variances[state], lowMean, highMean, highVariance)));
        }

        return states;
    }

    private static string LabelState(
        double mean,
        double variance,
        double lowMean,
        double highMean,
        double highVariance)
    {
        if (variance >= highVariance * 0.9d)
        {
            return mean < 0d ? "Bear / High Volatility" : "High Volatility";
        }

        if (mean <= lowMean + ((highMean - lowMean) * 0.33d))
        {
            return "Bear / Low Return";
        }

        if (mean >= lowMean + ((highMean - lowMean) * 0.67d))
        {
            return "Bull / Positive Trend";
        }

        return "Neutral";
    }

    private static double[] InitializeMeans(IReadOnlyList<double> observations, int stateCount)
    {
        var means = new double[stateCount];
        for (var state = 0; state < stateCount; state++)
        {
            var probability = stateCount == 1 ? 0.5d : state / (double)(stateCount - 1);
            means[state] = DescriptiveStatistics.Quantile(observations, probability);
        }

        return means;
    }

    private static double[] InitializeVariances(IReadOnlyList<double> observations, IReadOnlyList<double> means)
    {
        var variance = Math.Max(MinimumVariance, observations.Count < 2 ? 0d : DescriptiveStatistics.SampleVariance(observations));
        return Enumerable.Repeat(variance, means.Count).ToArray();
    }

    private static double[,] InitializeTransition(int stateCount)
    {
        var transition = new double[stateCount, stateCount];
        for (var from = 0; from < stateCount; from++)
        {
            for (var to = 0; to < stateCount; to++)
            {
                transition[from, to] = from == to ? 0.85d : 0.15d / (stateCount - 1);
            }
        }

        return transition;
    }

    private static double GaussianDensity(double value, double mean, double variance)
    {
        var safeVariance = Math.Max(MinimumVariance, variance);
        var residual = value - mean;
        return Math.Max(
            ProbabilityFloor,
            Math.Exp(-0.5d * residual * residual / safeVariance) / Math.Sqrt(2d * Math.PI * safeVariance));
    }

    private static double NormalizeRow(double[,] matrix, int row)
    {
        var length = matrix.GetLength(1);
        var sum = 0d;
        for (var column = 0; column < length; column++)
        {
            sum += matrix[row, column];
        }

        if (sum <= ProbabilityFloor)
        {
            for (var column = 0; column < length; column++)
            {
                matrix[row, column] = 1d / length;
            }

            return 1d;
        }

        for (var column = 0; column < length; column++)
        {
            matrix[row, column] /= sum;
        }

        return sum;
    }

    private static void Normalize(double[] values)
    {
        var sum = values.Sum();
        if (sum <= ProbabilityFloor)
        {
            var equal = 1d / values.Length;
            Array.Fill(values, equal);
            return;
        }

        for (var index = 0; index < values.Length; index++)
        {
            values[index] /= sum;
        }
    }

    private static void ValidateFinite(IReadOnlyList<double> observations)
    {
        for (var index = 0; index < observations.Count; index++)
        {
            if (!double.IsFinite(observations[index]))
            {
                throw new ArgumentException("HMM requires finite observations.", nameof(observations));
            }
        }
    }

    private sealed record ForwardBackwardOutput(
        double[,] Alpha,
        double[,] Gamma,
        double[,] XiSums,
        double LogLikelihood);
}
