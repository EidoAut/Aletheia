namespace Aletheia.Mathematics;

/// <summary>
/// Calculates first-component principal component analysis for state-space exploration.
/// </summary>
/// <remarks>
/// This implementation is intentionally limited to the first component. It is
/// enough to establish PCA infrastructure and tests, while keeping the initial
/// algorithm readable and avoiding a dependency leak from external math libraries.
/// </remarks>
public sealed class PrincipalComponentAnalysis
{
    private readonly PcaOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalComponentAnalysis"/> class.
    /// </summary>
    /// <param name="options">The PCA options.</param>
    public PrincipalComponentAnalysis(PcaOptions? options = null)
    {
        this.options = options ?? new PcaOptions();
    }

    /// <summary>
    /// Fits the first principal component.
    /// </summary>
    /// <param name="observations">Rows of observations, where each row contains aligned feature values.</param>
    /// <returns>The fitted PCA result.</returns>
    public PcaResult Fit(IReadOnlyList<IReadOnlyList<double>> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        if (observations.Count < 2)
        {
            throw new ArgumentException("At least two observations are required for PCA.", nameof(observations));
        }

        var featureCount = observations[0].Count;
        if (featureCount == 0)
        {
            throw new ArgumentException("At least one feature is required for PCA.", nameof(observations));
        }

        var matrix = new double[observations.Count][];
        for (var row = 0; row < observations.Count; row++)
        {
            if (observations[row].Count != featureCount)
            {
                throw new ArgumentException("All observations must have the same number of features.", nameof(observations));
            }

            matrix[row] = observations[row].ToArray();
        }

        var means = this.CalculateMeans(matrix, featureCount);
        var standardDeviations = this.CalculateStandardDeviations(matrix, means, featureCount);
        var centered = this.CenterAndScale(matrix, means, standardDeviations);
        var covariance = this.CalculateCovarianceMatrix(centered, featureCount);
        var loadings = this.CalculateDominantEigenvector(covariance);
        var explainedVariance = this.RayleighQuotient(covariance, loadings);
        var totalVariance = 0d;

        for (var index = 0; index < featureCount; index++)
        {
            totalVariance += covariance[index][index];
        }

        var scores = this.Project(centered, loadings);
        var explainedVarianceRatio = totalVariance == 0d ? 0d : explainedVariance / totalVariance;

        return new PcaResult(
            means,
            standardDeviations,
            loadings,
            scores,
            explainedVariance,
            explainedVarianceRatio);
    }

    private double[] CalculateMeans(double[][] matrix, int featureCount)
    {
        var means = new double[featureCount];
        for (var feature = 0; feature < featureCount; feature++)
        {
            var sum = 0d;
            for (var row = 0; row < matrix.Length; row++)
            {
                DescriptiveStatistics.EnsureFinite(matrix[row][feature], nameof(matrix));
                sum += matrix[row][feature];
            }

            means[feature] = sum / matrix.Length;
        }

        return means;
    }

    private double[] CalculateStandardDeviations(double[][] matrix, double[] means, int featureCount)
    {
        var standardDeviations = new double[featureCount];
        for (var feature = 0; feature < featureCount; feature++)
        {
            var sumSquaredDeviation = 0d;
            for (var row = 0; row < matrix.Length; row++)
            {
                var deviation = matrix[row][feature] - means[feature];
                sumSquaredDeviation += deviation * deviation;
            }

            var standardDeviation = Math.Sqrt(sumSquaredDeviation / (matrix.Length - 1));
            standardDeviations[feature] = this.options.Standardize && standardDeviation > 0d
                ? standardDeviation
                : 1d;
        }

        return standardDeviations;
    }

    private double[][] CenterAndScale(double[][] matrix, double[] means, double[] standardDeviations)
    {
        var centered = new double[matrix.Length][];
        for (var row = 0; row < matrix.Length; row++)
        {
            centered[row] = new double[means.Length];
            for (var feature = 0; feature < means.Length; feature++)
            {
                centered[row][feature] = (matrix[row][feature] - means[feature]) / standardDeviations[feature];
            }
        }

        return centered;
    }

    private double[][] CalculateCovarianceMatrix(double[][] centered, int featureCount)
    {
        var covariance = new double[featureCount][];
        for (var row = 0; row < featureCount; row++)
        {
            covariance[row] = new double[featureCount];
        }

        for (var row = 0; row < featureCount; row++)
        {
            for (var column = row; column < featureCount; column++)
            {
                var sum = 0d;
                for (var observation = 0; observation < centered.Length; observation++)
                {
                    sum += centered[observation][row] * centered[observation][column];
                }

                var value = sum / (centered.Length - 1);
                covariance[row][column] = value;
                covariance[column][row] = value;
            }
        }

        return covariance;
    }

    private double[] CalculateDominantEigenvector(double[][] covariance)
    {
        var vector = Enumerable.Repeat(1d / Math.Sqrt(covariance.Length), covariance.Length).ToArray();

        for (var iteration = 0; iteration < this.options.MaximumIterations; iteration++)
        {
            var next = this.Multiply(covariance, vector);
            this.NormalizeInPlace(next);

            var delta = this.EuclideanDistance(vector, next);
            vector = next;

            if (delta <= this.options.Tolerance)
            {
                break;
            }
        }

        return vector;
    }

    private double[] Multiply(double[][] matrix, double[] vector)
    {
        var result = new double[vector.Length];
        for (var row = 0; row < matrix.Length; row++)
        {
            var sum = 0d;
            for (var column = 0; column < vector.Length; column++)
            {
                sum += matrix[row][column] * vector[column];
            }

            result[row] = sum;
        }

        return result;
    }

    private void NormalizeInPlace(double[] vector)
    {
        var norm = Math.Sqrt(vector.Sum(value => value * value));
        if (norm == 0d)
        {
            vector[0] = 1d;
            return;
        }

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] /= norm;
        }
    }

    private double EuclideanDistance(double[] x, double[] y)
    {
        var sum = 0d;
        for (var index = 0; index < x.Length; index++)
        {
            var difference = x[index] - y[index];
            sum += difference * difference;
        }

        return Math.Sqrt(sum);
    }

    private double RayleighQuotient(double[][] matrix, double[] vector)
    {
        var multiplied = this.Multiply(matrix, vector);
        var numerator = 0d;
        for (var index = 0; index < vector.Length; index++)
        {
            numerator += vector[index] * multiplied[index];
        }

        return numerator;
    }

    private double[] Project(double[][] centered, double[] loadings)
    {
        var scores = new double[centered.Length];
        for (var row = 0; row < centered.Length; row++)
        {
            var sum = 0d;
            for (var feature = 0; feature < loadings.Length; feature++)
            {
                sum += centered[row][feature] * loadings[feature];
            }

            scores[row] = sum;
        }

        return scores;
    }
}
