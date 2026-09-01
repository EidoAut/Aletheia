using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Summarizes how often a model could participate in the evaluation schedule.
/// </summary>
public sealed class ModelCoverageDiagnostics
{
    private readonly IReadOnlyDictionary<ForecastStatus, int> failuresByStatus;
    private readonly IReadOnlyDictionary<string, int> failuresByReason;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelCoverageDiagnostics"/> class.
    /// </summary>
    /// <param name="model">The model descriptor.</param>
    /// <param name="eligibleEvents">The number of eligible walk-forward events.</param>
    /// <param name="successfulForecasts">The number of successful forecasts.</param>
    /// <param name="failures">The typed failures.</param>
    public ModelCoverageDiagnostics(
        ModelDescriptor model,
        int eligibleEvents,
        int successfulForecasts,
        IReadOnlyList<ForecastFailureRecord> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        this.EligibleEvents = eligibleEvents;
        this.SuccessfulForecasts = successfulForecasts;
        this.FailedForecasts = failures.Count;
        this.CoverageRatio = eligibleEvents == 0 ? null : successfulForecasts / (double)eligibleEvents;
        this.failuresByStatus = failures
            .GroupBy(item => item.Status)
            .ToDictionary(group => group.Key, group => group.Count());
        this.failuresByReason = failures
            .GroupBy(item => item.Reason)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the model descriptor.
    /// </summary>
    public ModelDescriptor Model { get; }

    /// <summary>
    /// Gets the number of events the model was asked to forecast.
    /// </summary>
    public int EligibleEvents { get; }

    /// <summary>
    /// Gets the number of successful forecasts.
    /// </summary>
    public int SuccessfulForecasts { get; }

    /// <summary>
    /// Gets the number of failed forecasts.
    /// </summary>
    public int FailedForecasts { get; }

    /// <summary>
    /// Gets the success ratio, or <see langword="null"/> when no events existed.
    /// </summary>
    public double? CoverageRatio { get; }

    /// <summary>
    /// Gets failure counts grouped by typed status.
    /// </summary>
    public IReadOnlyDictionary<ForecastStatus, int> FailuresByStatus => this.failuresByStatus;

    /// <summary>
    /// Gets failure counts grouped by reason text.
    /// </summary>
    public IReadOnlyDictionary<string, int> FailuresByReason => this.failuresByReason;
}
