using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class ModelArenaTests
{
    [Fact]
    public async Task EvaluateAsync_UsesIntersectionForCommonSupportAndRanking()
    {
        var actualReturns = new Dictionary<int, double>
        {
            [1] = 0d,
            [2] = 0d,
            [3] = 0d,
            [4] = 0d,
        };
        var modelA = new StaticForecastModel("unit.model.a", "Model A", ForecastCapabilities.PointForecast);
        var modelB = new StaticForecastModel("unit.model.b", "Model B", ForecastCapabilities.PointForecast);
        var modelC = new StaticForecastModel("unit.model.c", "Model C", ForecastCapabilities.PointForecast);
        var probabilityOnly = new StaticForecastModel("unit.probability", "Probability", ForecastCapabilities.ProbabilityPositive);
        var evaluator = new StubWalkForwardEvaluator(new Dictionary<string, WalkForwardModelEvaluationResult>
        {
            [modelA.Descriptor.Id] = CreateEvaluation(modelA, [1, 2, 3, 4], actualReturns, new Dictionary<int, double>
            {
                [1] = 1d,
                [2] = 0d,
                [3] = 0d,
                [4] = 1d,
            }),
            [modelB.Descriptor.Id] = CreateEvaluation(modelB, [2, 3, 4], actualReturns, new Dictionary<int, double>
            {
                [2] = 1d,
                [3] = 1d,
                [4] = 0d,
            }),
            [modelC.Descriptor.Id] = CreateEvaluation(modelC, [1, 2, 3], actualReturns, new Dictionary<int, double>
            {
                [1] = 0d,
                [2] = 2d,
                [3] = 2d,
            }),
            [probabilityOnly.Descriptor.Id] = CreateEvaluation(probabilityOnly, [4], actualReturns, new Dictionary<int, double>
            {
                [4] = 0d,
            }),
        });
        var arena = new ModelArena(evaluator);
        var options = new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = 2,
            ForecastHorizon = ForecastHorizon.Observations(1),
            MinimumEvaluationSamples = 2,
        };
        var arenaOptions = new ModelArenaOptions
        {
            PointForecastBaselineModelId = modelA.Descriptor.Id,
            ProbabilityBaselineModelId = "unit.probability.missing",
            MinimumCommonSupportSamples = 2,
        };

        var result = await arena.EvaluateAsync(
            [modelA, modelB, modelC, probabilityOnly],
            CreateDataset(),
            options,
            arenaOptions: arenaOptions);

        Assert.Equal(2, result.CommonSupportEventCount);
        Assert.Equal(2, result.PointCommonSupportEventCount);
        Assert.Equal(1, result.ProbabilityCommonSupportEventCount);
        Assert.Equal(0, result.QuantileCommonSupportEventCount);
        Assert.All(result.Models.Where(model => model.Capabilities.HasFlag(ForecastCapabilities.PointForecast)), model =>
        {
            Assert.Equal(new[] { 2, 3 }, model.PointCommonSupportSamples.Select(sample => sample.Prediction.PredictionCutoffIndex));
        });
        Assert.Empty(result.Models.Single(model => model.Model.Id == probabilityOnly.Descriptor.Id).PointCommonSupportSamples);
        Assert.Equal(new[] { 4 }, result.Models.Single(model => model.Model.Id == probabilityOnly.Descriptor.Id).ProbabilityCommonSupportSamples.Select(sample => sample.Prediction.PredictionCutoffIndex));
        Assert.Equal("Model A", result.Ranking[0].Model.Name);
        Assert.Equal(0d, result.Models.Single(model => model.Model.Id == modelA.Descriptor.Id).CommonSupportMetrics.Point.MeanAbsoluteError);
        Assert.Equal(0.5d, result.Models.Single(model => model.Model.Id == modelA.Descriptor.Id).Evaluation.AllSamplesMetrics.Point.MeanAbsoluteError);
    }

    [Fact]
    public async Task EvaluateAsync_RegistrationOrderDoesNotChangeBaselinesOrRanking()
    {
        var actualReturns = new Dictionary<int, double>
        {
            [1] = 0.01d,
            [2] = -0.01d,
        };
        var pointBaseline = new StaticForecastModel(ZeroReturnForecastModel.ModelId, "Zero", ForecastCapabilities.PointForecast);
        var challenger = new StaticForecastModel("unit.challenger", "Challenger", ForecastCapabilities.PointForecast);
        var probabilityBaseline = new StaticForecastModel(
            HistoricalProbabilityBaselineForecastModel.ModelId,
            "Probability",
            ForecastCapabilities.ProbabilityPositive);
        var evaluator = new StubWalkForwardEvaluator(new Dictionary<string, WalkForwardModelEvaluationResult>
        {
            [pointBaseline.Descriptor.Id] = CreateEvaluation(pointBaseline, [1, 2], actualReturns, new Dictionary<int, double>
            {
                [1] = 0d,
                [2] = 0d,
            }),
            [challenger.Descriptor.Id] = CreateEvaluation(challenger, [1, 2], actualReturns, new Dictionary<int, double>
            {
                [1] = 0.01d,
                [2] = -0.01d,
            }),
            [probabilityBaseline.Descriptor.Id] = CreateEvaluation(probabilityBaseline, [1, 2], actualReturns, new Dictionary<int, double>
            {
                [1] = 0d,
                [2] = 0d,
            }),
        });
        var arena = new ModelArena(evaluator);
        var options = new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = 2,
            ForecastHorizon = ForecastHorizon.Observations(1),
            MinimumEvaluationSamples = 2,
        };
        var arenaOptions = new ModelArenaOptions
        {
            PointForecastBaselineModelId = ZeroReturnForecastModel.ModelId,
            ProbabilityBaselineModelId = HistoricalProbabilityBaselineForecastModel.ModelId,
            MinimumCommonSupportSamples = 2,
        };

        var first = await arena.EvaluateAsync(
            [pointBaseline, challenger, probabilityBaseline],
            CreateDataset(),
            options,
            arenaOptions: arenaOptions);
        var second = await arena.EvaluateAsync(
            [probabilityBaseline, challenger, pointBaseline],
            CreateDataset(),
            options,
            arenaOptions: arenaOptions);

        Assert.Equal(ZeroReturnForecastModel.ModelId, first.PointForecastBaseline!.Id);
        Assert.Equal(ZeroReturnForecastModel.ModelId, second.PointForecastBaseline!.Id);
        Assert.Equal(HistoricalProbabilityBaselineForecastModel.ModelId, first.ProbabilityBaseline!.Id);
        Assert.Equal(HistoricalProbabilityBaselineForecastModel.ModelId, second.ProbabilityBaseline!.Id);
        Assert.Equal(
            first.Ranking.Select(entry => entry.Model.Id),
            second.Ranking.Select(entry => entry.Model.Id));
    }

    private static WalkForwardModelEvaluationResult CreateEvaluation(
        StaticForecastModel model,
        IReadOnlyList<int> successfulEvents,
        IReadOnlyDictionary<int, double> actualReturns,
        IReadOnlyDictionary<int, double> forecasts)
    {
        var samples = successfulEvents
            .Select(eventId => CreateSample(model, eventId, actualReturns[eventId], forecasts[eventId]))
            .ToArray();
        var failures = actualReturns.Keys
            .Except(successfulEvents)
            .Select(eventId => new ForecastFailureRecord(
                model.Descriptor,
                new DateOnly(2024, 1, eventId),
                eventId,
                ForecastStatus.InsufficientData,
                "Synthetic missing forecast."))
            .ToArray();
        var calculator = new ValidationMetricCalculator();
        var metrics = calculator.Calculate(samples, new CalibrationOptions { BinCount = 2 });

        return new WalkForwardModelEvaluationResult(
            model.Descriptor,
            model.ConfigurationFingerprint,
            samples,
            samples,
            failures,
            metrics,
            metrics,
            samples.Min(sample => sample.Prediction.Prediction.DataCutoffDate),
            samples.Max(sample => sample.Prediction.TargetDate),
            new ModelCoverageDiagnostics(model.Descriptor, actualReturns.Count, samples.Length, failures));
    }

    private static ForecastEvaluationSample CreateSample(
        StaticForecastModel model,
        int eventId,
        double actualReturn,
        double forecast)
    {
        var horizon = new ForecastHorizonResolution(
            ForecastHorizon.Observations(1),
            ObservationFrequency.Daily,
            1,
            new DateOnly(2024, 2, eventId),
            "Unit",
            false);
        var capabilities = model.Capabilities;
        var quantiles = capabilities.HasFlag(ForecastCapabilities.Quantiles)
            ? new Dictionary<int, double> { [10] = forecast, [90] = forecast }
            : new Dictionary<int, double>();
        var logicalKey = $"{model.Descriptor.Id}|{eventId}";
        var prediction = new PredictionRecord(
            DeterministicPredictionIdentity.CreateGuid(logicalKey),
            new FundIdentifier(FundIdentifierKind.Local, "unit"),
            new DateTimeOffset(2024, 1, eventId, 0, 0, 0, TimeSpan.Zero),
            new DateOnly(2024, 1, eventId),
            horizon,
            capabilities.HasFlag(ForecastCapabilities.PointForecast) ? forecast : 0d,
            capabilities.HasFlag(ForecastCapabilities.ExpectedReturn) ? forecast : 0d,
            capabilities.HasFlag(ForecastCapabilities.Median) ? forecast : 0d,
            capabilities.HasFlag(ForecastCapabilities.ProbabilityPositive) ? 0.5d : 0d,
            quantiles,
            model.Descriptor,
            model.Configuration,
            "test",
            "schema",
            "fingerprint",
            new DatasetIdentity("Unit", new string('0', 64), null),
            null,
            InvestmentSignal.NoReliableSignal,
            null,
            model.ConfigurationFingerprint,
            capabilities,
            model.PointForecastStatistic);
        var ledger = new PredictionLedgerRecord(
            prediction,
            logicalKey,
            model.ConfigurationFingerprint,
            PredictionOrigin.HistoricalWalkForward,
            null,
            0,
            eventId,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, eventId),
            eventId,
            eventId + 1,
            new DateOnly(2024, 2, eventId),
            new Dictionary<string, string>());
        var evaluation = PredictionEvaluationRecord.Create(
            ledger,
            actualReturn,
            new DateTimeOffset(2024, 3, eventId, 0, 0, 0, TimeSpan.Zero),
            0d);

        return new ForecastEvaluationSample(ledger, evaluation);
    }

    private static ForecastEvaluationDataset CreateDataset()
    {
        var fund = new Fund(new FundIdentifier(FundIdentifierKind.Local, "unit"), "Unit Fund");
        var series = new NavSeries(
            new[]
            {
                new NavPoint(new DateOnly(2024, 1, 1), 100m),
                new NavPoint(new DateOnly(2024, 1, 2), 101m),
                new NavPoint(new DateOnly(2024, 1, 3), 102m),
            },
            ObservationFrequency.Daily);
        return new ForecastEvaluationDataset(
            new FundHistory(fund, series),
            new DatasetIdentity("Unit", new string('0', 64), null));
    }

    private sealed class StaticForecastModel : IForecastModel
    {
        private readonly IReadOnlyDictionary<string, string> configuration = new Dictionary<string, string>();

        public StaticForecastModel(string id, string name, ForecastCapabilities capabilities)
        {
            this.Descriptor = new ModelDescriptor(id, name, "1.0");
            this.Capabilities = capabilities;
            this.PointForecastStatistic = capabilities.HasFlag(ForecastCapabilities.PointForecast)
                ? PointForecastStatistic.ExplicitModelPoint
                : PointForecastStatistic.None;
        }

        public ModelDescriptor Descriptor { get; }

        public IReadOnlyDictionary<string, string> Configuration => this.configuration;

        public ForecastCapabilities Capabilities { get; }

        public PointForecastStatistic PointForecastStatistic { get; }

        public string ConfigurationFingerprint => ModelConfigurationFingerprint.Calculate(this.Descriptor, this.Configuration);

        public ModelTrainingResult Train(ForecastTrainingContext context, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The fake evaluator supplies precomputed results.");

        public ForecastPredictionResult Predict(
            ModelTrainingResult trainingResult,
            ForecastPredictionContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The fake evaluator supplies precomputed results.");
    }

    private sealed class StubWalkForwardEvaluator : IWalkForwardEvaluator
    {
        private readonly IReadOnlyDictionary<string, WalkForwardModelEvaluationResult> results;

        public StubWalkForwardEvaluator(IReadOnlyDictionary<string, WalkForwardModelEvaluationResult> results)
        {
            this.results = results;
        }

        public Task<WalkForwardModelEvaluationResult> EvaluateAsync(
            IForecastModel model,
            ForecastEvaluationDataset dataset,
            WalkForwardEvaluationOptions options,
            IPredictionLedger? ledger = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.results[model.Descriptor.Id]);
        }
    }
}
