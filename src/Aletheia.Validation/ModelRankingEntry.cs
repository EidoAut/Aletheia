using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Explains one transparent Model Arena ranking entry.
/// </summary>
public sealed record ModelRankingEntry(
    int Rank,
    ModelDescriptor Model,
    string Reason);
