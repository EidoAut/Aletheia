using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Represents the typed outcome of asking a fitted model to forecast.
/// </summary>
public sealed class ForecastPredictionResult
{
    private readonly IReadOnlyDictionary<string, string> diagnostics;

    private ForecastPredictionResult(
        ForecastStatus status,
        ForecastDistribution? distribution,
        string? failureReason,
        IReadOnlyDictionary<string, string> diagnostics)
    {
        this.Status = status;
        this.Distribution = distribution;
        this.FailureReason = failureReason;
        this.diagnostics = new Dictionary<string, string>(diagnostics);
    }

    /// <summary>
    /// Gets the prediction status.
    /// </summary>
    public ForecastStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether prediction succeeded.
    /// </summary>
    public bool IsSuccess => this.Status == ForecastStatus.Success;

    /// <summary>
    /// Gets the forecast distribution when prediction succeeded.
    /// </summary>
    public ForecastDistribution? Distribution { get; }

    /// <summary>
    /// Gets the failure reason, when prediction failed.
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// Gets diagnostics captured during prediction.
    /// </summary>
    public IReadOnlyDictionary<string, string> Diagnostics => this.diagnostics;

    /// <summary>
    /// Creates a successful prediction result.
    /// </summary>
    /// <param name="distribution">The forecast distribution.</param>
    /// <param name="diagnostics">Prediction diagnostics.</param>
    /// <returns>A successful result.</returns>
    public static ForecastPredictionResult Success(
        ForecastDistribution distribution,
        IReadOnlyDictionary<string, string>? diagnostics = null) =>
        new(ForecastStatus.Success, distribution, null, diagnostics ?? new Dictionary<string, string>());

    /// <summary>
    /// Creates a failed prediction result.
    /// </summary>
    /// <param name="status">The typed failure status.</param>
    /// <param name="reason">The human-readable failure reason.</param>
    /// <param name="diagnostics">Prediction diagnostics.</param>
    /// <returns>A failed result.</returns>
    public static ForecastPredictionResult Failure(
        ForecastStatus status,
        string reason,
        IReadOnlyDictionary<string, string>? diagnostics = null)
    {
        if (status == ForecastStatus.Success)
        {
            throw new ArgumentException("Successful status cannot be used for a failure result.", nameof(status));
        }

        return new ForecastPredictionResult(
            status,
            null,
            string.IsNullOrWhiteSpace(reason) ? "No failure reason supplied." : reason,
            diagnostics ?? new Dictionary<string, string>());
    }
}
