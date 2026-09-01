namespace Aletheia.Core;

/// <summary>
/// Represents an International Securities Identification Number.
/// </summary>
/// <remarks>
/// The type performs structural validation only. It intentionally does not
/// query any external registry, keeping domain construction deterministic.
/// </remarks>
public readonly record struct Isin
{
    private const int ExpectedLength = 12;

    /// <summary>
    /// Initializes a new instance of the <see cref="Isin"/> struct.
    /// </summary>
    /// <param name="value">The ISIN value.</param>
    public Isin(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("The value is not a structurally valid ISIN.", nameof(value));
        }

        this.Value = value.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Gets the normalized ISIN value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns a value indicating whether the supplied value has the expected ISIN shape.
    /// </summary>
    /// <param name="value">The candidate ISIN.</param>
    /// <returns><see langword="true"/> when the value has twelve alphanumeric characters.</returns>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return trimmed.Length == ExpectedLength && trimmed.All(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Converts the ISIN to the generic Aletheia identifier model.
    /// </summary>
    /// <returns>A fund identifier in the ISIN namespace.</returns>
    public FundIdentifier ToFundIdentifier() => new(FundIdentifierKind.Isin, this.Value);

    /// <inheritdoc />
    public override string ToString() => this.Value;
}
