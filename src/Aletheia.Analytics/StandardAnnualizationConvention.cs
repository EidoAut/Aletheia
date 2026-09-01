using Aletheia.Core;

namespace Aletheia.Analytics;

/// <summary>
/// Provides Aletheia's default annualization periods by observation frequency.
/// </summary>
/// <remarks>
/// Irregular data is deliberately unsupported because there is no single
/// defensible periods-per-year value without using actual elapsed timestamps
/// or an explicit user-supplied convention.
/// </remarks>
public sealed class StandardAnnualizationConvention : IAnnualizationConvention
{
    /// <summary>
    /// Gets the shared default convention.
    /// </summary>
    public static StandardAnnualizationConvention Default { get; } = new();

    /// <inheritdoc />
    public double ResolvePeriodsPerYear(ObservationFrequency frequency)
    {
        return frequency switch
        {
            ObservationFrequency.Daily => 365.25d,
            ObservationFrequency.BusinessDaily => 252d,
            ObservationFrequency.Weekly => 52d,
            ObservationFrequency.Monthly => 12d,
            ObservationFrequency.Irregular => throw new InvalidOperationException(
                "Irregular observations cannot be annualized without an explicit convention."),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported observation frequency."),
        };
    }
}
