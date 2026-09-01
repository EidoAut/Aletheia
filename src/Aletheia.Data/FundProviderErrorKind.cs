namespace Aletheia.Data;

/// <summary>
/// Classifies provider failures for user-facing error handling.
/// </summary>
public enum FundProviderErrorKind
{
    /// <summary>
    /// No matching fund or share class was found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The provider could not be reached.
    /// </summary>
    ProviderUnavailable,

    /// <summary>
    /// The provider request timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// The provider response was malformed or unsupported.
    /// </summary>
    InvalidResponse,

    /// <summary>
    /// The fund exists, but no usable history is available.
    /// </summary>
    NoUsableHistory,
}
