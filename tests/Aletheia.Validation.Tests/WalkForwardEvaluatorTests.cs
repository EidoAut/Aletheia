using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class WalkForwardEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_ForPredictableArSeries_ArModelImprovesOnZeroBaseline()
    {
        var dataset = CreateDataset(CreateArSeries(700, 0.70d, 0.002d, 0d));
        var options = new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = 250,
            ForecastHorizon = ForecastHorizon.Observations(5),
            StepSize = 10,
            MinimumEvaluationSamples = 10,
        };
        var evaluator = new WalkForwardEvaluator();

        var zero = await evaluator.EvaluateAsync(new ZeroReturnForecastModel(), dataset, options);
        var ar = await evaluator.EvaluateAsync(new AutoregressiveForecastModel(), dataset, options);

        Assert.True(ar.Samples.Count >= options.MinimumEvaluationSamples);
        Assert.True(ar.AllSamplesMetrics.Point.MeanAbsoluteError < zero.AllSamplesMetrics.Point.MeanAbsoluteError);
    }

    [Fact]
    public async Task EvaluateAsync_ForIidSeries_HistoricalAnaloguesDoNotShowSuspiciousDirectionalSkill()
    {
        var dataset = CreateDataset(CreateIidSeries(650, 1234));
        var options = new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = 220,
            ForecastHorizon = ForecastHorizon.Observations(5),
            StepSize = 10,
            MinimumEvaluationSamples = 10,
        };
        var evaluator = new WalkForwardEvaluator();

        var analogues = await evaluator.EvaluateAsync(
            new HistoricalAnalogueForecastModel(maximumAnalogues: 20, minimumAnalogues: 5, exclusionWindowObservations: 10),
            dataset,
            options);

        Assert.True(analogues.Samples.Count >= options.MinimumEvaluationSamples);
        Assert.True(analogues.AllSamplesMetrics.Point.DirectionalAccuracy < 0.85d);
    }

    [Fact]
    public void ForecastModels_DoNotChangeWhenFuturePathIsRewrittenAfterCutoff()
    {
        var prefix = CreateArSeries(260, 0.50d, 0.001d, 0.0001d).Points.ToArray();
        var baseSeries = new NavSeries(prefix.Concat(CreateTail(prefix[^1].Date, prefix[^1].Value, 80, 0.001d)), ObservationFrequency.Daily);
        var shockedSeries = new NavSeries(prefix.Concat(CreateTail(prefix[^1].Date, prefix[^1].Value, 80, -0.05d)), ObservationFrequency.Daily);
        var cutoffIndex = 240;
        var horizon = ForecastHorizon.Observations(5);
        var models = new IForecastModel[]
        {
            new ZeroReturnForecastModel(),
            new HistoricalMeanForecastModel(lookbackObservations: 120, minimumSamples: 10),
            new AutoregressiveForecastModel(),
            new HistoricalAnalogueForecastModel(maximumAnalogues: 15, minimumAnalogues: 5, exclusionWindowObservations: 10),
        };

        foreach (var model in models)
        {
            var first = ForecastAtCutoff(model, baseSeries, cutoffIndex, horizon);
            var second = ForecastAtCutoff(model, shockedSeries, cutoffIndex, horizon);

            Assert.Equal(first.ExpectedReturn, second.ExpectedReturn, 12);
            Assert.Equal(first.MedianReturn, second.MedianReturn, 12);
            Assert.Equal(first.ProbabilityPositive, second.ProbabilityPositive, 12);
        }
    }

    [Fact]
    public void ZeroReturnForecastModel_AdvertisesPointCapabilityOnly()
    {
        var series = CreateArSeries(40, 0.10d, 0.001d, 0d);

        var distribution = ForecastAtCutoff(
            new ZeroReturnForecastModel(),
            series,
            30,
            ForecastHorizon.Observations(3));

        Assert.Equal(ForecastCapabilities.PointForecast, distribution.Capabilities);
        Assert.Equal(PointForecastStatistic.ExplicitModelPoint, distribution.PointForecastStatistic);
        Assert.Equal(0d, distribution.PointForecastReturn, 12);
        Assert.Equal(0d, distribution.ProbabilityPositive, 12);
        Assert.Empty(distribution.Percentiles);
    }

    [Fact]
    public void HistoricalProbabilityBaseline_UsesOnlyTrainingOutcomes()
    {
        var prefix = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 1), 100m),
            new NavPoint(new DateOnly(2024, 1, 2), 110m),
            new NavPoint(new DateOnly(2024, 1, 3), 100m),
            new NavPoint(new DateOnly(2024, 1, 4), 120m),
            new NavPoint(new DateOnly(2024, 1, 5), 110m),
        };
        var calmFuture = new NavSeries(prefix.Append(new NavPoint(new DateOnly(2024, 1, 6), 1000m)), ObservationFrequency.Daily);
        var shockedFuture = new NavSeries(prefix.Append(new NavPoint(new DateOnly(2024, 1, 6), 10m)), ObservationFrequency.Daily);
        var model = new HistoricalProbabilityBaselineForecastModel(minimumSamples: 1);

        var first = ForecastAtCutoff(model, calmFuture, 4, ForecastHorizon.Observations(1));
        var second = ForecastAtCutoff(model, shockedFuture, 4, ForecastHorizon.Observations(1));

        Assert.Equal(ForecastCapabilities.ProbabilityPositive, first.Capabilities);
        Assert.Equal(0.5d, first.ProbabilityPositive, 12);
        Assert.Equal(first.ProbabilityPositive, second.ProbabilityPositive, 12);
        Assert.Empty(first.Percentiles);
    }

    [Fact]
    public void WalkForwardEvaluationOptions_WhenRefitEveryStepIsFalse_RejectsUnsupportedConfiguration()
    {
        var options = new WalkForwardEvaluationOptions
        {
            RefitEveryStep = false,
        };

        Assert.Throws<NotSupportedException>(() => options.Validate());
    }

    [Fact]
    public void DynamicStatePipeline_IgnoresDataAfterTargetIndex()
    {
        var prefix = CreateArSeries(260, 0.50d, 0.001d, 0.0001d).Points.ToArray();
        var baseSeries = new NavSeries(prefix.Concat(CreateTail(prefix[^1].Date, prefix[^1].Value, 80, 0.001d)), ObservationFrequency.Daily);
        var shockedSeries = new NavSeries(prefix.Concat(CreateTail(prefix[^1].Date, prefix[^1].Value, 80, -0.05d)), ObservationFrequency.Daily);
        var pipeline = new Aletheia.Dynamics.DynamicStateFeaturePipeline();

        var first = pipeline.Build(baseSeries, 240);
        var second = pipeline.Build(shockedSeries, 240);

        foreach (var dimension in first.Dimensions.Keys)
        {
            Assert.Equal(first.Dimensions[dimension], second.Dimensions[dimension], 12);
        }
    }

    private static Aletheia.Forecasting.ForecastDistribution ForecastAtCutoff(
        IForecastModel model,
        NavSeries fullSeries,
        int cutoffIndex,
        ForecastHorizon horizon)
    {
        var training = new NavSeries(fullSeries.Points.Take(cutoffIndex + 1), fullSeries.ObservationFrequency);
        var split = new WalkForwardSplit(
            0,
            cutoffIndex,
            cutoffIndex + 1,
            cutoffIndex + horizon.Value,
            cutoffIndex,
            cutoffIndex + horizon.Value,
            fullSeries[cutoffIndex].Date,
            fullSeries[cutoffIndex + horizon.Value].Date);
        var resolution = new ForecastHorizonResolver().Resolve(horizon, fullSeries[cutoffIndex].Date, fullSeries.ObservationFrequency);
        var dataset = CreateDataset(fullSeries);
        var trainingContext = new ForecastTrainingContext(dataset, training, split, resolution);
        var predictionContext = new ForecastPredictionContext(dataset, training, split, resolution);
        var fit = model.Train(trainingContext);
        Assert.True(fit.IsSuccess, fit.FailureReason);
        var prediction = model.Predict(fit, predictionContext);
        Assert.True(prediction.IsSuccess, prediction.FailureReason);
        return prediction.Distribution!;
    }

    private static ForecastEvaluationDataset CreateDataset(NavSeries series)
    {
        var fund = new Fund(new FundIdentifier(FundIdentifierKind.Local, "synthetic"), "Synthetic");
        return new ForecastEvaluationDataset(
            new FundHistory(fund, series),
            new DatasetIdentity("Synthetic", "fingerprint", null));
    }

    private static NavSeries CreateArSeries(int count, double phi, double intercept, double alternatingNoise)
    {
        var points = new List<NavPoint>();
        var date = new DateOnly(2020, 1, 1);
        var nav = 100d;
        var previousReturn = 0.01d;
        for (var index = 0; index < count; index++)
        {
            var noise = alternatingNoise * (index % 2 == 0 ? 1d : -1d);
            previousReturn = intercept + (phi * previousReturn) + noise;
            nav *= Math.Exp(previousReturn);
            points.Add(new NavPoint(date.AddDays(index), (decimal)nav));
        }

        return new NavSeries(points, ObservationFrequency.Daily);
    }

    private static NavSeries CreateIidSeries(int count, int seed)
    {
        var random = new Random(seed);
        var points = new List<NavPoint>();
        var date = new DateOnly(2020, 1, 1);
        var nav = 100d;
        for (var index = 0; index < count; index++)
        {
            nav *= Math.Exp(0.01d * NextGaussian(random));
            points.Add(new NavPoint(date.AddDays(index), (decimal)nav));
        }

        return new NavSeries(points, ObservationFrequency.Daily);
    }

    private static IEnumerable<NavPoint> CreateTail(DateOnly lastPrefixDate, decimal lastPrefixValue, int count, double logReturn)
    {
        var nav = (double)lastPrefixValue;
        for (var index = 1; index <= count; index++)
        {
            nav *= Math.Exp(logReturn);
            yield return new NavPoint(lastPrefixDate.AddDays(index), (decimal)nav);
        }
    }

    private static double NextGaussian(Random random)
    {
        var u1 = 1d - random.NextDouble();
        var u2 = 1d - random.NextDouble();
        return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
    }
}
