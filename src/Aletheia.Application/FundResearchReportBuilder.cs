#pragma warning disable SA1204 // Workflow methods stay grouped before private static helpers.

using System.Globalization;
using Aletheia.Core;
using Aletheia.Dynamics;
using Aletheia.Forecasting;
using Aletheia.Simulation;
using Aletheia.Spectral;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Builds deterministic fund research reports from already computed analytics.
/// </summary>
public sealed class FundResearchReportBuilder
{
    private const double MaterialAdverseForecastThreshold = -0.05d;
    private const double MaterialConstructiveForecastThreshold = 0.05d;
    private const double MaterialProbabilityEdgeThreshold = 0.08d;
    private const double MaterialRegimeProbabilityThreshold = 0.60d;
    private const double MaterialDisagreementThreshold = 0.05d;

    private readonly FundScoringOptions options;
    private readonly GaussianHiddenMarkovModel regimeModel = new();
    private readonly SpectralEvidenceAnalyzer spectralEvidenceAnalyzer = new();
    private readonly StressScenarioAnalyzer stressScenarioAnalyzer = new();
    private readonly ForecastEnsemble ensemble = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FundResearchReportBuilder"/> class.
    /// </summary>
    /// <param name="options">The scoring options.</param>
    public FundResearchReportBuilder(FundScoringOptions? options = null)
    {
        this.options = options ?? new FundScoringOptions();
    }

    /// <summary>
    /// Builds the report.
    /// </summary>
    /// <param name="history">The fund history.</param>
    /// <param name="analysis">The computed fund analysis.</param>
    /// <param name="arena">The optional Model Arena result.</param>
    /// <param name="timing">The optional market-timing assessment.</param>
    /// <returns>The research report.</returns>
    public FundResearchReport Build(
        FundHistory history,
        FundAnalysisResult analysis,
        ModelArenaResult? arena = null,
        MarketTimingAssessment? timing = null)
    {
        var arenas = arena is null ? Array.Empty<ModelArenaResult>() : [arena];
        return this.Build(history, analysis, arenas, timing);
    }

    /// <summary>
    /// Builds the report with horizon-indexed Model Arena evidence.
    /// </summary>
    /// <param name="history">The fund history.</param>
    /// <param name="analysis">The computed fund analysis.</param>
    /// <param name="arenas">The optional Model Arena results by horizon.</param>
    /// <param name="timing">The optional market-timing assessment.</param>
    /// <returns>The research report.</returns>
    public FundResearchReport Build(
        FundHistory history,
        FundAnalysisResult analysis,
        IReadOnlyList<ModelArenaResult> arenas,
        MarketTimingAssessment? timing = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(arenas);

        var warnings = new List<string>();
        var freshness = this.ResolveFreshness(analysis.Dataset);
        this.AddDatasetWarnings(analysis.Dataset, freshness, warnings);
        var logReturns = analysis.LogReturns.Select(point => point.Value).ToArray();
        var regime = this.BuildRegime(logReturns, warnings);
        var spectralEvidence = this.spectralEvidenceAnalyzer.AnalyzeDominant(analysis.Spectrum, analysis.SpectralStability);
        var stress = this.BuildStress(history.NavSeries);
        var ensembleBuild = this.BuildEnsemble(analysis.Forecasts, arenas, warnings);
        var ensembleResult = ensembleBuild.Result;
        var score = this.BuildFundScore(analysis, arenas, warnings);
        var attractiveness = this.BuildAttractiveness(analysis, ensembleResult, regime, warnings);
        var signal = this.BuildDecisionSignal(analysis, attractiveness, ensembleResult, regime, timing, freshness);
        var actionability = this.BuildActionability(signal, timing, freshness);
        var provenance = new Dictionary<string, string>
        {
            ["DatasetFingerprint"] = analysis.Dataset.DatasetFingerprint,
            ["ScientificVersion"] = AletheiaRelease.ScientificVersion,
            ["StateSchemaFingerprint"] = analysis.CurrentState.Schema?.Fingerprint ?? "n/a",
            ["RegimeModel"] = regime is null ? "Unavailable" : "GaussianHMM",
            ["Ensemble"] = ensembleResult?.Distribution is null ? "Unavailable" : "EvidenceWeighted",
            ["EffectiveObservationPolicy"] = analysis.Dataset.EffectiveObservationPolicy,
            ["DataFreshness"] = freshness.Status.ToString(),
            ["EnsembleWeightSum"] = ensembleResult is null
                ? "n/a"
                : ensembleResult.Components.Sum(component => component.Weight).ToString("G17", CultureInfo.InvariantCulture),
        };

        return new FundResearchReport(
            analysis.Dataset,
            AletheiaRelease.ScientificVersion,
            freshness,
            analysis.Performance,
            analysis.CurrentState,
            regime,
            spectralEvidence,
            analysis.Forecasts,
            ensembleResult,
            stress,
            score,
            attractiveness,
            signal,
            actionability,
            ensembleBuild.Audit,
            warnings,
            provenance);
    }

    private double ClampScore(double value)
    {
        return Math.Clamp(value, 1d, 10d);
    }

    private double ScoreFromUnit(double unit)
    {
        return ClampScore(1d + (9d * Math.Clamp(unit, 0d, 1d)));
    }

    private double HigherIsBetter(double value, double poor, double strong)
    {
        if (strong <= poor)
        {
            throw new ArgumentException("Strong threshold must be above poor threshold.", nameof(strong));
        }

        return Math.Clamp((value - poor) / (strong - poor), 0d, 1d);
    }

    private double LowerIsBetter(double value, double strong, double poor)
    {
        if (poor <= strong)
        {
            throw new ArgumentException("Poor threshold must be above strong threshold.", nameof(poor));
        }

        return Math.Clamp((poor - value) / (poor - strong), 0d, 1d);
    }

