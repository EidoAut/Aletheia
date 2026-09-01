#pragma warning disable SA1118 // Readability is better with full record construction arguments aligned here.
#pragma warning disable SA1204 // Static timing helpers are grouped after the builder workflow.

using Aletheia.Core;
using Aletheia.Dynamics;
using Aletheia.Spectral;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Builds application-level market-timing assessments from the scientific timing arena.
/// </summary>
public sealed class MarketTimingAssessmentBuilder
{
    private readonly MarketTimingEngineOptions engineOptions;
    private readonly MarketTimingPresentationOptions presentationOptions;
    private readonly PredictionExplanationBuilder explanationBuilder = new();
    private readonly RegimeTransitionForecaster regimeTransitionForecaster = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketTimingAssessmentBuilder"/> class.
    /// </summary>
    /// <param name="engineOptions">Scientific engine options.</param>
    /// <param name="presentationOptions">Presentation thresholds.</param>
    public MarketTimingAssessmentBuilder(
        MarketTimingEngineOptions? engineOptions = null,
        MarketTimingPresentationOptions? presentationOptions = null)
    {
        this.engineOptions = engineOptions ?? new MarketTimingEngineOptions();
        this.presentationOptions = presentationOptions ?? new MarketTimingPresentationOptions();
    }

    /// <summary>
    /// Builds a timing assessment.
    /// </summary>
    /// <param name="history">The fund history.</param>
    /// <param name="analysis">The current analysis.</param>
    /// <returns>The market-timing assessment.</returns>
    public MarketTimingAssessment Build(FundHistory history, FundAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(analysis);
        var externalEvidence = BuildExternalEvidence(analysis);
        var arena = new MarketTimingModelArena(this.engineOptions).Evaluate(history.NavSeries, externalEvidence);
        var currentNav = analysis.Nav.Count == 0 ? 0d : analysis.Nav[^1].Value;
        var horizonAssessments = arena
            .Select(result => BuildHorizonAssessment(result, currentNav))
            .ToArray();
        var primarySelection = SelectPrimary(horizonAssessments, arena, this.engineOptions);
        var primary = primarySelection.Horizon;
        var primaryArena = primary is null
            ? null
            : arena.FirstOrDefault(result => result.Definition.Horizon == primary.Horizon);
        var zone = primary is null ? MarketTimingZone.InsufficientEvidence : primary.Zone;
        var warnings = arena.SelectMany(result => result.Warnings).Distinct(StringComparer.Ordinal).ToList();
        if (arena.Count == 0)
        {
            warnings.Add("Market timing unavailable: insufficient history.");
        }

        var decision = BuildDecision(primary, zone, warnings, primaryArena);
        var why = this.explanationBuilder.BuildWhy(primary, primaryArena);
        var narrative = this.explanationBuilder.Build(primary, decision, warnings);
        var regimeForecasts = this.BuildRegimeForecasts(analysis);
        var alerts = BuildAlerts(primary, primaryArena);
        var change = BuildChange(primaryArena, zone);
        return new MarketTimingAssessment(
            DateTimeOffset.UtcNow,
            history.NavSeries.EndDate,
            analysis.CurrentState,
            horizonAssessments,
            zone,
            decision,
            primarySelection.Reason,
            narrative,
            warnings,
            why.Evidence,
            why.CounterEvidence,
            alerts,
            change,
            regimeForecasts,
            arena,
            primaryArena?.CurrentFeature.Values.TryGetValue("garch_or_ewma_volatility", out _) == true
                ? "GARCH/EWMA volatility features are active in timing."
                : "Volatility features unavailable.",
            primaryArena?.CurrentFeature.Values.TryGetValue("garch_or_ewma_volatility", out _) == true);
    }

    private static MarketTimingExternalEvidence BuildExternalEvidence(FundAnalysisResult analysis)
    {
        var spectral = new SpectralEvidenceAnalyzer().AnalyzeDominant(analysis.Spectrum, analysis.SpectralStability);
        var ensemble = analysis.ResearchReport?.Ensemble;
        return new MarketTimingExternalEvidence(
            spectral?.Reliability,
            spectral?.Phase,
            spectral?.Stability,
            ensemble?.Distribution?.ExpectedReturn,
            ensemble?.Distribution?.ProbabilityPositive,
            ensemble?.Distribution?.ProbabilityLossGreaterThanTenPercent,
            ensemble?.ModelDisagreement,
            ensemble?.Reliability);
    }

