using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Describes how one model was treated by the strategic forecast ensemble.
/// </summary>
/// <param name="Model">The model descriptor.</param>
/// <param name="ValidationStatus">The validation gate status.</param>
/// <param name="ValidationScore">The validation loss used for weighting, when available.</param>
/// <param name="ArenaRank">The point-forecast arena rank, when eligible.</param>
/// <param name="EnsembleWeight">The effective ensemble weight.</param>
/// <param name="Included">Whether this model entered the ensemble.</param>
/// <param name="ExclusionReason">Why the model did not enter the ensemble.</param>
public sealed record ForecastEnsembleAuditEntry(
    ModelDescriptor Model,
    string ValidationStatus,
    double? ValidationScore,
    int? ArenaRank,
    double EnsembleWeight,
    bool Included,
    string ExclusionReason);