    private ConfidenceLevel ConfidenceFromUnit(double unit)
    {
        return unit >= 0.75d ? ConfidenceLevel.High : unit >= 0.45d ? ConfidenceLevel.Medium : ConfidenceLevel.Low;
    }

    private GaussianHmmResult? BuildRegime(IReadOnlyList<double> logReturns, ICollection<string> warnings)
    {
        if (logReturns.Count < 60)
        {
            warnings.Add("Regime model unavailable: insufficient log-return history.");
            return null;
        }

        var result = this.regimeModel.Fit(logReturns, logReturns.Count >= 300 ? 3 : 2);
        if (!result.Converged)
        {
            warnings.Add($"Regime model diagnostic: {result.Diagnostic}");
        }

        return result.States.Count == 0 ? null : result;
    }

    private IReadOnlyList<StressScenarioResult> BuildStress(NavSeries navSeries)
    {
        if (navSeries.Count < 2)
        {
            return Array.Empty<StressScenarioResult>();
        }

        return
        [
            this.stressScenarioAnalyzer.HistoricalWorstWindow(navSeries, Math.Min(252, navSeries.Count - 1)),
            this.stressScenarioAnalyzer.ReturnShock(-0.15d),
            this.stressScenarioAnalyzer.ProlongedBear(),
        ];
    }

    private (ForecastEnsembleResult? Result, IReadOnlyList<ForecastEnsembleAuditEntry> Audit) BuildEnsemble(
        ForecastCollectionResult forecasts,
        IReadOnlyList<ModelArenaResult> arenas,
        ICollection<string> warnings)
    {
        if (arenas.Count == 0)
        {
            warnings.Add("Forecast ensemble unavailable: Model Arena validation was not attached to this report.");
            return (null, Array.Empty<ForecastEnsembleAuditEntry>());
        }

        var selectedArena = SelectPreferredArenaForForecasts(forecasts, arenas);
        if (selectedArena is null)
        {
            warnings.Add("Forecast ensemble unavailable: no current forecast horizon matched attached Model Arena evidence.");
            return (null, Array.Empty<ForecastEnsembleAuditEntry>());
        }

        var horizonForecasts = SelectForecastsForHorizon(forecasts, selectedArena.Horizon);
        var members = new List<ForecastEnsembleMember>();
        foreach (var model in selectedArena.Models)
        {
            if (!horizonForecasts.TryGetValue(model.Model.Id, out var distribution))
            {
                continue;
            }

            var mae = model.PointCommonSupportMetrics.Point.MeanAbsoluteError;
            var calibrationPenalty = model.ProbabilityCommonSupportMetrics.Probability.ExpectedCalibrationError ?? 0d;
            var skill = model.RelativeSkill?.MeanAbsoluteErrorSkill ?? 0d;
            var eligible = model.IsRankingEligible && mae.HasValue && skill > 0d;
            members.Add(new ForecastEnsembleMember(
                model.Model.Id,
                distribution,
                mae ?? double.PositiveInfinity,
                calibrationPenalty,
                eligible,
                selectedArena.Horizon,
                model.PointCommonSupportSamples.Count));
        }

        var result = this.ensemble.Combine(members);
        if (result.Distribution is null)
        {
            warnings.Add($"Forecast ensemble unavailable: {result.Diagnostic}");
        }

        return (result, BuildEnsembleAudit(selectedArena, horizonForecasts, result));
    }

    private static ModelArenaResult? SelectPreferredArenaForForecasts(
        ForecastCollectionResult forecasts,
        IReadOnlyList<ModelArenaResult> arenas)
    {
        var forecastHorizons = forecasts.Runs
            .Where(run => run.Distribution is not null)
            .Select(run => run.RequestedHorizon)
            .ToHashSet();
        return arenas
            .Where(arena => forecastHorizons.Contains(arena.Horizon))
            .OrderByDescending(arena => arena.Horizon.Unit == ForecastHorizonUnit.CalendarDays && arena.Horizon.Value >= 360)
            .ThenByDescending(arena => arena.Horizon.Unit == ForecastHorizonUnit.CalendarDays)
            .ThenByDescending(arena => arena.Horizon.Value)
            .FirstOrDefault();
    }

    private static IReadOnlyDictionary<string, ForecastDistribution> SelectForecastsForHorizon(
        ForecastCollectionResult forecasts,
        ForecastHorizon horizon)
    {
        return forecasts.Runs
            .Where(run => run.Distribution is not null && run.RequestedHorizon.Equals(horizon))
            .ToDictionary(
                run => run.Model.Id,
                run => run.Distribution!,
                StringComparer.Ordinal);
    }

