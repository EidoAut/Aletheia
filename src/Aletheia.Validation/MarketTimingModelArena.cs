#pragma warning disable SA1204 // Static helpers are grouped after the arena workflow.

using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Evaluates market-timing event models on strict temporal common support.
/// </summary>
public sealed class MarketTimingModelArena
{
    private readonly MarketTimingEngineOptions options;
    private readonly TripleBarrierLabeler labeler;
    private readonly MarketTimingFeaturePipeline featurePipeline = new();
    private readonly MarketTimingEnsemble ensemble = new();
    private readonly CompetingRiskHazardModel hazardModel = new();
    private readonly OutOfDistributionDetector outOfDistributionDetector = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketTimingModelArena"/> class.
    /// </summary>
    /// <param name="options">Timing engine options.</param>
    /// <param name="labeler">Triple-barrier labeler.</param>
    public MarketTimingModelArena(
        MarketTimingEngineOptions? options = null,
        TripleBarrierLabeler? labeler = null)
    {
        this.options = options ?? new MarketTimingEngineOptions();
        this.labeler = labeler ?? new TripleBarrierLabeler();
        this.ValidateOptions();
    }

    /// <summary>
    /// Evaluates all configured horizons.
    /// </summary>
    /// <param name="navSeries">The NAV series.</param>
    /// <param name="externalEvidence">Optional external evidence.</param>
    /// <returns>Timing arena results per horizon.</returns>
    public IReadOnlyList<MarketTimingArenaResult> Evaluate(
        NavSeries navSeries,
        MarketTimingExternalEvidence? externalEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        return navSeries.Count == 0
            ? Array.Empty<MarketTimingArenaResult>()
            : this.EvaluateAtCutoff(navSeries, navSeries.Count - 1, externalEvidence);
    }

    /// <summary>
    /// Evaluates all configured horizons as they would have been known at a specific prediction cutoff.
    /// </summary>
    /// <param name="navSeries">The full NAV series.</param>
    /// <param name="predictionCutoffIndex">The last observation index allowed to affect the prediction.</param>
    /// <param name="externalEvidence">Optional external evidence available at the cutoff.</param>
    /// <returns>Timing arena results per horizon.</returns>
    public IReadOnlyList<MarketTimingArenaResult> EvaluateAtCutoff(
        NavSeries navSeries,
        int predictionCutoffIndex,
        MarketTimingExternalEvidence? externalEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        if (predictionCutoffIndex < 0 || predictionCutoffIndex >= navSeries.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(predictionCutoffIndex), predictionCutoffIndex, "Prediction cutoff must be a valid NAV observation index.");
        }

        var cutoffSeries = predictionCutoffIndex == navSeries.Count - 1
            ? navSeries
            : new NavSeries(navSeries.Points.Take(predictionCutoffIndex + 1), navSeries.ObservationFrequency);
        var cutoffEvidence = predictionCutoffIndex == navSeries.Count - 1
            ? externalEvidence
            : null;
        var featureResult = this.featurePipeline.Build(
            cutoffSeries,
            this.options.MinimumFeatureIndex,
            cutoffEvidence,
            this.options.EnableStateModelFeatures,
            this.options.HmmMaximumIterations);
        if (featureResult.Features.Count == 0)
        {
            return Array.Empty<MarketTimingArenaResult>();
        }

