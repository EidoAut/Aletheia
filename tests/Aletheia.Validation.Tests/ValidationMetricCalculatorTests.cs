using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class ValidationMetricCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsManualPointProbabilityAndDistributionMetrics()
    {
        var samples = new[]
        {
            CreateSample(expectedReturn: 0.05d, probabilityPositive: 0.70d, actualReturn: 0.03d),
            CreateSample(expectedReturn: -0.01d, probabilityPositive: 0.40d, actualReturn: 0.02d),
        };
        var calculator = new ValidationMetricCalculator();

        var metrics = calculator.Calculate(samples, new CalibrationOptions { BinCount = 2 });

        Assert.Equal(MetricStatus.Available, metrics.Point.Status);
        Assert.Equal(2, metrics.Point.SampleCount);
        Assert.Equal(0.025d, metrics.Point.MeanAbsoluteError!.Value, 12);
        Assert.Equal(0.00065d, metrics.Point.MeanSquaredError!.Value, 12);
        Assert.Equal(Math.Sqrt(0.00065d), metrics.Point.RootMeanSquaredError!.Value, 12);
        Assert.Equal(0.5d, metrics.Point.DirectionalAccuracy!.Value, 12);
        Assert.Equal(MetricStatus.Available, metrics.Probability.Status);
        Assert.Equal(0.225d, metrics.Probability.BrierScore!.Value, 12);
        Assert.Equal(2, metrics.Probability.CalibrationBins.Count);
        Assert.Equal(1, metrics.Probability.CalibrationBins[0].SampleCount);
        Assert.Equal(1d, metrics.Probability.CalibrationBins[0].ObservedPositiveFrequency!.Value, 12);
        Assert.True(metrics.Quantile.MeanPinballLossByPercentile.ContainsKey(50));
        Assert.Equal(2, metrics.IntervalCoverage.SampleCount);
        Assert.Equal(1d, metrics.IntervalCoverage.ObservedCoverage!.Value, 12);
    }

    [Fact]
    public void ForecastOutputValidator_RejectsInvalidProbabilityAndNonMonotonicQuantiles()
    {
        var validResolution = new ForecastHorizonResolution(
            ForecastHorizon.Observations(5),
            ObservationFrequency.Daily,
            5,
            new DateOnly(2024, 1, 6),
            "Unit",
            false);
        var invalidProbability = new Aletheia.Forecasting.ForecastDistribution(
            validResolution,
            0d,
            0d,
            new Dictionary<int, double> { [10] = -0.01d, [90] = 0.01d },
            1.4d,
            0d,
            0d);
        var invalidQuantiles = new Aletheia.Forecasting.ForecastDistribution(
            validResolution,
            0d,
            0d,
            new Dictionary<int, double> { [10] = 0.02d, [90] = 0.01d },
            0.5d,
            0d,
            0d);

        Assert.Contains("[0, 1]", ForecastOutputValidator.Validate(invalidProbability), StringComparison.Ordinal);
        Assert.Contains("monotonically", ForecastOutputValidator.Validate(invalidQuantiles), StringComparison.Ordinal);
    }

    [Fact]
    public void Calculate_WhenProbabilityUnsupported_ReportsNotSupported()
    {
        var samples = new[]
        {
            CreateSample(
                expectedReturn: 0.01d,
                probabilityPositive: 0d,
                actualReturn: 0.02d,
                capabilities: ForecastCapabilities.PointForecast,
                pointForecastStatistic: PointForecastStatistic.ExplicitModelPoint),
        };
        var calculator = new ValidationMetricCalculator();

        var metrics = calculator.Calculate(samples, new CalibrationOptions { BinCount = 2 });

        Assert.Equal(MetricStatus.Available, metrics.Point.Status);
        Assert.Equal(MetricStatus.NotSupported, metrics.Probability.Status);
        Assert.Null(metrics.Probability.BrierScore);
        Assert.Equal(0, metrics.Probability.SampleCount);
    }

    [Fact]
    public void PredictionEvaluation_UsesDeclaredPointStatisticForError()
    {
        var prediction = CreatePrediction(
            expectedReturn: 0.10d,
            probabilityPositive: 0.60d,
            capabilities: ForecastCapabilities.PointForecast | ForecastCapabilities.ExpectedReturn | ForecastCapabilities.ProbabilityPositive,
            pointForecastStatistic: PointForecastStatistic.Mean);

        var evaluation = PredictionEvaluationRecord.Create(
            prediction,
            0.04d,
            new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
            0d,
            DirectionPredictionRule.PointForecastSign);

        Assert.Equal(0.06d, evaluation.AbsoluteError, 12);
        Assert.Equal(DirectionPredictionRule.PointForecastSign, evaluation.DirectionRule);
    }

    [Fact]
    public void PredictionEvaluation_UsesConfiguredDirectionRule()
    {
        var prediction = CreatePrediction(
            expectedReturn: 0.10d,
            probabilityPositive: 0.40d,
            capabilities: ForecastCapabilities.PointForecast | ForecastCapabilities.ExpectedReturn | ForecastCapabilities.ProbabilityPositive,
            pointForecastStatistic: PointForecastStatistic.Mean);

        var probabilityRuleEvaluation = PredictionEvaluationRecord.Create(
            prediction,
            0.02d,
            new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
            0d,
            DirectionPredictionRule.ProbabilityPositiveThreshold);
        var pointRuleEvaluation = PredictionEvaluationRecord.Create(
            prediction,
            0.02d,
            new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
            0d,
            DirectionPredictionRule.PointForecastSign);
        var pointOnly = CreatePrediction(
            expectedReturn: 0.10d,
            probabilityPositive: 0d,
            capabilities: ForecastCapabilities.PointForecast,
            pointForecastStatistic: PointForecastStatistic.ExplicitModelPoint);

        Assert.Equal(ForecastDirection.Negative, probabilityRuleEvaluation.PredictedDirection);
        Assert.Equal(ForecastDirection.Positive, pointRuleEvaluation.PredictedDirection);
        Assert.Throws<InvalidOperationException>(() => PredictionEvaluationRecord.Create(
            pointOnly,
            0.02d,
            new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
            0d,
            DirectionPredictionRule.ProbabilityPositiveThreshold));
    }

    private static ForecastEvaluationSample CreateSample(
        double expectedReturn,
        double probabilityPositive,
        double actualReturn,
        ForecastCapabilities capabilities = ForecastCapabilities.PointForecast |
            ForecastCapabilities.ExpectedReturn |
            ForecastCapabilities.Median |
            ForecastCapabilities.ProbabilityPositive |
            ForecastCapabilities.Quantiles,
        PointForecastStatistic pointForecastStatistic = PointForecastStatistic.Mean)
    {
        var prediction = CreatePrediction(expectedReturn, probabilityPositive, capabilities, pointForecastStatistic);
        var evaluation = PredictionEvaluationRecord.Create(
            prediction,
            actualReturn,
            new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
            0d);
        return new ForecastEvaluationSample(prediction, evaluation);
    }

    private static PredictionLedgerRecord CreatePrediction(
        double expectedReturn,
        double probabilityPositive,
        ForecastCapabilities capabilities = ForecastCapabilities.PointForecast |
            ForecastCapabilities.ExpectedReturn |
            ForecastCapabilities.Median |
            ForecastCapabilities.ProbabilityPositive |
            ForecastCapabilities.Quantiles,
        PointForecastStatistic pointForecastStatistic = PointForecastStatistic.Mean)
    {
        var horizon = new ForecastHorizonResolution(
            ForecastHorizon.Observations(5),
            ObservationFrequency.Daily,
            5,
            new DateOnly(2024, 1, 6),
            "Unit",
            false);
        var model = new ModelDescriptor("unit.model", "Unit", "1.0");
        var quantiles = capabilities.HasFlag(ForecastCapabilities.Quantiles)
            ? new Dictionary<int, double>
            {
                [10] = -0.10d,
                [25] = -0.05d,
                [50] = 0.01d,
                [75] = 0.05d,
                [90] = 0.10d,
            }
            : new Dictionary<int, double>();
        var corePrediction = new PredictionRecord(
            Guid.NewGuid(),
            new FundIdentifier(FundIdentifierKind.Local, "unit"),
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateOnly(2024, 1, 1),
            horizon,
            expectedReturn,
            expectedReturn,
            expectedReturn,
            probabilityPositive,
            quantiles,
            model,
            new Dictionary<string, string>(),
            "test",
            "schema",
            "fingerprint",
            new DatasetIdentity("Unit", "dataset", null),
            null,
            InvestmentSignal.NoReliableSignal,
            null,
            "config",
            capabilities,
            pointForecastStatistic);
        var logicalKey = Guid.NewGuid().ToString();
        return new PredictionLedgerRecord(
            corePrediction,
            logicalKey,
            "config",
            PredictionOrigin.HistoricalWalkForward,
            null,
            0,
            10,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 1),
            10,
            15,
            new DateOnly(2024, 1, 6),
            new Dictionary<string, string>());
    }
}
