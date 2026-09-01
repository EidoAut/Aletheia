using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class WalkForwardSplitterTests
{
    [Fact]
    public void CreateExpandingWindowSplits_ReturnsChronologicalSplits()
    {
        var splitter = new WalkForwardSplitter();

        var splits = splitter.CreateExpandingWindowSplits(10, 4, 2, 2);

        Assert.Equal(3, splits.Count);
        Assert.Equal(0, splits[0].TrainStartIndex);
        Assert.Equal(3, splits[0].TrainEndIndex);
        Assert.Equal(4, splits[0].TestStartIndex);
        Assert.Equal(5, splits[0].TestEndIndex);
    }

    [Fact]
    public void CreateSplits_ForObservationHorizon_ReturnsManuallyCalculatedCount()
    {
        var series = CreateSeries(1000);
        var splitter = new WalkForwardSplitter();
        var options = new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = 500,
            ForecastHorizon = ForecastHorizon.Observations(20),
            StepSize = 1,
        };

        var splits = splitter.CreateSplits(series, options);

        Assert.Equal(481, splits.Count);
        Assert.Equal(499, splits[0].PredictionCutoffIndex);
        Assert.Equal(519, splits[0].TargetIndex);
        Assert.Equal(979, splits[^1].PredictionCutoffIndex);
        Assert.Equal(999, splits[^1].TargetIndex);
    }

    [Fact]
    public void CreateSplits_ForRollingWindow_KeepsFixedTrainingLength()
    {
        var series = CreateSeries(300);
        var splitter = new WalkForwardSplitter();
        var options = new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = 100,
            ForecastHorizon = ForecastHorizon.Observations(10),
            StepSize = 50,
            WindowMode = TrainingWindowMode.Rolling,
            TrainingWindowLength = 100,
        };

        var splits = splitter.CreateSplits(series, options);

        Assert.Equal(4, splits.Count);
        Assert.Equal(0, splits[0].TrainStartIndex);
        Assert.Equal(99, splits[0].TrainEndIndex);
        Assert.Equal(50, splits[1].TrainStartIndex);
        Assert.Equal(149, splits[1].TrainEndIndex);
    }

    [Fact]
    public void CreateSplits_WhenNonOverlappingRequired_SkipsOverlappingTargetWindows()
    {
        var series = CreateSeries(100);
        var splitter = new WalkForwardSplitter();
        var options = new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = 20,
            ForecastHorizon = ForecastHorizon.Observations(10),
            StepSize = 1,
            RequireNonOverlappingTargets = true,
        };

        var splits = splitter.CreateSplits(series, options);

        Assert.Equal([19, 29, 39, 49, 59, 69, 79, 89], splits.Select(split => split.PredictionCutoffIndex));
    }

    private static NavSeries CreateSeries(int count)
    {
        var date = new DateOnly(2020, 1, 1);
        var points = Enumerable.Range(0, count)
            .Select(index => new NavPoint(date.AddDays(index), 100m + index))
            .ToArray();
        return new NavSeries(points, ObservationFrequency.Daily);
    }
}