    private FundScore BuildFundScore(
        FundAnalysisResult analysis,
        IReadOnlyList<ModelArenaResult> arenas,
        ICollection<string> inheritedWarnings)
    {
        var positivePeriodRatio = analysis.SimpleReturns.Count == 0
            ? 0.5d
            : analysis.SimpleReturns.Count(point => point.Value > 0d) / (double)analysis.SimpleReturns.Count;
        var performanceUnit = Average(
            HigherIsBetter(analysis.Performance.Cagr, -0.05d, 0.12d),
            positivePeriodRatio,
            HigherIsBetter(analysis.Performance.CumulativeReturn, -0.20d, 1.00d));
        var riskUnit = Average(
            LowerIsBetter(analysis.Performance.AnnualizedVolatility, this.options.LowAnnualVolatility, this.options.HighAnnualVolatility),
            LowerIsBetter(Math.Abs(analysis.Performance.MaximumDrawdown.MaximumDrawdown), this.options.MildMaximumDrawdown, this.options.SevereMaximumDrawdown));
        var riskAdjustedUnit = Average(
            HigherIsBetter(analysis.Performance.SharpeRatio, -0.5d, 1.5d),
            HigherIsBetter(analysis.Performance.SortinoRatio, -0.5d, 2.0d));
        var rollingVolatilityStability = RollingStability(analysis.RollingVolatility);
        var stabilityUnit = Average(analysis.CurrentState.DataAdequacy, rollingVolatilityStability, 1d - Math.Min(1d, Math.Abs(analysis.Performance.Lag1Autocorrelation)));
        var predictiveUnit = PredictiveEvidenceUnit(arenas);
        var dataQualityUnit = Math.Clamp(analysis.DataQuality.QualityScore / 100d, 0d, 1d);
        var weightSum = this.options.PerformanceQualityWeight +
            this.options.RiskQualityWeight +
            this.options.RiskAdjustedPerformanceWeight +
            this.options.StabilityWeight +
            this.options.PredictiveEvidenceWeight +
            this.options.DataQualityWeight;
        var components = new[]
        {
            new ScoreComponent("Performance quality", ScoreFromUnit(performanceUnit), this.options.PerformanceQualityWeight / weightSum, "CAGR, cumulative return and positive-period ratio."),
            new ScoreComponent("Risk quality", ScoreFromUnit(riskUnit), this.options.RiskQualityWeight / weightSum, "Volatility and maximum drawdown severity."),
            new ScoreComponent("Risk-adjusted performance", ScoreFromUnit(riskAdjustedUnit), this.options.RiskAdjustedPerformanceWeight / weightSum, "Sharpe and Sortino ratios."),
            new ScoreComponent("Stability", ScoreFromUnit(stabilityUnit), this.options.StabilityWeight / weightSum, "State adequacy, rolling volatility stability and lag-1 autocorrelation."),
            new ScoreComponent("Predictive evidence", ScoreFromUnit(predictiveUnit), this.options.PredictiveEvidenceWeight / weightSum, "Validated Model Arena improvement when available."),
            new ScoreComponent("Data quality", ScoreFromUnit(dataQualityUnit), this.options.DataQualityWeight / weightSum, "Provider coverage, suspicious jumps, stale observations and history length."),
        };
        var score = components.Sum(component => component.Score * component.Weight);
        var confidenceUnit = Average(
            dataQualityUnit,
            Math.Clamp(analysis.Dataset.ObservationCount / (double)this.options.HighConfidenceObservationCount, 0d, 1d),
            predictiveUnit);
        var reasons = BuildScoreReasons(analysis, components);
        var warnings = inheritedWarnings.ToList();
        if (!analysis.DataQuality.HasSufficientHistory)
        {
            warnings.Add("Data quality warning: history may be too short for high-confidence conclusions.");
        }

        return new FundScore(ClampScore(score), ConfidenceFromUnit(confidenceUnit), components, reasons, warnings);
    }

    private CurrentOpportunityAssessment BuildAttractiveness(
        FundAnalysisResult analysis,
        ForecastEnsembleResult? ensembleResult,
        GaussianHmmResult? regime,
        ICollection<string> inheritedWarnings)
    {
        if (ensembleResult?.Distribution is null)
        {
            return new CurrentOpportunityAssessment(
                5d,
                CurrentAttractivenessCategory.Neutral,
                ConfidenceLevel.Low,
                ["Current opportunity cannot be scored directionally because no validation-weighted ensemble distribution is available."],
                inheritedWarnings.ToArray());
        }

        var distribution = ensembleResult.Distribution;
        var isValidated = ensembleResult.Reliability >= this.options.MinimumSignalReliability;
        var favorableUnit = Average(
            HigherIsBetter(distribution.ExpectedReturn, -0.05d, 0.10d),
            distribution.ProbabilityPositive,
            LowerIsBetter(distribution.ProbabilityLossGreaterThanTenPercent, 0.05d, 0.45d),
            LowerIsBetter(Math.Abs(analysis.Performance.CurrentDrawdown), 0d, 0.25d));
        var score = ScoreFromUnit(favorableUnit);
        var evidenceLabel = isValidated ? "Validation-gated ensemble" : "Unconfirmed ensemble";
        var evidence = new List<string>
        {
            $"{evidenceLabel} expected return: {distribution.ExpectedReturn:0.####}.",
            $"{evidenceLabel} P(positive): {distribution.ProbabilityPositive:0.####}.",
            $"{evidenceLabel} disagreement: {ensembleResult.ModelDisagreement:0.####}.",
        };
        var warnings = inheritedWarnings.ToList();
        if (!isValidated)
        {
            evidence.Add(
                $"Ensemble ReliabilityIndex {ensembleResult.Reliability.ToString("P2", CultureInfo.InvariantCulture)} is below the confirmation threshold {this.options.MinimumSignalReliability.ToString("P2", CultureInfo.InvariantCulture)}.");
            warnings.Add("Current opportunity is a directional estimate, not a fully validated decision signal.");
        }

        if (regime is not null && regime.LatestProbabilities.Count > 0)
        {
            var bestIndex = BestRegimeIndex(regime);
            var regimeLabel = regime.States[bestIndex].Label;
            if (!IsAdverseRegime(regimeLabel) || regime.LatestProbabilities[bestIndex] < MaterialRegimeProbabilityThreshold)
            {
                evidence.Add($"Most probable regime: {regimeLabel}.");
            }
        }

        return new CurrentOpportunityAssessment(
            score,
            CategoryFromScore(score),
            isValidated ? ConfidenceFromUnit(ensembleResult.Reliability) : ConfidenceLevel.Low,
            evidence,
            warnings.ToArray());
    }

