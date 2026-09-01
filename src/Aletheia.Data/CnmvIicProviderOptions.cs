using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Configures the CNMV IIC provider.
/// </summary>
public sealed class CnmvIicProviderOptions
{
    /// <summary>
    /// Gets the stable provider id.
    /// </summary>
    public string ProviderId { get; init; } = "cnmv-iic";

    /// <summary>
    /// Gets the human-readable provider name.
    /// </summary>
    public string DisplayName { get; init; } = "CNMV IIC";

    /// <summary>
    /// Gets the CNMV base URI.
    /// </summary>
    public Uri BaseUri { get; init; } = new("https://www.cnmv.es/");

    /// <summary>
    /// Gets the user-agent header sent to CNMV.
    /// </summary>
    public string UserAgent { get; init; } = $"Aletheia/{AletheiaRelease.ProductVersion} (+https://github.com/aletheia)";

    /// <summary>
    /// Gets the HTTP timeout.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the maximum redirect hops followed by the provider.
    /// </summary>
    public int MaximumRedirects { get; init; } = 5;

    /// <summary>
    /// Gets the maximum accepted HTTP response size in bytes.
    /// </summary>
    public long MaximumHttpPayloadBytes { get; init; } = 64L * 1024L * 1024L;

    /// <summary>
    /// Gets the default history length when no date range is supplied.
    /// </summary>
    public int DefaultHistoryMonths { get; init; } = 36;

    /// <summary>
    /// Gets a value indicating whether local payload caching is enabled.
    /// </summary>
    public bool EnableCache { get; init; } = true;

    /// <summary>
    /// Gets the maximum age for cached CNMV payloads.
    /// </summary>
    public TimeSpan CacheTimeToLive { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets the local cache directory.
    /// </summary>
    public string CacheDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Aletheia",
        "cache");

    /// <summary>
    /// Gets the maximum number of XML entries read from one CNMV ZIP archive.
    /// </summary>
    public int MaximumZipEntries { get; init; } = 64;

    /// <summary>
    /// Gets the maximum decompressed size for one ZIP entry.
    /// </summary>
    public long MaximumZipEntryBytes { get; init; } = 24L * 1024L * 1024L;

    /// <summary>
    /// Gets the maximum total decompressed XML size for one ZIP archive.
    /// </summary>
    public long MaximumZipTotalBytes { get; init; } = 64L * 1024L * 1024L;

    /// <summary>
    /// Gets the maximum decompressed-to-compressed ratio accepted for one ZIP entry.
    /// </summary>
    public double MaximumZipCompressionRatio { get; init; } = 100d;

    /// <summary>
    /// Gets the first year to consider when searching for the latest publication.
    /// </summary>
    public int MinimumPublicationYear { get; init; } = 2012;

    /// <summary>
    /// Creates a CNMV individual-information page URI for an exercise year.
    /// </summary>
    /// <param name="year">The exercise year.</param>
    /// <returns>The page URI.</returns>
    public Uri CreateDownloadPageUri(int year)
    {
        return new Uri(this.BaseUri, $"portal/publicaciones/descarga-informacion-individual?ejercicio={year.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lang=es");
    }
}
