using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Stores immutable prediction and evaluation detail records for presentation.
/// </summary>
public sealed record PredictionDetailsResult(
    PredictionLedgerRecord Prediction,
    IReadOnlyList<PredictionEvaluationRecord> Evaluations);
