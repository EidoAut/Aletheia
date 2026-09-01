using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Compares multiple models under the same walk-forward evaluation rules.
/// </summary>
public sealed class ModelArena
{
    private readonly IWalkForwardEvaluator evaluator;
    private readonly ValidationMetricCalculator metricCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelArena"/> class.
    /// </summary>
    /// <param name="evaluator">The walk-forward evaluator.</param>
    /// <param name="metricCalculator">The metric calculator used for common-support metrics.</param>
    public ModelArena(
        IWalkForwardEvaluator? evaluator = null,
        ValidationMetricCalculator? metricCalculator = null)
    {
        this.evaluator = evaluator ?? new WalkForwardEvaluator();
        this.metricCalculator = metricCalculator ?? new ValidationMetricCalculator();
    }

    /// <summary>
    /// Evaluates all supplied models and builds a transparent scorecard.
    /// </summary>
    /// <param name="models">The models to evaluate.</param>
    /// <param name="dataset">The evaluation dataset.</param>
    /// <param name="options">The walk-forward options.</param>
    /// <param name="ledger">The optional prediction ledger.</param>
    /// <param name="cancellationToken">A token used to cancel long-running validation.</param>
    /// <param name="arenaOptions">The cross-model comparison options.</param>
    /// <returns>The Model Arena result.</returns>
    public async Task<ModelArenaResult> EvaluateAsync(
        IReadOnlyList<IForecastModel> models,
        ForecastEvaluationDataset dataset,
        WalkForwardEvaluationOptions options,
        IPredictionLedger? ledger = null,
        CancellationToken cancellationToken = default,
        ModelArenaOptions? arenaOptions = null)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(options);
        arenaOptions ??= new ModelArenaOptions();
        arenaOptions.Validate();

        if (models.Count == 0)
        {
            throw new ArgumentException("At least one model is required.", nameof(models));
        }

        var evaluations = new List<ModelEvaluationWithContract>(models.Count);
        foreach (var model in models)
        {
            var evaluation = await this.evaluator.EvaluateAsync(
                model,
                dataset,
                options,
                ledger,
                cancellationToken).ConfigureAwait(false);
            evaluations.Add(new ModelEvaluationWithContract(model, evaluation));
        }

        var pointCommonSupport = BuildCommonSupport(evaluations, ForecastCapabilities.PointForecast, options.ForecastHorizon);
        var probabilityCommonSupport = BuildCommonSupport(evaluations, ForecastCapabilities.ProbabilityPositive, options.ForecastHorizon);
        var quantileCommonSupport = BuildCommonSupport(evaluations, ForecastCapabilities.Quantiles, options.ForecastHorizon);
        var minimumCommonSupportSamples = arenaOptions.ResolveMinimumCommonSupportSamples(options);
        var preliminary = evaluations
            .Select(item =>
            {
                var pointCommonSamples = GetFamilySamples(
                    item,
                    ForecastCapabilities.PointForecast,
                    pointCommonSupport,
                    options.ForecastHorizon);
                var probabilityCommonSamples = GetFamilySamples(
                    item,
                    ForecastCapabilities.ProbabilityPositive,
                    probabilityCommonSupport,
                    options.ForecastHorizon);
                var quantileCommonSamples = GetFamilySamples(
                    item,
                    ForecastCapabilities.Quantiles,
                    quantileCommonSupport,
                    options.ForecastHorizon);
                var pointCommonMetrics = this.metricCalculator.Calculate(pointCommonSamples, options.Calibration);
                var probabilityCommonMetrics = this.metricCalculator.Calculate(probabilityCommonSamples, options.Calibration);
                var quantileCommonMetrics = this.metricCalculator.Calculate(quantileCommonSamples, options.Calibration);
                return new PreliminaryArenaModel(
                    item.Model,
                    item.Evaluation,
                    pointCommonSamples,
                    pointCommonMetrics,
                    probabilityCommonSamples,
                    probabilityCommonMetrics,
                    quantileCommonSamples,
                    quantileCommonMetrics,
                    IsRankingEligible(item.Model, item.Evaluation, pointCommonMetrics, pointCommonSamples.Length, arenaOptions, minimumCommonSupportSamples));
            })
            .ToArray();
        var pointBaseline = SelectBaseline(preliminary, arenaOptions.PointForecastBaselineModelId);
        var probabilityBaseline = SelectBaseline(preliminary, arenaOptions.ProbabilityBaselineModelId);
        var baselineDiagnostics = BuildBaselineDiagnostics(
            arenaOptions,
            pointBaseline.MatchCount,
            probabilityBaseline.MatchCount);
        var arenaModels = preliminary
            .Select(item => new ModelArenaModelResult(
                item.Evaluation.Model,
                item.Evaluation,
                item.Model.Capabilities,
                item.Model.PointForecastStatistic,
                item.PointCommonSupportSamples,
                item.PointCommonSupportMetrics,
                item.ProbabilityCommonSupportSamples,
                item.ProbabilityCommonSupportMetrics,
                item.QuantileCommonSupportSamples,
                item.QuantileCommonSupportMetrics,
                CalculateRelativeSkill(pointBaseline.Model, probabilityBaseline.Model, item),
                item.IsRankingEligible))
            .ToArray();
        var rankingDiagnostic = BuildRankingDiagnostic(pointCommonSupport.Count, minimumCommonSupportSamples);
        var ranking = pointCommonSupport.Count < minimumCommonSupportSamples
            ? Array.Empty<ModelRankingEntry>()
            : BuildRanking(arenaModels);
        options.EventSink.Record(new ValidationEvent(
            ValidationEventType.ModelArenaCompleted,
            null,
            null,
            "Model Arena completed."));

