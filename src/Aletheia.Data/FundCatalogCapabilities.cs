namespace Aletheia.Data;

/// <summary>
/// Describes what a fund catalog provider can supply.
/// </summary>
public sealed record FundCatalogCapabilities(
    bool SupportsFreeTextSearch,
    bool SupportsIsinSearch,
    bool SupportsPartialIsinSearch,
    bool SupportsManagerSearch,
    bool ProvidesHistoricalData,
    string HistoricalResolution);
