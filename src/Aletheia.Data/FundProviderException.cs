namespace Aletheia.Data;

/// <summary>
/// Represents a classified external provider failure.
/// </summary>
public sealed class FundProviderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundProviderException"/> class.
    /// </summary>
    /// <param name="kind">The provider failure kind.</param>
    /// <param name="message">The user-facing message.</param>
    /// <param name="innerException">The optional technical exception.</param>
    public FundProviderException(
        FundProviderErrorKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        this.Kind = kind;
    }

    /// <summary>
    /// Gets the provider failure kind.
    /// </summary>
    public FundProviderErrorKind Kind { get; }
}
