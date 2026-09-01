using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Stores walk-forward predictions, failures, and metrics for one model.
/// </summary>
public sealed class WalkForwardModelEvaluationResult
{
    private readonly IReadOnlyList<ForecastEvaluationSample> samples;
    private readonly IReadOnlyList<ForecastEvaluationSample> nonOverlappingSamples;
    private readonly IReadOnlyList<ForecastFailureRecord> failures;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalkForwardModelEvaluationResult"/> class.
    /// </summary>
    /// <param name="model">The evaluated model descriptor.</param>
    /// <param name="configurationFingerprint">The configuration fingerprint.</param>
    /// <param name="samples">All evaluated samples.</param>
    /// <param name="nonOverlappingSamples">The deterministic non-overlapping subset.</param>
    /// <param name="failures">Typed model failures.</param>
    /// <param name="allSamplesMetrics">All-sample metrics.</param>
    /// <param name="nonOverlappingMetrics">Non-overlapping-subset metrics.</param>
    /// <param name="evaluationStartDate">The first evaluated cutoff date.</param>
    /// <param name="evaluationEndDate">The last evaluated target date.</param>
    /// <param name="coverage">The model coverage diagnostics.</param>
    public WalkForwardModelEvaluationResult(
        ModelDescriptor model,
        string configurationFingerprint,
        IReadOnlyList<ForecastEvaluationSample> samples,
        IReadOnlyList<ForecastEvaluationSample> nonOverlappingSamples,
        IReadOnlyList<ForecastFailureRecord> failures,
        MetricSummary allSamplesMetrics,
        MetricSummary nonOverlappingMetrics,
        DateOnly? evaluationStartDate,
        DateOnly? evaluationEndDate,
        ModelCoverageDiagnostics coverage)
    {
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        this.ConfigurationFingerprint = string.IsNullOrWhiteSpace(configurationFingerprint)
            ? throw new ArgumentException("Configuration fingerprint cannot be empty.", nameof(configurationFingerprint))
            : configurationFingerprint;
        this.samples = samples ?? throw new ArgumentNullException(nameof(samples));
        this.nonOverlappingSamples = nonOverlappingSamples ?? throw new ArgumentNullException(nameof(nonOverlappingSamples));
        this.failures = failures ?? throw new ArgumentNullException(nameof(failures));
        this.AllSamplesMetrics = allSamplesMetrics ?? throw new ArgumentNullException(nameof(allSamplesMetrics));
        this.NonOverlappingMetrics = nonOverlappingMetrics ?? throw new ArgumentNullException(nameof(nonOverlappingMetrics));
        this.EvaluationStartDate = evaluationStartDate;
        this.EvaluationEndDate = evaluationEndDate;
        this.Coverage = coverage ?? throw new ArgumentNullException(nameof(coverage));
    }

    /// <summary>
    /// Gets the evaluated model descriptor.
    /// </summary>
    public ModelDescriptor Model { get; }

    /// <summary>
    /// Gets the configuration fingerprint.
    /// </summary>
    public string ConfigurationFingerprint { get; }

    /// <summary>
    /// Gets all evaluated prediction samples.
    /// </summary>
    public IReadOnlyList<ForecastEvaluationSample> Samples => this.samples;

    /// <summary>
    /// Gets the deterministic non-overlapping subset.
    /// </summary>
    public IReadOnlyList<ForecastEvaluationSample> NonOverlappingSamples => this.nonOverlappingSamples;

    /// <summary>
    /// Gets typed model failures.
    /// </summary>
    public IReadOnlyList<ForecastFailureRecord> Failures => this.failures;

    /// <summary>
    /// Gets all-sample metrics.
    /// </summary>
    public MetricSummary AllSamplesMetrics { get; }

    /// <summary>
    /// Gets non-overlapping-subset metrics.
    /// </summary>
    public MetricSummary NonOverlappingMetrics { get; }

    /// <summary>
    /// Gets the first evaluated cutoff date.
    /// </summary>
    public DateOnly? EvaluationStartDate { get; }

    /// <summary>
    /// Gets the last evaluated target date.
    /// </summary>
    public DateOnly? EvaluationEndDate { get; }

    /// <summary>
    /// Gets coverage diagnostics for the model.
    /// </summary>
    public ModelCoverageDiagnostics Coverage { get; }
}
