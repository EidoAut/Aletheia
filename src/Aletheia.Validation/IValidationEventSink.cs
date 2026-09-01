namespace Aletheia.Validation;

/// <summary>
/// Receives coarse validation lifecycle events.
/// </summary>
public interface IValidationEventSink
{
    /// <summary>
    /// Records a validation event.
    /// </summary>
    /// <param name="validationEvent">The event to record.</param>
    void Record(ValidationEvent validationEvent);
}
