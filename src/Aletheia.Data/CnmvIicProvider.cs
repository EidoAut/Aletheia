#pragma warning disable SA1204 // ZIP validation helpers are grouped with archive loading workflow.
#pragma warning disable SA1201 // CNMV-specific value objects are grouped after the provider workflow.
#pragma warning disable SA1402 // CNMV-specific helper records are intentionally kept with the provider.

using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Provides official Spanish IIC fund discovery and NAV history from CNMV XML publications.
/// </summary>
public sealed class CnmvIicProvider : IFundCatalogProvider, IProvenanceAwareFundDataProvider
{
    private static readonly Regex DownloadTablePattern = new(
        "<table\\b(?=[^>]*\\bid\\s*=\\s*[\"'][^\"']*grdDescargas[^\"']*[\"'])[^>]*>.*?</table>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex RowPattern = new(
        "<tr\\b[^>]*>(?<row>.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex CellPattern = new(
        "<td\\b[^>]*>(?<cell>.*?)</td>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex LinkPattern = new(
        "<a\\b[^>]*\\bhref\\s*=\\s*[\"'](?<href>[^\"']+)[\"'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex AttributePattern = new(
        "\\b(?<name>title|alt|aria-label)\\s*=\\s*[\"'](?<value>[^\"']*)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex HtmlTagPattern = new(
        "<[^>]+>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private readonly HttpClient httpClient;
    private readonly CnmvIicProviderOptions options;
    private readonly CnmvIicParser parser;
    private readonly LocalProviderCache? cache;
    private readonly DatasetFingerprintCalculator fingerprintCalculator = new();
    private readonly Dictionary<int, IReadOnlyList<CnmvDocumentCandidate>> candidateCache = new();
    private readonly Dictionary<string, CnmvArchive> archiveCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="CnmvIicProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="options">Provider options.</param>
    /// <param name="parser">The XML parser.</param>
    /// <param name="cache">The local payload cache.</param>
    public CnmvIicProvider(
        HttpClient? httpClient = null,
        CnmvIicProviderOptions? options = null,
        CnmvIicParser? parser = null,
        LocalProviderCache? cache = null)
    {
        this.options = options ?? new CnmvIicProviderOptions();
        this.httpClient = httpClient ?? new HttpClient();
        this.httpClient.Timeout = this.options.Timeout;
        if (!this.httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(this.options.UserAgent);
        }

        this.parser = parser ?? new CnmvIicParser();
        this.cache = this.options.EnableCache
            ? cache ?? new LocalProviderCache(this.options.CacheDirectory, this.options.CacheTimeToLive)
            : null;
    }

    /// <inheritdoc />
    public string ProviderId => this.options.ProviderId;

    /// <inheritdoc />
    public string DisplayName => this.options.DisplayName;

    /// <inheritdoc />
    public FundCatalogCapabilities Capabilities { get; } = new(
        SupportsFreeTextSearch: true,
        SupportsIsinSearch: true,
        SupportsPartialIsinSearch: true,
        SupportsManagerSearch: true,
        ProvidesHistoricalData: true,
        HistoricalResolution: "Official CNMV reported daily NAV fields inside monthly IIC XML ZIP files; no interpolation.");

    /// <inheritdoc />
    public async Task<IReadOnlyList<FundSearchResult>> SearchAsync(
        FundSearchQuery query,
        int maximumResults = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.IsEmpty)
        {
            return Array.Empty<FundSearchResult>();
        }

        var archive = await this.LoadLatestArchiveAsync(cancellationToken).ConfigureAwait(false);
        var registrations = this.parser.ParseRegistrations(archive.GetRequiredTextEntry("FONDREGISTRO"));
        var monthlyXml = archive.GetRequiredTextEntry("FONDMENS");
        var matches = registrations
            .Where(registration => MatchesQuery(registration, query))
            .OrderByDescending(registration => string.Equals(registration.Isin, query.Isin, StringComparison.Ordinal))
            .ThenBy(registration => registration.FundName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maximumResults))
            .ToArray();
        var results = new List<FundSearchResult>(matches.Length);
        foreach (var registration in matches)
        {
            var latestPoints = this.parser.ParseMonthlyNavs(monthlyXml, registration.Isin);
            var latestObservation = latestPoints.Count == 0 ? (DateOnly?)null : latestPoints[^1].Date;
            var frequency = latestPoints.Count < 2
                ? (ObservationFrequency?)null
                : ObservationFrequencyDetector.Detect(latestPoints);
            results.Add(registration.ToSearchResult(
                this.ProviderId,
                this.DisplayName,
                latestObservation,
                frequency,
                latestPoints.Count > 0));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<Fund?> FindByIsinAsync(string isin, CancellationToken cancellationToken = default)
    {
        var normalized = FundSearchQuery.NormalizeIsin(isin);
        if (normalized is null || !Isin.IsValid(normalized))
        {
            return null;
        }

        var result = (await this.SearchAsync(
            new FundSearchQuery(isin: normalized),
            1,
            cancellationToken).ConfigureAwait(false)).FirstOrDefault(item =>
                string.Equals(item.Isin, normalized, StringComparison.Ordinal));
        return result is null
            ? null
            : new Fund(result.FundIdentifier, result.FundName, this.DisplayName, result.Currency);
    }

    /// <inheritdoc />
    public async Task<FundHistory> GetHistoryAsync(
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        return (await this.GetHistoryWithProvenanceAsync(fundIdentifier, from, to, cancellationToken).ConfigureAwait(false)).History;
    }

    /// <inheritdoc />
    public async Task<FundHistoryResult> GetHistoryWithProvenanceAsync(
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var isin = ResolveIsin(fundIdentifier);
        if (!Isin.IsValid(isin))
        {
            throw new FundProviderException(FundProviderErrorKind.NotFound, "CNMV history loading requires an exact valid ISIN.");
        }

        var requestedTo = to;
        var requestedFrom = from;
        var effectiveTo = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveFrom = from ?? FirstDayOfMonth(effectiveTo.AddMonths(-Math.Max(1, this.options.DefaultHistoryMonths) + 1));
        if (effectiveFrom > effectiveTo)
        {
            throw new ArgumentException("The requested start date must not be after the end date.", nameof(from));
        }

        var firstPeriod = ToYearMonth(FirstDayOfMonth(effectiveFrom));
        var lastPeriod = ToYearMonth(FirstDayOfMonth(effectiveTo));
        var latestArchive = await this.LoadLatestArchiveAsync(cancellationToken).ConfigureAwait(false);
        var registration = this.parser.ParseRegistrations(latestArchive.GetRequiredTextEntry("FONDREGISTRO"))
            .FirstOrDefault(item => string.Equals(item.Isin, isin, StringComparison.Ordinal));
        var candidates = await this.GetCandidatesForRangeAsync(firstPeriod, lastPeriod, cancellationToken).ConfigureAwait(false);
        var allPoints = new List<NavPoint>();
        var sources = new List<string>();
        var periods = new List<string>();
        var anyCached = false;
        var cacheKeys = new List<string>();
        foreach (var candidate in candidates)
        {
            var archive = await this.DownloadArchiveAsync(candidate, cancellationToken).ConfigureAwait(false);
            var monthlyXml = archive.GetRequiredTextEntry("FONDMENS");
            var archivePoints = this.parser.ParseMonthlyNavs(monthlyXml, isin);
            if (archivePoints.Count == 0)
            {
                continue;
            }

            allPoints.AddRange(archivePoints);
            periods.Add(candidate.Period.ToString());
            sources.Add(FormatSource(archive));
            anyCached |= archive.Payload.IsFromCache;
            cacheKeys.Add(archive.Payload.CacheKey);
        }

        var filtered = allPoints
            .Where(point => point.Date >= effectiveFrom && point.Date <= effectiveTo)
            .GroupBy(point => point.Date)
            .Select(group => group.Last())
            .OrderBy(point => point.Date)
            .ToArray();
        if (filtered.Length == 0)
        {
            throw new FundProviderException(
                FundProviderErrorKind.NoUsableHistory,
                $"CNMV returned no usable NAV observations for ISIN {isin} in {effectiveFrom:yyyy-MM-dd}..{effectiveTo:yyyy-MM-dd}; monthly ZIP candidates={candidates.Count.ToString(CultureInfo.InvariantCulture)}.");
        }

        var frequency = ObservationFrequencyDetector.Detect(filtered);
        var navSeries = new NavSeries(filtered, frequency);
        var fund = new Fund(new Isin(isin).ToFundIdentifier(), registration?.FundName ?? isin, this.DisplayName, null);
        var history = new FundHistory(fund, navSeries);
        var fingerprint = this.fingerprintCalculator.CalculateSha256(navSeries);
        var sourceReference = string.Join(
            "; ",
            new[]
            {
                $"CNMV FONDMENS official XML daily VL fields",
                $"periods={string.Join(",", periods.Distinct(StringComparer.Ordinal))}",
                $"archives={sources.Count.ToString(CultureInfo.InvariantCulture)}",
                $"origins={(anyCached ? "cache/network" : "network")}",
                "A good Brier score or ReliabilityIndex is not evidence of economic profitability.",
            });
        var provenance = new FundDataProvenance(
            this.ProviderId,
            this.DisplayName,
            DateTimeOffset.UtcNow,
            fund.Identifier,
            isin,
            candidates.LastOrDefault()?.Uri,
            sourceReference,
            frequency,
            requestedFrom,
            requestedTo,
            navSeries.StartDate,
            navSeries.EndDate,
            allPoints.Count,
            navSeries.Count,
            fingerprint,
            anyCached,
            cacheKeys.Count == 0 ? null : string.Join(",", cacheKeys.Distinct(StringComparer.Ordinal).Take(6)));
        return new FundHistoryResult(history, provenance);
    }

    private static bool MatchesQuery(CnmvFundRegistration registration, FundSearchQuery query)
    {
        var isinMatch = query.Isin is not null &&
            registration.Isin.Contains(query.Isin, StringComparison.OrdinalIgnoreCase);
        if (isinMatch)
        {
            return true;
        }

        if (query.Text is null)
        {
            return false;
        }

        var fundName = FundSearchQuery.NormalizeSearchText(registration.FundName);
        var managerName = FundSearchQuery.NormalizeSearchText(registration.ManagerName);
        var className = FundSearchQuery.NormalizeSearchText(registration.ClassName);
        return ContainsNormalized(fundName, query.Text) ||
            ContainsNormalized(managerName, query.Text) ||
            ContainsNormalized(className, query.Text);
    }

    private static bool ContainsNormalized(string? value, string query)
    {
        return value is not null && value.Contains(query, StringComparison.Ordinal);
    }

    private static string ResolveIsin(FundIdentifier fundIdentifier)
    {
        if (fundIdentifier.Kind == FundIdentifierKind.Isin)
        {
            return FundSearchQuery.NormalizeIsin(fundIdentifier.Value) ?? fundIdentifier.Value;
        }

        if (fundIdentifier.Kind == FundIdentifierKind.Provider &&
            fundIdentifier.Value.StartsWith("cnmv:", StringComparison.OrdinalIgnoreCase))
        {
            return FundSearchQuery.NormalizeIsin(fundIdentifier.Value[5..]) ?? fundIdentifier.Value[5..];
        }

        return FundSearchQuery.NormalizeIsin(fundIdentifier.Value) ?? fundIdentifier.Value;
    }

    private static DateOnly FirstDayOfMonth(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    private static YearMonth ToYearMonth(DateOnly date)
    {
        return new YearMonth(date.Year, date.Month);
    }

    private async Task<CnmvArchive> LoadLatestArchiveAsync(CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        FundProviderException? firstFailure = null;
        for (var year = currentYear; year >= this.options.MinimumPublicationYear; year--)
        {
            IReadOnlyList<CnmvDocumentCandidate> candidates;
            try
            {
                candidates = await this.GetDocumentLinksAsync(year, cancellationToken).ConfigureAwait(false);
            }
            catch (FundProviderException exception) when (exception.Kind is FundProviderErrorKind.ProviderUnavailable or FundProviderErrorKind.Timeout)
            {
                firstFailure ??= exception;
                continue;
            }

            foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Period))
            {
                var archive = await this.DownloadArchiveAsync(candidate, cancellationToken).ConfigureAwait(false);
                if (archive.TryGetTextEntry("FONDREGISTRO") is not null)
                {
                    return archive;
                }
            }
        }

        var message = firstFailure is null
            ? "CNMV did not publish an IIC registration archive in the configured year range."
            : $"CNMV did not publish an IIC registration archive in the configured year range. Last provider failure: {firstFailure.Message}";
        throw new FundProviderException(firstFailure?.Kind ?? FundProviderErrorKind.NotFound, message, firstFailure);
    }