        return this.options.Horizons
            .Select(horizon => this.EvaluateHorizon(cutoffSeries, horizon, featureResult))
            .ToArray();
    }

    private MarketTimingArenaResult EvaluateHorizon(
        NavSeries navSeries,
        ForecastHorizon horizon,
        MarketTimingFeaturePipelineResult featureResult)
    {
        var definition = new TripleBarrierDefinition(
            horizon,
            this.options.UpsideThreshold,
            this.options.DownsideThreshold,
            this.options.BarrierPolicy,
            this.options.UpsideVolatilityMultiplier,
            this.options.DownsideVolatilityMultiplier);
        var currentFeature = featureResult.Features[^1];
        var currentBarriers = ResolveCurrentBarriers(definition, currentFeature);
        var trainingLabelEndIndexCutoff = currentFeature.ObservationIndex -
            this.options.PurgeObservations -
            this.options.EmbargoObservations;
        var volatilityPath = BuildVolatilityPath(navSeries, featureResult.Features);
        var labels = this.labeler.Label(navSeries, definition, volatilityPath)
            .Where(label => IsFullHorizonLabel(navSeries, label, horizon))
            .ToArray();
        var trainingLabels = labels
            .Where(label => label.EndIndex <= trainingLabelEndIndexCutoff)
            .OrderBy(label => label.StartIndex)
            .ToArray();
        var trainingIndexes = trainingLabels.Select(label => label.StartIndex).ToHashSet();
        var trainingFeatures = featureResult.Features
            .Where(feature => trainingIndexes.Contains(feature.ObservationIndex))
            .ToArray();
        var warnings = new List<string>();
        if (trainingLabels.Length < this.options.MinimumTrainingSamples)
        {
            warnings.Add("Timing validation has limited historical event samples.");
        }

        var baselineCurrent = HistoricalPrevalence(trainingLabels);
        var evaluations = this.BuildWalkForwardEvaluations(featureResult.Features, labels, horizon);
        var minimumOosSamples = this.ResolveMinimumOosSamples(evaluations.Count);
        if (evaluations.Count < minimumOosSamples)
        {
            warnings.Add($"Timing evidence is insufficient for {horizon}: {evaluations.Count} OOS sample(s), minimum {minimumOosSamples}.");
        }

        var ood = this.outOfDistributionDetector.Evaluate(
            trainingFeatures,
            currentFeature,
            featureResult.FeatureNames,
            this.options.OutOfDistributionThreshold,
            this.options.SlightlyUnusualThreshold);
        var modelResults = new[]
        {
            this.BuildBaselineResult(evaluations, baselineCurrent),
            this.BuildRegimeResult(evaluations, currentFeature, baselineCurrent),
            this.BuildAnalogueResult(featureResult.Features, trainingLabels, evaluations, currentFeature, baselineCurrent),
            this.BuildClassifierResult(featureResult.Features, trainingLabels, evaluations, currentFeature, baselineCurrent),
            this.BuildHazardResult(trainingLabels, evaluations, horizon, baselineCurrent),
            this.BuildSpectralResult(evaluations, currentFeature, baselineCurrent),
        };
        var hazard = this.hazardModel.Fit(trainingLabels, horizon).Forecast();
        var combined = this.ensemble.Combine(
            modelResults,
            baselineCurrent,
            this.options.TargetOosSamplesForEligibility,
            ood.RobustDistance,
            this.options.OutOfDistributionThreshold);
        if (ood.Level == OutOfDistributionLevel.OutOfDistribution)
        {
            warnings.Add("Current timing state is outside historical feature support; reliability is reduced.");
        }
        else if (ood.Level == OutOfDistributionLevel.SlightlyUnusual)
        {
            warnings.Add("Current timing state is slightly unusual relative to historical feature support.");
        }

        var terminalDistribution = this.BuildTerminalReturnDistribution(navSeries, trainingLabels, horizon);
        var reconstructed = this.BuildHistoricalPredictions(evaluations, modelResults);
        return new MarketTimingArenaResult(
            definition,
            currentBarriers,
            currentFeature,
            modelResults,
            combined,
            hazard,
            terminalDistribution.Quantiles,
            terminalDistribution.ExpectedReturn,
            reconstructed,
            ood,
            warnings,
            trainingLabels,
            trainingLabelEndIndexCutoff);
    }

    private IReadOnlyList<WalkForwardTimingEvaluation> BuildWalkForwardEvaluations(
        IReadOnlyList<MarketTimingFeatureVector> features,
        IReadOnlyList<TripleBarrierOutcome> labels,
        ForecastHorizon horizon)
    {
        var candidates = labels
            .Where(label => label.StartIndex >= this.options.MinimumFeatureIndex + this.options.MinimumTrainingSamples)
            .OrderBy(label => label.StartIndex)
            .ToArray();
        if (candidates.Length == 0)
        {
            return Array.Empty<WalkForwardTimingEvaluation>();
        }

        var maximumEvaluations = Math.Max(this.options.MaximumWalkForwardEvaluations, this.options.TargetOosSamplesForEligibility);
        var selected = SelectEvaluationCandidates(candidates, maximumEvaluations);
        var featureByIndex = features.ToDictionary(feature => feature.ObservationIndex);
        var evaluations = new List<WalkForwardTimingEvaluation>();
        foreach (var label in selected)
        {
            var cutoff = label.StartIndex;
            var trainingEndIndexCutoff = cutoff - this.options.PurgeObservations - this.options.EmbargoObservations;
            var trainLabels = labels
                .Where(item => item.EndIndex <= trainingEndIndexCutoff)
                .OrderBy(item => item.StartIndex)
                .ToArray();
            if (trainLabels.Length < this.options.MinimumTrainingSamples)
            {
                continue;
            }

            if (!featureByIndex.TryGetValue(cutoff, out var feature))
            {
                continue;
            }

            evaluations.Add(new WalkForwardTimingEvaluation(
                feature,
                label,
                trainLabels,
                HistoricalPrevalence(trainLabels)));
        }

        return evaluations;
    }

    private MarketTimingModelResult BuildBaselineResult(
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        MarketEventPrediction current)
    {
        var predictions = evaluations.Select(item => item.BaselinePrediction).ToArray();
        return this.BuildResult(
            MarketTimingModelKind.HistoricalEventRateBaseline,
            "Historical event prevalence",
            current,
            predictions,
            evaluations,
            baselinePredictions: predictions,
            eligibleOverride: false,
            "Unconditional historical event prevalence baseline.");
    }

    private MarketTimingModelResult BuildRegimeResult(
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        MarketTimingFeatureVector currentFeature,
        MarketEventPrediction baselineCurrent)
    {
        var predictions = evaluations.Select(item => RegimeAdjusted(item.BaselinePrediction, item.Feature)).ToArray();
        return this.BuildResult(
            MarketTimingModelKind.RegimeTransitionTimingModel,
            "Regime transition timing",
            RegimeAdjusted(baselineCurrent, currentFeature),
            predictions,
            evaluations,
            evaluations.Select(item => item.BaselinePrediction).ToArray(),
            null,
            "Baseline event rates adjusted by causal HMM regime probabilities.");
    }

    private MarketTimingModelResult BuildAnalogueResult(
        IReadOnlyList<MarketTimingFeatureVector> features,
        IReadOnlyList<TripleBarrierOutcome> trainingLabels,
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        MarketTimingFeatureVector currentFeature,
        MarketEventPrediction baselineCurrent)
    {
        var predictions = evaluations.Select(item =>
        {
            var trainFeatures = features.Where(feature => feature.ObservationIndex < item.Feature.ObservationIndex).ToArray();
            return AnaloguePrediction(trainFeatures, item.TrainingLabels, item.Feature, item.BaselinePrediction);
        }).ToArray();
        var current = AnaloguePrediction(features, trainingLabels, currentFeature, baselineCurrent);
        return this.BuildResult(
            MarketTimingModelKind.HistoricalAnalogueTimingModel,
            "Historical analogue timing",
            current,
            predictions,
            evaluations,
            evaluations.Select(item => item.BaselinePrediction).ToArray(),
            null,
            "Empirical event distribution among nearest causal feature analogues.");
    }

    private MarketTimingModelResult BuildClassifierResult(
        IReadOnlyList<MarketTimingFeatureVector> features,
        IReadOnlyList<TripleBarrierOutcome> trainingLabels,
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        MarketTimingFeatureVector currentFeature,
        MarketEventPrediction baselineCurrent)
    {
        var featureNames = currentFeature.Values.Keys.Order(StringComparer.Ordinal).ToArray();
        var classifier = new MarketEventClassifier(this.options.ClassifierOptions);
        var currentFit = classifier.Fit(features, trainingLabels, featureNames);
        var current = currentFit.IsSuccess ? currentFit.Predict(currentFeature) : baselineCurrent;
        var predictions = evaluations.Select(item =>
        {
            var trainFeatures = features.Where(feature => feature.ObservationIndex < item.Feature.ObservationIndex).ToArray();
            var fit = classifier.Fit(trainFeatures, item.TrainingLabels, featureNames);
            return fit.IsSuccess ? fit.Predict(item.Feature) : item.BaselinePrediction;
        }).ToArray();
        return this.BuildResult(
            MarketTimingModelKind.RegularizedEventClassifier,
            "Regularized event classifier",
            current,
            predictions,
            evaluations,
            evaluations.Select(item => item.BaselinePrediction).ToArray(),
            null,
            currentFit.Diagnostic);
    }

    private MarketTimingModelResult BuildHazardResult(
        IReadOnlyList<TripleBarrierOutcome> trainingLabels,
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        ForecastHorizon horizon,
        MarketEventPrediction baselineCurrent)
    {
        var fit = this.hazardModel.Fit(trainingLabels, horizon);
        var current = FromHazard(fit.Forecast());
        var predictions = evaluations.Select(item =>
            FromHazard(this.hazardModel.Fit(item.TrainingLabels, horizon).Forecast())).ToArray();
        return this.BuildResult(
            MarketTimingModelKind.CompetingRiskHazardModel,
            "Competing-risk hazard",
            current,
            predictions,
            evaluations,
            evaluations.Select(item => item.BaselinePrediction).ToArray(),
            false,
            $"{fit.Diagnostic} Unconditional hazards are reported for event timing and cumulative incidence but are not counted as independent ensemble diversity.");
    }

    private MarketTimingModelResult BuildSpectralResult(
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        MarketTimingFeatureVector currentFeature,
        MarketEventPrediction baselineCurrent)
    {
        var predictions = evaluations.Select(item => item.BaselinePrediction).ToArray();
        var diagnostic = "Spectral timing is experimental: historical spectral features are not reconstructed for causal OOS validation, so the candidate is not eligible for ensemble weighting.";
        return this.BuildResult(
            MarketTimingModelKind.SpectralTimingModel,
            "Experimental spectral timing candidate",
            baselineCurrent,
            predictions,
            evaluations,
            evaluations.Select(item => item.BaselinePrediction).ToArray(),
            false,
            diagnostic);
    }

    private MarketTimingModelResult BuildResult(
        MarketTimingModelKind kind,
        string name,
        MarketEventPrediction current,
        IReadOnlyList<MarketEventPrediction> predictions,
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        IReadOnlyList<MarketEventPrediction> baselinePredictions,
        bool? eligibleOverride,
        string diagnostic)
    {
        var outcomes = evaluations.Select(item => item.Label.Outcome).ToArray();
        var rawPredictions = predictions.Select(Normalize).ToArray();
        var rawCurrent = Normalize(current);
        var calibratedPredictions = this.CalibratePrequential(rawPredictions, outcomes);
        var currentCalibration = this.CalibrateCurrent(rawCurrent, rawPredictions, outcomes);
        var calibration = TimingProbabilityMetrics.Summarize(calibratedPredictions, outcomes);
        var rawCalibration = TimingProbabilityMetrics.Summarize(rawPredictions, outcomes);
        var brier = TimingProbabilityMetrics.BrierScore(calibratedPredictions, outcomes);
        var baselineBrier = TimingProbabilityMetrics.BrierScore(baselinePredictions, outcomes);
        var improvement = baselineBrier - brier;
        var perSampleImprovement = calibratedPredictions.Select((prediction, index) =>
            SingleBrier(baselinePredictions[index], outcomes[index]) - SingleBrier(prediction, outcomes[index])).ToArray();
        var interval = BlockBootstrap.MeanInterval(perSampleImprovement, this.ResolveBootstrapBlockSize(evaluations, perSampleImprovement.Length));
        var minimumOosSamples = this.ResolveMinimumOosSamples(evaluations.Count);
        var eligibility = this.ResolveEligibility(
            kind,
            eligibleOverride,
            calibration,
            improvement,
            interval,
            evaluations.Count,
            minimumOosSamples);
        var evidence = this.ResolveEvidence(calibration.SampleCount, minimumOosSamples, improvement, interval);
        return new MarketTimingModelResult(
            kind,
            name,
            rawCurrent,
            currentCalibration.Calibrated,
            calibratedPredictions,
            currentCalibration,
            calibration,
            rawCalibration,
            improvement,
            interval,
            eligibility.Status == ModelEligibilityStatus.Eligible,
            eligibility.Status,
            eligibility.Reason,
            evidence,
            diagnostic);
    }

    private MarketEventPrediction[] CalibratePrequential(
        IReadOnlyList<MarketEventPrediction> rawPredictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        var calibrated = new MarketEventPrediction[rawPredictions.Count];
        for (var index = 0; index < rawPredictions.Count; index++)
        {
            if (index < this.options.MinimumCalibrationSamples)
            {
                calibrated[index] = rawPredictions[index];
                continue;
            }

            try
            {
                var calibrator = new PlattProbabilityCalibrator().Fit(
                    rawPredictions.Take(index).ToArray(),
                    outcomes.Take(index).ToArray());
                calibrated[index] = Normalize(calibrator.Calibrate(rawPredictions[index]));
            }
            catch (ArgumentException)
            {
                calibrated[index] = rawPredictions[index];
            }
            catch (InvalidOperationException)
            {
                calibrated[index] = rawPredictions[index];
            }
        }

        return calibrated;
    }

    private ProbabilityCalibrationDiagnostic CalibrateCurrent(
        MarketEventPrediction rawCurrent,
        IReadOnlyList<MarketEventPrediction> rawPredictions,
        IReadOnlyList<TripleBarrierOutcomeType> outcomes)
    {
        if (rawPredictions.Count < this.options.MinimumCalibrationSamples)
        {
            return new ProbabilityCalibrationDiagnostic(
                rawCurrent,
                rawCurrent,
                ProbabilityCalibrationStatus.InsufficientData,
                "Raw probabilities; not enough prior OOS samples for Platt calibration.",
                rawPredictions.Count);
        }

        try
        {
            var calibrator = new PlattProbabilityCalibrator().Fit(rawPredictions, outcomes);
            return new ProbabilityCalibrationDiagnostic(
                rawCurrent,
                Normalize(calibrator.Calibrate(rawCurrent)),
                ProbabilityCalibrationStatus.Calibrated,
                "One-vs-rest Platt scaling fitted only on prior OOS predictions.",
                rawPredictions.Count);
        }
        catch (ArgumentException exception)
        {
            return new ProbabilityCalibrationDiagnostic(
                rawCurrent,
                rawCurrent,
                ProbabilityCalibrationStatus.Failed,
                $"Raw probabilities; calibration failed: {exception.Message}",
                rawPredictions.Count);
        }
        catch (InvalidOperationException exception)
        {
            return new ProbabilityCalibrationDiagnostic(
                rawCurrent,
                rawCurrent,
                ProbabilityCalibrationStatus.Failed,
                $"Raw probabilities; calibration failed: {exception.Message}",
                rawPredictions.Count);
        }
    }

    private IReadOnlyList<HistoricalTimingPrediction> BuildHistoricalPredictions(
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        IReadOnlyList<MarketTimingModelResult> modelResults)
    {
        if (evaluations.Count == 0)
        {
            return Array.Empty<HistoricalTimingPrediction>();
        }

        var result = new List<HistoricalTimingPrediction>(evaluations.Count);
        var candidates = modelResults
            .Where(model => model.Kind != MarketTimingModelKind.HistoricalEventRateBaseline)
            .ToArray();
        for (var index = 0; index < evaluations.Count; index++)
        {
            var historical = this.BuildHistoricalEnsemblePrediction(index, evaluations, candidates);
            var zone = historical.IsActive
                ? ResolveZone(historical.Prediction, historical.Reliability, false)
                : MarketTimingZone.InsufficientEvidence;
            result.Add(new HistoricalTimingPrediction(
                evaluations[index].Feature.Date,
                historical.Prediction.ProbabilityUpFirst,
                historical.Prediction.ProbabilityDownFirst,
                historical.Prediction.ProbabilityNoEvent,
                zone,
                historical.Reliability,
                historical.Evidence,
                evaluations[index].Label.Outcome));
        }

        return result;
    }

    private HistoricalEnsemblePoint BuildHistoricalEnsemblePrediction(
        int evaluationIndex,
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        IReadOnlyList<MarketTimingModelResult> candidates)
    {
        var priorCount = evaluationIndex;
        var minimumSamples = this.ResolveMinimumOosSamples(priorCount);
        if (priorCount < minimumSamples)
        {
            return new HistoricalEnsemblePoint(
                evaluations[evaluationIndex].BaselinePrediction,
                0d,
                EvidenceStrength.Insufficient,
                false);
        }

        var outcomes = evaluations.Take(priorCount).Select(item => item.Label.Outcome).ToArray();
        var baseline = evaluations.Take(priorCount).Select(item => item.BaselinePrediction).ToArray();
        var eligible = new List<(MarketTimingModelResult Model, double Skill, double Ece)>();
        foreach (var model in candidates)
        {
            if (model.OutOfSamplePredictions.Count <= evaluationIndex)
            {
                continue;
            }

            var predictions = model.OutOfSamplePredictions.Take(priorCount).ToArray();
            var skill = TimingProbabilityMetrics.BrierScore(baseline, outcomes) -
                TimingProbabilityMetrics.BrierScore(predictions, outcomes);
            var ece = TimingProbabilityMetrics.ExpectedCalibrationError(predictions, outcomes);
            var perSampleSkill = predictions.Select((prediction, sampleIndex) =>
                SingleBrier(baseline[sampleIndex], outcomes[sampleIndex]) - SingleBrier(prediction, outcomes[sampleIndex])).ToArray();
            var interval = BlockBootstrap.MeanInterval(perSampleSkill, this.ResolveBootstrapBlockSize(evaluations.Take(priorCount).ToArray(), perSampleSkill.Length));
            if (skill >= this.options.MinimumBrierImprovement &&
                interval.Lower >= Math.Max(0d, this.options.MinimumBootstrapSkillLowerBound) &&
                ece <= this.options.MaximumAcceptableEce)
            {
                eligible.Add((model, skill, ece));
            }
        }

        if (eligible.Count == 0)
        {
            return new HistoricalEnsemblePoint(
                evaluations[evaluationIndex].BaselinePrediction,
                0d,
                EvidenceStrength.Insufficient,
                false);
        }

        var rawWeights = eligible
            .Select(item => Math.Exp((12d * item.Skill) - (2d * item.Ece)))
            .ToArray();
        var rawSum = rawWeights.Sum();
        if (rawSum <= 0d || !double.IsFinite(rawSum))
        {
            return new HistoricalEnsemblePoint(
                evaluations[evaluationIndex].BaselinePrediction,
                0d,
                EvidenceStrength.Insufficient,
                false);
        }

        var weights = rawWeights.Select(weight => weight / rawSum).ToArray();
        var up = Weighted(eligible, weights, item => item.Model.OutOfSamplePredictions[evaluationIndex].ProbabilityUpFirst);
        var down = Weighted(eligible, weights, item => item.Model.OutOfSamplePredictions[evaluationIndex].ProbabilityDownFirst);
        var neutral = Weighted(eligible, weights, item => item.Model.OutOfSamplePredictions[evaluationIndex].ProbabilityNoEvent);
        var prediction = Normalize(new MarketEventPrediction(up, down, neutral));
        var disagreement = WeightedDisagreement(eligible, weights, prediction, evaluationIndex);
        var effectiveCount = 1d / weights.Sum(weight => weight * weight);
        var reliability = ReliabilityIndexCalculator.Calculate(
            priorCount,
            this.options.TargetOosSamplesForEligibility,
            eligible.Average(item => Math.Clamp(item.Ece, 0d, 1d)),
            eligible.Average(item => Math.Max(0d, item.Skill)),
            skillIntervalWidth: 0d,
            temporalInstability: 0d,
            effectiveCount / eligible.Count,
            disagreement,
            oodDistance: 0d,
            oodThreshold: 1d);
        return new HistoricalEnsemblePoint(
            prediction,
            reliability,
            reliability >= 0.65d ? EvidenceStrength.Moderate : EvidenceStrength.Weak,
            true);
    }

    private (ForecastReturnQuantiles? Quantiles, double? ExpectedReturn) BuildTerminalReturnDistribution(
        NavSeries navSeries,
        IReadOnlyList<TripleBarrierOutcome> labels,
        ForecastHorizon horizon)
    {
        var returns = labels
            .Select(label => TerminalReturn(navSeries, label.StartIndex, horizon))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Where(double.IsFinite)
            .Order()
            .ToArray();
        if (returns.Length == 0)
        {
            return (null, null);
        }

        var expected = returns.Average();
        if (returns.Length < this.options.MinimumQuantileSamples)
        {
            return (null, expected);
        }

        return (new ForecastReturnQuantiles(
            Quantile(returns, 0.10d),
            Quantile(returns, 0.25d),
            Quantile(returns, 0.50d),
            Quantile(returns, 0.75d),
            Quantile(returns, 0.90d),
            returns.Length,
            "Empirical terminal horizon returns from historical training labels."), expected);
    }

    private ModelEligibility ResolveEligibility(
        MarketTimingModelKind kind,
        bool? eligibleOverride,
        TimingCalibrationSummary calibration,
        double improvement,
        ProbabilityInterval interval,
        int availableOosSamples,
        int minimumOosSamples)
    {
        if (kind == MarketTimingModelKind.HistoricalEventRateBaseline)
        {
            return new ModelEligibility(ModelEligibilityStatus.BaselineOnly, "Baseline is reported but never ensemble-weighted.");
        }

        if (eligibleOverride == false)
        {
            return new ModelEligibility(ModelEligibilityStatus.ExplicitlyRejected, "Model-family gate rejected this candidate.");
        }

        if (availableOosSamples < minimumOosSamples || calibration.SampleCount < minimumOosSamples)
        {
            return new ModelEligibility(
                ModelEligibilityStatus.InsufficientEvidence,
                $"Insufficient OOS samples ({availableOosSamples} < {minimumOosSamples}).");
        }

        if (calibration.ExpectedCalibrationError > this.options.MaximumAcceptableEce)
        {
            return new ModelEligibility(
                ModelEligibilityStatus.CalibrationRejected,
                $"ECE too high ({calibration.ExpectedCalibrationError:0.###} > {this.options.MaximumAcceptableEce:0.###}).");
        }

        if (improvement < this.options.MinimumBrierImprovement)
        {
            return new ModelEligibility(
                ModelEligibilityStatus.NoPositiveSkill,
                $"Brier skill below threshold ({improvement:0.###} < {this.options.MinimumBrierImprovement:0.###}).");
        }

        var requiredLowerBound = Math.Max(0d, this.options.MinimumBootstrapSkillLowerBound);
        if (interval.Lower < requiredLowerBound)
        {
            return new ModelEligibility(
                ModelEligibilityStatus.UnstableSkill,
                $"Bootstrap skill lower bound too weak ({interval.Lower:0.###} < {requiredLowerBound:0.###}).");
        }

        return new ModelEligibility(ModelEligibilityStatus.Eligible, "Eligible for the horizon-specific ensemble.");
    }

    private int ResolveMinimumOosSamples(int availableOosSamples)
    {
        var fractionBased = (int)Math.Ceiling(Math.Max(0d, this.options.MinimumOosSampleFraction) * availableOosSamples);
        return Math.Min(
            Math.Max(this.options.MinimumOosSamplesAbsolute, this.options.TargetOosSamplesForEligibility),
            Math.Max(this.options.MinimumOosSamplesAbsolute, fractionBased));
    }

    private int ResolveBootstrapBlockSize(
        IReadOnlyList<WalkForwardTimingEvaluation> evaluations,
        int sampleCount)
    {
        if (sampleCount <= 1)
        {
            return 1;
        }

        var medianTimeToEvent = evaluations.Count == 0
            ? 1d
            : Quantile(evaluations.Select(item => (double)Math.Max(1, item.Label.TimeToEvent)).Order().ToArray(), 0.5d);
        var dependenceBlock = (int)Math.Ceiling(Math.Sqrt(Math.Max(1d, medianTimeToEvent)));
        var sampleBlock = (int)Math.Ceiling(Math.Sqrt(sampleCount));
        return Math.Clamp(Math.Max(2, dependenceBlock), 1, Math.Max(1, sampleBlock));
    }

    private EvidenceStrength ResolveEvidence(
        int sampleCount,
        int minimumOosSamples,
        double improvement,
        ProbabilityInterval interval)
    {
        if (sampleCount < minimumOosSamples)
        {
            return EvidenceStrength.Insufficient;
        }

        if (improvement <= 0d)
        {
            return EvidenceStrength.Weak;
        }

        if (interval.Lower > 0.03d && sampleCount >= this.options.TargetOosSamplesForEligibility * 2)
        {
            return EvidenceStrength.Strong;
        }

        if (interval.Lower > 0d && sampleCount >= this.options.TargetOosSamplesForEligibility)
        {
            return EvidenceStrength.Moderate;
        }

        return EvidenceStrength.Weak;
    }

    private void ValidateOptions()
    {
        if (this.options.MaximumWalkForwardEvaluations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.options.MaximumWalkForwardEvaluations), "Maximum walk-forward evaluations must be positive.");
        }

        if (this.options.MinimumOosSamplesAbsolute <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.options.MinimumOosSamplesAbsolute), "Minimum OOS samples must be positive.");
        }

        if (this.options.TargetOosSamplesForEligibility < this.options.MinimumOosSamplesAbsolute)
        {
            throw new ArgumentOutOfRangeException(nameof(this.options.TargetOosSamplesForEligibility), "Target OOS samples must be at least the absolute minimum.");
        }

        if (this.options.MinimumCalibrationSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.options.MinimumCalibrationSamples), "Minimum calibration samples must be positive.");
        }

        if (this.options.OutOfDistributionThreshold <= this.options.SlightlyUnusualThreshold)
        {
            throw new ArgumentOutOfRangeException(nameof(this.options.OutOfDistributionThreshold), "OOD threshold must exceed the slightly-unusual threshold.");
        }
    }

    private static IReadOnlyList<TripleBarrierOutcome> SelectEvaluationCandidates(
        IReadOnlyList<TripleBarrierOutcome> candidates,
        int maximumEvaluations)
    {
        if (candidates.Count <= maximumEvaluations)
        {
            return candidates;
        }

        var selected = new List<TripleBarrierOutcome>(maximumEvaluations);
        var used = new HashSet<int>();
        var step = (candidates.Count - 1d) / Math.Max(1, maximumEvaluations - 1);
        for (var index = 0; index < maximumEvaluations; index++)
        {
            var candidateIndex = (int)Math.Round(index * step);
            if (used.Add(candidateIndex))
            {
                selected.Add(candidates[candidateIndex]);
            }
        }

        return selected.OrderBy(item => item.StartIndex).ToArray();
    }

    private static bool IsFullHorizonLabel(NavSeries navSeries, TripleBarrierOutcome label, ForecastHorizon horizon)
    {
        return horizon.Unit == ForecastHorizonUnit.Observations
            ? label.IsHorizonComplete
            : label.IsHorizonComplete && label.EffectiveValuationDate is not null;
    }

    private static double[] BuildVolatilityPath(NavSeries navSeries, IReadOnlyList<MarketTimingFeatureVector> features)
    {
        var result = Enumerable.Repeat(double.NaN, navSeries.Count).ToArray();
        var logReturns = new double[Math.Max(0, navSeries.Count - 1)];
        for (var index = 1; index < navSeries.Count; index++)
        {
            var previous = (double)navSeries[index - 1].Value;
            var current = (double)navSeries[index].Value;
            if (previous > 0d && current > 0d)
            {
                logReturns[index - 1] = Math.Log(current / previous);
            }
        }

        var ewmaVariance = BuildCausalEwmaVariancePath(logReturns);
        for (var index = 1; index < result.Length; index++)
        {
            var returnIndex = index - 1;
            if (returnIndex < ewmaVariance.Length)
            {
                var volatility = Math.Sqrt(Math.Max(0d, ewmaVariance[returnIndex]));
                result[index] = volatility > 0d && double.IsFinite(volatility) ? volatility : double.NaN;
            }
        }

        foreach (var feature in features)
        {
            var volatility = Value(feature, "garch_or_ewma_volatility");
            if (volatility > 0d && double.IsFinite(volatility))
            {
                result[feature.ObservationIndex] = Math.Max(0.0001d, volatility);
            }
        }

        for (var index = 1; index < result.Length; index++)
        {
            if (!double.IsFinite(result[index]) || result[index] <= 0d)
            {
                result[index] = result[index - 1];
            }
        }

        return result;
    }

    private static EffectiveBarrierDiagnostic ResolveCurrentBarriers(
        TripleBarrierDefinition definition,
        MarketTimingFeatureVector currentFeature)
    {
        if (definition.Policy == BarrierThresholdPolicy.FixedPercentage)
        {
            return new EffectiveBarrierDiagnostic(
                definition.UpsideThreshold,
                definition.DownsideThreshold,
                definition.Policy,
                "Fixed percentage barriers.");
        }

        var volatility = Math.Max(0.0001d, Value(currentFeature, "garch_or_ewma_volatility"));
        return new EffectiveBarrierDiagnostic(
            Math.Max(0.0001d, definition.UpsideVolatilityMultiplier * volatility),
            Math.Max(0.0001d, definition.DownsideVolatilityMultiplier * volatility),
            definition.Policy,
            $"Volatility-scaled barriers from causal volatility {volatility:0.####}.");
    }

    private static MarketEventPrediction HistoricalPrevalence(IReadOnlyList<TripleBarrierOutcome> labels)
    {
        if (labels.Count == 0)
        {
            return new MarketEventPrediction(1d / 3d, 1d / 3d, 1d / 3d);
        }

        var up = labels.Count(label => label.Outcome == TripleBarrierOutcomeType.UpperHitFirst) + 1d;
        var down = labels.Count(label => label.Outcome == TripleBarrierOutcomeType.LowerHitFirst) + 1d;
        var neutral = labels.Count(label => label.Outcome == TripleBarrierOutcomeType.NoBarrierHit) + 1d;
        var sum = up + down + neutral;
        return new MarketEventPrediction(up / sum, down / sum, neutral / sum);
    }

    private static MarketEventPrediction RegimeAdjusted(
        MarketEventPrediction baseline,
        MarketTimingFeatureVector feature)
    {
        var bull = Value(feature, "hmm_bull_probability");
        var bear = Value(feature, "hmm_bear_probability");
        return Normalize(new MarketEventPrediction(
            baseline.ProbabilityUpFirst * (0.8d + (0.6d * bull)),
            baseline.ProbabilityDownFirst * (0.8d + (0.6d * bear)),
            baseline.ProbabilityNoEvent));
    }

    private static MarketEventPrediction AnaloguePrediction(
        IReadOnlyList<MarketTimingFeatureVector> features,
        IReadOnlyList<TripleBarrierOutcome> labels,
        MarketTimingFeatureVector current,
        MarketEventPrediction fallback)
    {
        var labelByIndex = labels.ToDictionary(label => label.StartIndex);
        var candidates = features
            .Where(feature => feature.ObservationIndex < current.ObservationIndex && labelByIndex.ContainsKey(feature.ObservationIndex))
            .ToArray();
        var scaler = MarketTimingRobustFeatureScaler.Fit(candidates, current.Values.Keys);
        var nearest = candidates
            .OrderBy(feature => scaler.Distance(feature, current))
            .Take(25)
            .ToArray();
        if (nearest.Length < 5)
        {
            return fallback;
        }

        var selected = nearest.Select(feature => labelByIndex[feature.ObservationIndex]).ToArray();
        return HistoricalPrevalence(selected);
    }

    private static MarketEventPrediction SpectralPrediction(
        MarketTimingFeatureVector feature,
        MarketEventPrediction baseline)
    {
        var stability = Value(feature, "spectral_stability");
        if (stability <= 0d)
        {
            return baseline;
        }

        var phase = Value(feature, "spectral_phase");
        var directional = Math.Cos(phase);
        return Normalize(new MarketEventPrediction(
            baseline.ProbabilityUpFirst * (1d + (0.25d * stability * Math.Max(0d, directional))),
            baseline.ProbabilityDownFirst * (1d + (0.25d * stability * Math.Max(0d, -directional))),
            baseline.ProbabilityNoEvent));
    }

    private static MarketEventPrediction FromHazard(CompetingRiskForecast forecast)
    {
        return Normalize(new MarketEventPrediction(
            forecast.ProbabilityUpByHorizon,
            forecast.ProbabilityDownByHorizon,
            forecast.ProbabilityNoEventByHorizon));
    }

    private static MarketTimingZone ResolveZone(
        MarketEventPrediction prediction,
        double reliability,
        bool outOfDistribution)
    {
        if (outOfDistribution)
        {
            return MarketTimingZone.InsufficientEvidence;
        }

        if (reliability < 0.25d)
        {
            return MarketTimingZone.Neutral;
        }

        var edge = prediction.ProbabilityUpFirst - prediction.ProbabilityDownFirst;
        return edge switch
        {
            >= 0.30d => MarketTimingZone.StrongAccumulation,
            >= 0.18d => MarketTimingZone.Accumulation,
            >= 0.08d => MarketTimingZone.WatchPositive,
            <= -0.30d => MarketTimingZone.StrongReduction,
            <= -0.18d => MarketTimingZone.Reduction,
            <= -0.08d => MarketTimingZone.WatchNegative,
            _ => MarketTimingZone.Neutral,
        };
    }

    private static double SingleBrier(MarketEventPrediction prediction, TripleBarrierOutcomeType outcome)
    {
        var actual = TimingProbabilityMetrics.ToClass(outcome);
        var sum = 0d;
        for (var klass = 0; klass < 3; klass++)
        {
            var error = prediction.Probabilities[klass] - (klass == actual ? 1d : 0d);
            sum += error * error;
        }

        return sum;
    }

    private static double Value(MarketTimingFeatureVector feature, string name)
    {
        return feature.TryGetFeature(name, out var value) ? value : 0d;
    }

    private static double[] BuildCausalEwmaVariancePath(IReadOnlyList<double> values, double lambda = 0.94d)
    {
        var variances = new double[values.Count];
        if (values.Count == 0)
        {
            return variances;
        }

        variances[0] = double.NaN;
        for (var index = 1; index < values.Count; index++)
        {
            var previousVariance = double.IsFinite(variances[index - 1])
                ? variances[index - 1]
                : values[index - 1] * values[index - 1];
            variances[index] = (lambda * previousVariance) + ((1d - lambda) * values[index - 1] * values[index - 1]);
        }

        return variances;
    }

    private static MarketEventPrediction Normalize(MarketEventPrediction prediction)
    {
        var up = Math.Clamp(prediction.ProbabilityUpFirst, 0d, 1d);
        var down = Math.Clamp(prediction.ProbabilityDownFirst, 0d, 1d);
        var neutral = Math.Clamp(prediction.ProbabilityNoEvent, 0d, 1d);
        var sum = up + down + neutral;
        return sum <= 0d || !double.IsFinite(sum)
            ? new MarketEventPrediction(1d / 3d, 1d / 3d, 1d / 3d)
            : new MarketEventPrediction(up / sum, down / sum, neutral / sum);
    }

    private static double? TerminalReturn(NavSeries navSeries, int startIndex, ForecastHorizon horizon)
    {
        var terminalIndex = horizon.Unit == ForecastHorizonUnit.Observations
            ? startIndex + horizon.Value
            : FindIndexOnOrAfter(navSeries, navSeries[startIndex].Date.AddDays(horizon.Value));
        if (terminalIndex <= startIndex || terminalIndex >= navSeries.Count)
        {
            return null;
        }

        var start = (double)navSeries[startIndex].Value;
        if (start <= 0d || !double.IsFinite(start))
        {
            return null;
        }

        return ((double)navSeries[terminalIndex].Value / start) - 1d;
    }

    private static int FindIndexOnOrAfter(NavSeries navSeries, DateOnly date)
    {
        for (var index = 0; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= date)
            {
                return index;
            }
        }

        return -1;
    }

    private static double Quantile(IReadOnlyList<double> sorted, double probability)
    {
        if (sorted.Count == 0)
        {
            return 0d;
        }

        if (sorted.Count == 1)
        {
            return sorted[0];
        }

        var position = probability * (sorted.Count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        var weight = position - lower;
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * weight);
    }

    private static double Weighted<T>(
        IReadOnlyList<T> models,
        IReadOnlyList<double> weights,
        Func<T, double> selector)
    {
        var sum = 0d;
        for (var index = 0; index < models.Count; index++)
        {
            sum += selector(models[index]) * weights[index];
        }

        return sum;
    }

    private static double WeightedDisagreement(
        IReadOnlyList<(MarketTimingModelResult Model, double Skill, double Ece)> models,
        IReadOnlyList<double> weights,
        MarketEventPrediction prediction,
        int evaluationIndex)
    {
        var sum = 0d;
        for (var index = 0; index < models.Count; index++)
        {
            var modelPrediction = models[index].Model.OutOfSamplePredictions[evaluationIndex];
            var upDeviation = modelPrediction.ProbabilityUpFirst - prediction.ProbabilityUpFirst;
            var downDeviation = modelPrediction.ProbabilityDownFirst - prediction.ProbabilityDownFirst;
            sum += weights[index] * ((upDeviation * upDeviation) + (downDeviation * downDeviation));
        }

        return Math.Sqrt(sum);
    }

    private sealed record WalkForwardTimingEvaluation(
        MarketTimingFeatureVector Feature,
        TripleBarrierOutcome Label,
        IReadOnlyList<TripleBarrierOutcome> TrainingLabels,
        MarketEventPrediction BaselinePrediction);

    private sealed record ModelEligibility(ModelEligibilityStatus Status, string Reason);

    private sealed record HistoricalEnsemblePoint(
        MarketEventPrediction Prediction,
        double Reliability,
        EvidenceStrength Evidence,
        bool IsActive);
}
