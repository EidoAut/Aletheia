namespace Aletheia.Application;

/// <summary>
/// Stores one date-aligned numeric value for presentation.
/// </summary>
/// <param name="Date">The observation date.</param>
/// <param name="Value">The numeric value.</param>
public sealed record DatedValue(DateOnly Date, double Value);
