namespace Aletheia.Validation;

/// <summary>
/// Indicates that an immutable prediction ledger identity was reused with
/// different scientific content.
/// </summary>
public sealed class PredictionLedgerIntegrityException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionLedgerIntegrityException"/> class.
    /// </summary>
    /// <param name="message">The integrity failure message.</param>
    public PredictionLedgerIntegrityException(string message)
        : base(message)
    {
    }
}