    private MarketTimingHorizonAssessment BuildHorizonAssessment(MarketTimingArenaResult result, double currentNav)
    {
        var prediction = result.Ensemble.Prediction;
        var upBeforeDown = prediction.ProbabilityUpFirst + prediction.ProbabilityDownFirst <= 0d
            ? 0.5d
            : prediction.ProbabilityUpFirst / (prediction.ProbabilityUpFirst + prediction.ProbabilityDownFirst);
        var downBeforeUp = 1d - upBeforeDown;
        var upper = result.CurrentBarriers.Upside;
        var lower = result.CurrentBarriers.Downside;
        var expectedBarrierPayoff = (prediction.ProbabilityUpFirst * upper) - (prediction.ProbabilityDownFirst * lower);
        var agreement = Math.Clamp(1d - result.Ensemble.ModelDisagreement, 0d, 1d);
        var reliability = result.OutOfDistribution.Level switch
        {
            OutOfDistributionLevel.OutOfDistribution => result.Ensemble.Reliability * 0.5d,
            OutOfDistributionLevel.SlightlyUnusual => result.Ensemble.Reliability * 0.8d,
            _ => result.Ensemble.Reliability,
        };
        var evidence = ResolveEvidence(result.Models);
        var zone = ResolveZone(prediction, expectedBarrierPayoff, reliability, evidence, result.OutOfDistribution.OutOfDistribution, result.Ensemble.IsActive);
        var upOutcomes = result.HistoricalPredictions
            .Select(item => item.RealizedOutcome == TripleBarrierOutcomeType.UpperHitFirst ? 1d : 0d)
            .ToArray();
        var downOutcomes = result.HistoricalPredictions
            .Select(item => item.RealizedOutcome == TripleBarrierOutcomeType.LowerHitFirst ? 1d : 0d)
            .ToArray();
        var quantiles = result.TerminalReturnQuantiles;
        return new MarketTimingHorizonAssessment(
            result.Definition.Horizon,
            prediction.ProbabilityUpFirst,
            prediction.ProbabilityDownFirst,
            prediction.ProbabilityNoEvent,
            upBeforeDown,
            downBeforeUp,
            result.ForecastExpectedReturn,
            expectedBarrierPayoff,
            prediction.ProbabilityDownFirst,
            upper,
            lower,
            quantiles,
            result.HazardForecast.MedianTimeToUp,
            result.HazardForecast.MedianTimeToDown,
            result.HazardForecast.ExpectedTimeToFirstEvent,
            BlockBootstrap.MeanInterval(upOutcomes),
            BlockBootstrap.MeanInterval(downOutcomes),
            reliability,
            agreement,
            evidence,
            zone,
            quantiles is null ? null : currentNav * (1d + quantiles.P10),
            quantiles is null ? null : currentNav * (1d + quantiles.P50),
            quantiles is null ? null : currentNav * (1d + quantiles.P90));
    }

