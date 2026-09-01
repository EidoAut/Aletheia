using Aletheia.Core;
using Aletheia.Dynamics;

namespace Aletheia.Dynamics.Tests;

public sealed class DynamicStateEstimatorTests
{
    [Fact]
    public void Estimate_WithSufficientSeries_ProducesExpectedDimensions()
    {
        var pipeline = new DynamicStateFeaturePipeline(options: new DynamicStateEstimatorOptions { FullDataAdequacyObservationCount = 5 });
        var estimator = new DynamicStateEstimator(pipeline);
        var series = new NavSeries(
            Enumerable.Range(0, 20).Select(index =>
                new NavPoint(new DateOnly(2024, 1, 1).AddDays(index), 100m + index)),
            ObservationFrequency.BusinessDaily);

        var state = estimator.Estimate(series);

        Assert.True(state.DataAdequacy > 0d);
        Assert.True(state.Dimensions.ContainsKey(StandardStateDimensions.Momentum));
        Assert.True(state.Dimensions.ContainsKey(StandardStateDimensions.Volatility));
        Assert.True(state.Dimensions.ContainsKey(StandardStateDimensions.SimpleReturn));
        Assert.True(state.Dimensions.ContainsKey(StandardStateDimensions.LogReturn));
        Assert.True(state.Dimensions.ContainsKey(StandardStateDimensions.LogNavAccelerationPerObservationSquared));
        Assert.Equal("v1.2", state.Schema?.Version);
        Assert.False(string.IsNullOrWhiteSpace(state.Schema?.Fingerprint));
    }

    [Fact]
    public void Build_ForHistoricalIndex_IsNotChangedByFutureObservations()
    {
        var pipeline = new DynamicStateFeaturePipeline(options: new DynamicStateEstimatorOptions { FullDataAdequacyObservationCount = 5 });
        var baseSeries = new NavSeries(
            Enumerable.Range(0, 20).Select(index =>
                new NavPoint(new DateOnly(2024, 1, 1).AddDays(index), 100m + index)),
            ObservationFrequency.BusinessDaily);
        var extendedSeries = new NavSeries(
            baseSeries.Points.Concat(
            [
                new NavPoint(new DateOnly(2024, 1, 21), 1_000m),
                new NavPoint(new DateOnly(2024, 1, 22), 5m),
            ]),
            ObservationFrequency.BusinessDaily);

        var stateBeforeFuture = pipeline.Build(baseSeries, 19);
        var stateAfterFuture = pipeline.Build(extendedSeries, 19);

        foreach (var dimension in stateBeforeFuture.Dimensions.Keys)
        {
            Assert.Equal(stateBeforeFuture.Dimensions[dimension], stateAfterFuture.Dimensions[dimension], 12);
        }
    }

    [Fact]
    public void SchemaFingerprint_ChangesWhenFeatureConfigurationChanges()
    {
        var shortMomentum = new DynamicStateFeaturePipeline(options: new DynamicStateEstimatorOptions { MomentumLookback = 10 });
        var longMomentum = new DynamicStateFeaturePipeline(options: new DynamicStateEstimatorOptions { MomentumLookback = 20 });

        Assert.NotEqual(shortMomentum.Schema.Fingerprint, longMomentum.Schema.Fingerprint);
    }
}
