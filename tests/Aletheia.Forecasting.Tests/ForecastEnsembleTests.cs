using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Forecasting.Tests;

public sealed class ForecastEnsembleTests
{
    [Fact]
    public void Combine_UsesOnlyEligibleMembers()
    {
        var horizon = new ForecastHorizonResolver().Resolve(
            ForecastHorizon.CalendarDays(30),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Daily);
        var first = new ForecastDistribution(horizon, 0.05d, 0.04d, new Dictionary<int, double> { [50] = 0.04d }, 0.70d, 0.45d, 0.05d);
        var second = new ForecastDistribution(horizon, -0.50d, -0.40d, new Dictionary<int, double> { [50] = -0.40d }, 0.10d, 0.02d, 0.80d);

        var result = new ForecastEnsemble().Combine(
        [
            new ForecastEnsembleMember("eligible", first, 0.01d, 0d, true),
            new ForecastEnsembleMember("ineligible", second, 0.001d, 0d, false),
        ]);

        Assert.NotNull(result.Distribution);
        Assert.Single(result.Components);
        Assert.Equal("eligible", result.Components[0].ModelId);
        Assert.Equal(0.05d, result.Distribution!.ExpectedReturn, 12);
    }

    [Fact]
    public void Combine_ExcludesEligibleMembersWithoutRequiredDistributionCapabilities()
    {
        var horizon = new ForecastHorizonResolver().Resolve(
            ForecastHorizon.CalendarDays(30),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Daily);
        var full = new ForecastDistribution(horizon, 0.05d, 0.04d, new Dictionary<int, double> { [50] = 0.04d }, 0.70d, 0.45d, 0.05d);
        var probabilityOnly = new ForecastDistribution(
            horizon,
            0d,
            0d,
            new Dictionary<int, double>(),
            0.85d,
            0d,
            0d,
            ForecastCapabilities.ProbabilityPositive,
            PointForecastStatistic.None,
            0d);

        var result = new ForecastEnsemble().Combine(
        [
            new ForecastEnsembleMember("probability-only", probabilityOnly, 0.001d, 0d, true),
            new ForecastEnsembleMember("full", full, 0.050d, 0d, true),
        ]);

        Assert.NotNull(result.Distribution);
        Assert.Single(result.Components);
        Assert.Equal("full", result.Components[0].ModelId);
    }

    [Fact]
    public void Combine_ReturnsNoDistributionWhenEveryEligibleMemberLacksRequiredCapabilities()
    {
        var horizon = new ForecastHorizonResolver().Resolve(
            ForecastHorizon.CalendarDays(30),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Daily);
        var probabilityOnly = new ForecastDistribution(
            horizon,
            0d,
            0d,
            new Dictionary<int, double>(),
            0.85d,
            0d,
            0d,
            ForecastCapabilities.ProbabilityPositive,
            PointForecastStatistic.None,
            0d);

        var result = new ForecastEnsemble().Combine(
        [
            new ForecastEnsembleMember("probability-only", probabilityOnly, 0.001d, 0d, true),
        ]);

        Assert.Null(result.Distribution);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void Combine_ComputesQuantilesFromMixtureDistribution()
    {
        var horizon = new ForecastHorizonResolver().Resolve(
            ForecastHorizon.CalendarDays(365),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Daily);
        var zeroMass = new ForecastDistribution(
            horizon,
            0d,
            0d,
            new Dictionary<int, double> { [10] = 0d, [25] = 0d, [50] = 0d, [75] = 0d, [90] = 0d },
            0d,
            0d,
            0d);
        var tenMass = new ForecastDistribution(
            horizon,
            10d,
            10d,
            new Dictionary<int, double> { [10] = 10d, [25] = 10d, [50] = 10d, [75] = 10d, [90] = 10d },
            1d,
            1d,
            0d);

        var result = new ForecastEnsemble().Combine(
        [
            new ForecastEnsembleMember("zero", zeroMass, 0d, 0d, true),
            new ForecastEnsembleMember("ten", tenMass, Math.Log(9d), 0d, true),
        ],
        lambda: 1d);

        Assert.NotNull(result.Distribution);
        Assert.Equal(1d, result.Distribution!.ExpectedReturn, 12);
        Assert.Equal(0d, result.Distribution.Percentiles[50], 12);
        Assert.Equal(0d, result.Distribution.Percentiles[90], 12);
    }

    [Fact]
    public void Combine_IgnoresValidationEvidenceFromDifferentHorizon()
    {
        var horizon365 = new ForecastHorizonResolver().Resolve(
            ForecastHorizon.CalendarDays(365),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Daily);
        var forecastA = new ForecastDistribution(horizon365, 0.10d, 0.08d, new Dictionary<int, double> { [50] = 0.08d }, 0.65d, 0.45d, 0.05d);
        var forecastB = new ForecastDistribution(horizon365, 0.20d, 0.18d, new Dictionary<int, double> { [50] = 0.18d }, 0.75d, 0.55d, 0.02d);
        var polluted = new ForecastDistribution(horizon365, 9d, 9d, new Dictionary<int, double> { [50] = 9d }, 1d, 1d, 0d);
        var mismatchedHorizon = ForecastHorizon.CalendarDays(90);
        var cleanMembers = new[]
        {
            new ForecastEnsembleMember("a", forecastA, 0.02d, 0d, true, ForecastHorizon.CalendarDays(365), 30),
            new ForecastEnsembleMember("b", forecastB, 0.03d, 0d, true, ForecastHorizon.CalendarDays(365), 30),
            new ForecastEnsembleMember("polluted", polluted, 0d, 0d, true, mismatchedHorizon, 1000),
        };
        var changedOnly90DayEvidence = new[]
        {
            cleanMembers[0],
            cleanMembers[1],
            cleanMembers[2] with { ValidatedLoss = 100d, CalibrationPenalty = 100d },
        };

        var first = new ForecastEnsemble().Combine(cleanMembers);
        var second = new ForecastEnsemble().Combine(changedOnly90DayEvidence);

        Assert.NotNull(first.Distribution);
        Assert.NotNull(second.Distribution);
        Assert.Equal(first.Distribution!.ExpectedReturn, second.Distribution!.ExpectedReturn, 12);
        Assert.Equal(first.Components.Select(component => component.ModelId), second.Components.Select(component => component.ModelId));
        Assert.DoesNotContain(first.Components, component => component.ModelId == "polluted");
    }
}
