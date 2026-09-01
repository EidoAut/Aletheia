namespace Aletheia.Validation;

/// <summary>
/// Identifies coarse validation lifecycle events suitable for structured logging.
/// </summary>
public enum ValidationEventType
{
    /// <summary>
    /// Walk-forward evaluation started.
    /// </summary>
    WalkForwardEvaluationStarted,

    /// <summary>
    /// Model training started.
    /// </summary>
    ModelTrainingStarted,

    /// <summary>
    /// A forecast was generated.
    /// </summary>
    ForecastGenerated,

    /// <summary>
    /// A forecast was rejected with a typed reason.
    /// </summary>
    ForecastRejected,

    /// <summary>
    /// A prediction was stored.
    /// </summary>
    PredictionStored,

    /// <summary>
    /// A prediction evaluation was stored.
    /// </summary>
    EvaluationStored,

    /// <summary>
    /// Model evaluation completed.
    /// </summary>
    ModelEvaluationCompleted,

    /// <summary>
    /// Model Arena completed.
    /// </summary>
    ModelArenaCompleted,
}