    private DecisionSignal BuildDecisionSignal(
        FundAnalysisResult analysis,
        CurrentOpportunityAssessment attractiveness,
        ForecastEnsembleResult? ensembleResult,
        GaussianHmmResult? regime,
        MarketTimingAssessment? timing,
        DataFreshnessAssessment freshness)
    {
        var counterEvidence = BuildCounterEvidence(analysis, ensembleResult, regime, timing, freshness);
        var candidate = this.BuildStrategicDirectionalAssessment(analysis, attractiveness, ensembleResult);
        var hasConfirmedEnsemble = ensembleResult?.Distribution is not null &&
            ensembleResult.Reliability >= this.options.MinimumSignalReliability;
        var qualification = hasConfirmedEnsemble
            ? SignalQualification.Confirmed
            : candidate.Direction == DirectionalSignal.None
                ? SignalQualification.Unavailable
                : SignalQualification.Tentative;
        var reasons = candidate.Reasons.ToList();
        var evidence = candidate.Evidence.Count == 0
            ? ["No validated ensemble or consistent model forecast can support a strategic signal."]
            : candidate.Evidence;
        var warnings = attractiveness.Warnings.ToList();
        if (!warnings.Contains("This is research output, not financial advice.", StringComparer.Ordinal))
        {
            warnings.Add("This is research output, not financial advice.");
        }

        if (freshness.Status == DataFreshnessStatus.Stale && candidate.Direction != DirectionalSignal.None)
        {
            qualification = SignalQualification.Tentative;
            reasons.Add(
                $"Signal direction is historical as of latest effective observation {freshness.LastEffectiveObservationDate:yyyy-MM-dd}; stale data block current actionability.");
        }

        var primaryTimingArena = timing is null ? null : FindPrimaryTimingArena(timing);
        if (primaryTimingArena?.OutOfDistribution.Level == OutOfDistributionLevel.OutOfDistribution && !hasConfirmedEnsemble)
        {
            reasons.Add("The tactical state is out-of-distribution, so the unconfirmed strategic estimate is not promoted to a current call.");
            return new DecisionSignal(
                DecisionSignalAction.Neutral,
                0d,
                ConfidenceLevel.Low,
                candidate.PrimaryHorizon,
                evidence,
                counterEvidence.Count == 0 ? [DecisionSignalLabels.NoCallExplanation] : counterEvidence,
                warnings)
            {
                Direction = DirectionalSignal.None,
                Qualification = SignalQualification.Unavailable,
                DirectionalStrength = 0d,
                ValidationStrength = Math.Clamp(ensembleResult?.Reliability ?? 0d, 0d, 1d),
                Reasons = reasons,
            };
        }

        if (qualification == SignalQualification.Unavailable)
        {
            if (reasons.Count == 0)
            {
                reasons.Add(DecisionSignalLabels.NoCallExplanation);
            }

            var insufficientEvidenceCounterEvidence = counterEvidence.Count == 0
                ? ["At least one current model forecast may be extreme, but it is not accepted without validation."]
                : counterEvidence;
            return new DecisionSignal(
                DecisionSignalAction.Neutral,
                0d,
                ConfidenceLevel.Low,
                candidate.PrimaryHorizon,
                evidence,
                insufficientEvidenceCounterEvidence,
                warnings)
            {
                Direction = DirectionalSignal.None,
                Qualification = SignalQualification.Unavailable,
                DirectionalStrength = 0d,
                ValidationStrength = Math.Clamp(ensembleResult?.Reliability ?? 0d, 0d, 1d),
                Reasons = reasons,
            };
        }

        if (qualification == SignalQualification.Tentative &&
            !reasons.Contains(DecisionSignalLabels.TentativeMarkerExplanation, StringComparer.Ordinal))
        {
            reasons.Add(DecisionSignalLabels.TentativeMarkerExplanation);
        }

        var neutralCounterEvidence = counterEvidence.Count == 0 && candidate.Direction == DirectionalSignal.Hold
            ? ["Expected outcome and risk evidence are balanced."]
            : counterEvidence;
        return new DecisionSignal(
            candidate.Action,
            candidate.Strength,
            qualification == SignalQualification.Confirmed ? attractiveness.Confidence : ConfidenceLevel.Low,
            candidate.PrimaryHorizon,
            evidence,
            neutralCounterEvidence,
            warnings)
        {
            Direction = candidate.Direction,
            Qualification = qualification,
            DirectionalStrength = candidate.Strength,
            ValidationStrength = Math.Clamp(ensembleResult?.Reliability ?? 0d, 0d, 1d),
            Reasons = reasons,
        };
    }

    private StrategicDirectionalAssessment BuildStrategicDirectionalAssessment(
        FundAnalysisResult analysis,
        CurrentOpportunityAssessment attractiveness,
        ForecastEnsembleResult? ensembleResult)
    {
        if (ensembleResult?.Distribution is not null)
        {
            var action = ActionFromScore(attractiveness.Score);
            var direction = DecisionSignalLabels.ToDirection(action);
            var reasons = new List<string>();
            if (ensembleResult.Reliability >= this.options.MinimumSignalReliability)
            {
                reasons.Add(
                    $"Strategic ensemble passed the ReliabilityIndex threshold ({ensembleResult.Reliability.ToString("P2", CultureInfo.InvariantCulture)} >= {this.options.MinimumSignalReliability.ToString("P2", CultureInfo.InvariantCulture)}).");
            }
            else
            {
                reasons.Add(
                    $"Strategic ensemble has a directional estimate but ReliabilityIndex is below threshold ({ensembleResult.Reliability.ToString("P2", CultureInfo.InvariantCulture)} < {this.options.MinimumSignalReliability.ToString("P2", CultureInfo.InvariantCulture)}).");
            }

            if (direction == DirectionalSignal.Hold)
            {
                reasons.Add("Expected outcome and risk evidence are balanced enough to support HOLD rather than NO CALL.");
            }

            return new StrategicDirectionalAssessment(
                action,
                direction,
                StrengthFromScore(attractiveness.Score, direction),
                ensembleResult.Distribution.RequestedHorizon,
                attractiveness.Evidence,
                reasons);
        }

        return BuildForecastRunDirectionalAssessment(analysis.Forecasts);
    }

