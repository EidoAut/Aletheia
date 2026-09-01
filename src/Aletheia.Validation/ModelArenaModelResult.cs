using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Stores Model Arena metrics and ranking metadata for one model.
/// </summary>
public sealed class ModelArenaModelResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelArenaModelResult"/> class.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <param name="evaluation">The walk-forward evaluation.</param>
    /// <param name="capabilities">The model's declared forecast capabilities.</param>
    /// <param name="pointForecastStatistic">The model's declared point forecast statistic.</param>
    /// <param name="pointCommonSupportSamples">Samples on the point-forecast common support.</param>
    /// <param name="pointCommonSupportMetrics">Point-family metrics on common support.</param>
    /// <param name="probabilityCommonSupportSamples">Samples on the probability common support.</param>
    /// <param name="probabilityCommonSupportMetrics">Probability-family metrics on common support.</param>
    /// <param name="quantileCommonSupportSamples">Samples on the quantile common support.</param>
    /// <param name="quantileCommonSupportMetrics">Quantile-family metrics on common support.</param>
    /// <param name="relativeSkill">Baseline-relative skill values where meaningful.</param>
    /// <param name="isRankingEligible">Whether the model has enough samples for ranking.</param>
    public ModelArenaModelResult(
        ModelDescriptor model,
        WalkForwardModelEvaluationResult evaluation,
        ForecastCapabilities capabilities,
        PointForecastStatistic pointForecastStatistic,
        IReadOnlyList<ForecastEvaluationSample> pointCommonSupportSamples,
        MetricSummary pointCommonSupportMetrics,
        IReadOnlyList<ForecastEvaluationSample> probabilityCommonSupportSamples,
        MetricSummary probabilityCommonSupportMetrics,
        IReadOnlyList<ForecastEvaluationSample> quantileCommonSupportSamples,
        MetricSummary quantileCommonSupportMetrics,
        RelativeSkill? relativeSkill,
        bool isRankingEligible)
    {
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        this.Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
        this.Capabilities = capabilities;
        this.PointForecastStatistic = pointForecastStatistic;
        this.PointCommonSupportSamples = pointCommonSupportSamples ?? throw new ArgumentNullException(nameof(pointCommonSupportSamples));
        this.PointCommonSupportMetrics = pointCommonSupportMetrics ?? throw new ArgumentNullException(nameof(pointCommonSupportMetrics));
        this.ProbabilityCommonSupportSamples = probabilityCommonSupportSamples ?? throw new ArgumentNullException(nameof(probabilityCommonSupportSamples));
        this.ProbabilityCommonSupportMetrics = probabilityCommonSupportMetrics ?? throw new ArgumentNullException(nameof(probabilityCommonSupportMetrics));
        this.QuantileCommonSupportSamples = quantileCommonSupportSamples ?? throw new ArgumentNullException(nameof(quantileCommonSupportSamples));
        this.QuantileCommonSupportMetrics = quantileCommonSupportMetrics ?? throw new ArgumentNullException(nameof(quantileCommonSupportMetrics));
        this.RelativeSkill = relativeSkill;
        this.IsRankingEligible = isRankingEligible;
    }

    /// <summary>
    /// Gets the model descriptor.
    /// </summary>
    public ModelDescriptor Model { get; }

    /// <summary>
    /// Gets the walk-forward evaluation.
    /// </summary>
    public WalkForwardModelEvaluationResult Evaluation { get; }

    /// <summary>
    /// Gets the model's declared forecast capabilities.
    /// </summary>
    public ForecastCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the model's declared point forecast statistic.
    /// </summary>
    public PointForecastStatistic PointForecastStatistic { get; }

    /// <summary>
    /// Gets samples on the point-forecast common support.
    /// </summary>
    public IReadOnlyList<ForecastEvaluationSample> PointCommonSupportSamples { get; }

    /// <summary>
    /// Gets point-family metrics calculated on common support.
    /// </summary>
    public MetricSummary PointCommonSupportMetrics { get; }

    /// <summary>
    /// Gets samples on the probability common support.
    /// </summary>
    public IReadOnlyList<ForecastEvaluationSample> ProbabilityCommonSupportSamples { get; }

    /// <summary>
    /// Gets probability-family metrics calculated on common support.
    /// </summary>
    public MetricSummary ProbabilityCommonSupportMetrics { get; }

    /// <summary>
    /// Gets samples on the quantile common support.
    /// </summary>
    public IReadOnlyList<ForecastEvaluationSample> QuantileCommonSupportSamples { get; }

    /// <summary>
    /// Gets quantile-family metrics calculated on common support.
    /// </summary>
    public MetricSummary QuantileCommonSupportMetrics { get; }

    /// <summary>
    /// Gets samples on the point-forecast common support.
    /// </summary>
    public IReadOnlyList<ForecastEvaluationSample> CommonSupportSamples => this.PointCommonSupportSamples;

    /// <summary>
    /// Gets point-family metrics calculated on common support.
    /// </summary>
    public MetricSummary CommonSupportMetrics => this.PointCommonSupportMetrics;

    /// <summary>
    /// Gets coverage diagnostics.
    /// </summary>
    public ModelCoverageDiagnostics Coverage => this.Evaluation.Coverage;

    /// <summary>
    /// Gets baseline-relative skill values where meaningful.
    /// </summary>
    public RelativeSkill? RelativeSkill { get; }

    /// <summary>
    /// Gets a value indicating whether the model has enough samples for ranking.
    /// </summary>
    public bool IsRankingEligible { get; }
}
