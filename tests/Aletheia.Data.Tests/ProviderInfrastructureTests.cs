using System.Text;
using Aletheia.Data;

namespace Aletheia.Data.Tests;

public sealed class ProviderInfrastructureTests
{
    [Fact]
    public async Task LocalProviderCache_RoundTripsPayloadWithStableKey()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Aletheia.Tests", Guid.NewGuid().ToString("N"));
        var cache = new LocalProviderCache(directory);
        var retrievedAt = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var bytes = Encoding.UTF8.GetBytes("official payload");

        try
        {
            var written = await cache.WriteAsync("cnmv-iic", "https://example.test/source", bytes, retrievedAt);
            var read = await cache.TryReadAsync("cnmv-iic", "https://example.test/source");

            Assert.NotNull(read);
            Assert.False(written.IsFromCache);
            Assert.True(read!.IsFromCache);
            Assert.Equal(written.CacheKey, read.CacheKey);
            Assert.Equal(retrievedAt, read.RetrievalTimestampUtc);
            Assert.Equal(bytes, read.Content);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task LocalProviderCache_ReturnsNullAndDeletesCorruptPayload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Aletheia.Tests", Guid.NewGuid().ToString("N"));
        var cache = new LocalProviderCache(directory);
        var bytes = Encoding.UTF8.GetBytes("official payload");
        var source = "https://example.test/source";

        try
        {
            var written = await cache.WriteAsync("cnmv-iic", source, bytes, DateTimeOffset.UtcNow);
            var payloadPath = Path.Combine(directory, "cnmv-iic", $"{written.CacheKey}.bin");
            await File.WriteAllTextAsync(payloadPath, "tampered");

            var read = await cache.TryReadAsync("cnmv-iic", source);

            Assert.Null(read);
            Assert.False(File.Exists(payloadPath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task LocalProviderCache_ReturnsNullAndDeletesCorruptMetadata()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Aletheia.Tests", Guid.NewGuid().ToString("N"));
        var cache = new LocalProviderCache(directory);
        var source = "https://example.test/source";

        try
        {
            var written = await cache.WriteAsync("cnmv-iic", source, Encoding.UTF8.GetBytes("official payload"), DateTimeOffset.UtcNow);
            var metadataPath = Path.Combine(directory, "cnmv-iic", $"{written.CacheKey}.json");
            await File.WriteAllTextAsync(metadataPath, "{ not-json");

            var read = await cache.TryReadAsync("cnmv-iic", source);

            Assert.Null(read);
            Assert.False(File.Exists(metadataPath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void CnmvParser_RejectsDtd()
    {
        var xml = """
            <!DOCTYPE root [
              <!ENTITY xxe SYSTEM "file:///c:/windows/win.ini">
            ]>
            <root><FechaDatos>&xxe;</FechaDatos></root>
            """;

        var exception = Assert.ThrowsAny<System.Xml.XmlException>(() => new CnmvIicParser().ParseRegistrations(xml));

        Assert.Contains("DTD", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CnmvProvider_HonorsCancellationBeforeNetworkWork()
    {
        var provider = new CnmvIicProvider(options: new CnmvIicProviderOptions { EnableCache = false });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await provider.SearchAsync(new FundSearchQuery(text: "alfa"), cancellationToken: cancellation.Token));
    }
}
