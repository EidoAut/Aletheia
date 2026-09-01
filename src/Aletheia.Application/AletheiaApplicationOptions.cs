using Aletheia.Data;

namespace Aletheia.Application;

/// <summary>
/// Configures reusable Aletheia application use cases.
/// </summary>
public sealed class AletheiaApplicationOptions
{
    /// <summary>
    /// Gets or sets the optional SQLite prediction ledger path.
    /// </summary>
    public string? LedgerPath { get; set; }

    /// <summary>
    /// Gets or sets the rolling window used for presentation series.
    /// </summary>
    public int RollingWindowObservations { get; set; } = 63;

    /// <summary>
    /// Gets or sets the maximum analogue paths exposed for charting.
    /// </summary>
    public int MaximumAnaloguePaths { get; set; } = 25;

    /// <summary>
    /// Gets or sets the analogue path horizon in observations.
    /// </summary>
    public int AnaloguePathHorizonObservations { get; set; } = 180;

    /// <summary>
    /// Gets or sets the maximum data age, in calendar days, still considered fresh.
    /// </summary>
    public int FreshDataMaxAgeDays { get; set; } = 45;

    /// <summary>
    /// Gets or sets the maximum data age, in calendar days, allowed for qualified actionability.
    /// </summary>
    public int ActionableDataMaxAgeDays { get; set; } = 75;

    /// <summary>
    /// Gets or sets an optional deterministic analysis timestamp for tests and reproducible reports.
    /// </summary>
    public DateTimeOffset? ReportGeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets configured fund catalog providers.
    /// </summary>
    public IReadOnlyList<IFundCatalogProvider>? CatalogProviders { get; set; }

    /// <summary>
    /// Gets or sets configured history providers keyed by provider id.
    /// </summary>
    public IReadOnlyDictionary<string, IProvenanceAwareFundDataProvider>? HistoryProviders { get; set; }
}
