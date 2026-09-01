namespace Aletheia.Dynamics;

/// <summary>
/// Indicates that a dynamic model received a state lacking required semantics.
/// </summary>
public sealed class IncompatibleDynamicStateException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncompatibleDynamicStateException"/> class.
    /// </summary>
    /// <param name="message">The compatibility failure message.</param>
    public IncompatibleDynamicStateException(string message)
        : base(message)
    {
    }
}