    private async Task<IReadOnlyList<CnmvDocumentCandidate>> GetCandidatesForRangeAsync(
        YearMonth firstPeriod,
        YearMonth lastPeriod,
        CancellationToken cancellationToken)
    {
        var result = new List<CnmvDocumentCandidate>();
        for (var year = firstPeriod.Year; year <= lastPeriod.Year; year++)
        {
            var candidates = await this.GetDocumentLinksAsync(year, cancellationToken).ConfigureAwait(false);
            result.AddRange(candidates.Where(candidate => candidate.Period >= firstPeriod && candidate.Period <= lastPeriod));
        }

        return result
            .GroupBy(candidate => new { candidate.Period, candidate.Uri.AbsoluteUri })
            .Select(group => group.First())
            .OrderBy(candidate => candidate.Period)
            .ThenBy(candidate => candidate.Uri.AbsoluteUri, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyList<CnmvDocumentCandidate>> GetDocumentLinksAsync(
        int year,
        CancellationToken cancellationToken)
    {
        if (this.candidateCache.TryGetValue(year, out var cached))
        {
            return cached;
        }

        var pageUri = this.options.CreateDownloadPageUri(year);
        var context = new CnmvPayloadContext(CnmvPayloadRole.IndexPage, year, null, pageUri) { Options = this.options };
        var payload = await this.DownloadPayloadAsync(pageUri, context, cancellationToken).ConfigureAwait(false);
        var html = Encoding.UTF8.GetString(payload.Content);
        var links = ExtractMonthlyDocumentCandidates(html, year, pageUri, this.options.BaseUri);
        this.candidateCache[year] = links;
        return links;
    }

    private async Task<CnmvArchive> DownloadArchiveAsync(
        CnmvDocumentCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (this.archiveCache.TryGetValue(candidate.Uri.AbsoluteUri, out var cachedArchive))
        {
            return cachedArchive;
        }

        var context = new CnmvPayloadContext(CnmvPayloadRole.MonthlyIicZip, candidate.Period.Year, candidate.Period, candidate.Uri) { Options = this.options };
        var payload = await this.DownloadPayloadAsync(candidate.Uri, context, cancellationToken).ConfigureAwait(false);
        try
        {
            var archive = CnmvArchive.FromZip(candidate, payload, this.options);
            this.archiveCache[candidate.Uri.AbsoluteUri] = archive;
            return archive;
        }
        catch (InvalidDataException exception)
        {
            throw new FundProviderException(
                FundProviderErrorKind.InvalidResponse,
                BuildDiagnostic(context, payload, $"ZIP corrupt or structurally invalid: {exception.Message}"),
                exception);
        }
    }

    private async Task<CachedProviderPayload> DownloadPayloadAsync(
        Uri uri,
        CnmvPayloadContext context,
        CancellationToken cancellationToken)
    {
        var sourceKey = uri.AbsoluteUri;
        if (this.cache is not null)
        {
            var cached = await this.cache.TryReadAsync(this.ProviderId, sourceKey, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                var enriched = cached with
                {
                    RequestedUri = uri,
                    FinalUri = uri,
                };
                var rejection = TryValidatePayload(enriched, context);
                if (rejection is null)
                {
                    return enriched;
                }

                await this.cache.InvalidateAsync(this.ProviderId, sourceKey, cancellationToken).ConfigureAwait(false);
            }
        }

        var downloaded = await this.DownloadFromNetworkAsync(uri, context, cancellationToken).ConfigureAwait(false);
        var networkRejection = TryValidatePayload(downloaded, context);
        if (networkRejection is not null)
        {
            throw new FundProviderException(
                FundProviderErrorKind.InvalidResponse,
                BuildDiagnostic(context, downloaded, networkRejection));
        }

        if (this.cache is null)
        {
            return downloaded;
        }

        var written = await this.cache.WriteAsync(
            this.ProviderId,
            sourceKey,
            downloaded.Content,
            downloaded.RetrievalTimestampUtc,
            cancellationToken).ConfigureAwait(false);
        return written with
        {
            RequestedUri = downloaded.RequestedUri,
            FinalUri = downloaded.FinalUri,
            ContentType = downloaded.ContentType,
        };
    }

    private async Task<CachedProviderPayload> DownloadFromNetworkAsync(
        Uri uri,
        CnmvPayloadContext context,
        CancellationToken cancellationToken)
    {
        var requestedUri = uri;
        var currentUri = uri;
        try
        {
            for (var redirect = 0; redirect <= Math.Max(0, this.options.MaximumRedirects); redirect++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                var finalUri = response.RequestMessage?.RequestUri ?? currentUri;
                if (IsRedirect(response.StatusCode))
                {
                    if (response.Headers.Location is null)
                    {
                        throw new FundProviderException(
                            FundProviderErrorKind.ProviderUnavailable,
                            BuildHttpDiagnostic(context, requestedUri, finalUri, response, "redirect response did not include a Location header"));
                    }

                    currentUri = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(finalUri, response.Headers.Location);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new FundProviderException(
                        FundProviderErrorKind.ProviderUnavailable,
                        BuildHttpDiagnostic(context, requestedUri, finalUri, response, $"HTTP {(int)response.StatusCode}"));
                }

                if (response.Content.Headers.ContentLength > this.options.MaximumHttpPayloadBytes)
                {
                    throw new FundProviderException(
                        FundProviderErrorKind.InvalidResponse,
                        BuildHttpDiagnostic(context, requestedUri, finalUri, response, "response exceeded the configured size limit"));
                }

                var bytes = await ReadLimitedAsync(response.Content, this.options.MaximumHttpPayloadBytes, cancellationToken).ConfigureAwait(false);
                return new CachedProviderPayload(
                    bytes,
                    DateTimeOffset.UtcNow,
                    false,
                    LocalProviderCache.CreateCacheKey(this.ProviderId, requestedUri.AbsoluteUri),
                    requestedUri,
                    finalUri,
                    response.Content.Headers.ContentType?.MediaType);
            }

            throw new FundProviderException(
                FundProviderErrorKind.ProviderUnavailable,
                $"CNMV request followed too many redirects; year={context.Year?.ToString(CultureInfo.InvariantCulture) ?? "n/a"} requested={requestedUri.AbsoluteUri}");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FundProviderException(FundProviderErrorKind.Timeout, $"CNMV request timed out; requested={requestedUri.AbsoluteUri}.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new FundProviderException(FundProviderErrorKind.ProviderUnavailable, $"CNMV is unavailable or the network request failed; requested={requestedUri.AbsoluteUri}.", exception);
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code is >= 300 and <= 399;
    }

    private static string? TryValidatePayload(CachedProviderPayload payload, CnmvPayloadContext context)
    {
        if (payload.Content.LongLength == 0)
        {
            return "payload is empty";
        }

        if (payload.Content.LongLength > 0 && payload.Content.LongLength > long.MaxValue)
        {
            return "payload length is invalid";
        }

        return context.Role switch
        {
            CnmvPayloadRole.IndexPage => TryValidateIndexPayload(payload),
            CnmvPayloadRole.MonthlyIicZip => TryValidateZipPayload(payload, context),
            _ => "unknown CNMV payload role",
        };
    }

    private static string? TryValidateIndexPayload(CachedProviderPayload payload)
    {
        var mediaType = NormalizeMediaType(payload.ContentType);
        if (mediaType is not null && mediaType is not "text/html" and not "application/xhtml+xml")
        {
            return $"index page has unsupported Content-Type '{payload.ContentType}'";
        }

        var prefix = Encoding.UTF8.GetString(payload.Content.AsSpan(0, Math.Min(payload.Content.Length, 512)));
        if (!prefix.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) &&
            !prefix.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            return "index page is not HTML";
        }

        return null;
    }

    private static string? TryValidateZipPayload(CachedProviderPayload payload, CnmvPayloadContext context)
    {
        var mediaType = NormalizeMediaType(payload.ContentType);
        if (mediaType is "text/html" or "application/xhtml+xml")
        {
            return "expected monthly IIC ZIP but received HTML";
        }

        if (mediaType is not null &&
            mediaType is not "application/zip" and not "application/octet-stream" and not "application/x-zip-compressed" and not "binary/octet-stream")
        {
            return $"monthly IIC ZIP has unsupported Content-Type '{payload.ContentType}'";
        }

        if (payload.FinalUri is not null &&
            payload.FinalUri.AbsolutePath.Contains("descarga-informacion-individual", StringComparison.OrdinalIgnoreCase))
        {
            return "expected monthly IIC ZIP but final URI is the HTML index page";
        }

        if (!HasZipSignature(payload.Content))
        {
            return LooksLikeHtml(payload.Content)
                ? "expected monthly IIC ZIP but received an HTML/error response with HTTP 200"
                : "ZIP signature is missing";
        }

        try
        {
            ValidateZipArchive(payload.Content, context);
            return null;
        }
        catch (InvalidDataException exception)
        {
            return exception.Message;
        }
    }

    private static void ValidateZipArchive(byte[] content, CnmvPayloadContext context)
    {
        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        if (archive.Entries.Count > context.Options.MaximumZipEntries)
        {
            throw new InvalidDataException($"ZIP has too many entries ({archive.Entries.Count.ToString(CultureInfo.InvariantCulture)} > {context.Options.MaximumZipEntries.ToString(CultureInfo.InvariantCulture)}).");
        }

        var hasFondmens = false;
        var totalUncompressedBytes = 0L;
        foreach (var entry in archive.Entries)
        {
            var isFondmens = IsExpectedXmlEntry(entry, "FONDMENS", context.Period);
            var isRegistration = IsExpectedXmlEntry(entry, "FONDREGISTRO", context.Period);
            if (!isFondmens && !isRegistration)
            {
                continue;
            }

            ValidateZipEntry(entry, context.Options, ref totalUncompressedBytes);
            if (isFondmens)
            {
                hasFondmens = true;
            }
        }

        if (!hasFondmens)
        {
            throw new InvalidDataException($"ZIP is valid but does not contain expected FONDMENS XML for period {context.Period?.ToString() ?? "n/a"}.");
        }
    }

    private static IReadOnlyList<CnmvDocumentCandidate> ExtractMonthlyDocumentCandidates(
        string html,
        int year,
        Uri pageUri,
        Uri baseUri)
    {
        var tableMatch = DownloadTablePattern.Match(html);
        var scope = tableMatch.Success ? tableMatch.Value : html;
        var candidates = new List<CnmvDocumentCandidate>();
        foreach (Match rowMatch in RowPattern.Matches(scope))
        {
            var row = rowMatch.Groups["row"].Value;
            var cells = CellPattern.Matches(row).Cast<Match>().Select(match => match.Groups["cell"].Value).ToArray();
            if (cells.Length < 2 || !TryResolveMonth(StripHtml(cells[0]), out var month))
            {
                continue;
            }

            var linkMatch = LinkPattern.Match(cells[1]);
            if (!linkMatch.Success)
            {
                continue;
            }

            var href = WebUtility.HtmlDecode(linkMatch.Groups["href"].Value).ReplaceLineEndings(string.Empty).Trim();
            if (href.Length == 0 || !href.Contains("/webservices/verdocumento/ver", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryCreateUri(baseUri, pageUri, href, out var documentUri))
            {
                continue;
            }

            var attributes = string.Join(" ", AttributePattern.Matches(cells[1]).Cast<Match>().Select(match => WebUtility.HtmlDecode(match.Groups["value"].Value)));
            if (attributes.Length > 0 && !ContainsMonthLabel(attributes, month))
            {
                continue;
            }

            candidates.Add(new CnmvDocumentCandidate(documentUri, new YearMonth(year, month), StripHtml(cells[0])));
        }

        return candidates
            .GroupBy(candidate => new { candidate.Period, candidate.Uri.AbsoluteUri })
            .Select(group => group.First())
            .OrderBy(candidate => candidate.Period)
            .ThenBy(candidate => candidate.Uri.AbsoluteUri, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryCreateUri(Uri baseUri, Uri pageUri, string raw, out Uri uri)
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out uri!))
        {
            return true;
        }

        if (Uri.TryCreate(pageUri, raw, out uri!))
        {
            return true;
        }

        return Uri.TryCreate(baseUri, raw, out uri!);
    }

    private static bool ContainsMonthLabel(string value, int expectedMonth)
    {
        foreach (var part in value.Split([' ', '-', '_', '/', '\\', '.', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryResolveMonth(part, out var month) && month == expectedMonth)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveMonth(string value, out int month)
    {
        var normalized = NormalizeMonthText(value);
        month = normalized switch
        {
            "ENERO" or "JANUARY" or "GENER" or "XANEIRO" or "URTARRILA" => 1,
            "FEBRERO" or "FEBRUARY" or "FEBRER" or "FEBREIRO" or "OTSAILA" => 2,
            "MARZO" or "MARCH" or "MARC" or "MARZ" or "MARTXOA" => 3,
            "ABRIL" or "APRIL" or "APIRILA" => 4,
            "MAYO" or "MAY" or "MAIG" or "MAIO" or "MAIATZA" => 5,
            "JUNIO" or "JUNE" or "JUNY" or "XUNO" or "EKAINA" => 6,
            "JULIO" or "JULY" or "JULIOL" or "XULLO" or "UZTAILA" => 7,
            "AGOSTO" or "AUGUST" or "AGOST" or "ABUZTUA" => 8,
            "SEPTIEMBRE" or "SEPTEMBER" or "SETEMBRE" or "SETEMBRO" or "IRAILA" => 9,
            "OCTUBRE" or "OCTOBER" or "OUTUBRO" or "URRIA" => 10,
            "NOVIEMBRE" or "NOVEMBER" or "NOVEMBRE" or "AZAROA" => 11,
            "DICIEMBRE" or "DECEMBER" or "DESEMBRE" or "DECEMBRO" or "ABENDUA" => 12,
            _ => 0,
        };
        return month != 0;
    }

    private static string NormalizeMonthText(string value)
    {
        var decoded = WebUtility.HtmlDecode(value);
        var normalized = decoded.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark && char.IsLetter(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string StripHtml(string value)
    {
        return WebUtility.HtmlDecode(HtmlTagPattern.Replace(value, " ")).Trim();
    }

    private static string? NormalizeMediaType(string? mediaType)
    {
        return string.IsNullOrWhiteSpace(mediaType) ? null : mediaType.Trim().ToLowerInvariant();
    }

    private static bool HasZipSignature(byte[] bytes)
    {
        return bytes.Length >= 4 &&
            bytes[0] == 0x50 &&
            bytes[1] == 0x4B &&
            bytes[2] is 0x03 or 0x05 or 0x07 &&
            bytes[3] is 0x04 or 0x06 or 0x08;
    }

    private static bool LooksLikeHtml(byte[] bytes)
    {
        var prefix = Encoding.UTF8.GetString(bytes.AsSpan(0, Math.Min(bytes.Length, 256))).TrimStart();
        return prefix.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("<", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<byte[]> ReadLimitedAsync(
        HttpContent content,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        if (maximumBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes), maximumBytes, "Maximum HTTP payload size must be positive.");
        }

        await using var source = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var target = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (target.Length + read > maximumBytes)
            {
                throw new FundProviderException(FundProviderErrorKind.InvalidResponse, "CNMV response exceeded the configured size limit.");
            }

            target.Write(buffer, 0, read);
        }

        return target.ToArray();
    }

    private static string BuildDiagnostic(CnmvPayloadContext context, CachedProviderPayload payload, string reason)
    {
        var source = payload.IsFromCache ? "cache" : "network";
        return $"CNMV {RoleName(context.Role)} rejected: year={context.Year?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}; period={context.Period?.ToString() ?? "n/a"}; requested={payload.RequestedUri?.AbsoluteUri ?? context.RequestedUri.AbsoluteUri}; final={payload.FinalUri?.AbsoluteUri ?? "n/a"}; contentType={payload.ContentType ?? "n/a"}; bytes={payload.Content.LongLength.ToString(CultureInfo.InvariantCulture)}; source={source}; reason={reason}.";
    }

    private static string BuildHttpDiagnostic(
        CnmvPayloadContext context,
        Uri requestedUri,
        Uri finalUri,
        HttpResponseMessage response,
        string reason)
    {
        return $"CNMV {RoleName(context.Role)} failed: year={context.Year?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}; period={context.Period?.ToString() ?? "n/a"}; requested={requestedUri.AbsoluteUri}; final={finalUri.AbsoluteUri}; status={(int)response.StatusCode}; contentType={response.Content.Headers.ContentType?.MediaType ?? "n/a"}; source=network; reason={reason}.";
    }

    private static string RoleName(CnmvPayloadRole role)
    {
        return role == CnmvPayloadRole.IndexPage ? "index HTML" : "monthly ZIP";
    }

    private static string FormatSource(CnmvArchive archive)
    {
        var source = archive.Payload.IsFromCache ? "cache" : "network";
        return $"{archive.Period}; requested={archive.Payload.RequestedUri?.AbsoluteUri ?? archive.SourceUri.AbsoluteUri}; final={archive.Payload.FinalUri?.AbsoluteUri ?? "n/a"}; source={source}";
    }

    private static bool IsExpectedXmlEntry(ZipArchiveEntry entry, string prefix, YearMonth? period)
    {
        if (!entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(entry.Name);
        if (name.Equals(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!name.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return period is null || TryParsePeriod(entry.Name) == period;
    }

    private static void ValidateZipEntry(
        ZipArchiveEntry entry,
        CnmvIicProviderOptions options,
        ref long totalUncompressedBytes)
    {
        if (entry.Length > options.MaximumZipEntryBytes)
        {
            throw new InvalidDataException($"ZIP entry '{entry.FullName}' exceeded the configured decompressed size limit.");
        }

        if (entry.CompressedLength <= 0 && entry.Length > 0)
        {
            throw new InvalidDataException($"ZIP entry '{entry.FullName}' has invalid compressed length metadata.");
        }

        if (entry.CompressedLength > 0)
        {
            var ratio = entry.Length / (double)entry.CompressedLength;
            if (ratio > options.MaximumZipCompressionRatio)
            {
                throw new InvalidDataException($"ZIP entry '{entry.FullName}' compression ratio exceeded the configured limit.");
            }
        }

        totalUncompressedBytes += entry.Length;
        if (totalUncompressedBytes > options.MaximumZipTotalBytes)
        {
            throw new InvalidDataException("ZIP archive exceeded the configured total decompressed size limit.");
        }
    }

    private static string ReadZipEntryText(ZipArchiveEntry entry, long maximumBytes)
    {
        using var entryStream = entry.Open();
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = entryStream.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (output.Length + read > maximumBytes)
            {
                throw new InvalidDataException($"ZIP entry '{entry.FullName}' exceeded the configured decompressed size limit.");
            }

            output.Write(buffer, 0, read);
        }

        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static YearMonth? TryParsePeriod(string fileName)
    {
        var match = Regex.Match(fileName, "_(?<period>[0-9]{6})\\.xml$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["period"].Value;
        return int.TryParse(value[..4], NumberStyles.None, CultureInfo.InvariantCulture, out var year) &&
            int.TryParse(value[4..], NumberStyles.None, CultureInfo.InvariantCulture, out var month) &&
            month is >= 1 and <= 12
            ? new YearMonth(year, month)
            : null;
    }

    private readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
    {
        public int CompareTo(YearMonth other)
        {
            var year = this.Year.CompareTo(other.Year);
            return year != 0 ? year : this.Month.CompareTo(other.Month);
        }

        public override string ToString()
        {
            return $"{this.Year.ToString("0000", CultureInfo.InvariantCulture)}-{this.Month.ToString("00", CultureInfo.InvariantCulture)}";
        }

        public static bool operator <(YearMonth left, YearMonth right) => left.CompareTo(right) < 0;

        public static bool operator >(YearMonth left, YearMonth right) => left.CompareTo(right) > 0;

        public static bool operator <=(YearMonth left, YearMonth right) => left.CompareTo(right) <= 0;

        public static bool operator >=(YearMonth left, YearMonth right) => left.CompareTo(right) >= 0;
    }

    private enum CnmvPayloadRole
    {
        IndexPage,
        MonthlyIicZip,
    }

    private sealed record CnmvDocumentCandidate(Uri Uri, YearMonth Period, string Label);

    private sealed record CnmvPayloadContext(
        CnmvPayloadRole Role,
        int? Year,
        YearMonth? Period,
        Uri RequestedUri)
    {
        public CnmvIicProviderOptions Options { get; init; } = new();
    }

    private sealed class CnmvArchive
    {
        private readonly Dictionary<string, string> entries;

        private CnmvArchive(
            CnmvDocumentCandidate candidate,
            CachedProviderPayload payload,
            Dictionary<string, string> entries)
        {
            this.SourceUri = candidate.Uri;
            this.Payload = payload;
            this.entries = entries;
            this.Period = candidate.Period;
        }

        public Uri SourceUri { get; }

        public CachedProviderPayload Payload { get; }

        public YearMonth Period { get; }

        public static CnmvArchive FromZip(
            CnmvDocumentCandidate candidate,
            CachedProviderPayload payload,
            CnmvIicProviderOptions options)
        {
            using var stream = new MemoryStream(payload.Content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (archive.Entries.Count > options.MaximumZipEntries)
            {
                throw new InvalidDataException("CNMV ZIP archive contains too many entries.");
            }

            var totalUncompressedBytes = 0L;
            foreach (var entry in archive.Entries)
            {
                var isFondmens = IsExpectedXmlEntry(entry, "FONDMENS", candidate.Period);
                var isRegistration = IsExpectedXmlEntry(entry, "FONDREGISTRO", candidate.Period);
                if (isFondmens || isRegistration)
                {
                    ValidateZipEntry(entry, options, ref totalUncompressedBytes);
                    var key = isFondmens ? "FONDMENS" : "FONDREGISTRO";
                    entries[key] = ReadZipEntryText(entry, options.MaximumZipEntryBytes);
                }
            }

            if (!entries.ContainsKey("FONDMENS"))
            {
                throw new InvalidDataException($"CNMV ZIP is valid but does not contain FONDMENS XML for {candidate.Period}.");
            }

            return new CnmvArchive(candidate, payload, entries);
        }

        public string GetRequiredTextEntry(string prefix)
        {
            return this.TryGetTextEntry(prefix)
                ?? throw new FundProviderException(FundProviderErrorKind.InvalidResponse, $"CNMV archive for {this.Period} does not contain {prefix} XML.");
        }

        public string? TryGetTextEntry(string prefix)
        {
            return this.entries.TryGetValue(prefix, out var value) ? value : null;
        }
    }
}
