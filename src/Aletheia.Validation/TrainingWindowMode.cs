namespace Aletheia.Validation;

/// <summary>
/// Defines how historical observations are selected before each prediction cutoff.
/// </summary>
public enum TrainingWindowMode
{
    /// <summary>
    /// The training window starts at the first available observation and expands over time.
    /// </summary>
    Expanding,

    /// <summary>
    /// The training window keeps a fixed trailing observation count.
    /// </summary>
    Rolling,
}
