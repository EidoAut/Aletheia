using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class StateSpaceForecastModelTests
{
    [Fact]
    public void Predict_ForHistoricalCutoff_DoesNotUseFutureObservations()
    {
        var baseHistory = CreateHistory(80, index => 100m + (index * 0.5m));
        var shockedHistory = CreateHistory(90, index => index < 80 ? 100m + (index * 0.5m) : 10_000m - (index * 100m));
        var model = new StateSpaceForecastModel(minimumLogReturns: 10);
        var first = PredictAtCutoff(model, baseHistory, 70);
        var second = PredictAtCutoff(model, shockedHistory, 70);

        Assert.Equal(first.ExpectedReturn, second.ExpectedReturn, 12);
        Assert.Equal(first.ProbabilityPositive, second.ProbabilityPositive, 12);
        Assert.Equal(first.Percentiles[50], second.Percentiles[50], 12);
    }

    [Fact]
    public void Predict_WhenProjectionExplodes_ReturnsModelRejected()
    {
        var history = CreateHistory(160, index => 100m * (decimal)Math.Exp(0.0008d * index * index));
        var model = new StateSpaceForecastModel(minimumLogReturns: 10);
        var result = PredictAtCutoffResult(model, history, 120, ForecastHorizon.CalendarDays(365));

        Assert.False(result.IsSuccess);
        Assert.Equal(ForecastStatus.ModelRejected, result.Status);
        Assert.Contains("plausibility", result.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Predict_ForNearlyFlatLowVolatilityNav_RemainsBounded()
    {
        var history = CreateHistory(280, index => 100m + ((decimal)Math.Sin(index / 11d) * 0.02m));
        var model = new StateSpaceForecastModel(minimumLogReturns: 10);
        var result = PredictAtCutoffResult(model, history, 240, ForecastHorizon.CalendarDays(365));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.InRange(result.Distribution!.ExpectedReturn, -0.10d, 0.10d);
        Assert.InRange(result.Distribution.MedianReturn, -0.10d, 0.10d);
    }

    [Fact]
    public void Predict_ForLogLinearNav_UsesTerminalLogNavProjection()
    {
        var history = CreateHistory(280, index => 100m * (decimal)Math.Exp(0.0001d * index));
        var model = new StateSpaceForecastModel(minimumLogReturns: 10);
        var result = PredictAtCutoffResult(model, history, 240, ForecastHorizon.CalendarDays(365));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.InRange(result.Distribution!.ExpectedReturn, 0d, 0.25d);
        Assert.InRange(result.Distribution.MedianReturn, 0d, 0.25d);
    }

    private static Aletheia.Forecasting.ForecastDistribution PredictAtCutoff(
        StateSpaceForecastModel model,
        FundHistory history,
        int cutoffIndex)
    {
        var prediction = PredictAtCutoffResult(model, history, cutoffIndex, ForecastHorizon.Observations(5));

        Assert.True(prediction.IsSuccess, prediction.FailureReason);
        return prediction.Distribution!;
    }

    private static ForecastPredictionResult PredictAtCutoffResult(
        StateSpaceForecastModel model,
        FundHistory history,
        int cutoffIndex,
        ForecastHorizon horizon)
    {
        var trainingSeries = new NavSeries(history.NavSeries.Points.Take(cutoffIndex + 1), ObservationFrequency.Daily);
        var dataset = new ForecastEvaluationDataset(
            history,
            new DatasetIdentity("unit", "fingerprint", null),
            "test");
        var resolution = new ForecastHorizonResolver().Resolve(horizon, trainingSeries.EndDate, trainingSeries.ObservationFrequency);
        var split = new WalkForwardSplit(0, cutoffIndex, cutoffIndex, cutoffIndex, cutoffIndex, null, trainingSeries.EndDate, resolution.TargetDate);
        var trainingContext = new ForecastTrainingContext(dataset, trainingSeries, split, resolution);
        var training = model.Train(trainingContext);
        return model.Predict(training, new ForecastPredictionContext(dataset, trainingSeries, split, resolution));
    }

    private static FundHistory CreateHistory(int count, Func<int, decimal> navFactory)
    {
        var fund = new Fund(new FundIdentifier(FundIdentifierKind.Local, "unit"), "Unit", "Unit", "EUR");
        var points = Enumerable.Range(0, count)
            .Select(index => new NavPoint(new DateOnly(2024, 1, 1).AddDays(index), navFactory(index)));
        return new FundHistory(fund, new NavSeries(points, ObservationFrequency.Daily));
    }
}
