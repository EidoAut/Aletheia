namespace Aletheia.Mathematics;

/// <summary>
/// Provides deterministic descriptive statistics for numerical finance algorithms.
/// </summary>
public static class DescriptiveStatistics
{
    /// <summary>
    /// Calculates the arithmetic mean.
    /// </summary>
    /// <param name="values">The finite values to average.</param>
    /// <returns>The arithmetic mean.</returns>
    public static double Mean(IReadOnlyList<double> values)
    {
        EnsureNotEmpty(values);

        var sum = 0d;
        for (var index = 0; index < values.Count; index++)
        {
            EnsureFinite(values[index], nameof(values));
            sum += values[index];
        }

        return sum / values.Count;
    }

    /// <summary>
    /// Calculates the median.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The median value.</returns>
    public static double Median(IReadOnlyList<double> values)
    {
        return Percentile(values, 50d);
    }

    /// <summary>
    /// Calculates sample variance using Bessel's correction.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The sample variance.</returns>
    public static double SampleVariance(IReadOnlyList<double> values)
    {
        EnsureMinimumCount(values, 2);

        var mean = Mean(values);
        var sumSquaredDeviation = 0d;
        for (var index = 0; index < values.Count; index++)
        {
            var deviation = values[index] - mean;
            sumSquaredDeviation += deviation * deviation;
        }

        return sumSquaredDeviation / (values.Count - 1);
    }

    /// <summary>
    /// Calculates sample variance using Bessel's correction.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The sample variance.</returns>
    public static double Variance(IReadOnlyList<double> values)
    {
        return SampleVariance(values);
    }

    /// <summary>
    /// Calculates sample standard deviation using Bessel's correction.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The sample standard deviation.</returns>
    public static double SampleStandardDeviation(IReadOnlyList<double> values)
    {
        return Math.Sqrt(SampleVariance(values));
    }

    /// <summary>
    /// Calculates population standard deviation.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The population standard deviation.</returns>
    public static double PopulationStandardDeviation(IReadOnlyList<double> values)
    {
        EnsureNotEmpty(values);

        var mean = Mean(values);
        var sumSquaredDeviation = 0d;
        for (var index = 0; index < values.Count; index++)
        {
            var deviation = values[index] - mean;
            sumSquaredDeviation += deviation * deviation;
        }

        return Math.Sqrt(sumSquaredDeviation / values.Count);
    }

    /// <summary>
    /// Calculates the median absolute deviation from the median.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The median absolute deviation.</returns>
    public static double MedianAbsoluteDeviation(IReadOnlyList<double> values)
    {
        EnsureNotEmpty(values);

        var median = Median(values);
        var deviations = new double[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            EnsureFinite(values[index], nameof(values));
            deviations[index] = Math.Abs(values[index] - median);
        }

        return Median(deviations);
    }

    /// <summary>
    /// Calculates Fisher-Pearson sample skewness.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The sample skewness, or 0 for a constant series.</returns>
    public static double Skewness(IReadOnlyList<double> values)
    {
        EnsureMinimumCount(values, 3);

        var mean = Mean(values);
        var standardDeviation = SampleStandardDeviation(values);
        if (standardDeviation == 0d)
        {
            return 0d;
        }

        var sumCubed = 0d;
        for (var index = 0; index < values.Count; index++)
        {
            sumCubed += Math.Pow((values[index] - mean) / standardDeviation, 3d);
        }

        var count = values.Count;
        return (count / ((count - 1d) * (count - 2d))) * sumCubed;
    }

    /// <summary>
    /// Calculates unbiased excess sample kurtosis.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The excess kurtosis, or 0 for a constant series.</returns>
    public static double ExcessKurtosis(IReadOnlyList<double> values)
    {
        EnsureMinimumCount(values, 4);

        var mean = Mean(values);
        var standardDeviation = SampleStandardDeviation(values);
        if (standardDeviation == 0d)
        {
            return 0d;
        }

        var sumFourth = 0d;
        for (var index = 0; index < values.Count; index++)
        {
            sumFourth += Math.Pow((values[index] - mean) / standardDeviation, 4d);
        }

        var count = values.Count;
        var first = (count * (count + 1d) * sumFourth) / ((count - 1d) * (count - 2d) * (count - 3d));
        var second = (3d * Math.Pow(count - 1d, 2d)) / ((count - 2d) * (count - 3d));
        return first - second;
    }

    /// <summary>
    /// Calculates non-excess kurtosis.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <returns>The kurtosis.</returns>
    public static double Kurtosis(IReadOnlyList<double> values)
    {
        return ExcessKurtosis(values) + 3d;
    }

    /// <summary>
    /// Calculates a percentile using linear interpolation between sorted observations.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <param name="percentile">The percentile in the interval [0, 100].</param>
    /// <returns>The interpolated percentile value.</returns>
    public static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        EnsureNotEmpty(values);

