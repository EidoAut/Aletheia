namespace Aletheia.Application;

/// <summary>
/// Stores one histogram bin.
/// </summary>
public sealed record HistogramBin(double LowerBoundInclusive, double UpperBoundExclusive, int Count);
