namespace Aletheia.Validation;

/// <summary>
/// Ignores validation events.
/// </summary>
public sealed class NullValidationEventSink : IValidationEventSink
{
    private NullValidationEventSink()
    {
    }

    /// <summary>
    /// Gets the shared no-op event sink.
    /// </summary>
    public static NullValidationEventSink Instance { get; } = new();

    /// <inheritdoc />
    public void Record(ValidationEvent validationEvent)
    {
        ArgumentNullException.ThrowIfNull(validationEvent);
    }
}
