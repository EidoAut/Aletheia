namespace Aletheia.Core;

/// <summary>
/// Describes the namespace in which a fund identifier is meaningful.
/// </summary>
public enum FundIdentifierKind
{
    /// <summary>
    /// The identifier is an International Securities Identification Number.
    /// </summary>
    Isin,

    /// <summary>
    /// The identifier belongs to a specific external data provider.
    /// </summary>
    Provider,

    /// <summary>
    /// The identifier is a local path or locally assigned key.
    /// </summary>
    Local,
}
