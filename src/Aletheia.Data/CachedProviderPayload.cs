namespace Aletheia.Data;

/// <summary>
/// Represents a cached provider payload and its retrieval metadata.
/// </summary>
public sealed record CachedProviderPayload(
    byte[] Content,
    DateTimeOffset RetrievalTimestampUtc,
    bool IsFromCache,
    string CacheKey,
    Uri? RequestedUri = null,
    Uri? FinalUri = null,
    string? ContentType = null);
