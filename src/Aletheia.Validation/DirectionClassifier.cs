namespace Aletheia.Validation;

/// <summary>
/// Converts decimal simple returns into positive, negative, or flat categories.
/// </summary>
public static class DirectionClassifier
{
    /// <summary>
    /// Classifies a return with an explicit zero tolerance.
    /// </summary>
    /// <param name="value">The decimal simple return.</param>
    /// <param name="zeroTolerance">The nonnegative absolute tolerance around zero.</param>
    /// <returns>The directional category.</returns>
    public static ForecastDirection Classify(double value, double zeroTolerance)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("Directional classification requires a finite value.", nameof(value));
        }

        if (zeroTolerance < 0d || double.IsNaN(zeroTolerance) || double.IsInfinity(zeroTolerance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(zeroTolerance),
                zeroTolerance,
                "Zero tolerance must be finite and nonnegative.");
        }

        if (value > zeroTolerance)
        {
            return ForecastDirection.Positive;
        }

        return value < -zeroTolerance ? ForecastDirection.Negative : ForecastDirection.Flat;
    }
}
