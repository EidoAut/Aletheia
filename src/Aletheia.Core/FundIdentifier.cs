namespace Aletheia.Core;

/// <summary>
/// Identifies a fund in a provider-independent way.
/// </summary>
/// <remarks>
/// Aletheia keeps identifiers explicit because a single financial instrument can
/// have different provider identifiers even when the ISIN is stable.
/// </remarks>
public readonly record struct FundIdentifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundIdentifier"/> struct.
    /// </summary>
    /// <param name="kind">The identifier namespace.</param>
    /// <param name="value">The identifier value in that namespace.</param>
    public FundIdentifier(FundIdentifierKind kind, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A fund identifier cannot be empty.", nameof(value));
        }

        this.Kind = kind;
        this.Value = value.Trim();
    }

    /// <summary>
    /// Gets the identifier namespace.
    /// </summary>
    public FundIdentifierKind Kind { get; }

    /// <summary>
    /// Gets the identifier value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => $"{this.Kind}:{this.Value}";
}
