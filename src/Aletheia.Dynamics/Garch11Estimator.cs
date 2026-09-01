using Aletheia.Mathematics;

namespace Aletheia.Dynamics;

/// <summary>
/// Estimates a constrained Gaussian GARCH(1,1) volatility model.
/// </summary>
public sealed class Garch11Estimator
{
    private const double MinimumVariance = 1e-12d;

    /// <summary>
    /// Fits a GARCH(1,1) model to finite residuals or de-meaned returns.
    /// </summary>
    /// <param name="values">The finite residual or return observations.</param>
    /// <returns>The fit result.</returns>
    public Garch11FitResult Fit(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count < 30)
        {
            return Failure("GARCH(1,1) requires at least 30 observations.");
        }

        ValidateFinite(values);
        var mean = DescriptiveStatistics.Mean(values);
        var centered = Center(values, mean);
        var sampleVariance = Math.Max(MinimumVariance, DescriptiveStatistics.SampleVariance(centered));
        if (sampleVariance <= MinimumVariance)
        {
            return Failure("GARCH(1,1) cannot be estimated from a near-constant series.");
        }

        Candidate? best = null;
        foreach (var alpha in Grid(0.02d, 0.22d, 0.02d))
        {
            foreach (var beta in Grid(0.50d, 0.96d, 0.02d))
            {
                if (alpha + beta >= 0.995d)
                {
                    continue;
                }

                var omega = sampleVariance * (1d - alpha - beta);
                best = SelectBetter(best, Evaluate(centered, omega, alpha, beta, sampleVariance));
            }
        }

        if (best is null)
        {
            return Failure("No admissible GARCH(1,1) parameter set was found.");
        }

        var step = 0.01d;
        for (var iteration = 0; iteration < 8; iteration++)
        {
            var improved = false;
            foreach (var alphaDelta in new[] { -step, 0d, step })
            {
                foreach (var betaDelta in new[] { -step, 0d, step })
                {
                    var alpha = best.Alpha + alphaDelta;
                    var beta = best.Beta + betaDelta;
                    if (alpha < 0d || beta < 0d || alpha + beta >= 0.999d)
                    {
                        continue;
                    }

                    var omega = sampleVariance * (1d - alpha - beta);
                    var candidate = Evaluate(centered, omega, alpha, beta, sampleVariance);
                    if (candidate.LogLikelihood > best.LogLikelihood)
                    {
                        best = candidate;
                        improved = true;
                    }
                }
            }

            if (!improved)
            {
                step *= 0.5d;
            }
        }

        if (!double.IsFinite(best.LogLikelihood))
        {
            return Failure("GARCH(1,1) likelihood was not finite.");
        }

        return new Garch11FitResult(
            best.Omega,
            best.Alpha,
            best.Beta,
            best.LogLikelihood,
            true,
            "Constrained deterministic likelihood search converged.",
            best.Variances,
            mean);
    }

    private static Garch11FitResult Failure(string diagnostic)
    {
        return new Garch11FitResult(0d, 0d, 0d, double.NegativeInfinity, false, diagnostic, Array.Empty<double>(), 0d);
    }

    private static Candidate Evaluate(
        IReadOnlyList<double> values,
        double omega,
        double alpha,
        double beta,
        double initialVariance)
    {
        var variances = new double[values.Count];
        variances[0] = initialVariance;
        var logLikelihood = 0d;
        for (var index = 1; index < values.Count; index++)
        {
            var laggedShock = values[index - 1] * values[index - 1];
            variances[index] = Math.Max(MinimumVariance, omega + (alpha * laggedShock) + (beta * variances[index - 1]));
            logLikelihood += -0.5d * (Math.Log(2d * Math.PI) + Math.Log(variances[index]) + (values[index] * values[index] / variances[index]));
        }

        return new Candidate(omega, alpha, beta, logLikelihood, variances);
    }

    private static Candidate SelectBetter(Candidate? current, Candidate candidate)
    {
        return current is null || candidate.LogLikelihood > current.LogLikelihood ? candidate : current;
    }

    private static double[] Center(IReadOnlyList<double> values, double mean)
    {
        var centered = new double[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            centered[index] = values[index] - mean;
        }

        return centered;
    }

    private static IEnumerable<double> Grid(double start, double end, double step)
    {
        for (var value = start; value <= end + (step / 2d); value += step)
        {
            yield return value;
        }
    }

    private static void ValidateFinite(IReadOnlyList<double> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (!double.IsFinite(values[index]))
            {
                throw new ArgumentException("GARCH(1,1) requires finite observations.", nameof(values));
            }
        }
    }

    private sealed record Candidate(
        double Omega,
        double Alpha,
        double Beta,
        double LogLikelihood,
        IReadOnlyList<double> Variances);
}
