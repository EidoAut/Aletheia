namespace Aletheia.Validation;

/// <summary>
/// Provides deterministic normal-distribution utilities for simple forecast diagnostics.
/// </summary>
internal static class NormalDistribution
{
    /// <summary>
    /// Calculates an approximation to the standard normal cumulative distribution function.
    /// </summary>
    /// <param name="x">The z-score.</param>
    /// <returns>The approximate probability that Z is less than or equal to <paramref name="x"/>.</returns>
    internal static double StandardCdf(double x)
    {
        if (double.IsNaN(x))
        {
            throw new ArgumentException("Normal CDF requires a finite value.", nameof(x));
        }

        if (x == double.PositiveInfinity)
        {
            return 1d;
        }

        if (x == double.NegativeInfinity)
        {
            return 0d;
        }

        var sign = x < 0d ? -1d : 1d;
        var absolute = Math.Abs(x) / Math.Sqrt(2d);
        var t = 1d / (1d + (0.3275911d * absolute));
        var polynomial = 1.061405429d;
        polynomial = (-1.453152027d) + (polynomial * t);
        polynomial = 1.421413741d + (polynomial * t);
        polynomial = (-0.284496736d) + (polynomial * t);
        polynomial = 0.254829592d + (polynomial * t);
        var erf = 1d - (polynomial * t * Math.Exp(-absolute * absolute));

        return 0.5d * (1d + (sign * erf));
    }

    /// <summary>
    /// Calculates an approximation to the inverse standard normal CDF.
    /// </summary>
    /// <param name="probability">The probability in the open interval (0, 1).</param>
    /// <returns>The z-score.</returns>
    internal static double StandardInverseCdf(double probability)
    {
        if (!double.IsFinite(probability) || probability <= 0d || probability >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), probability, "Probability must be in (0, 1).");
        }

        var a = new[]
        {
            -3.969683028665376e+01,
            2.209460984245205e+02,
            -2.759285104469687e+02,
            1.383577518672690e+02,
            -3.066479806614716e+01,
            2.506628277459239e+00,
        };
        var b = new[]
        {
            -5.447609879822406e+01,
            1.615858368580409e+02,
            -1.556989798598866e+02,
            6.680131188771972e+01,
            -1.328068155288572e+01,
        };
        var c = new[]
        {
            -7.784894002430293e-03,
            -3.223964580411365e-01,
            -2.400758277161838e+00,
            -2.549732539343734e+00,
            4.374664141464968e+00,
            2.938163982698783e+00,
        };
        var d = new[]
        {
            7.784695709041462e-03,
            3.224671290700398e-01,
            2.445134137142996e+00,
            3.754408661907416e+00,
        };

        const double low = 0.02425d;
        const double high = 1d - low;
        if (probability < low)
        {
            var q = Math.Sqrt(-2d * Math.Log(probability));
            return EvaluatePolynomial(c, q) / EvaluatePolynomial([d[0], d[1], d[2], d[3], 1d], q);
        }

        if (probability > high)
        {
            var q = Math.Sqrt(-2d * Math.Log(1d - probability));
            return -(EvaluatePolynomial(c, q) / EvaluatePolynomial([d[0], d[1], d[2], d[3], 1d], q));
        }

        var qCentral = probability - 0.5d;
        var r = qCentral * qCentral;
        return (EvaluatePolynomial(a, r) * qCentral) / EvaluatePolynomial([b[0], b[1], b[2], b[3], b[4], 1d], r);
    }

    private static double EvaluatePolynomial(IReadOnlyList<double> coefficients, double value)
    {
        var result = 0d;
        for (var index = 0; index < coefficients.Count; index++)
        {
            result = (result * value) + coefficients[index];
        }

        return result;
    }
}
