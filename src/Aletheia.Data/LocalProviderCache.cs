using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aletheia.Data;

/// <summary>
/// Stores remote provider payloads in a small transparent local cache.
/// </summary>
public sealed class LocalProviderCache
{
    private readonly string rootDirectory;
    private readonly TimeSpan? timeToLive;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalProviderCache"/> class.
    /// </summary>
    /// <param name="rootDirectory">The cache root directory.</param>
    /// <param name="timeToLive">Optional maximum cache age.</param>
    public LocalProviderCache(string rootDirectory, TimeSpan? timeToLive = null)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Cache directory cannot be empty.", nameof(rootDirectory));
        }

        this.rootDirectory = rootDirectory;
        this.timeToLive = timeToLive;
    }

    /// <summary>
    /// Creates a stable opaque cache key.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="sourceKey">The source key.</param>
    /// <returns>The cache key.</returns>
    public static string CreateCacheKey(string providerId, string sourceKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{providerId}\n{sourceKey}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Reads a cached payload.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="sourceKey">The provider source key.</param>
    /// <param name="cancellationToken">A token used to cancel file I/O.</param>
    /// <returns>The cached payload, or <see langword="null"/>.</returns>
    public async Task<CachedProviderPayload?> TryReadAsync(
        string providerId,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        var key = CreateCacheKey(providerId, sourceKey);
        var payloadPath = this.GetPayloadPath(providerId, key);
        var metadataPath = this.GetMetadataPath(providerId, key);
        if (!File.Exists(payloadPath) || !File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken).ConfigureAwait(false);
            var metadata = JsonSerializer.Deserialize<CacheMetadata>(metadataJson);
            if (metadata is null ||
                !string.Equals(metadata.SourceKey, sourceKey, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(metadata.ContentSha256))
            {
                this.DeleteEntry(payloadPath, metadataPath);
                return null;
            }

            if (this.timeToLive.HasValue && DateTimeOffset.UtcNow - metadata.RetrievalTimestampUtc > this.timeToLive.Value)
            {
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(payloadPath, cancellationToken).ConfigureAwait(false);
            var actualHash = CalculateSha256(bytes);
            if (bytes.LongLength != metadata.ContentLength || !string.Equals(actualHash, metadata.ContentSha256, StringComparison.OrdinalIgnoreCase))
            {
                this.DeleteEntry(payloadPath, metadataPath);
                return null;
            }

            return new CachedProviderPayload(bytes, metadata.RetrievalTimestampUtc, true, key);
        }
        catch (IOException)
        {
            this.DeleteEntry(payloadPath, metadataPath);
            return null;
        }
        catch (JsonException)
        {
            this.DeleteEntry(payloadPath, metadataPath);
            return null;
        }
    }

    /// <summary>
    /// Invalidates one cached provider payload.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="sourceKey">The provider source key.</param>
    /// <param name="cancellationToken">A token used to cancel the operation before file work starts.</param>
    /// <returns>A completed task.</returns>
    public Task InvalidateAsync(
        string providerId,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateCacheKey(providerId, sourceKey);
        this.DeleteEntry(this.GetPayloadPath(providerId, key), this.GetMetadataPath(providerId, key));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes a provider payload to the cache.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="sourceKey">The provider source key.</param>
    /// <param name="content">The payload bytes.</param>
    /// <param name="retrievalTimestampUtc">The retrieval timestamp.</param>
    /// <param name="cancellationToken">A token used to cancel file I/O.</param>
    /// <returns>The cached payload metadata.</returns>
    public async Task<CachedProviderPayload> WriteAsync(
        string providerId,
        string sourceKey,
        byte[] content,
        DateTimeOffset retrievalTimestampUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var key = CreateCacheKey(providerId, sourceKey);
        var directory = Path.Combine(this.rootDirectory, SanitizeProviderId(providerId));
        Directory.CreateDirectory(directory);
        var payloadPath = this.GetPayloadPath(providerId, key);
        var metadataPath = this.GetMetadataPath(providerId, key);
        var contentHash = CalculateSha256(content);
        await WriteAtomicallyAsync(payloadPath, content, cancellationToken).ConfigureAwait(false);
        var metadata = new CacheMetadata(sourceKey, retrievalTimestampUtc, contentHash, content.LongLength);
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await WriteAtomicallyAsync(metadataPath, Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
        return new CachedProviderPayload(content, retrievalTimestampUtc, false, key);
    }

    private static string CalculateSha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    private static async Task WriteAtomicallyAsync(
        string targetPath,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, content, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string SanitizeProviderId(string providerId)
    {
        return new string(providerId.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private string GetPayloadPath(string providerId, string key)
    {
        return Path.Combine(this.rootDirectory, SanitizeProviderId(providerId), $"{key}.bin");
    }

    private string GetMetadataPath(string providerId, string key)
    {
        return Path.Combine(this.rootDirectory, SanitizeProviderId(providerId), $"{key}.json");
    }

    private void DeleteEntry(string payloadPath, string metadataPath)
    {
        TryDelete(payloadPath);
        TryDelete(metadataPath);
    }

    private sealed record CacheMetadata(
        string SourceKey,
        DateTimeOffset RetrievalTimestampUtc,
        string? ContentSha256,
        long ContentLength);
}
