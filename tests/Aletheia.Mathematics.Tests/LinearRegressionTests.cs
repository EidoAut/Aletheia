using Aletheia.Mathematics;

namespace Aletheia.Mathematics.Tests;

public sealed class LinearRegressionTests
{
    [Fact]
    public void Fit_WithPerfectLine_ReturnsExpectedSlopeAndIntercept()
    {
        var result = LinearRegression.Fit([0d, 1d, 2d, 3d], [1d, 3d, 5d, 7d]);

        Assert.Equal(1d, result.Intercept, 9);
        Assert.Equal(2d, result.Slope, 9);
        Assert.Equal(1d, result.RSquared, 9);
    }
}