        var startDates = evaluations
            .Select(item => item.Evaluation.EvaluationStartDate)
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .ToArray();
        var endDates = evaluations
            .Select(item => item.Evaluation.EvaluationEndDate)
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .ToArray();

        return new ModelArenaResult(
            dataset,
            options.ForecastHorizon,
            startDates.Length == 0 ? null : startDates.Min(),
            endDates.Length == 0 ? null : endDates.Max(),
            arenaModels,
            ranking,
            pointCommonSupport.Count,
            probabilityCommonSupport.Count,
            quantileCommonSupport.Count,
            minimumCommonSupportSamples,
            pointBaseline.Model?.Evaluation.Model,
            probabilityBaseline.Model?.Evaluation.Model,
            rankingDiagnostic,
            baselineDiagnostics);
    }

    private static ForecastEvaluationSample[] GetFamilySamples(
        ModelEvaluationWithContract evaluation,
        ForecastCapabilities capability,
        IReadOnlySet<EvaluationEventKey> commonSupport,
        ForecastHorizon horizon)
    {
        if (!evaluation.Model.Capabilities.HasFlag(capability))
        {
            return [];
        }

        return evaluation.Evaluation.Samples
                    .Where(sample => sample.Prediction.Prediction.RequestedHorizon.Equals(horizon) &&
                        commonSupport.Contains(sample.EventKey))
                    .OrderBy(sample => sample.Prediction.PredictionCutoffIndex)
                    .ToArray();
    }

    private static RelativeSkill? CalculateRelativeSkill(
        PreliminaryArenaModel? pointBaseline,
        PreliminaryArenaModel? probabilityBaseline,
        PreliminaryArenaModel model)
    {
        if (pointBaseline is null && probabilityBaseline is null)
        {
            return null;
        }

        return new RelativeSkill(
            pointBaseline?.Evaluation.Model.Id,
            probabilityBaseline?.Evaluation.Model.Id,
            Skill(model.PointCommonSupportMetrics.Point.MeanAbsoluteError, pointBaseline?.PointCommonSupportMetrics.Point.MeanAbsoluteError),
            Skill(model.PointCommonSupportMetrics.Point.RootMeanSquaredError, pointBaseline?.PointCommonSupportMetrics.Point.RootMeanSquaredError),
            Skill(model.ProbabilityCommonSupportMetrics.Probability.BrierScore, probabilityBaseline?.ProbabilityCommonSupportMetrics.Probability.BrierScore));
    }

    private static double? Skill(double? modelMetric, double? baselineMetric)
    {
        if (!modelMetric.HasValue || !baselineMetric.HasValue || baselineMetric.Value == 0d)
        {
            return null;
        }

        return 1d - (modelMetric.Value / baselineMetric.Value);
    }

    private static IReadOnlySet<EvaluationEventKey> BuildCommonSupport(
        IReadOnlyList<ModelEvaluationWithContract> evaluations,
        ForecastCapabilities capability,
        ForecastHorizon horizon)
    {
        var eligibleEvaluations = evaluations
            .Where(item => item.Model.Capabilities.HasFlag(capability))
            .Select(item => item.Evaluation)
            .ToArray();
        if (eligibleEvaluations.Length == 0)
        {
            return new HashSet<EvaluationEventKey>();
        }

        HashSet<EvaluationEventKey>? common = null;
        foreach (var evaluation in eligibleEvaluations)
        {
            var keys = evaluation.Samples
                .Where(sample => sample.Prediction.Prediction.RequestedHorizon.Equals(horizon))
                .Select(sample => sample.EventKey)
                .ToHashSet();
            common = common is null
                ? keys
                : common.Intersect(keys).ToHashSet();
        }

        return common ?? new HashSet<EvaluationEventKey>();
    }

    private static bool IsRankingEligible(
        IForecastModel model,
        WalkForwardModelEvaluationResult evaluation,
        MetricSummary commonSupportMetrics,
        int commonSupportSampleCount,
        ModelArenaOptions arenaOptions,
        int minimumCommonSupportSamples)
    {
        return model.Capabilities.HasFlag(ForecastCapabilities.PointForecast) &&
            evaluation.Samples.Count >= arenaOptions.MinimumAllSamples &&
            commonSupportSampleCount >= minimumCommonSupportSamples &&
            evaluation.NonOverlappingSamples.Count >= arenaOptions.MinimumNonOverlappingSamples &&
            commonSupportMetrics.Point.Status == MetricStatus.Available;
    }

    private static BaselineSelection SelectBaseline(
        IReadOnlyList<PreliminaryArenaModel> models,
        string baselineModelId)
    {
        var matches = models
            .Where(model => string.Equals(model.Evaluation.Model.Id, baselineModelId, StringComparison.Ordinal))
            .ToArray();
        return new BaselineSelection(matches.Length == 1 ? matches[0] : null, matches.Length);
    }

    private static IReadOnlyList<string> BuildBaselineDiagnostics(
        ModelArenaOptions options,
        int pointBaselineMatches,
        int probabilityBaselineMatches)
    {
        var diagnostics = new List<string>();
        AddBaselineDiagnostic(diagnostics, "Point", options.PointForecastBaselineModelId, pointBaselineMatches);
        AddBaselineDiagnostic(diagnostics, "Probability", options.ProbabilityBaselineModelId, probabilityBaselineMatches);
        return diagnostics;
    }

    private static void AddBaselineDiagnostic(
        ICollection<string> diagnostics,
        string family,
        string modelId,
        int matchCount)
    {
        if (matchCount == 1)
        {
            return;
        }

        diagnostics.Add(matchCount == 0
            ? $"{family} baseline unavailable: model id '{modelId}' was not registered."
            : $"{family} baseline unavailable: model id '{modelId}' matched {matchCount} registered models.");
    }

    private static string BuildRankingDiagnostic(int commonSupportCount, int minimumCommonSupportSamples)
    {
        return commonSupportCount < minimumCommonSupportSamples
            ? $"Ranking unavailable: insufficient common-support samples ({commonSupportCount} < {minimumCommonSupportSamples})."
            : "Ranking uses common-support point-forecast metrics.";
    }

    private static IReadOnlyList<ModelRankingEntry> BuildRanking(IReadOnlyList<ModelArenaModelResult> models)
    {
        var eligible = models
            .Where(model => model.IsRankingEligible)
            .OrderBy(model => model.CommonSupportMetrics.Point.MeanAbsoluteError ?? double.PositiveInfinity)
            .ThenBy(model => model.CommonSupportMetrics.Point.RootMeanSquaredError ?? double.PositiveInfinity)
            .ThenByDescending(model => model.CommonSupportMetrics.Point.DirectionalAccuracy ?? double.NegativeInfinity)
            .ThenBy(model => model.Model.Id, StringComparer.Ordinal)
            .ToArray();
        var ranking = new List<ModelRankingEntry>(eligible.Length);
        for (var index = 0; index < eligible.Length; index++)
        {
            var model = eligible[index];
            var metrics = model.PointCommonSupportMetrics;
            var reason = $"PointCommonSupport={model.PointCommonSupportSamples.Count}, MAE={Format(metrics.Point.MeanAbsoluteError)}, RMSE={Format(metrics.Point.RootMeanSquaredError)}, Direction={Format(metrics.Point.DirectionalAccuracy)}";
            ranking.Add(new ModelRankingEntry(index + 1, model.Model, reason));
        }

        return ranking;
    }

    private static string Format(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) : "n/a";
    }

    private sealed record ModelEvaluationWithContract(
        IForecastModel Model,
        WalkForwardModelEvaluationResult Evaluation);

    private sealed record PreliminaryArenaModel(
        IForecastModel Model,
        WalkForwardModelEvaluationResult Evaluation,
        IReadOnlyList<ForecastEvaluationSample> PointCommonSupportSamples,
        MetricSummary PointCommonSupportMetrics,
        IReadOnlyList<ForecastEvaluationSample> ProbabilityCommonSupportSamples,
        MetricSummary ProbabilityCommonSupportMetrics,
        IReadOnlyList<ForecastEvaluationSample> QuantileCommonSupportSamples,
        MetricSummary QuantileCommonSupportMetrics,
        bool IsRankingEligible);

    private sealed record BaselineSelection(
        PreliminaryArenaModel? Model,
        int MatchCount);
}
