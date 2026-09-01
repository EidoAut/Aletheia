using Aletheia.Core;

#pragma warning disable SA1402 // Scientific protocol DTOs are intentionally colocated.
#pragma warning disable SA1649 // File name follows the primary protocol type.

namespace Aletheia.Validation;

/// <summary>
/// Configures a final frozen holdout segment after development/walk-forward data.
/// </summary>
public sealed record FinalHoldoutOptions
{
    /// <summary>
    /// Gets the number of final observations reserved for holdout evaluation.
    /// </summary>
    public int HoldoutObservationCount { get; init; }

    /// <summary>
    /// Gets the minimum observations that must remain available for development.
    /// </summary>
    public int MinimumDevelopmentObservations { get; init; } = 100;

    /// <summary>
    /// Validates the options.
    /// </summary>
    public void Validate()
    {
        if (this.HoldoutObservationCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.HoldoutObservationCount), this.HoldoutObservationCount, "Holdout observation count must be positive.");
        }

        if (this.MinimumDevelopmentObservations <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.MinimumDevelopmentObservations), this.MinimumDevelopmentObservations, "Minimum development observations must exceed one.");
        }
    }
}

/// <summary>
/// Stores a non-overlapping development and final-holdout partition.
/// </summary>
/// <param name="DevelopmentStartIndex">The first development index.</param>
/// <param name="DevelopmentEndIndex">The last development index.</param>
/// <param name="HoldoutStartIndex">The first frozen holdout index.</param>
/// <param name="HoldoutEndIndex">The last frozen holdout index.</param>
/// <param name="DevelopmentSeries">The development NAV series.</param>
/// <param name="HoldoutSeries">The holdout NAV series.</param>
public sealed record FinalHoldoutSplit(
    int DevelopmentStartIndex,
    int DevelopmentEndIndex,
    int HoldoutStartIndex,
    int HoldoutEndIndex,
    NavSeries DevelopmentSeries,
    NavSeries HoldoutSeries);

/// <summary>
/// Creates final frozen holdout partitions for validation protocols.
/// </summary>
public sealed class FinalHoldoutSplitter
{
    /// <summary>
    /// Splits a NAV series into development and final holdout segments.
    /// </summary>
    /// <param name="navSeries">The full NAV series.</param>
    /// <param name="options">Holdout options.</param>
    /// <returns>The holdout split.</returns>
    public FinalHoldoutSplit Split(NavSeries navSeries, FinalHoldoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var holdoutStart = navSeries.Count - options.HoldoutObservationCount;
        if (holdoutStart < options.MinimumDevelopmentObservations)
        {
            throw new ArgumentException("Final holdout would leave insufficient development observations.", nameof(options));
        }

        var development = new NavSeries(
            navSeries.Points.Take(holdoutStart),
            navSeries.ObservationFrequency);
        var holdout = new NavSeries(
            navSeries.Points.Skip(holdoutStart),
            navSeries.ObservationFrequency);
        return new FinalHoldoutSplit(
            0,
            holdoutStart - 1,
            holdoutStart,
            navSeries.Count - 1,
            development,
            holdout);
    }
}
