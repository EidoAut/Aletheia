using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Represents one coarse validation lifecycle event.
/// </summary>
public sealed record ValidationEvent(
    ValidationEventType EventType,
    ModelDescriptor? Model,
    DateOnly? CutoffDate,
    string Message);
