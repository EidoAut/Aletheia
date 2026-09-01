using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Contains analogue matches plus compatibility diagnostics.
/// </summary>
public sealed class HistoricalAnalogueSearchResult
{
    private readonly IReadOnlyList<HistoricalAnalogueResult> matches;
    private readonly IReadOnlyList<StateDimension> dimensionsUsed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoricalAnalogueSearchResult"/> class.
    /// </summary>
    /// <param name="matches">The distance-qualified analogue matches.</param>
    /// <param name="candidateCount">The number of historical candidates before compatibility filtering.</param>
    /// <param name="schemaCompatibleCount">The number of schema-compatible candidates.</param>
    /// <param name="rejectedSchemaIncompatibleCount">The number of candidates rejected by schema fingerprint.</param>
    /// <param name="rejectedMissingDimensionCount">The number of compatible-schema candidates missing required dimensions.</param>
    /// <param name="dimensionsUsed">The deterministic dimensions used for distance calculation.</param>
    public HistoricalAnalogueSearchResult(
        IReadOnlyList<HistoricalAnalogueResult> matches,
        int candidateCount,
        int schemaCompatibleCount,
        int rejectedSchemaIncompatibleCount,
        int rejectedMissingDimensionCount,
        IReadOnlyList<StateDimension> dimensionsUsed)
    {
        this.matches = matches ?? throw new ArgumentNullException(nameof(matches));
        this.CandidateCount = candidateCount;
        this.SchemaCompatibleCount = schemaCompatibleCount;
        this.RejectedSchemaIncompatibleCount = rejectedSchemaIncompatibleCount;
        this.RejectedMissingDimensionCount = rejectedMissingDimensionCount;
        this.dimensionsUsed = dimensionsUsed ?? throw new ArgumentNullException(nameof(dimensionsUsed));
    }

    /// <summary>
    /// Gets the distance-qualified analogue matches.
    /// </summary>
    public IReadOnlyList<HistoricalAnalogueResult> Matches => this.matches;

    /// <summary>
    /// Gets the number of historical candidates before compatibility filtering.
    /// </summary>
    public int CandidateCount { get; }

    /// <summary>
    /// Gets the number of schema-compatible candidates.
    /// </summary>
    public int SchemaCompatibleCount { get; }

    /// <summary>
    /// Gets the number of candidates rejected by schema fingerprint.
    /// </summary>
    public int RejectedSchemaIncompatibleCount { get; }

    /// <summary>
    /// Gets the number of compatible-schema candidates missing required dimensions.
    /// </summary>
    public int RejectedMissingDimensionCount { get; }

    /// <summary>
    /// Gets the deterministic dimensions used for distance calculation.
    /// </summary>
    public IReadOnlyList<StateDimension> DimensionsUsed => this.dimensionsUsed;
}
