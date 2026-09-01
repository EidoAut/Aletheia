#pragma warning disable SA1204 // Test fixtures and member data are grouped with the tests they exercise.

using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Aletheia.Core;
using Aletheia.Data;

namespace Aletheia.Data.Tests;

public sealed class CnmvIicProviderTests
{
    private const int FixtureYear = 2026;
    private const string ValidIsin = "ES0000000001";
    private const string OtherIsin = "ES0000000002";
    private static readonly Uri BaseUri = new("https://cnmv.test/");

    [Fact]
    public async Task SearchAsync_ParsesCurrentDownloadPageAndLoadsValidMonthlyZip()
    {
        var janUrl = DocumentUrl(1);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
            [janUrl] = () => ZipResponse(MonthlyZip(2026, 1, [100m, 101m])),
        });
        var provider = CreateProvider(handler);

        var results = await provider.SearchAsync(FundSearchQuery.FromUserText("alfa"), maximumResults: 5);

        var result = Assert.Single(results);
        Assert.Equal(ValidIsin, result.Isin);
        Assert.Equal("FONDO ALFA GLOBAL", result.FundName);
        Assert.Equal("GESTORA ALFA", result.ManagementCompany);
        Assert.True(result.HasHistoricalData);
        Assert.Equal(new DateOnly(2026, 1, 2), result.LatestAvailableObservation);
        Assert.Equal(1, handler.Count(IndexUrl()));
        Assert.Equal(1, handler.Count(janUrl));
    }

    [Fact]
    public async Task SearchAsync_FollowsRedirectedDownloadPage()
    {
        var janUrl = DocumentUrl(1);
        var redirectedIndexUrl = $"{BaseUri.AbsoluteUri}portal/publicaciones/descarga-informacion-individual.aspx?ejercicio=2026";
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => RedirectResponse(redirectedIndexUrl),
            [redirectedIndexUrl] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
            [janUrl] = () => ZipResponse(MonthlyZip(2026, 1, [100m, 101m])),
        });
        var provider = CreateProvider(handler);

        var results = await provider.SearchAsync(FundSearchQuery.FromUserText(ValidIsin), maximumResults: 5);

        Assert.Single(results);
        Assert.Equal(1, handler.Count(IndexUrl()));
        Assert.Equal(1, handler.Count(redirectedIndexUrl));
        Assert.Equal(1, handler.Count(janUrl));
    }

    [Fact]
    public async Task SearchAsync_IgnoresNonMonthlyAndNonDocumentRows()
    {
        var janUrl = DocumentUrl(1);
        var badUrl = $"{BaseUri.AbsoluteUri}portal/publicaciones/manual";
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(
                "<tr><td>Manual</td><td><a href=\"/webservices/verdocumento/ver?manual=1\" title=\"Manual\">Manual</a></td></tr>",
                $"<tr><td>Febrero</td><td><a href=\"{badUrl}\" title=\"Febrero\">Bad</a></td></tr>",
                MonthRow("Enero", janUrl))),
            [janUrl] = () => ZipResponse(MonthlyZip(2026, 1, [100m, 101m])),
        });
        var provider = CreateProvider(handler);

        var results = await provider.SearchAsync(FundSearchQuery.FromUserText("GESTORA"), maximumResults: 5);

        Assert.Single(results);
        Assert.Equal(0, handler.Count(badUrl));
        Assert.Equal(0, handler.RequestedUris.Count(uri => uri.Contains("manual=1", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SearchAsync_IgnoresOversizedIrrelevantXmlEntriesInsideValidMonthlyZip()
    {
        var janUrl = DocumentUrl(1);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
            [janUrl] = () => ZipResponse(MonthlyZipWithIrrelevantLargeXml(2026, 1)),
        });
        var provider = CreateProvider(handler, maximumZipEntryBytes: 2_048);

        var results = await provider.SearchAsync(FundSearchQuery.FromUserText("alfa"), maximumResults: 5);

        Assert.Single(results);
        Assert.Equal(1, handler.Count(janUrl));
    }

    [Fact]
    public async Task SearchAsync_AcceptsCanonicalMonthlyZipEntriesWithoutPeriodSuffix()
    {
        var janUrl = DocumentUrl(1);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
            [janUrl] = () => ZipResponse(MonthlyZipWithoutPeriodSuffix(2026, 1)),
        });
        var provider = CreateProvider(handler);

        var results = await provider.SearchAsync(FundSearchQuery.FromUserText("alfa"), maximumResults: 5);

        Assert.Single(results);
        Assert.Equal(1, handler.Count(janUrl));
    }

    [Fact]
    public async Task GetHistoryWithProvenanceAsync_DownloadsOnlyRequestedMonthsAndDeduplicatesObservations()
    {
        var janUrl = DocumentUrl(1);
        var febUrl = DocumentUrl(2);
        var marUrl = DocumentUrl(3);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(
                MonthRow("Enero", janUrl),
                MonthRow("Febrero", febUrl),
                MonthRow("Marzo", marUrl))),
            [janUrl] = () => ZipResponse(MonthlyZip(2026, 1, [100m, 101m])),
            [febUrl] = () => ZipResponse(MonthlyZip(2026, 2, [102m, 103m])),
            [marUrl] = () => ZipResponse(MonthlyZip(2026, 3, [104m, 105m])),
        });
        var provider = CreateProvider(handler);

        var result = await provider.GetHistoryWithProvenanceAsync(
            new FundIdentifier(FundIdentifierKind.Isin, ValidIsin),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 2));

        Assert.Equal(4, result.History.NavSeries.Count);
        Assert.Equal(new DateOnly(2026, 2, 1), result.History.NavSeries.StartDate);
        Assert.Equal(new DateOnly(2026, 3, 2), result.History.NavSeries.EndDate);
        Assert.Equal(0, handler.Count(janUrl));
        Assert.Equal(1, handler.Count(febUrl));
        Assert.Equal(1, handler.Count(marUrl));
        Assert.False(result.Provenance.IsFromCache);
        Assert.Contains("A good Brier score", result.Provenance.SourceReference, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetHistoryWithProvenanceAsync_ReturnsNoUsableHistoryForUnknownIsin()
    {
        var janUrl = DocumentUrl(1);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
            [janUrl] = () => ZipResponse(MonthlyZip(2026, 1, [100m, 101m])),
        });
        var provider = CreateProvider(handler);

        var exception = await Assert.ThrowsAsync<FundProviderException>(async () =>
            await provider.GetHistoryWithProvenanceAsync(
                new FundIdentifier(FundIdentifierKind.Isin, OtherIsin),
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31)));

        Assert.Equal(FundProviderErrorKind.NoUsableHistory, exception.Kind);
        Assert.Contains(OtherIsin, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_UsesValidCachedMonthlyZipWithoutNetworkDownload()
    {
        var cacheDirectory = CreateTempDirectory();
        var janUrl = DocumentUrl(1);
        var cache = new LocalProviderCache(cacheDirectory);
        await cache.WriteAsync("cnmv-iic", janUrl, MonthlyZip(2026, 1, [100m, 101m]), DateTimeOffset.UtcNow);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
        });
        var provider = CreateProvider(handler, cacheDirectory);

        try
        {
            var results = await provider.SearchAsync(FundSearchQuery.FromUserText(ValidIsin), maximumResults: 5);

            Assert.Single(results);
            Assert.Equal(0, handler.Count(janUrl));
        }
        finally
        {
            DeleteDirectory(cacheDirectory);
        }
    }

    [Fact]
    public async Task SearchAsync_InvalidatesHtmlContaminatedCacheBeforeWritingValidZip()
    {
        var cacheDirectory = CreateTempDirectory();
        var janUrl = DocumentUrl(1);
        var cache = new LocalProviderCache(cacheDirectory);
        await cache.WriteAsync("cnmv-iic", janUrl, Encoding.UTF8.GetBytes("<html>Error</html>"), DateTimeOffset.UtcNow);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
            [janUrl] = () => ZipResponse(MonthlyZip(2026, 1, [100m, 101m])),
        });
        var provider = CreateProvider(handler, cacheDirectory);

        try
        {
            var results = await provider.SearchAsync(FundSearchQuery.FromUserText(ValidIsin), maximumResults: 5);
            var cached = await cache.TryReadAsync("cnmv-iic", janUrl);

            Assert.Single(results);
            Assert.Equal(1, handler.Count(janUrl));
            Assert.NotNull(cached);
            Assert.StartsWith("PK", Encoding.ASCII.GetString(cached!.Content, 0, 2), StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(cacheDirectory);
        }
    }

    [Theory]
    [MemberData(nameof(RejectedMonthlyZipPayloads))]
    public async Task SearchAsync_RejectsInvalidMonthlyZipPayloads(
        string contentType,
        byte[] payload,
        int? maximumZipEntries,
        long? maximumZipEntryBytes,
        double? maximumZipCompressionRatio,
        string expectedMessage)
    {
        var janUrl = DocumentUrl(1);
        var handler = new StubHttpMessageHandler(new Dictionary<string, Func<HttpResponseMessage>>(StringComparer.Ordinal)
        {
            [IndexUrl()] = () => HtmlResponse(IndexHtml(MonthRow("Enero", janUrl))),
            [janUrl] = () => BytesResponse(payload, contentType),
        });
        var provider = CreateProvider(
            handler,
            maximumZipEntries: maximumZipEntries,
            maximumZipEntryBytes: maximumZipEntryBytes,
            maximumZipCompressionRatio: maximumZipCompressionRatio);

        var exception = await Assert.ThrowsAsync<FundProviderException>(async () =>
            await provider.SearchAsync(FundSearchQuery.FromUserText(ValidIsin), maximumResults: 5));

        Assert.Equal(FundProviderErrorKind.InvalidResponse, exception.Kind);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("year=2026", exception.Message, StringComparison.Ordinal);
        Assert.Contains("period=2026-01", exception.Message, StringComparison.Ordinal);
        Assert.Contains($"requested={janUrl}", exception.Message, StringComparison.Ordinal);
        Assert.Contains("source=network", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_ClassifiesProviderTimeout()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new TaskCanceledException("synthetic timeout"));
        var provider = CreateProvider(handler);

        var exception = await Assert.ThrowsAsync<FundProviderException>(async () =>
            await provider.SearchAsync(FundSearchQuery.FromUserText(ValidIsin), maximumResults: 5));

        Assert.Equal(FundProviderErrorKind.Timeout, exception.Kind);
        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Integration test: requires live CNMV network access and a current valid IIC ISIN.")]
    public async Task CnmvProvider_Live_SearchesAndLoadsFundHistory_WhenNetworkIsAvailable()
    {
        var provider = new CnmvIicProvider();
        var searchResults = await provider.SearchAsync(FundSearchQuery.FromUserText("SANTANDER"), maximumResults: 5);
        var result = Assert.Single(searchResults.Take(1));

        var history = await provider.GetHistoryWithProvenanceAsync(
            result.FundIdentifier,
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
            DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.True(history.History.NavSeries.Count > 0);
        Assert.Equal("cnmv-iic", history.Provenance.ProviderId);
    }

    public static IEnumerable<object[]> RejectedMonthlyZipPayloads()
    {
        yield return new object[]
        {
            "text/html",
            Encoding.UTF8.GetBytes("<html><body>error page</body></html>"),
            null!,
            null!,
            null!,
            "expected monthly IIC ZIP but received HTML",
        };
        yield return new object[]
        {
            "application/pdf",
            MonthlyZip(2026, 1, [100m, 101m]),
            null!,
            null!,
            null!,
            "unsupported Content-Type",
        };
        yield return new object[]
        {
            "application/zip",
            Encoding.UTF8.GetBytes("not a zip"),
            null!,
            null!,
            null!,
            "ZIP signature is missing",
        };
        yield return new object[]
        {
            "application/zip",
            new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 },
            null!,
            null!,
            null!,
            "corrupt",
        };
        yield return new object[]
        {
            "application/zip",
            ZipWithoutMonthlyXml(2026, 1),
            null!,
            null!,
            null!,
            "does not contain expected FONDMENS",
        };
        yield return new object[]
        {
            "application/zip",
            ZipWithExtraEntries(2026, 1, 9),
            8,
            null!,
            null!,
            "too many entries",
        };
        yield return new object[]
        {
            "application/zip",
            MonthlyZip(2026, 1, [100m, 101m], repeatedPaddingCharacters: 8_000),
            null!,
            null!,
            2d,
            "compression ratio exceeded",
        };
        yield return new object[]
        {
            "application/zip",
            MonthlyZip(2026, 1, [100m, 101m]),
            null!,
            64L,
            null!,
            "decompressed size limit",
        };
    }

    private static CnmvIicProvider CreateProvider(
        StubHttpMessageHandler handler,
        string? cacheDirectory = null,
        int? maximumZipEntries = null,
        long? maximumZipEntryBytes = null,
        double? maximumZipCompressionRatio = null)
    {
        var options = new CnmvIicProviderOptions
        {
            BaseUri = BaseUri,
            EnableCache = cacheDirectory is not null,
            CacheDirectory = cacheDirectory ?? Path.Combine(Path.GetTempPath(), "Aletheia.Tests", Guid.NewGuid().ToString("N")),
            MinimumPublicationYear = FixtureYear,
            MaximumRedirects = 2,
            MaximumZipEntries = maximumZipEntries ?? new CnmvIicProviderOptions().MaximumZipEntries,
            MaximumZipEntryBytes = maximumZipEntryBytes ?? (24L * 1024L * 1024L),
            MaximumZipCompressionRatio = maximumZipCompressionRatio ?? 100d,
        };
        return new CnmvIicProvider(new HttpClient(handler, disposeHandler: false), options);
    }

    private static string IndexUrl()
    {
        return $"{BaseUri.AbsoluteUri}portal/publicaciones/descarga-informacion-individual?ejercicio=2026&lang=es";
    }

    private static string DocumentUrl(int month)
    {
        return $"{BaseUri.AbsoluteUri}webservices/verdocumento/ver?year=2026&month={month.ToString("00", System.Globalization.CultureInfo.InvariantCulture)}";
    }

    private static string IndexHtml(params string[] rows)
    {
        return $"""
            <!DOCTYPE html>
            <html>
              <body>
                <table id="ctl00_ContentPrincipal_grdDescargas">
                  <tbody>
                    {string.Join(Environment.NewLine, rows)}
                  </tbody>
                </table>
              </body>
            </html>
            """;
    }

    private static string MonthRow(string monthName, string href)
    {
        return $"""
            <tr>
              <td>{monthName}</td>
              <td>
                <a href="{href}" title="{monthName}"><img alt="{monthName}" /></a>
              </td>
            </tr>
            """;
    }

    private static byte[] MonthlyZip(
        int year,
        int month,
        IReadOnlyList<decimal> values,
        int repeatedPaddingCharacters = 0)
    {
        return CreateZip(
            ($"FONDMENS_{year}{month:00}.xml", MonthlyXml(year, month, values, repeatedPaddingCharacters)),
            ($"FONDREGISTRO_{year}{month:00}.xml", RegistrationXml(year, month)));
    }

    private static byte[] ZipWithoutMonthlyXml(int year, int month)
    {
        return CreateZip(($"FONDREGISTRO_{year}{month:00}.xml", RegistrationXml(year, month)));
    }

    private static byte[] MonthlyZipWithIrrelevantLargeXml(int year, int month)
    {
        return CreateZip(
            ($"FONDMENS_{year}{month:00}.xml", MonthlyXml(year, month, [100m, 101m], 0)),
            ($"FONDREGISTRO_{year}{month:00}.xml", RegistrationXml(year, month)),
            ($"FONDCART_{year}{month:00}.xml", $"<Datos>{new string('x', 12_000)}</Datos>"));
    }

    private static byte[] MonthlyZipWithoutPeriodSuffix(int year, int month)
    {
        return CreateZip(
            ("FONDMENS.XML", MonthlyXml(year, month, [100m, 101m], 0)),
            ("FONDREGISTRO.XML", RegistrationXml(year, month)));
    }

    private static byte[] ZipWithExtraEntries(int year, int month, int extraEntryCount)
    {
        var entries = new List<(string Name, string Content)>
        {
            ($"FONDMENS_{year}{month:00}.xml", MonthlyXml(year, month, [100m, 101m], 0)),
            ($"FONDREGISTRO_{year}{month:00}.xml", RegistrationXml(year, month)),
        };
        for (var index = 0; index < extraEntryCount; index++)
        {
            entries.Add(($"EXTRA_{index}.xml", "<Datos />"));
        }

        return CreateZip(entries.ToArray());
    }

    private static byte[] CreateZip(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(content);
            }
        }

        return stream.ToArray();
    }

    private static string RegistrationXml(int year, int month)
    {
        return $"""
            <Datos>
              <FechaDatos>{year}{month:00}</FechaDatos>
              <Entidad>
                <Tipo>FI</Tipo>
                <NumeroRegistro>123</NumeroRegistro>
                <Denominacion>FONDO ALFA GLOBAL</Denominacion>
                <Gestora>
                  <DenominacionGestora>GESTORA ALFA</DenominacionGestora>
                </Gestora>
                <Compartimento>
                  <NumeroCompartimento>1</NumeroCompartimento>
                  <Clase>
                    <NumeroClase>1</NumeroClase>
                    <DenominacionClase>Clase A</DenominacionClase>
                    <ISIN>{ValidIsin}</ISIN>
                  </Clase>
                </Compartimento>
              </Entidad>
            </Datos>
            """;
    }

    private static string MonthlyXml(
        int year,
        int month,
        IReadOnlyList<decimal> values,
        int repeatedPaddingCharacters)
    {
        var padding = repeatedPaddingCharacters <= 0 ? string.Empty : new string('x', repeatedPaddingCharacters);
        var fields = string.Join(
            Environment.NewLine,
            values.Select((value, index) =>
                $"                  <VL_Dia{index + 1}>{value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}</VL_Dia{index + 1}>"));
        return $"""
            <Datos>
              <FechaDatos>{year}{month:00}</FechaDatos>
              <Entidad>
                <Compartimento>
                  <Clase>
                    <ISIN>{ValidIsin}</ISIN>
                    <VLDiario>
            {fields}
                    </VLDiario>
                    <Notas>{padding}</Notas>
                  </Clase>
                </Compartimento>
              </Entidad>
            </Datos>
            """;
    }

    private static HttpResponseMessage HtmlResponse(string html)
    {
        return BytesResponse(Encoding.UTF8.GetBytes(html), "text/html");
    }

    private static HttpResponseMessage ZipResponse(byte[] bytes)
    {
        return BytesResponse(bytes, "application/zip");
    }

    private static HttpResponseMessage BytesResponse(byte[] bytes, string contentType)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return response;
    }

    private static HttpResponseMessage RedirectResponse(string location)
    {
        return new HttpResponseMessage(HttpStatusCode.Found)
        {
            Headers =
            {
                Location = new Uri(location, UriKind.RelativeOrAbsolute),
            },
        };
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Aletheia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder;
        private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);
        private readonly List<string> requestedUris = [];

        public StubHttpMessageHandler(IReadOnlyDictionary<string, Func<HttpResponseMessage>> routes)
            : this((request, _) =>
            {
                var uri = request.RequestUri?.AbsoluteUri ?? string.Empty;
                return routes.TryGetValue(uri, out var response)
                    ? response()
                    : new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent($"No stub route for {uri}"),
                    };
            })
        {
        }

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        {
            this.responder = responder;
        }

        public IReadOnlyList<string> RequestedUris => this.requestedUris;

        public int Count(string uri)
        {
            return this.counts.TryGetValue(uri, out var count) ? count : 0;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var uri = request.RequestUri?.AbsoluteUri ?? string.Empty;
            this.requestedUris.Add(uri);
            this.counts[uri] = this.Count(uri) + 1;
            return Task.FromResult(this.responder(request, cancellationToken));
        }
    }
}
