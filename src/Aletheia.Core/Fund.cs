namespace Aletheia.Core;

/// <summary>
/// Describes a fund independently of its historical observations.
/// </summary>
public sealed record Fund
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Fund"/> class.
    /// </summary>
    /// <param name="identifier">The primary fund identifier.</param>
    /// <param name="name">The human-readable fund name.</param>
    /// <param name="providerName">The optional data provider name.</param>
    /// <param name="currency">The optional reporting currency.</param>
    public Fund(FundIdentifier identifier, string name, string? providerName = null, string? currency = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A fund name cannot be empty.", nameof(name));
        }

        this.Identifier = identifier;
        this.Name = name.Trim();
        this.ProviderName = providerName;
        this.Currency = currency;
    }

    /// <summary>
    /// Gets the primary fund identifier.
    /// </summary>
    public FundIdentifier Identifier { get; }

    /// <summary>
    /// Gets the human-readable fund name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional source provider name.
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// Gets the optional reporting currency.
    /// </summary>
    public string? Currency { get; }
}
