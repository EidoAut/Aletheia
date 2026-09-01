namespace Aletheia.Validation;

/// <summary>
/// Stores immutable predictions and separate realized evaluations.
/// </summary>
public interface IPredictionLedger
{
    /// <summary>
    /// Initializes the backing store.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>A task representing the operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a prediction without overwriting an existing logical prediction.
    /// </summary>
    /// <param name="prediction">The prediction to store.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>A task representing the operation.</returns>
    Task StorePredictionAsync(PredictionLedgerRecord prediction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an evaluation linked to an existing prediction.
    /// </summary>
    /// <param name="evaluation">The evaluation to store.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>A task representing the operation.</returns>
    Task StoreEvaluationAsync(PredictionEvaluationRecord evaluation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a prediction by identifier.
    /// </summary>
    /// <param name="predictionId">The prediction identifier.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>The prediction, when found.</returns>
    Task<PredictionLedgerRecord?> GetPredictionAsync(Guid predictionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a prediction by logical idempotency key.
    /// </summary>
    /// <param name="logicalKey">The logical prediction key.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>The prediction, when found.</returns>
    Task<PredictionLedgerRecord?> GetPredictionByLogicalKeyAsync(string logicalKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists recent predictions.
    /// </summary>
    /// <param name="limit">The maximum number of rows.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>The recent predictions.</returns>
    Task<IReadOnlyList<PredictionLedgerRecord>> ListPredictionsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists evaluations for one prediction.
    /// </summary>
    /// <param name="predictionId">The prediction identifier.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>The evaluations.</returns>
    Task<IReadOnlyList<PredictionEvaluationRecord>> GetEvaluationsAsync(Guid predictionId, CancellationToken cancellationToken = default);
}