    private static StrategicDirectionalAssessment BuildForecastRunDirectionalAssessment(ForecastCollectionResult forecasts)
    {
        var eligible = forecasts.Runs
            .Where(run => run.Status == ForecastStatus.Success &&
                run.Distribution is not null &&
                (run.Distribution.ExpectedReturnOrNull.HasValue || run.Distribution.ProbabilityPositiveOrNull.HasValue))
            .ToArray();
        if (eligible.Length == 0)
        {
            return new StrategicDirectionalAssessment(
                DecisionSignalAction.Neutral,
                DirectionalSignal.None,
                0d,
                null,
                Array.Empty<string>(),
                ["No current forecast exposes expected-return or probability-positive evidence."]);
        }

        var selectedGroup = eligible
            .GroupBy(run => run.RequestedHorizon)
            .OrderByDescending(group => group.Key.Unit == ForecastHorizonUnit.CalendarDays && group.Key.Value >= 360)
            .ThenByDescending(group => group.Key.Unit == ForecastHorizonUnit.CalendarDays)
            .ThenByDescending(group => group.Key.Value)
            .ThenByDescending(group => group.Count())
            .First();
        var selected = selectedGroup.ToArray();
        var expectedValues = selected
            .Select(run => run.Distribution!.ExpectedReturnOrNull)
            .Where(value => value.HasValue && double.IsFinite(value.Value))
            .Select(value => value!.Value)
            .ToArray();
        var probabilityValues = selected
            .Select(run => run.Distribution!.ProbabilityPositiveOrNull)
            .Where(value => value.HasValue && double.IsFinite(value.Value))
            .Select(value => value!.Value)
            .ToArray();
        var meanExpectedReturn = expectedValues.Length == 0 ? (double?)null : expectedValues.Average();
        var meanProbabilityPositive = probabilityValues.Length == 0 ? (double?)null : probabilityValues.Average();
        var expectedDirection = DirectionFromExpectedReturn(meanExpectedReturn);
        var probabilityDirection = DirectionFromProbabilityPositive(meanProbabilityPositive);
        var direction = ResolveForecastDirection(expectedDirection, probabilityDirection);
        var evidence = new List<string>
        {
            $"Unvalidated current forecast blend uses {selected.Length.ToString(CultureInfo.InvariantCulture)} model(s) over {selectedGroup.Key}.",
        };
        if (meanExpectedReturn.HasValue)
        {
            evidence.Add($"Unvalidated mean expected return: {meanExpectedReturn.Value.ToString("P2", CultureInfo.InvariantCulture)}.");
        }

        if (meanProbabilityPositive.HasValue)
        {
            evidence.Add($"Unvalidated mean P(positive): {meanProbabilityPositive.Value.ToString("P2", CultureInfo.InvariantCulture)}.");
        }

        var reasons = new List<string>
        {
            "No validation-gated ensemble is available; individual current forecasts can only produce a qualified label.",
        };
        if (direction == DirectionalSignal.None)
        {
            reasons.Add("Individual current forecast summaries do not agree on a defensible direction.");
            return new StrategicDirectionalAssessment(
                DecisionSignalAction.Neutral,
                DirectionalSignal.None,
                0d,
                selectedGroup.Key,
                evidence,
                reasons);
        }

        if (direction == DirectionalSignal.Hold)
        {
            reasons.Add("Current forecasts are balanced around neutral; HOLD is tentative because no validation-gated ensemble is available.");
        }
        else
        {
            reasons.Add($"Current forecasts lean {direction}, but this is not accepted as a fully validated signal.");
        }

        var strength = StrengthFromForecastBlend(meanExpectedReturn, meanProbabilityPositive, direction);
        return new StrategicDirectionalAssessment(
            ActionFromDirection(direction, strength),
            direction,
            strength,
            selectedGroup.Key,
            evidence,
            reasons);
    }

