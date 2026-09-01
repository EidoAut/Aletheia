namespace Aletheia.Core;

/// <summary>
/// Couples a fund descriptor with its dated NAV observations.
/// </summary>
public sealed record FundHistory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundHistory"/> class.
    /// </summary>
    /// <param name="fund">The fund descriptor.</param>
    /// <param name="navSeries">The historical NAV series.</param>
    public FundHistory(Fund fund, NavSeries navSeries)
    {
        this.Fund = fund ?? throw new ArgumentNullException(nameof(fund));
        this.NavSeries = navSeries ?? throw new ArgumentNullException(nameof(navSeries));
    }

    /// <summary>
    /// Gets the fund descriptor.
    /// </summary>
    public Fund Fund { get; }

    /// <summary>
    /// Gets the historical NAV observations.
    /// </summary>
    public NavSeries NavSeries { get; }
}
