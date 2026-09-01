using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Stores UI-independent Model Arena results for a dataset and horizon.
/// </summary>
public sealed class ModelArenaResult
{
    private readonly IReadOnlyList<ModelArenaModelResult> models;
    private readonly IReadOnlyList<ModelRankingEntry> ranking;
    private readonly IReadOnlyList<string> baselineDiagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelArenaResult"/> class.
    /// </summary>
    /// <param name="dataset">The evaluation dataset.</param>
    /// <param name="horizon">The evaluated forecast horizon.</param>
    /// <param name="evaluationStartDate">The first evaluated cutoff date.</param>
    /// <param name="evaluationEndDate">The last evaluated target date.</param>
    /// <param name="models">The model results.</param>
    /// <param name="ranking">Transparent ranking entries.</param>
    /// <param name="pointCommonSupportEventCount">The point-forecast common-support event count.</param>
    /// <param name="probabilityCommonSupportEventCount">The probability common-support event count.</param>
    /// <param name="quantileCommonSupportEventCount">The quantile common-support event count.</param>
    /// <param name="minimumCommonSupportSamples">The configured minimum common-support count for ranking.</param>
    /// <param name="pointForecastBaseline">The selected point-forecast baseline.</param>
    /// <param name="probabilityBaseline">The selected probability baseline.</param>
    /// <param name="rankingDiagnostic">The ranking diagnostic.</param>
    /// <param name="baselineDiagnostics">Baseline selection diagnostics.</param>
    public ModelArenaResult(
        ForecastEvaluationDataset dataset,
        ForecastHorizon horizon,
        DateOnly? evaluationStartDate,
        DateOnly? evaluationEndDate,
        IReadOnlyList<ModelArenaModelResult> models,
        IReadOnlyList<ModelRankingEntry> ranking,
        int pointCommonSupportEventCount,
        int probabilityCommonSupportEventCount,
        int quantileCommonSupportEventCount,
        int minimumCommonSupportSamples,
        ModelDescriptor? pointForecastBaseline,
        ModelDescriptor? probabilityBaseline,
        string rankingDiagnostic,
        IReadOnlyList<string> baselineDiagnostics)
    {
        this.Dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
        this.Horizon = horizon;
        this.EvaluationStartDate = evaluationStartDate;
        this.EvaluationEndDate = evaluationEndDate;
        this.models = models ?? throw new ArgumentNullException(nameof(models));
        this.ranking = ranking ?? throw new ArgumentNullException(nameof(ranking));
        this.PointCommonSupportEventCount = pointCommonSupportEventCount;
        this.ProbabilityCommonSupportEventCount = probabilityCommonSupportEventCount;
        this.QuantileCommonSupportEventCount = quantileCommonSupportEventCount;
        this.MinimumCommonSupportSamples = minimumCommonSupportSamples;
        this.PointForecastBaseline = pointForecastBaseline;
        this.ProbabilityBaseline = probabilityBaseline;
        this.RankingDiagnostic = rankingDiagnostic;
        this.baselineDiagnostics = baselineDiagnostics ?? throw new ArgumentNullException(nameof(baselineDiagnostics));
    }

    /// <summary>
    /// Gets the evaluation dataset.
    /// </summary>
    public ForecastEvaluationDataset Dataset { get; }

    /// <summary>
    /// Gets the evaluated forecast horizon.
    /// </summary>
    public ForecastHorizon Horizon { get; }

    /// <summary>
    /// Gets the first evaluated cutoff date.
    /// </summary>
    public DateOnly? EvaluationStartDate { get; }

    /// <summary>
    /// Gets the last evaluated target date.
    /// </summary>
    public DateOnly? EvaluationEndDate { get; }

    /// <summary>
    /// Gets the model results.
    /// </summary>
    public IReadOnlyList<ModelArenaModelResult> Models => this.models;

    /// <summary>
    /// Gets transparent ranking entries.
    /// </summary>
    public IReadOnlyList<ModelRankingEntry> Ranking => this.ranking;

    /// <summary>
    /// Gets the point-forecast common-support event count.
    /// </summary>
    public int PointCommonSupportEventCount { get; }

    /// <summary>
    /// Gets the probability common-support event count.
    /// </summary>
    public int ProbabilityCommonSupportEventCount { get; }

    /// <summary>
    /// Gets the quantile common-support event count.
    /// </summary>
    public int QuantileCommonSupportEventCount { get; }

    /// <summary>
    /// Gets the point-forecast common-support event count.
    /// </summary>
    public int CommonSupportEventCount => this.PointCommonSupportEventCount;

    /// <summary>
    /// Gets the configured minimum common-support sample count for ranking.
    /// </summary>
    public int MinimumCommonSupportSamples { get; }

    /// <summary>
    /// Gets the selected point-forecast baseline, when exactly one was available.
    /// </summary>
    public ModelDescriptor? PointForecastBaseline { get; }

    /// <summary>
    /// Gets the selected probability baseline, when exactly one was available.
    /// </summary>
    public ModelDescriptor? ProbabilityBaseline { get; }

    /// <summary>
    /// Gets a human-readable ranking diagnostic.
    /// </summary>
    public string RankingDiagnostic { get; }

    /// <summary>
    /// Gets baseline selection diagnostics.
    /// </summary>
    public IReadOnlyList<string> BaselineDiagnostics => this.baselineDiagnostics;
}