        if (double.IsNaN(percentile) || percentile < 0d || percentile > 100d)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Percentile must be in [0, 100].");
        }

        var sorted = values.ToArray();
        for (var index = 0; index < sorted.Length; index++)
        {
            EnsureFinite(sorted[index], nameof(values));
        }

        Array.Sort(sorted);

        if (sorted.Length == 1)
        {
            return sorted[0];
        }

        var position = (percentile / 100d) * (sorted.Length - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        var weight = position - lowerIndex;

        return sorted[lowerIndex] + ((sorted[upperIndex] - sorted[lowerIndex]) * weight);
    }

    /// <summary>
    /// Calculates a quantile using linear interpolation between sorted observations.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <param name="probability">The probability in the interval [0, 1].</param>
    /// <returns>The interpolated quantile value.</returns>
    public static double Quantile(IReadOnlyList<double> values, double probability)
    {
        if (double.IsNaN(probability) || probability < 0d || probability > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), probability, "Probability must be in [0, 1].");
        }

        return Percentile(values, probability * 100d);
    }

    /// <summary>
    /// Calculates sample covariance between two aligned series.
    /// </summary>
    /// <param name="x">The first finite series.</param>
    /// <param name="y">The second finite series.</param>
    /// <returns>The sample covariance.</returns>
    public static double Covariance(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        EnsureAlignedMinimumCount(x, y, 2);

        var meanX = Mean(x);
        var meanY = Mean(y);
        var sum = 0d;
        for (var index = 0; index < x.Count; index++)
        {
            sum += (x[index] - meanX) * (y[index] - meanY);
        }

        return sum / (x.Count - 1);
    }

    /// <summary>
    /// Calculates Pearson correlation between two aligned series.
    /// </summary>
    /// <param name="x">The first finite series.</param>
    /// <param name="y">The second finite series.</param>
    /// <returns>The Pearson correlation, or 0 when one series has zero variance.</returns>
    public static double Correlation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        var covariance = Covariance(x, y);
        var standardDeviationX = SampleStandardDeviation(x);
        var standardDeviationY = SampleStandardDeviation(y);
        var denominator = standardDeviationX * standardDeviationY;

        return denominator == 0d ? 0d : covariance / denominator;
    }

    /// <summary>
    /// Calculates sample autocorrelation at a positive lag.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <param name="lag">The lag in observations.</param>
    /// <returns>The autocorrelation, or 0 for a constant series.</returns>
    public static double Autocorrelation(IReadOnlyList<double> values, int lag)
    {
        EnsureMinimumCount(values, 2);

        if (lag <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lag), lag, "Lag must be positive.");
        }

        if (lag >= values.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(lag), lag, "Lag must be smaller than the sample count.");
        }

        var mean = Mean(values);
        var numerator = 0d;
        var denominator = 0d;
        for (var index = 0; index < values.Count; index++)
        {
            var centered = values[index] - mean;
            denominator += centered * centered;
            if (index >= lag)
            {
                numerator += centered * (values[index - lag] - mean);
            }
        }

        return denominator == 0d ? 0d : numerator / denominator;
    }

    /// <summary>
    /// Calculates partial autocorrelations from lag 1 through <paramref name="maximumLag"/>.
    /// </summary>
    /// <param name="values">The finite values to evaluate.</param>
    /// <param name="maximumLag">The maximum lag.</param>
    /// <returns>Partial autocorrelation values indexed from lag 1.</returns>
    public static IReadOnlyList<double> PartialAutocorrelation(IReadOnlyList<double> values, int maximumLag)
    {
        EnsureMinimumCount(values, 2);

        if (maximumLag <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLag), maximumLag, "Maximum lag must be positive.");
        }

        if (maximumLag >= values.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLag), maximumLag, "Maximum lag must be smaller than the sample count.");
        }

        var autocorrelations = new double[maximumLag + 1];
        autocorrelations[0] = 1d;
        for (var lag = 1; lag <= maximumLag; lag++)
        {
            autocorrelations[lag] = Autocorrelation(values, lag);
        }

        var pacf = new double[maximumLag];
        var previous = new double[maximumLag + 1];
        var current = new double[maximumLag + 1];
        var predictionErrorVariance = 1d;

        for (var lag = 1; lag <= maximumLag; lag++)
        {
            var numerator = autocorrelations[lag];
            for (var j = 1; j < lag; j++)
            {
                numerator -= previous[j] * autocorrelations[lag - j];
            }

            var reflection = predictionErrorVariance == 0d ? 0d : numerator / predictionErrorVariance;
            current[lag] = reflection;
            for (var j = 1; j < lag; j++)
            {
                current[j] = previous[j] - (reflection * previous[lag - j]);
            }

            pacf[lag - 1] = reflection;
            predictionErrorVariance *= 1d - (reflection * reflection);
            Array.Copy(current, previous, current.Length);
            Array.Clear(current);
        }

        return pacf;
    }

    /// <summary>
    /// Ensures a series contains at least one value.
    /// </summary>
    /// <param name="values">The series to validate.</param>
    internal static void EnsureNotEmpty(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            throw new ArgumentException("At least one value is required.", nameof(values));
        }
    }

    /// <summary>
    /// Ensures a series contains at least the requested number of values.
    /// </summary>
    /// <param name="values">The series to validate.</param>
    /// <param name="minimumCount">The minimum required count.</param>
    internal static void EnsureMinimumCount(IReadOnlyList<double> values, int minimumCount)
    {
        EnsureNotEmpty(values);

        if (values.Count < minimumCount)
        {
            throw new ArgumentException($"At least {minimumCount} values are required.", nameof(values));
        }
    }

    /// <summary>
    /// Ensures two aligned series have equal length and enough observations.
    /// </summary>
    /// <param name="x">The first series.</param>
    /// <param name="y">The second series.</param>
    /// <param name="minimumCount">The minimum required count.</param>
    internal static void EnsureAlignedMinimumCount(IReadOnlyList<double> x, IReadOnlyList<double> y, int minimumCount)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        if (x.Count != y.Count)
        {
            throw new ArgumentException("Series must have the same length.", nameof(y));
        }

        EnsureMinimumCount(x, minimumCount);
        EnsureMinimumCount(y, minimumCount);
    }

    /// <summary>
    /// Ensures a numerical value is finite.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The parameter name to report if validation fails.</param>
    internal static void EnsureFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("Values must be finite.", parameterName);
        }
    }
}
