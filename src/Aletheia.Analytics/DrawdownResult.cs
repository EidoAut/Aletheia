namespace Aletheia.Analytics;

/// <summary>
/// Describes the worst observed peak-to-trough loss.
/// </summary>
public sealed class DrawdownResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawdownResult"/> class.
    /// </summary>
    /// <param name="maximumDrawdown">The worst drawdown as a negative return.</param>
    /// <param name="peakDate">The high-water-mark date before the trough.</param>
    /// <param name="troughDate">The trough date.</param>
    /// <param name="durationDays">The number of calendar days from peak to trough.</param>
    /// <param name="recoveryDate">The first date that recovered the prior high-water mark, when known.</param>
    public DrawdownResult(
        double maximumDrawdown,
        DateOnly? peakDate,
        DateOnly? troughDate,
        int durationDays,
        DateOnly? recoveryDate)
    {
        this.MaximumDrawdown = maximumDrawdown;
        this.PeakDate = peakDate;
        this.TroughDate = troughDate;
        this.DurationDays = durationDays;
        this.RecoveryDate = recoveryDate;
    }

    /// <summary>
    /// Gets the worst drawdown as a negative return.
    /// </summary>
    public double MaximumDrawdown { get; }

    /// <summary>
    /// Gets the high-water-mark date before the trough.
    /// </summary>
    public DateOnly? PeakDate { get; }

    /// <summary>
    /// Gets the trough date.
    /// </summary>
    public DateOnly? TroughDate { get; }

    /// <summary>
    /// Gets the number of calendar days from peak to trough.
    /// </summary>
    public int DurationDays { get; }

    /// <summary>
    /// Gets the first date that recovered the prior high-water mark, when known.
    /// </summary>
    public DateOnly? RecoveryDate { get; }
}