    private int BestRegimeIndex(GaussianHmmResult regime)
    {
        var bestIndex = 0;
        for (var index = 1; index < regime.LatestProbabilities.Count; index++)
        {
            if (regime.LatestProbabilities[index] > regime.LatestProbabilities[bestIndex])
            {
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private DataFreshnessAssessment ResolveFreshness(DatasetSummary dataset)
    {
        if (dataset.Freshness is not null)
        {
            return dataset.Freshness;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var lastEffectiveObservationDate = dataset.LastEffectiveObservationDate ?? dataset.EndDate;
        return new DataFreshnessAssessment(
            generatedAt,
            lastEffectiveObservationDate,
            0,
            DataFreshnessStatus.Fresh,
            45,
            75,
            "Freshness metadata was not supplied by the caller; legacy report treated the effective dataset as fresh.");
    }

    private void AddDatasetWarnings(
        DatasetSummary dataset,
        DataFreshnessAssessment freshness,
        ICollection<string> warnings)
    {
        if (dataset.SyntheticObservationCount > 0)
        {
            warnings.Add(
                $"Effective observation policy excluded {dataset.SyntheticObservationCount.ToString(CultureInfo.InvariantCulture)} calendar carry-forward row(s) from {dataset.SourceObservationCount.ToString(CultureInfo.InvariantCulture)} source observation(s).");
        }

        if (freshness.Status != DataFreshnessStatus.Fresh)
        {
            warnings.Add($"Data freshness warning: {freshness.Diagnostic}");
        }
    }

    private IReadOnlyList<ForecastEnsembleAuditEntry> BuildEnsembleAudit(
        ModelArenaResult arena,
        IReadOnlyDictionary<string, ForecastDistribution> horizonForecasts,
        ForecastEnsembleResult result)
    {
        var includedWeights = result.Components.ToDictionary(
            component => component.ModelId,
            component => component.Weight,
            StringComparer.Ordinal);
        var ranks = arena.Ranking.ToDictionary(
            entry => entry.Model.Id,
            entry => entry.Rank,
            StringComparer.Ordinal);

        return arena.Models
            .Select(model =>
            {
                horizonForecasts.TryGetValue(model.Model.Id, out var distribution);
                includedWeights.TryGetValue(model.Model.Id, out var weight);
                ranks.TryGetValue(model.Model.Id, out var rank);
                var included = weight > 0d;
                return new ForecastEnsembleAuditEntry(
                    model.Model,
                    BuildValidationStatus(model),
                    model.PointCommonSupportMetrics.Point.MeanAbsoluteError,
                    rank == 0 ? null : rank,
                    weight,
                    included,
                    included ? "Included" : BuildEnsembleExclusionReason(model, distribution));
            })
            .ToArray();
    }

    private string BuildValidationStatus(ModelArenaModelResult model)
    {
        if (!model.IsRankingEligible)
        {
            return "NotRankEligible";
        }

        var skill = model.RelativeSkill?.MeanAbsoluteErrorSkill;
        if (!skill.HasValue)
        {
            return "MissingSkill";
        }

        return skill.Value > 0d ? "PositiveSkill" : "NoPositiveSkill";
    }

    private string BuildEnsembleExclusionReason(
        ModelArenaModelResult model,
        ForecastDistribution? distribution)
    {
        if (distribution is null)
        {
            return "No current forecast matched this model.";
        }

        if (!model.IsRankingEligible)
        {
            return "Not ranking eligible on common walk-forward support.";
        }

        var skill = model.RelativeSkill?.MeanAbsoluteErrorSkill;
        if (!skill.HasValue)
        {
            return "Missing baseline-relative MAE skill.";
        }

        if (skill.Value <= 0d)
        {
            return "No positive MAE skill versus baseline.";
        }

        if (!ForecastEnsemble.HasRequiredEnsembleCapabilities(distribution))
        {
            return $"Distribution lacks required ensemble quantities; capabilities={FormatCapabilities(distribution.Capabilities)}.";
        }

        var mae = model.PointCommonSupportMetrics.Point.MeanAbsoluteError;
        if (!mae.HasValue || !double.IsFinite(mae.Value) || mae.Value < 0d)
        {
            return "Invalid validated point-forecast loss.";
        }

        var calibration = model.ProbabilityCommonSupportMetrics.Probability.ExpectedCalibrationError ?? 0d;
        if (!double.IsFinite(calibration) || calibration < 0d)
        {
            return "Invalid probability calibration penalty.";
        }

        return "Zero numerical weight after normalization.";
    }

    private IReadOnlyList<string> BuildCounterEvidence(
        FundAnalysisResult analysis,
        ForecastEnsembleResult? ensembleResult,
        GaussianHmmResult? regime,
        MarketTimingAssessment? timing,
        DataFreshnessAssessment freshness)
    {
        var counterEvidence = new List<string>();

        if (freshness.Status == DataFreshnessStatus.Stale)
        {
            counterEvidence.Add(freshness.Diagnostic);
        }
        else if (freshness.Status == DataFreshnessStatus.Aging)
        {
            counterEvidence.Add($"Data is aging: {freshness.Diagnostic}");
        }

        if (regime is not null && regime.LatestProbabilities.Count > 0)
        {
            var bestIndex = BestRegimeIndex(regime);
            var regimeLabel = regime.States[bestIndex].Label;
            var regimeProbability = regime.LatestProbabilities[bestIndex];
            if (IsAdverseRegime(regimeLabel) && regimeProbability >= MaterialRegimeProbabilityThreshold)
            {
                counterEvidence.Add(
                    $"Adverse regime evidence: {regimeLabel} at {regimeProbability.ToString("P2", CultureInfo.InvariantCulture)} probability.");
            }
        }

        if (ensembleResult is not null && ensembleResult.ModelDisagreement >= MaterialDisagreementThreshold)
        {
            counterEvidence.Add(
                $"Validated forecast disagreement is elevated at {ensembleResult.ModelDisagreement.ToString("P2", CultureInfo.InvariantCulture)}.");
        }

        foreach (var run in analysis.Forecasts.Runs
            .Where(run => run.Distribution is not null &&
                run.Distribution.Supports(ForecastCapabilities.ExpectedReturn) &&
                run.RequestedHorizon.Unit == ForecastHorizonUnit.CalendarDays &&
                run.RequestedHorizon.Value >= 90 &&
                run.Distribution.ExpectedReturn <= MaterialAdverseForecastThreshold)
            .OrderBy(run => run.Distribution!.ExpectedReturn)
            .Take(3))
        {
            counterEvidence.Add(
                $"Adverse model forecast: {run.Model.Name} {run.RequestedHorizon} expected return {run.Distribution!.ExpectedReturn.ToString("P2", CultureInfo.InvariantCulture)}.");
        }

        if (timing is null)
        {
            counterEvidence.Add("Tactical market timing was not evaluated for this report.");
        }
        else
        {
            if (IsInsufficientTiming(timing))
            {
                counterEvidence.Add("Tactical market timing is InsufficientEvidence and cannot confirm the strategic signal.");
            }

            var primaryArena = FindPrimaryTimingArena(timing);
            if (primaryArena?.OutOfDistribution.Level is OutOfDistributionLevel.SlightlyUnusual or OutOfDistributionLevel.OutOfDistribution)
            {
                counterEvidence.Add(
                    $"Timing OOD diagnostic: {primaryArena.OutOfDistribution.Level}; robust distance {primaryArena.OutOfDistribution.RobustDistance.ToString("0.###", CultureInfo.InvariantCulture)} vs threshold {primaryArena.OutOfDistribution.Threshold.ToString("0.###", CultureInfo.InvariantCulture)}.");
            }

            foreach (var item in timing.CounterEvidence)
            {
                if (!counterEvidence.Contains(item, StringComparer.Ordinal))
                {
                    counterEvidence.Add(item);
                }
            }
        }

        return counterEvidence;
    }

    private ActionabilityAssessment BuildActionability(
        DecisionSignal signal,
        MarketTimingAssessment? timing,
        DataFreshnessAssessment freshness)
    {
        var reasons = new List<string>();
        var confidence = signal.Confidence;

        if (freshness.Status == DataFreshnessStatus.Stale)
        {
            reasons.Add(freshness.Diagnostic);
            return new ActionabilityAssessment(
                "CurrentDecisionUnavailable",
                ConfidenceLevel.Low,
                freshness.LastEffectiveObservationDate,
                reasons);
        }

        if (signal.Qualification == SignalQualification.Unavailable)
        {
            reasons.Add(DecisionSignalLabels.NoCallExplanation);
            return new ActionabilityAssessment(
                "NoDefensibleCurrentSignal",
                ConfidenceLevel.Low,
                freshness.LastEffectiveObservationDate,
                reasons);
        }

        if (freshness.Status == DataFreshnessStatus.Aging)
        {
            reasons.Add(freshness.Diagnostic);
            confidence = MinConfidence(confidence, ConfidenceLevel.Medium);
        }

        if (timing is null)
        {
            reasons.Add("Market timing was not evaluated; only the strategic research signal is available.");
            return new ActionabilityAssessment(
                "StrategicOnlyTimingNotEvaluated",
                MinConfidence(confidence, ConfidenceLevel.Low),
                freshness.LastEffectiveObservationDate,
                reasons);
        }

        if (IsInsufficientTiming(timing))
        {
            reasons.Add("Market timing returned InsufficientEvidence; the tactical layer does not support a current action.");
            return new ActionabilityAssessment(
                "StrategicOnlyTimingUnavailable",
                MinConfidence(confidence, ConfidenceLevel.Low),
                freshness.LastEffectiveObservationDate,
                reasons);
        }

        var primaryArena = FindPrimaryTimingArena(timing);
        if (primaryArena?.OutOfDistribution.Level == OutOfDistributionLevel.OutOfDistribution)
        {
            reasons.Add(
                $"Current timing state is out of historical feature support: robust distance {primaryArena.OutOfDistribution.RobustDistance.ToString("0.###", CultureInfo.InvariantCulture)} vs threshold {primaryArena.OutOfDistribution.Threshold.ToString("0.###", CultureInfo.InvariantCulture)}.");
            confidence = MinConfidence(confidence, ConfidenceLevel.Low);
        }
        else if (primaryArena?.OutOfDistribution.Level == OutOfDistributionLevel.SlightlyUnusual)
        {
            reasons.Add(
                $"Current timing state is unusual: robust distance {primaryArena.OutOfDistribution.RobustDistance.ToString("0.###", CultureInfo.InvariantCulture)} vs threshold {primaryArena.OutOfDistribution.Threshold.ToString("0.###", CultureInfo.InvariantCulture)}.");
            confidence = MinConfidence(confidence, ConfidenceLevel.Medium);
        }

        confidence = MinConfidence(confidence, timing.Decision.Confidence);
        if (reasons.Count == 0)
        {
            reasons.Add("Strategic signal, data freshness and tactical timing are internally usable.");
        }

        if (signal.Qualification == SignalQualification.Tentative)
        {
            reasons.Add("Strategic label is tentative; it can inform research but is not a fully validated current action.");
            return new ActionabilityAssessment(
                "QualifiedTentativeSignal",
                MinConfidence(confidence, ConfidenceLevel.Low),
                freshness.LastEffectiveObservationDate,
                reasons);
        }

        return new ActionabilityAssessment(
            "QualifiedActionable",
            confidence,
            freshness.LastEffectiveObservationDate,
            reasons);
    }

    private MarketTimingArenaResult? FindPrimaryTimingArena(MarketTimingAssessment timing)
    {
        var primary = timing.Decision.PrimaryHorizon;
        if (primary is not null)
        {
            var match = timing.ModelArenaResults.FirstOrDefault(result => result.Definition.Horizon.Equals(primary));
            if (match is not null)
            {
                return match;
            }
        }

        return timing.ModelArenaResults.FirstOrDefault();
    }

    private bool IsInsufficientTiming(MarketTimingAssessment timing) =>
        timing.Decision.Action == TimingDecisionAction.InsufficientEvidence ||
        timing.CurrentTimingZone == MarketTimingZone.InsufficientEvidence;

    private bool IsAdverseRegime(string label)
    {
        return label.Contains("Bear", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Low Return", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("High Volatility", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Stress", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Drawdown", StringComparison.OrdinalIgnoreCase);
    }

    private ConfidenceLevel MinConfidence(ConfidenceLevel first, ConfidenceLevel second) =>
        (ConfidenceLevel)Math.Min((int)first, (int)second);

    private string FormatCapabilities(ForecastCapabilities capabilities)
    {
        return capabilities == ForecastCapabilities.None ? "None" : capabilities.ToString();
    }

    private CurrentAttractivenessCategory CategoryFromScore(double score)
    {
        return score switch
        {
            >= 8.0d => CurrentAttractivenessCategory.VeryFavorable,
            >= 6.2d => CurrentAttractivenessCategory.Favorable,
            <= 2.2d => CurrentAttractivenessCategory.VeryUnfavorable,
            <= 3.8d => CurrentAttractivenessCategory.Unfavorable,
            _ => CurrentAttractivenessCategory.Neutral,
        };
    }

    private static DecisionSignalAction ActionFromScore(double score)
    {
        return score switch
        {
            >= 8.0d => DecisionSignalAction.Accumulate,
            >= 6.2d => DecisionSignalAction.MildAccumulate,
            <= 2.2d => DecisionSignalAction.StrongReduce,
            <= 3.8d => DecisionSignalAction.Reduce,
            _ => DecisionSignalAction.Neutral,
        };
    }

    private static DecisionSignalAction ActionFromDirection(DirectionalSignal direction, double strength)
    {
        return direction switch
        {
            DirectionalSignal.Buy => strength >= 0.7d ? DecisionSignalAction.Accumulate : DecisionSignalAction.MildAccumulate,
            DirectionalSignal.Sell => strength >= 0.7d ? DecisionSignalAction.StrongReduce : DecisionSignalAction.Reduce,
            _ => DecisionSignalAction.Neutral,
        };
    }

    private static DirectionalSignal DirectionFromExpectedReturn(double? expectedReturn)
    {
        if (!expectedReturn.HasValue)
        {
            return DirectionalSignal.None;
        }

        return expectedReturn.Value switch
        {
            >= MaterialConstructiveForecastThreshold => DirectionalSignal.Buy,
            <= MaterialAdverseForecastThreshold => DirectionalSignal.Sell,
            _ => DirectionalSignal.Hold,
        };
    }

    private static DirectionalSignal DirectionFromProbabilityPositive(double? probabilityPositive)
    {
        if (!probabilityPositive.HasValue)
        {
            return DirectionalSignal.None;
        }

        var edge = probabilityPositive.Value - 0.5d;
        return edge switch
        {
            >= MaterialProbabilityEdgeThreshold => DirectionalSignal.Buy,
            <= -MaterialProbabilityEdgeThreshold => DirectionalSignal.Sell,
            _ => DirectionalSignal.Hold,
        };
    }

    private static DirectionalSignal ResolveForecastDirection(
        DirectionalSignal expectedDirection,
        DirectionalSignal probabilityDirection)
    {
        var signedDirections = new[] { expectedDirection, probabilityDirection }
            .Where(direction => direction is DirectionalSignal.Buy or DirectionalSignal.Sell)
            .Distinct()
            .ToArray();
        if (signedDirections.Length > 1)
        {
            return DirectionalSignal.None;
        }

        if (signedDirections.Length == 1)
        {
            return signedDirections[0];
        }

        return expectedDirection == DirectionalSignal.Hold || probabilityDirection == DirectionalSignal.Hold
            ? DirectionalSignal.Hold
            : DirectionalSignal.None;
    }

    private static double StrengthFromScore(double score, DirectionalSignal direction)
    {
        if (direction == DirectionalSignal.Hold)
        {
            return Math.Clamp(1d - (Math.Abs(score - 5d) / 1.2d), 0d, 1d);
        }

        return Math.Clamp(Math.Abs(score - 5d) / 5d, 0d, 1d);
    }

    private static double StrengthFromForecastBlend(
        double? meanExpectedReturn,
        double? meanProbabilityPositive,
        DirectionalSignal direction)
    {
        var components = new List<double>();
        if (meanExpectedReturn.HasValue)
        {
            components.Add(direction == DirectionalSignal.Hold
                ? 1d - Math.Clamp(Math.Abs(meanExpectedReturn.Value) / MaterialConstructiveForecastThreshold, 0d, 1d)
                : Math.Clamp(Math.Abs(meanExpectedReturn.Value) / (MaterialConstructiveForecastThreshold * 2d), 0d, 1d));
        }

        if (meanProbabilityPositive.HasValue)
        {
            components.Add(direction == DirectionalSignal.Hold
                ? 1d - Math.Clamp(Math.Abs(meanProbabilityPositive.Value - 0.5d) / MaterialProbabilityEdgeThreshold, 0d, 1d)
                : Math.Clamp(Math.Abs(meanProbabilityPositive.Value - 0.5d) / (MaterialProbabilityEdgeThreshold * 2d), 0d, 1d));
        }

        return components.Count == 0 ? 0d : Math.Clamp(components.Average(), 0d, 1d);
    }

    private double PredictiveEvidenceUnit(IReadOnlyList<ModelArenaResult> arenas)
    {
        if (arenas.Count == 0 || arenas.All(arena => arena.Models.Count == 0))
        {
            return 0.25d;
        }

        return arenas
            .Where(arena => arena.Models.Count > 0)
            .Select(arena =>
            {
                var eligible = arena.Models
                    .Where(model => model.IsRankingEligible && (model.RelativeSkill?.MeanAbsoluteErrorSkill ?? 0d) > 0d)
                    .ToArray();
                return Math.Clamp(eligible.Length / (double)Math.Max(1, arena.Models.Count), 0d, 1d);
            })
            .DefaultIfEmpty(0.25d)
            .Max();
    }

    private double RollingStability(IReadOnlyList<DatedValue> rollingVolatility)
    {
        if (rollingVolatility.Count < 3)
        {
            return 0.5d;
        }

        var values = rollingVolatility.Select(point => point.Value).Where(double.IsFinite).ToArray();
        if (values.Length < 3)
        {
            return 0.5d;
        }

        var mean = Math.Abs(values.Average());
        if (mean == 0d)
        {
            return 1d;
        }

        var variance = values.Sum(value =>
        {
            var deviation = value - mean;
            return deviation * deviation;
        }) / (values.Length - 1d);
        var coefficientOfVariation = Math.Sqrt(variance) / mean;
        return Math.Clamp(1d - coefficientOfVariation, 0d, 1d);
    }

    private IReadOnlyList<string> BuildScoreReasons(
        FundAnalysisResult analysis,
        IReadOnlyList<ScoreComponent> components)
    {
        var reasons = new List<string>();
        var strongest = components.OrderByDescending(component => component.Score).First();
        var weakest = components.OrderBy(component => component.Score).First();
        reasons.Add($"Strongest component: {strongest.Name} ({strongest.Score:0.0}/10).");
        reasons.Add($"Weakest component: {weakest.Name} ({weakest.Score:0.0}/10).");
        reasons.Add($"CAGR {analysis.Performance.Cagr:0.####}, volatility {analysis.Performance.AnnualizedVolatility:0.####}, maximum drawdown {analysis.Performance.MaximumDrawdown.MaximumDrawdown:0.####}.");
        return reasons;
    }

    private double Average(params double[] values)
    {
        return values.Length == 0 ? 0d : values.Average();
    }

    private sealed record StrategicDirectionalAssessment(
        DecisionSignalAction Action,
        DirectionalSignal Direction,
        double Strength,
        ForecastHorizon? PrimaryHorizon,
        IReadOnlyList<string> Evidence,
        IReadOnlyList<string> Reasons);
}
