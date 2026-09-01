namespace Aletheia.Mathematics;

/// <summary>
/// Fits a univariate ordinary least squares line.
/// </summary>
/// <remarks>
/// Milestone 1 uses this for first-order trend estimation. The algorithm is
/// intentionally transparent: slope is covariance divided by variance.
/// </remarks>
public static class LinearRegression
{
    /// <summary>
    /// Fits <c>y = intercept + slope * x</c>.
    /// </summary>
    /// <param name="x">The finite explanatory values.</param>
    /// <param name="y">The finite response values.</param>
    /// <returns>The fitted line and in-sample <c>R^2</c>.</returns>
    public static LinearRegressionResult Fit(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        DescriptiveStatistics.EnsureAlignedMinimumCount(x, y, 2);

        var meanX = DescriptiveStatistics.Mean(x);
        var meanY = DescriptiveStatistics.Mean(y);
        var sumSquaredX = 0d;
        var sumCross = 0d;
        var totalSumSquares = 0d;

        for (var index = 0; index < x.Count; index++)
        {
            var xDeviation = x[index] - meanX;
            var yDeviation = y[index] - meanY;
            sumSquaredX += xDeviation * xDeviation;
            sumCross += xDeviation * yDeviation;
            totalSumSquares += yDeviation * yDeviation;
        }

        if (sumSquaredX == 0d)
        {
            return new LinearRegressionResult(meanY, 0d, 0d);
        }

        var slope = sumCross / sumSquaredX;
        var intercept = meanY - (slope * meanX);
        var residualSumSquares = 0d;

        for (var index = 0; index < x.Count; index++)
        {
            var residual = y[index] - (intercept + (slope * x[index]));
            residualSumSquares += residual * residual;
        }

        var rSquared = totalSumSquares == 0d ? 1d : 1d - (residualSumSquares / totalSumSquares);
        return new LinearRegressionResult(intercept, slope, rSquared);
    }
}
