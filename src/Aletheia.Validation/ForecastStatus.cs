namespace Aletheia.Validation;

/// <summary>
/// Describes whether a model produced a usable forecast at a walk-forward cutoff.
/// </summary>
public enum ForecastStatus
{
    /// <summary>
    /// The model trained and produced a mathematically valid forecast.
    /// </summary>
    Success,

    /// <summary>
    /// The model did not have enough historical observations for a defensible forecast.
    /// </summary>
    InsufficientData,

    /// <summary>
    /// The available state vector or series semantics were incompatible with the model.
    /// </summary>
    IncompatibleState,

    /// <summary>
    /// The model fit was mathematically unsuitable, such as a rejected nonstationary AR process.
    /// </summary>
    ModelRejected,

    /// <summary>
    /// The model output failed probability, quantile, or finiteness validation.
    /// </summary>
    InvalidOutput,

    /// <summary>
    /// The dataset or horizon could not be evaluated.
    /// </summary>
    InvalidData,
}
