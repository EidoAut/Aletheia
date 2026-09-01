using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Forecasting.Tests;

public sealed class ForecastDistributionTests
{
    [Fact]
    public void FromSamples_WithKnownSamples_CalculatesProbabilities()
    {
        var distribution = ForecastDistribution.FromSamples(
            new ForecastHorizonResolution(
                ForecastHorizon.Observations(30),
                ObservationFrequency.BusinessDaily,
                30,
                null,
                "UnitTestPolicy",
                false),
            [-0.20d, -0.01d, 0.02d, 0.08d]);

        Assert.Equal(0.5d, distribution.ProbabilityPositive, 9);
        Assert.Equal(0.25d, distribution.ProbabilityReturnGreaterThanFivePercent, 9);
        Assert.Equal(0.25d, distribution.ProbabilityLossGreaterThanTenPercent, 9);
    }

    [Fact]
    public void CapabilityAccessors_ReturnNullForUnsupportedQuantities()
    {
        var distribution = new ForecastDistribution(
            CreateHorizon(),
            0d,
            0d,
            new Dictionary<int, double>(),
            0.72d,
            0d,
            0d,
            ForecastCapabilities.ProbabilityPositive,
            PointForecastStatistic.None,
            0d);

        Assert.Null(distribution.ExpectedReturnOrNull);
        Assert.Null(distribution.PointForecastReturnOrNull);
        Assert.Null(distribution.MedianReturnOrNull);
        Assert.Equal(0.72d, distribution.ProbabilityPositiveOrNull);
        Assert.True(distribution.Supports(ForecastCapabilities.ProbabilityPositive));
        Assert.False(distribution.Supports(ForecastCapabilities.ExpectedReturn));
    }

    private static ForecastHorizonResolution CreateHorizon()
    {
        return new ForecastHorizonResolver().Resolve(
            ForecastHorizon.CalendarDays(30),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Daily);
    }
}
