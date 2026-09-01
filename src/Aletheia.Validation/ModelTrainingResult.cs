namespace Aletheia.Validation;

/// <summary>
/// Represents the typed outcome of fitting a forecast model at a cutoff.
/// </summary>
public sealed class ModelTrainingResult
{
    private readonly IReadOnlyDictionary<string, string> diagnostics;

    private ModelTrainingResult(
        ForecastStatus status,
        object? fittedState,
        string? failureReason,
        IReadOnlyDictionary<string, string> diagnostics)
    {
        this.Status = status;
        this.FittedState = fittedState;
        this.FailureReason = failureReason;
        this.diagnostics = new Dictionary<string, string>(diagnostics);
    }

    /// <summary>
    /// Gets the training status.
    /// </summary>
    public ForecastStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether training succeeded.
    /// </summary>
    public bool IsSuccess => this.Status == ForecastStatus.Success;

    /// <summary>
    /// Gets the model-specific fitted state.
    /// </summary>
    public object? FittedState { get; }

    /// <summary>
    /// Gets the failure reason, when training failed.
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// Gets model diagnostics captured during training.
    /// </summary>
    public IReadOnlyDictionary<string, string> Diagnostics => this.diagnostics;

    /// <summary>
    /// Creates a successful training result.
    /// </summary>
    /// <param name="fittedState">The model-specific fitted state.</param>
    /// <param name="diagnostics">The model diagnostics.</param>
    /// <returns>A successful result.</returns>
    public static ModelTrainingResult Success(
        object? fittedState,
        IReadOnlyDictionary<string, string>? diagnostics = null) =>
        new(ForecastStatus.Success, fittedState, null, diagnostics ?? new Dictionary<string, string>());

    /// <summary>
    /// Creates a failed training result.
    /// </summary>
    /// <param name="status">The typed failure status.</param>
    /// <param name="reason">The human-readable failure reason.</param>
    /// <param name="diagnostics">The model diagnostics.</param>
    /// <returns>A failed result.</returns>
    public static ModelTrainingResult Failure(
        ForecastStatus status,
        string reason,
        IReadOnlyDictionary<string, string>? diagnostics = null)
    {
        if (status == ForecastStatus.Success)
        {
            throw new ArgumentException("Successful status cannot be used for a failure result.", nameof(status));
        }

        return new ModelTrainingResult(
            status,
            null,
            string.IsNullOrWhiteSpace(reason) ? "No failure reason supplied." : reason,
            diagnostics ?? new Dictionary<string, string>());
    }
}
