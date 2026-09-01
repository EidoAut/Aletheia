using Aletheia.Mathematics;

namespace Aletheia.Mathematics.Tests;

public sealed class DescriptiveStatisticsTests
{
    [Fact]
    public void SampleStandardDeviation_WithKnownValues_ReturnsExpectedValue()
    {
        var result = DescriptiveStatistics.SampleStandardDeviation([2d, 4d, 4d, 4d, 5d, 5d, 7d, 9d]);

        Assert.Equal(2.138089935, result, 9);
    }

    [Fact]
    public void Percentile_WithInterpolation_ReturnsExpectedValue()
    {
        var result = DescriptiveStatistics.Percentile([0d, 10d], 25d);

        Assert.Equal(2.5d, result, 9);
    }

    [Fact]
    public void RobustAndShapeStatistics_WithKnownValues_ReturnExpectedValues()
    {
        var values = new[] { 1d, 2d, 3d, 4d, 100d };

        Assert.Equal(3d, DescriptiveStatistics.Median(values), 12);
        Assert.Equal(1d, DescriptiveStatistics.MedianAbsoluteDeviation(values), 12);
        Assert.True(DescriptiveStatistics.Skewness(values) > 1d);
        Assert.True(double.IsFinite(DescriptiveStatistics.ExcessKurtosis(values)));
    }

    [Fact]
    public void Autocorrelation_WithAlternatingSeries_IsNegative()
    {
        var result = DescriptiveStatistics.Autocorrelation([1d, -1d, 1d, -1d, 1d, -1d], 1);

        Assert.True(result < 0d);
    }

    [Fact]
    public void CausalNormalizer_DoesNotUseFutureObservations()
    {
        var normalizer = new CausalNormalizer();
        var original = new[] { 1d, 2d, 3d, 4d, 5d, 6d };
        var shockedFuture = new[] { 1d, 2d, 3d, 4d, 5_000d, -9_000d };

        var first = normalizer.Normalize(original, CausalNormalizationMode.ExpandingZScore, minimumSamples: 2);
        var second = normalizer.Normalize(shockedFuture, CausalNormalizationMode.ExpandingZScore, minimumSamples: 2);

        for (var index = 0; index < 4; index++)
        {
            Assert.Equal(first[index].NormalizedValue, second[index].NormalizedValue, 12);
            Assert.Equal(first[index].Location, second[index].Location, 12);
            Assert.Equal(first[index].Scale, second[index].Scale, 12);
        }
    }
}