    private TimingDecision BuildDecision(
        MarketTimingHorizonAssessment? primary,
        MarketTimingZone zone,
        IReadOnlyList<string> warnings,
        MarketTimingArenaResult? primaryArena)
    {
        if (primary is null)
        {
            return new TimingDecision(
                TimingDecisionAction.InsufficientEvidence,
                0d,
                0d,
                ConfidenceLevel.Low,
                null,
                0d,
                0d,
                0d,
                0d,
                Array.Empty<string>(),
                ["Predictive evidence is insufficient for a directional timing signal."])
            {
                Direction = DirectionalSignal.None,
                Qualification = SignalQualification.Unavailable,
                DirectionalStrength = 0d,
                ValidationStrength = 0d,
                Reasons = [DecisionSignalLabels.NoCallExplanation],
            };
        }

        var severeOutOfDistribution = primaryArena?.OutOfDistribution.Level == OutOfDistributionLevel.OutOfDistribution;
        if (severeOutOfDistribution)
        {
            return new TimingDecision(
                TimingDecisionAction.InsufficientEvidence,
                0d,
                primary.ProbabilityUp,
                ConfidenceLevel.Low,
                primary.Horizon,
                0d,
                0d,
                0d,
                0d,
                BuildTimingDirectionalEvidence(primary),
                ["Current timing state is out-of-distribution, so Aletheia does not issue a current timing call."])
            {
                Direction = DirectionalSignal.None,
                Qualification = SignalQualification.Unavailable,
                DirectionalStrength = 0d,
                ValidationStrength = Math.Clamp(primary.Reliability, 0d, 1d),
                Reasons = ["Out-of-distribution current features force NO CALL for timing."],
            };
        }

        if (primary.Zone == MarketTimingZone.InsufficientEvidence ||
            primary.Reliability < this.presentationOptions.MinimumDirectionalReliability ||
            primary.EvidenceStrength < this.presentationOptions.MinimumDirectionalEvidence)
        {
            var tentativeDirection = DirectionFromTimingEdge(primary);
            var strength = StrengthFromTimingEdge(primary, tentativeDirection);
            return new TimingDecision(
                TimingDecisionAction.InsufficientEvidence,
                strength,
                tentativeDirection == DirectionalSignal.Sell ? primary.ProbabilityDown : primary.ProbabilityUp,
                ConfidenceLevel.Low,
                primary.Horizon,
                0d,
                0d,
                0d,
                0d,
                BuildTimingDirectionalEvidence(primary),
                ["Predictive evidence is insufficient for a fully validated timing signal."])
            {
                Direction = tentativeDirection,
                Qualification = SignalQualification.Tentative,
                DirectionalStrength = strength,
                ValidationStrength = Math.Clamp(primary.Reliability, 0d, 1d),
                Reasons =
                [
                    "Timing probabilities have a readable direction, but reliability or evidence strength is below the presentation gate.",
                    DecisionSignalLabels.TentativeMarkerExplanation,
                ],
            };
        }

        var action = zone switch
        {
            MarketTimingZone.StrongAccumulation => TimingDecisionAction.StrongBuy,
            MarketTimingZone.Accumulation => TimingDecisionAction.Buy,
            MarketTimingZone.WatchPositive => TimingDecisionAction.Hold,
            MarketTimingZone.WatchNegative => TimingDecisionAction.WatchNegative,
            MarketTimingZone.Reduction => TimingDecisionAction.Reduce,
            MarketTimingZone.StrongReduction => TimingDecisionAction.Sell,
            _ => TimingDecisionAction.Hold,
        };
        var payoff = primary.ExpectedBarrierPayoff;
        var expectedUpside = primary.ProbabilityUp * primary.UpsideBarrier;
        var expectedDownside = primary.ProbabilityDown * primary.DownsideBarrier;
        var utility = payoff - (this.presentationOptions.RiskPenaltyLambda * primary.DownsideProbability * Math.Abs(payoff));
        var direction = DirectionFromTimingZone(zone);
        var qualification = zone is MarketTimingZone.WatchPositive or MarketTimingZone.WatchNegative
            ? SignalQualification.Tentative
            : SignalQualification.Confirmed;
        var reasons = new List<string>();
        if (qualification == SignalQualification.Confirmed)
        {
            reasons.Add("Timing horizon passed reliability and evidence gates for the selected zone.");
        }
        else
        {
            reasons.Add("Timing edge is visible but remains in a watch zone, so the investor label stays tentative.");
            reasons.Add(DecisionSignalLabels.TentativeMarkerExplanation);
        }

        return new TimingDecision(
            action,
            StrengthFromTimingEdge(primary, direction),
            action is TimingDecisionAction.Reduce or TimingDecisionAction.Sell or TimingDecisionAction.WatchNegative
                ? primary.ProbabilityDown
                : primary.ProbabilityUp,
            ResolveConfidence(primary, warnings),
            primary.Horizon,
            expectedUpside,
            expectedDownside,
            payoff,
            utility,
            [$"Expected barrier payoff: {payoff:0.####}.", $"Model agreement: {primary.ModelAgreement:0.####}."],
            primary.ProbabilityDown > 0.25d ? [$"Downside probability remains {primary.ProbabilityDown:0.####}."] : Array.Empty<string>())
        {
            Direction = direction,
            Qualification = qualification,
            DirectionalStrength = StrengthFromTimingEdge(primary, direction),
            ValidationStrength = Math.Clamp(primary.Reliability, 0d, 1d),
            Reasons = reasons,
        };
    }

    private IReadOnlyList<RegimeTransitionForecast> BuildRegimeForecasts(FundAnalysisResult analysis)
    {
        var regime = analysis.ResearchReport?.RegimeModel;
        if (regime is null || regime.States.Count == 0)
        {
            return Array.Empty<RegimeTransitionForecast>();
        }

        return this.engineOptions.Horizons
            .Select(horizon => this.regimeTransitionForecaster.Forecast(regime, horizon))
            .ToArray();
    }

