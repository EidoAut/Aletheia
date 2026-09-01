using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class MarketTimingEngineTests
{
    [Fact]
    public void TripleBarrierLabeler_LabelsFirstBarrierHit()
    {
        var series = CreateSeries(100m, 104m, 91m, 93m);
        var definition = new TripleBarrierDefinition(
            ForecastHorizon.Observations(3),
            0.03d,
            0.03d);
        var labeler = new TripleBarrierLabeler();

        var upper = labeler.LabelAt(series, 0, definition);
        var lower = labeler.LabelAt(series, 1, definition);

        Assert.NotNull(upper);
        Assert.Equal(TripleBarrierOutcomeType.UpperHitFirst, upper!.Outcome);
        Assert.Equal(1, upper.TimeToEvent);
        Assert.NotNull(lower);
        Assert.Equal(TripleBarrierOutcomeType.LowerHitFirst, lower!.Outcome);
        Assert.Equal(1, lower.TimeToEvent);
    }

    [Fact]
    public void TripleBarrierLabeler_EndIndexTracksFirstHitOrVerticalBarrier()
    {
        var series = CreateSeries(100m, 101m, 102m, 103m, 104m);
        var definition = new TripleBarrierDefinition(
            ForecastHorizon.Observations(3),
            0.02d,
            0.20d);
        var labeler = new TripleBarrierLabeler();

        var upper = labeler.LabelAt(series, 0, definition);
        var none = labeler.LabelAt(series, 1, definition with { UpsideThreshold = 0.50d });

        Assert.NotNull(upper);
        Assert.Equal(2, upper!.EndIndex);
        Assert.Equal(upper.StartIndex + upper.TimeToEvent, upper.EndIndex);
        Assert.NotNull(none);
        Assert.Equal(4, none!.EndIndex);
        Assert.Equal(TripleBarrierOutcomeType.NoBarrierHit, none.Outcome);
    }

    [Fact]
    public void TripleBarrierLabeler_VolatilityScaledBarriersSkipUnavailableVolatility()
    {
        var series = CreateSeries(100m, 101m, 102m);
        var definition = new TripleBarrierDefinition(
            ForecastHorizon.Observations(1),
            0.03d,
            0.03d,
            BarrierThresholdPolicy.VolatilityScaled,
            1d,
            1d);
        var labeler = new TripleBarrierLabeler();

        var unavailable = labeler.LabelAt(series, 0, definition, [double.NaN, 0.01d, 0.01d]);
        var available = labeler.LabelAt(series, 1, definition, [double.NaN, 0.01d, 0.01d]);

        Assert.Null(unavailable);
        Assert.NotNull(available);
    }

    [Fact]
    public void MarketTimingFeaturePipeline_DoesNotUseFutureObservationsForHistoricalFeatures()
    {
        var prefix = Enumerable.Range(0, 90)
            .Select(index => 100m + (decimal)(index * 0.12d) + (decimal)(Math.Sin(index * 0.31d) * 0.7d))
            .ToArray();
        var calm = CreateExtendedSeries(prefix, 40, index => 0.001d + (0.0003d * Math.Sin(index)));
        var shocked = CreateExtendedSeries(prefix, 40, index => -0.05d + (0.004d * Math.Cos(index)));
        var pipeline = new MarketTimingFeaturePipeline();

        var calmFeature = pipeline.Build(calm, minimumIndex: 20).Features.Single(feature => feature.ObservationIndex == 80);
        var shockedFeature = pipeline.Build(shocked, minimumIndex: 20).Features.Single(feature => feature.ObservationIndex == 80);

        Assert.Equal(calmFeature.Values.Keys.Order(StringComparer.Ordinal), shockedFeature.Values.Keys.Order(StringComparer.Ordinal));
        foreach (var key in calmFeature.Values.Keys)
        {
            Assert.Equal(calmFeature.Values[key], shockedFeature.Values[key], 12);
        }
    }

    [Fact]
    public void MarketTimingFeaturePipeline_DoesNotBackfillCurrentExternalEvidenceIntoHistoricalCutoffs()
    {
        var series = CreateTrendingSeries(180, 0.001d, 0.0002d);
        var externalEvidence = new MarketTimingExternalEvidence(
            SpectralReliability: 0.95d,
            SpectralPhase: Math.PI,
            SpectralStability: 0.90d,
            EnsembleExpectedReturn: 0.12d,
            EnsembleProbabilityPositive: 0.80d,
            EnsembleDownsideProbability: 0.10d,
            EnsembleDisagreement: 0.05d,
            EnsembleReliability: 0.88d);
        var pipeline = new MarketTimingFeaturePipeline();

        var result = pipeline.Build(series, minimumIndex: 30, externalEvidence: externalEvidence, enableStateModelFeatures: false);
        var historical = result.Features.Single(feature => feature.ObservationIndex == 120);
        var current = result.Features[^1];

        Assert.False(historical.HasFeature("spectral_phase"));
        Assert.False(historical.HasFeature("spectral_stability"));
        Assert.False(historical.HasFeature("ensemble_expected_return"));
        Assert.False(historical.HasFeature("ensemble_probability_positive"));
        Assert.False(historical.HasFeature("ensemble_downside_probability"));
        Assert.False(historical.HasFeature("forecast_dispersion"));
        Assert.False(historical.HasFeature("model_disagreement"));
        Assert.False(historical.HasFeature("ensemble_reliability"));
        Assert.True(current.HasFeature("spectral_phase"));
        Assert.True(current.HasFeature("ensemble_probability_positive"));
    }

    [Fact]
    public void OutOfDistributionDetector_ClassifiesExtremeCurrentFeatureAsOod()
    {
        var training = Enumerable.Range(0, 60)
            .Select(index => new MarketTimingFeatureVector(
                new DateOnly(2024, 1, 1).AddDays(index),
                index,
                new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["x"] = Math.Sin(index * 0.1d),
                    ["y"] = Math.Cos(index * 0.1d),
                }))
            .ToArray();
        var current = new MarketTimingFeatureVector(
            new DateOnly(2024, 4, 1),
            90,
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["x"] = 25d,
                ["y"] = -25d,
            });

        var diagnostic = new OutOfDistributionDetector().Evaluate(training, current, ["x", "y"]);

        Assert.True(diagnostic.OutOfDistribution);
        Assert.Equal(OutOfDistributionLevel.OutOfDistribution, diagnostic.Level);
        Assert.True(diagnostic.RobustDistance > diagnostic.Threshold);
    }

    [Fact]
    public void MarketTimingModelArena_OnDeterministicUptrend_ActivatesEnsemble()
    {
        var result = EvaluateScenario(CreateTrendingSeries(360, 0.0026d, 0.0004d)).Single();

        Assert.True(result.Models.Max(model => model.Calibration.SampleCount) >= 24);
        Assert.True(result.Ensemble.CandidateModelCount > 0);
        Assert.True(result.Ensemble.EligibleModelCount > 0);
        Assert.True(result.Ensemble.IsActive, result.Ensemble.FallbackReason);
        Assert.True(result.Ensemble.Prediction.ProbabilityUpFirst >= result.Models[0].CurrentPrediction.ProbabilityUpFirst);
        Assert.True(result.Ensemble.Prediction.ProbabilityUpFirst > result.Ensemble.Prediction.ProbabilityDownFirst);
        Assert.True(result.Ensemble.Reliability > 0d);
    }

    [Fact]
    public void MarketTimingModelArena_OnDeterministicDowntrend_ActivatesBearishEnsemble()
    {
        var result = EvaluateScenario(CreateTrendingSeries(360, -0.0026d, 0.0004d)).Single();

        Assert.True(result.Ensemble.CandidateModelCount > 0);
        Assert.True(result.Ensemble.EligibleModelCount > 0);
        Assert.True(result.Ensemble.IsActive, result.Ensemble.FallbackReason);
        Assert.True(result.Ensemble.Prediction.ProbabilityDownFirst >= result.Models[0].CurrentPrediction.ProbabilityDownFirst);
        Assert.True(result.Ensemble.Prediction.ProbabilityDownFirst > result.Ensemble.Prediction.ProbabilityUpFirst);
        Assert.True(result.Ensemble.Reliability > 0d);
    }

    [Fact]
    public void MarketTimingModelArena_OnMeanReversion_DoesNotForcePermanentContinuation()
    {
        var result = EvaluateScenario(CreateMeanRevertingSeries(380)).Single();
        var edge = Math.Abs(result.Ensemble.Prediction.ProbabilityUpFirst - result.Ensemble.Prediction.ProbabilityDownFirst);

        Assert.True(result.Models.Max(model => model.Calibration.SampleCount) >= 24);
        Assert.True(result.Ensemble.CandidateModelCount > 0);
        Assert.True(edge < 0.70d);
    }

    [Fact]
    public void MarketTimingModelArena_OnRegimeChange_ReflectsRecentBearishRegime()
    {
        var result = EvaluateScenario(CreateRegimeChangeSeries(420)).Single();

        Assert.True(result.Models.Max(model => model.Calibration.SampleCount) >= 24);
        Assert.True(result.Ensemble.CandidateModelCount > 0);
        Assert.True(result.Ensemble.Prediction.ProbabilityDownFirst >= result.Ensemble.Prediction.ProbabilityUpFirst);
    }

    [Fact]
    public void MarketTimingModelArena_OnRandomWalk_GivesModelsRealOpportunityButStaysLowReliability()
    {
        var result = EvaluateScenario(CreateRandomWalkSeries(420)).Single();

        Assert.True(result.Models.Count(model => model.Kind != MarketTimingModelKind.HistoricalEventRateBaseline) > 0);
        Assert.All(result.Models, model => Assert.True(model.Calibration.SampleCount >= 24));
        Assert.True(result.Ensemble.CandidateModelCount > 0);
        Assert.True(result.Ensemble.Reliability < 0.75d);
        Assert.NotEqual(string.Empty, result.Ensemble.IsActive ? result.Ensemble.Diagnostic : result.Ensemble.FallbackReason);
    }

    [Fact]
    public void MarketTimingModelArena_ProducesOrderedTerminalQuantilesWhenEvidenceExists()
    {
        var result = EvaluateScenario(CreateTrendingSeries(360, 0.0012d, 0.001d)).Single();

        Assert.NotNull(result.TerminalReturnQuantiles);
        var quantiles = result.TerminalReturnQuantiles!;
        Assert.True(quantiles.P10 <= quantiles.P25);
        Assert.True(quantiles.P25 <= quantiles.P50);
        Assert.True(quantiles.P50 <= quantiles.P75);
        Assert.True(quantiles.P75 <= quantiles.P90);
        Assert.True(double.IsFinite(result.ForecastExpectedReturn!.Value));
    }

    [Fact]
    public void MarketTimingFeaturePipeline_CausalStateFeaturesIgnoreExtremeFutureTail()
    {
        var prefix = Enumerable.Range(0, 150)
            .Select(index => 100m + (decimal)(index * 0.05d) + (decimal)(Math.Sin(index * 0.2d) * 0.8d))
            .ToArray();
        var rallyCrash = CreateExtendedSeries(prefix, 80, index => index < 40 ? 0.12d : -0.12d);
        var crashRally = CreateExtendedSeries(prefix, 80, index => index < 40 ? -0.12d : 0.12d);
        var pipeline = new MarketTimingFeaturePipeline();

        var first = pipeline.Build(rallyCrash, minimumIndex: 40, enableStateModelFeatures: true, hmmMaximumIterations: 8)
            .Features.Single(feature => feature.ObservationIndex == 120);
        var second = pipeline.Build(crashRally, minimumIndex: 40, enableStateModelFeatures: true, hmmMaximumIterations: 8)
            .Features.Single(feature => feature.ObservationIndex == 120);

        foreach (var key in first.Values.Keys)
        {
            Assert.Equal(first.Values[key], second.Values[key], 10);
        }
    }

    [Fact]
    public void MarketTimingModelArena_FutureMutationAfterCutoffDoesNotChangePrediction()
    {
        var series = CreateRegimeChangeSeries(280);
        var cutoff = 230;
        var mutated = MutateFuture(series, cutoff);
        var options = new MarketTimingEngineOptions
        {
            Horizons = [ForecastHorizon.Observations(10)],
            EnableStateModelFeatures = false,
            MinimumFeatureIndex = 30,
            MinimumTrainingSamples = 45,
            MaximumWalkForwardEvaluations = 24,
            MinimumOosSamplesAbsolute = 6,
            TargetOosSamplesForEligibility = 12,
            MinimumOosSampleFraction = 0.25d,
            MinimumCalibrationSamples = 8,
            MinimumBrierImprovement = 0d,
            MaximumAcceptableEce = 0.40d,
            ClassifierOptions = new MarketEventClassifierOptions { MaxIterations = 80, Tolerance = 1e-4d, MinimumSamplesPerClass = 1 },
        };
        var arena = new MarketTimingModelArena(options);

        var first = arena.EvaluateAtCutoff(series, cutoff).Single();
        var second = arena.EvaluateAtCutoff(mutated, cutoff).Single();

        AssertEqualFeature(first.CurrentFeature, second.CurrentFeature);
        Assert.Equal(
            first.TrainingLabels!.Select(label => (label.StartIndex, label.EndIndex, label.Outcome)),
            second.TrainingLabels!.Select(label => (label.StartIndex, label.EndIndex, label.Outcome)));
        Assert.Equal(first.Models.Count, second.Models.Count);
        for (var index = 0; index < first.Models.Count; index++)
        {
            Assert.Equal(first.Models[index].Kind, second.Models[index].Kind);
            Assert.Equal(first.Models[index].EligibleForEnsemble, second.Models[index].EligibleForEnsemble);
            Assert.Equal(first.Models[index].EligibilityStatus, second.Models[index].EligibilityStatus);
            Assert.Equal(first.Models[index].RawCurrentPrediction.Probabilities, second.Models[index].RawCurrentPrediction.Probabilities);
            Assert.Equal(first.Models[index].CurrentPrediction.Probabilities, second.Models[index].CurrentPrediction.Probabilities);
            Assert.Equal(first.Models[index].Calibration.BrierScore, second.Models[index].Calibration.BrierScore, 12);
            Assert.Equal(first.Models[index].Calibration.ExpectedCalibrationError, second.Models[index].Calibration.ExpectedCalibrationError, 12);
        }

        Assert.Equal(first.Ensemble.IsActive, second.Ensemble.IsActive);
        Assert.Equal(first.Ensemble.Prediction.Probabilities, second.Ensemble.Prediction.Probabilities);
        Assert.Equal(first.Ensemble.Reliability, second.Ensemble.Reliability, 12);
        Assert.Equal(first.Ensemble.Components.Select(component => component.ModelName), second.Ensemble.Components.Select(component => component.ModelName));
        Assert.Equal(first.OutOfDistribution.RobustDistance, second.OutOfDistribution.RobustDistance, 12);
    }

    [Fact]
    public void MarketTimingModelArena_TrainingLabelsAreAvailableOnlyAfterEndIndex()
    {
        var series = CreateOscillatingBarrierSeries(120);
        var options = new MarketTimingEngineOptions
        {
            Horizons = [ForecastHorizon.Observations(8)],
            BarrierPolicy = BarrierThresholdPolicy.FixedPercentage,
            UpsideThreshold = 0.015d,
            DownsideThreshold = 0.015d,
            EnableStateModelFeatures = false,
            MinimumFeatureIndex = 10,
            MinimumTrainingSamples = 12,
            MaximumWalkForwardEvaluations = 10,
            MinimumOosSamplesAbsolute = 3,
            TargetOosSamplesForEligibility = 5,
            MinimumCalibrationSamples = 3,
        };

        var result = new MarketTimingModelArena(options).EvaluateAtCutoff(series, 70).Single();

        Assert.NotEmpty(result.TrainingLabels!);
        Assert.All(result.TrainingLabels!, label => Assert.True(label.EndIndex <= result.TrainingLabelEndIndexCutoff));
        Assert.Contains(
            new TripleBarrierLabeler().Label(series, result.Definition)
                .Where(label => label.StartIndex <= result.TrainingLabelEndIndexCutoff && label.EndIndex > result.TrainingLabelEndIndexCutoff),
            label => label.StartIndex < result.TrainingLabelEndIndexCutoff);
    }

    [Fact]
    public void MarketTimingRobustFeatureScaler_IsInvariantToLinearUnitChanges()
    {
        var training = Enumerable.Range(0, 40)
            .Select(index => new MarketTimingFeatureVector(
                new DateOnly(2024, 1, 1).AddDays(index),
                index,
                new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["return"] = Math.Sin(index * 0.2d) * 0.01d,
                    ["duration"] = 100d + index,
                }))
            .ToArray();
        var current = new MarketTimingFeatureVector(
            new DateOnly(2024, 3, 1),
            60,
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["return"] = 0.002d,
                ["duration"] = 125d,
            });
        var scaledTraining = training
            .Select(feature => feature with
            {
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["return"] = feature.Values["return"],
                    ["duration"] = feature.Values["duration"] * 1000d,
                },
            })
            .ToArray();
        var scaledCurrent = current with
        {
            Values = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["return"] = current.Values["return"],
                ["duration"] = current.Values["duration"] * 1000d,
            },
        };

        var scaler = MarketTimingRobustFeatureScaler.Fit(training, ["return", "duration"]);
        var scaledUnitsScaler = MarketTimingRobustFeatureScaler.Fit(scaledTraining, ["return", "duration"]);
        var nearest = training.OrderBy(feature => scaler.Distance(feature, current)).Select(feature => feature.ObservationIndex).Take(5);
        var nearestScaled = scaledTraining.OrderBy(feature => scaledUnitsScaler.Distance(feature, scaledCurrent)).Select(feature => feature.ObservationIndex).Take(5);

        Assert.Equal(nearest, nearestScaled);
    }

    [Fact]
    public void ReliabilityIndexCalculator_IsMonotoneInScientificPenaltyFactors()
    {
        var lowEvidence = ReliabilityIndexCalculator.Calculate(5, 30, 0.05d, 0.04d, 0.01d, 0d, 1d, 0.05d, 0.5d, 3.5d);
        var moreEvidence = ReliabilityIndexCalculator.Calculate(30, 30, 0.05d, 0.04d, 0.01d, 0d, 1d, 0.05d, 0.5d, 3.5d);
        var worseCalibration = ReliabilityIndexCalculator.Calculate(30, 30, 0.25d, 0.04d, 0.01d, 0d, 1d, 0.05d, 0.5d, 3.5d);
        var higherOod = ReliabilityIndexCalculator.Calculate(30, 30, 0.05d, 0.04d, 0.01d, 0d, 1d, 0.05d, 5d, 3.5d);
        var higherUncertainty = ReliabilityIndexCalculator.Calculate(30, 30, 0.05d, 0.04d, 0.20d, 0d, 1d, 0.05d, 0.5d, 3.5d);
        var singleModelScarce = ReliabilityIndexCalculator.Calculate(5, 30, 0d, 1d, 0d, 0d, 1d, 0d, 0d, 3.5d);

        Assert.True(moreEvidence >= lowEvidence);
        Assert.True(worseCalibration <= moreEvidence);
        Assert.True(higherOod <= moreEvidence);
        Assert.True(higherUncertainty <= moreEvidence);
        Assert.True(singleModelScarce < 0.95d);
    }

    [Fact]
    public void NestedWalkForwardSelection_InnerLoopSeesOnlyOuterTrainingPrefix()
    {
        var series = CreateTrendingSeries(90, 0.001d, 0.0002d);
        var options = new NestedWalkForwardOptions
        {
            CandidateHorizons = [ForecastHorizon.Observations(3), ForecastHorizon.Observations(5)],
            MinimumOuterTrainingObservations = 40,
            MinimumInnerTrainingObservations = 20,
            OuterStepSize = 10,
            InnerStepSize = 3,
        };

        var selections = new NestedWalkForwardValidator().Select(
            series,
            options,
            context =>
            {
                Assert.Equal(context.OuterPredictionCutoffIndex + 1, context.TrainingPrefix.Count);
                Assert.True(context.TrainingPrefix.EndDate <= series[context.OuterPredictionCutoffIndex].Date);
                return context.CandidateHorizon.Value;
            });

        Assert.NotEmpty(selections);
        Assert.All(selections, selection => Assert.Equal(selection.OuterPredictionCutoffIndex, selection.InnerSelectionEndIndex));
        Assert.All(selections, selection => Assert.True(selection.OuterTargetIndex > selection.OuterPredictionCutoffIndex));
    }

    [Fact]
    public void TripleBarrierLabeler_CalendarHorizonWithoutTargetDate_DoesNotCreateNoBarrierHit()
    {
        var series = CreateSeries(100m, 100.5m, 100.3m, 100.2m);
        var definition = new TripleBarrierDefinition(
            ForecastHorizon.CalendarDays(10),
            0.10d,
            0.10d);
        var labeler = new TripleBarrierLabeler();

        var labels = labeler.Label(series, definition);
        var label = labeler.LabelAt(series, 0, definition);

        Assert.Empty(labels);
        Assert.Null(label);
    }

    [Fact]
    public void TripleBarrierLabeler_CalendarHorizonUsesFirstObservationOnOrAfterTargetDate()
    {
        var start = new DateOnly(2024, 1, 1);
        var series = new NavSeries(
            [
                new NavPoint(start, 100m),
                new NavPoint(start.AddDays(7), 101m),
                new NavPoint(start.AddDays(14), 102m),
            ],
            ObservationFrequency.Weekly);
        var definition = new TripleBarrierDefinition(
            ForecastHorizon.CalendarDays(10),
            0.50d,
            0.50d);
        var labeler = new TripleBarrierLabeler();

        var label = labeler.LabelAt(series, 0, definition);

        Assert.NotNull(label);
        Assert.Equal(TripleBarrierOutcomeType.NoBarrierHit, label!.Outcome);
        Assert.Equal(start.AddDays(10), label.RequestedTargetDate);
        Assert.Equal(start.AddDays(14), label.EffectiveValuationDate);
        Assert.True(label.IsCalendarValuationApproximation);
        Assert.True(label.IsHorizonComplete);
    }

    [Fact]
    public void MarketEventClassifier_ProbabilitiesSumToOne()
    {
        var featureNames = new[] { "x" };
        var features = Enumerable.Range(0, 30)
            .Select(index => new MarketTimingFeatureVector(
                new DateOnly(2024, 1, 1).AddDays(index),
                index,
                new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["x"] = 1d,
                }))
            .ToArray();
        var labels = features
            .Select(feature => new TripleBarrierOutcome(
                feature.Date,
                feature.ObservationIndex,
                ToOutcome(feature.ObservationIndex),
                1,
                0d,
                0d,
                0d,
                0.03d,
                0.03d))
            .ToArray();
        var classifier = new MarketEventClassifier(new MarketEventClassifierOptions { MaxIterations = 200, Tolerance = 1e-4d, MinimumSamplesPerClass = 2 });

        var fit = classifier.Fit(features, labels, featureNames);
        var prediction = fit.Predict(features[0]);

        Assert.True(fit.IsSuccess);
        Assert.Equal(MarketEventClassifierFitStatus.Converged, fit.Status);
        Assert.All(prediction.Probabilities, probability => Assert.InRange(probability, 0d, 1d));
        Assert.Equal(1d, prediction.Probabilities.Sum(), 12);
    }

    [Fact]
    public void MarketEventClassifier_ReportsMaxIterationsWithoutPretendingSuccess()
    {
        var featureNames = new[] { "x" };
        var features = Enumerable.Range(0, 30)
            .Select(index => new MarketTimingFeatureVector(
                new DateOnly(2024, 1, 1).AddDays(index),
                index,
                new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["x"] = index % 3,
                }))
            .ToArray();
        var labels = features
            .Select(feature => new TripleBarrierOutcome(
                feature.Date,
                feature.ObservationIndex,
                ToOutcome(feature.ObservationIndex),
                1,
                0d,
                0d,
                0d,
                0.03d,
                0.03d))
            .ToArray();

        var fit = new MarketEventClassifier(new MarketEventClassifierOptions { MaxIterations = 1, Tolerance = 1e-12d, MinimumSamplesPerClass = 2 })
            .Fit(features, labels, featureNames);

        Assert.False(fit.IsSuccess);
        Assert.Equal(MarketEventClassifierFitStatus.MaxIterationsReached, fit.Status);
    }

    [Fact]
    public void CompetingRiskHazardModel_SeparatesCifAndSurvival()
    {
        var horizon = ForecastHorizon.Observations(3);
        var outcomes = new[]
        {
            Outcome(0, TripleBarrierOutcomeType.UpperHitFirst, 1),
            Outcome(1, TripleBarrierOutcomeType.LowerHitFirst, 2),
            Outcome(2, TripleBarrierOutcomeType.NoBarrierHit, 3),
            Outcome(3, TripleBarrierOutcomeType.UpperHitFirst, 3),
        };

        var forecast = new CompetingRiskHazardModel().Fit(outcomes, horizon).Forecast();

        Assert.Equal(3, forecast.HazardPoints.Count);
        Assert.InRange(forecast.ProbabilityUpByHorizon, 0d, 1d);
        Assert.InRange(forecast.ProbabilityDownByHorizon, 0d, 1d);
        Assert.InRange(forecast.ProbabilityNoEventByHorizon, 0d, 1d);
        Assert.Equal(
            1d,
            forecast.ProbabilityUpByHorizon + forecast.ProbabilityDownByHorizon + forecast.ProbabilityNoEventByHorizon,
            12);
        Assert.True(forecast.ProbabilityUpByHorizon > forecast.ProbabilityDownByHorizon);
    }

    [Fact]
    public void MarketTimingModelArena_OnRandomWalk_DoesNotInventStrongEvidence()
    {
        var series = CreateRandomWalkSeries(260);
        var options = new MarketTimingEngineOptions
        {
            Horizons = [ForecastHorizon.Observations(5), ForecastHorizon.Observations(10)],
            BarrierPolicy = BarrierThresholdPolicy.FixedPercentage,
            UpsideThreshold = 0.02d,
            DownsideThreshold = 0.02d,
            MinimumFeatureIndex = 30,
            MinimumTrainingSamples = 40,
            MaximumWalkForwardEvaluations = 10,
            ClassifierOptions = new MarketEventClassifierOptions { Iterations = 20, MinimumSamplesPerClass = 2 },
        };

        var results = new MarketTimingModelArena(options).Evaluate(series);

        Assert.NotEmpty(results);
        Assert.DoesNotContain(
            results.SelectMany(result => result.Models),
            model => model.Evidence == EvidenceStrength.Strong && model.EligibleForEnsemble);
    }

    private static IReadOnlyList<MarketTimingArenaResult> EvaluateScenario(NavSeries series)
    {
        var options = new MarketTimingEngineOptions
        {
            Horizons = [ForecastHorizon.Observations(10)],
            BarrierPolicy = BarrierThresholdPolicy.FixedPercentage,
            UpsideThreshold = 0.015d,
            DownsideThreshold = 0.015d,
            EnableStateModelFeatures = false,
            MinimumFeatureIndex = 30,
            MinimumTrainingSamples = 45,
            MaximumWalkForwardEvaluations = 48,
            MinimumOosSamplesAbsolute = 12,
            TargetOosSamplesForEligibility = 24,
            MinimumOosSampleFraction = 0.40d,
            MinimumCalibrationSamples = 18,
            MinimumBrierImprovement = 0d,
            MaximumAcceptableEce = 0.30d,
            ClassifierOptions = new MarketEventClassifierOptions { Iterations = 18, MinimumSamplesPerClass = 1 },
        };
        return new MarketTimingModelArena(options).Evaluate(series);
    }

    private static void AssertEqualFeature(MarketTimingFeatureVector first, MarketTimingFeatureVector second)
    {
        Assert.Equal(first.Date, second.Date);
        Assert.Equal(first.ObservationIndex, second.ObservationIndex);
        Assert.Equal(first.Values.Keys.Order(StringComparer.Ordinal), second.Values.Keys.Order(StringComparer.Ordinal));
        foreach (var key in first.Values.Keys)
        {
            Assert.Equal(first.Values[key], second.Values[key], 12);
        }
    }

    private static NavSeries MutateFuture(NavSeries source, int cutoff)
    {
        var points = source.Points
            .Select((point, index) =>
                index <= cutoff
                    ? point
                    : new NavPoint(point.Date, (decimal)(10_000d + (index * 137d) + (index % 2 == 0 ? 5_000d : -2_500d))))
            .ToArray();
        return new NavSeries(points, source.ObservationFrequency);
    }

    private static NavSeries CreateSeries(params decimal[] values)
    {
        var start = new DateOnly(2024, 1, 1);
        return new NavSeries(
            values.Select((value, index) => new NavPoint(start.AddDays(index), value)),
            ObservationFrequency.Daily);
    }

    private static NavSeries CreateOscillatingBarrierSeries(int count)
    {
        return CreateLogReturnSeries(
            count,
            index => 0.012d * Math.Sin(index * 0.75d));
    }

    private static NavSeries CreateExtendedSeries(
        IReadOnlyList<decimal> prefix,
        int tailCount,
        Func<int, double> logReturnFactory)
    {
        var points = new List<NavPoint>();
        var start = new DateOnly(2024, 1, 1);
        for (var index = 0; index < prefix.Count; index++)
        {
            points.Add(new NavPoint(start.AddDays(index), prefix[index]));
        }

        var nav = (double)prefix[^1];
        for (var index = 0; index < tailCount; index++)
        {
            nav *= Math.Exp(logReturnFactory(index));
            points.Add(new NavPoint(start.AddDays(prefix.Count + index), (decimal)nav));
        }

        return new NavSeries(points, ObservationFrequency.Daily);
    }

    private static NavSeries CreateRandomWalkSeries(int count)
    {
        var random = new Random(12345);
        var points = new List<NavPoint>(count);
        var start = new DateOnly(2020, 1, 1);
        var nav = 100d;
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
            {
                nav *= Math.Exp((random.NextDouble() - 0.5d) * 0.018d);
            }

            points.Add(new NavPoint(start.AddDays(index), (decimal)nav));
        }

        return new NavSeries(points, ObservationFrequency.Daily);
    }

    private static NavSeries CreateTrendingSeries(int count, double drift, double noiseAmplitude)
    {
        return CreateLogReturnSeries(count, index => drift + (noiseAmplitude * Math.Sin(index * 0.37d)));
    }

    private static NavSeries CreateMeanRevertingSeries(int count)
    {
        var nav = 100d;
        var points = new List<NavPoint>(count);
        var start = new DateOnly(2020, 1, 1);
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
            {
                var pull = Math.Log(100d / nav) * 0.18d;
                var cycle = 0.006d * Math.Sin(index * 0.55d);
                nav *= Math.Exp(pull + cycle);
            }

            points.Add(new NavPoint(start.AddDays(index), (decimal)nav));
        }

        return new NavSeries(points, ObservationFrequency.Daily);
    }

    private static NavSeries CreateRegimeChangeSeries(int count)
    {
        return CreateLogReturnSeries(
            count,
            index => index < count / 2
                ? 0.0024d + (0.0004d * Math.Sin(index * 0.19d))
                : -0.0030d + (0.0004d * Math.Cos(index * 0.23d)));
    }

    private static NavSeries CreateLogReturnSeries(int count, Func<int, double> logReturnFactory)
    {
        var points = new List<NavPoint>(count);
        var start = new DateOnly(2020, 1, 1);
        var nav = 100d;
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
            {
                nav *= Math.Exp(logReturnFactory(index));
            }

            points.Add(new NavPoint(start.AddDays(index), (decimal)nav));
        }

        return new NavSeries(points, ObservationFrequency.Daily);
    }

    private static TripleBarrierOutcome Outcome(
        int startIndex,
        TripleBarrierOutcomeType outcome,
        int timeToEvent)
    {
        return new TripleBarrierOutcome(
            new DateOnly(2024, 1, 1).AddDays(startIndex),
            startIndex,
            outcome,
            timeToEvent,
            0d,
            0d,
            0d,
            0.03d,
            0.03d);
    }

    private static TripleBarrierOutcomeType ToOutcome(int observationIndex)
    {
        return (observationIndex % 3) switch
        {
            0 => TripleBarrierOutcomeType.UpperHitFirst,
            1 => TripleBarrierOutcomeType.LowerHitFirst,
            _ => TripleBarrierOutcomeType.NoBarrierHit,
        };
    }
}
