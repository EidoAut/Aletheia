using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Describes the source NAV series and the effective series used by scientific calculations.
/// </summary>
/// <param name="NavSeries">The effective NAV series.</param>
/// <param name="SourceObservationCount">The number of dated rows supplied by the source.</param>
/// <param name="EffectiveObservationCount">The number of observations retained for calculations.</param>
/// <param name="SyntheticObservationCount">The number of calendar carry-forward rows excluded from calculations.</param>
/// <param name="SourceStartDate">The first source observation date.</param>
/// <param name="SourceEndDate">The last source observation date.</param>
/// <param name="LastEffectiveObservationDate">The last retained observation date.</param>
/// <param name="Policy">The deterministic effective-observation policy.</param>
public sealed record EffectiveNavSeriesResult(
    NavSeries NavSeries,
    int SourceObservationCount,
    int EffectiveObservationCount,
    int SyntheticObservationCount,
    DateOnly SourceStartDate,
    DateOnly SourceEndDate,
    DateOnly LastEffectiveObservationDate,
    string Policy);