    private static IReadOnlyList<TimingAlertCondition> BuildAlerts(
        MarketTimingHorizonAssessment? primary,
        MarketTimingArenaResult? arena)
    {
        if (primary is null)
        {
            return Array.Empty<TimingAlertCondition>();
        }

        return
        [
            new TimingAlertCondition(
                TimingAlertKind.AccumulationZoneEntered,
                primary.Zone is MarketTimingZone.Accumulation or MarketTimingZone.StrongAccumulation,
                "Accumulation zone is active."),
            new TimingAlertCondition(
                TimingAlertKind.ReductionZoneEntered,
                primary.Zone is MarketTimingZone.Reduction or MarketTimingZone.StrongReduction,
                "Reduction zone is active."),
            new TimingAlertCondition(
                TimingAlertKind.StructuralChangeDetected,
                arena?.CurrentFeature.Values.TryGetValue("change_point_probability", out var changePoint) == true && changePoint > 0.6d,
                "Structural change probability is elevated."),
        ];
    }

    private static TimingAssessmentChange? BuildChange(
        MarketTimingArenaResult? arena,
        MarketTimingZone currentZone)
    {
        if (arena is null || arena.HistoricalPredictions.Count < 2)
        {
            return null;
        }

        var previous = arena.HistoricalPredictions.LastOrDefault(item => item.Zone != currentZone);
        if (previous is null)
        {
            return null;
        }

        var changedAgo = arena.HistoricalPredictions.Count -
            arena.HistoricalPredictions.ToList().FindLastIndex(item => item.Zone == previous.Zone) -
            1;
        return new TimingAssessmentChange(
            previous.Zone,
            currentZone,
            Math.Max(0, changedAgo),
            [$"Current zone differs from the last reconstructed {previous.Zone} state."]);
    }

    private MarketTimingZone ResolveZone(
        MarketEventPrediction prediction,
        double expectedBarrierPayoff,
        double reliability,
        EvidenceStrength evidence,
        bool outOfDistribution,
        bool ensembleActive)
    {
        if (!ensembleActive ||
            outOfDistribution ||
            reliability < this.presentationOptions.MinimumDirectionalReliability ||
            evidence < this.presentationOptions.MinimumDirectionalEvidence)
        {
            return MarketTimingZone.InsufficientEvidence;
        }

        var edge = prediction.ProbabilityUpFirst - prediction.ProbabilityDownFirst;
        if (edge >= this.presentationOptions.StrongEdge && expectedBarrierPayoff > 0d)
        {
            return MarketTimingZone.StrongAccumulation;
        }

        if (edge >= this.presentationOptions.DirectionalEdge && expectedBarrierPayoff > 0d)
        {
            return MarketTimingZone.Accumulation;
        }

        if (edge >= this.presentationOptions.WatchEdge)
        {
            return MarketTimingZone.WatchPositive;
        }

        if (edge <= -this.presentationOptions.StrongEdge && expectedBarrierPayoff < 0d)
        {
            return MarketTimingZone.StrongReduction;
        }

        if (edge <= -this.presentationOptions.DirectionalEdge && expectedBarrierPayoff < 0d)
        {
            return MarketTimingZone.Reduction;
        }

        return edge <= -this.presentationOptions.WatchEdge ? MarketTimingZone.WatchNegative : MarketTimingZone.Neutral;
    }

    private DirectionalSignal DirectionFromTimingEdge(MarketTimingHorizonAssessment primary)
    {
        var edge = primary.ProbabilityUp - primary.ProbabilityDown;
        if (edge >= this.presentationOptions.WatchEdge)
        {
            return DirectionalSignal.Buy;
        }

        if (edge <= -this.presentationOptions.WatchEdge)
        {
            return DirectionalSignal.Sell;
        }

        return DirectionalSignal.Hold;
    }

    private static DirectionalSignal DirectionFromTimingZone(MarketTimingZone zone)
    {
        return zone switch
        {
            MarketTimingZone.StrongAccumulation or MarketTimingZone.Accumulation or MarketTimingZone.WatchPositive => DirectionalSignal.Buy,
            MarketTimingZone.Reduction or MarketTimingZone.StrongReduction or MarketTimingZone.WatchNegative => DirectionalSignal.Sell,
            MarketTimingZone.Neutral => DirectionalSignal.Hold,
            _ => DirectionalSignal.None,
        };
    }

