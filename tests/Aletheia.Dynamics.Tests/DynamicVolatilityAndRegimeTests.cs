using Aletheia.Dynamics;

namespace Aletheia.Dynamics.Tests;

public sealed class DynamicVolatilityAndRegimeTests
{
    [Fact]
    public void EwmaVolatility_WithDeterministicSeries_ReturnsVariancePath()
    {
        var result = new EwmaVolatilityEstimator().Estimate([0.01d, -0.02d, 0.015d, 0.005d], 0.90d);

        Assert.Equal(4, result.VariancePath.Count);
        Assert.InRange(result.LastVolatility, 0d, 1d);
    }

    [Fact]
    public void Garch11Estimator_WithSyntheticVolatilityCluster_RespectsConstraints()
    {
        var values = Enumerable.Range(0, 120)
            .Select(index => index < 60 ? 0.003d * Math.Sin(index) : 0.02d * Math.Sin(index))
            .ToArray();

        var result = new Garch11Estimator().Fit(values);

        Assert.True(result.Converged, result.Diagnostic);
        Assert.True(result.Omega > 0d);
        Assert.True(result.Alpha >= 0d);
        Assert.True(result.Beta >= 0d);
        Assert.True(result.Persistence < 1d);
    }

    [Fact]
    public void Garch11FitResult_UpdatesConditionalVarianceWithEachNewObservation()
    {
        var fit = new Garch11FitResult(
            0.01d,
            0.20d,
            0.70d,
            0d,
            true,
            "unit",
            [0.04d],
            0d);

        var afterShock = fit.NextConditionalVariance(1d, 0.04d);
        var afterCalm = fit.NextConditionalVariance(0d, afterShock);

        Assert.Equal(0.238d, afterShock, 12);
        Assert.Equal(0.1766d, afterCalm, 12);
        Assert.NotEqual(afterShock, afterCalm);
    }

    [Fact]
    public void LocalLinearTrendKalmanModel_WithLinearSignal_RecoversPositiveTrend()
    {
        var observations = Enumerable.Range(0, 80)
            .Select(index => 10d + (0.25d * index))
            .ToArray();
        var model = new LocalLinearTrendKalmanModel();

        var fit = model.Filter(observations, observationVariance: 0.01d, levelVariance: 0.001d, trendVariance: 0.0001d);
        var forecast = model.Forecast(fit, 5);

        Assert.NotNull(fit.LastEstimate);
        Assert.InRange(fit.LastEstimate!.Trend, 0.20d, 0.30d);
        Assert.True(forecast[^1].ExpectedValue > observations[^1]);
    }

    [Fact]
    public void LocalLinearTrendKalmanModel_ForecastPropagatesLevelTrendCovariance()
    {
        var last = new KalmanStateEstimate(
            0,
            2d,
            2d,
            0.5d,
            4d,
            1d,
            0d,
            1d,
            0.75d);
        var filter = new KalmanFilterResult(
            [last],
            0d,
            0.25d,
            0.10d,
            0.20d);

        var forecast = new LocalLinearTrendKalmanModel().Forecast(filter, 2);

        Assert.Equal(6.85d, forecast[0].Variance, 12);
        Assert.Equal(11.65d, forecast[1].Variance, 12);
    }

    [Fact]
    public void GaussianHmm_WithTwoRegimeSeries_AssignsDifferentPosteriorStates()
    {
        var observations = Enumerable.Range(0, 80)
            .Select(index => index < 40 ? -0.02d + (0.001d * Math.Sin(index)) : 0.02d + (0.001d * Math.Sin(index)))
            .ToArray();

        var result = new GaussianHiddenMarkovModel().Fit(observations, stateCount: 2, maximumIterations: 50);
        var firstBest = BestState(result.PosteriorProbabilities, 5);
        var lastBest = BestState(result.PosteriorProbabilities, 75);

        Assert.NotEmpty(result.States);
        Assert.NotEqual(firstBest, lastBest);
        Assert.InRange(result.LatestProbabilities.Max(), 0.5d, 1d);
    }

    [Fact]
    public void GaussianHmm_FilterNext_UpdatesFilteredProbabilitiesWithoutSmoothing()
    {
        var observations = Enumerable.Range(0, 80)
            .Select(index => index < 40 ? -0.02d + (0.001d * Math.Sin(index)) : 0.02d + (0.001d * Math.Sin(index)))
            .ToArray();
        var model = new GaussianHiddenMarkovModel();
        var fit = model.Fit(observations, stateCount: 2, maximumIterations: 50);
        var previous = fit.LatestProbabilities;

        var updated = model.FilterNext(fit, previous, 0.025d);

        Assert.Equal(fit.States.Count, updated.Count);
        Assert.Equal(1d, updated.Sum(), 12);
        Assert.All(updated, probability => Assert.InRange(probability, 0d, 1d));
    }

    private static int BestState(double[,] posterior, int row)
    {
        var best = 0;
        for (var state = 1; state < posterior.GetLength(1); state++)
        {
            if (posterior[row, state] > posterior[row, best])
            {
                best = state;
            }
        }

        return best;
    }
}