    private double StrengthFromTimingEdge(MarketTimingHorizonAssessment primary, DirectionalSignal direction)
    {
        var edge = Math.Abs(primary.ProbabilityUp - primary.ProbabilityDown);
        if (direction == DirectionalSignal.Hold)
        {
            return Math.Clamp(1d - (edge / this.presentationOptions.WatchEdge), 0d, 1d);
        }

        return Math.Clamp(edge, 0d, 1d);
    }

    private static IReadOnlyList<string> BuildTimingDirectionalEvidence(MarketTimingHorizonAssessment primary)
    {
        return
        [
            $"Timing probabilities: P(up first) {primary.ProbabilityUp:0.####}, P(down first) {primary.ProbabilityDown:0.####}.",
            $"Expected barrier payoff: {primary.ExpectedBarrierPayoff:0.####}.",
            $"ReliabilityIndex {primary.ReliabilityIndex:0.####}; evidence strength {primary.EvidenceStrength}.",
        ];
    }

    private static (MarketTimingHorizonAssessment? Horizon, string Reason) SelectPrimary(
        IReadOnlyList<MarketTimingHorizonAssessment> horizons,
        IReadOnlyList<MarketTimingArenaResult> arena,
        MarketTimingEngineOptions options)
    {
        if (horizons.Count == 0)
        {
            return (null, "No timing horizon was available.");
        }

        var scored = horizons.Select(horizon =>
        {
            var arenaResult = arena.FirstOrDefault(result => result.Definition.Horizon == horizon.Horizon);
            var calibrationPenalty = arenaResult is null || arenaResult.Models.Count == 0
                ? 0.25d
                : arenaResult.Models
                    .Where(model => model.EligibleForEnsemble)
                    .DefaultIfEmpty()
                    .Average(model => model is null ? 0.25d : Math.Clamp(model.Calibration.ExpectedCalibrationError, 0d, 1d));
            var oodPenalty = arenaResult?.OutOfDistribution.Level switch
            {
                OutOfDistributionLevel.OutOfDistribution => 0.35d,
                OutOfDistributionLevel.SlightlyUnusual => 0.15d,
                _ => 0d,
            };
            var preference = horizon.Horizon.Unit == ForecastHorizonUnit.Observations
                ? 1d / (1d + (Math.Abs(horizon.Horizon.Value - options.PreferredPrimaryHorizonObservations) / 60d))
                : 0.5d;
            var evidence = horizon.EvidenceStrength switch
            {
                EvidenceStrength.Strong => 1d,
                EvidenceStrength.Moderate => 0.75d,
                EvidenceStrength.Weak => 0.45d,
                _ => 0d,
            };
            var active = arenaResult?.Ensemble.IsActive == true ? 1d : 0d;
            var score = (0.35d * horizon.Reliability) +
                (0.20d * horizon.ModelAgreement) +
                (0.20d * evidence) +
                (0.10d * preference) +
                (0.15d * active) -
                (0.15d * calibrationPenalty) -
                oodPenalty;
            return (Horizon: horizon, Score: score, Active: active);
        })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Horizon.Reliability)
            .ToArray();

        var selected = scored[0];
        return (
            selected.Horizon,
            selected.Active <= 0d
                ? $"Selected {selected.Horizon.Horizon} as the least-weak horizon, but evidence is insufficient for a decision."
                : $"Selected {selected.Horizon.Horizon} because it had the best ReliabilityIndex/evidence/agreement score ({selected.Score:0.###}).");
    }

    private static EvidenceStrength ResolveEvidence(IReadOnlyList<MarketTimingModelResult> models)
    {
        if (models.Count == 0)
        {
            return EvidenceStrength.Insufficient;
        }

        return models.Max(model => model.Evidence);
    }

    private static ConfidenceLevel ResolveConfidence(
        MarketTimingHorizonAssessment primary,
        IReadOnlyList<string> warnings)
    {
        var score = primary.Reliability;
        if (primary.EvidenceStrength == EvidenceStrength.Strong)
        {
            score += 0.20d;
        }
        else if (primary.EvidenceStrength == EvidenceStrength.Insufficient)
        {
            score -= 0.25d;
        }

        if (warnings.Count > 0)
        {
            score -= 0.15d;
        }

        return score >= 0.72d ? ConfidenceLevel.High : score >= 0.40d ? ConfidenceLevel.Medium : ConfidenceLevel.Low;
    }
}
