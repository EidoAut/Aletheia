namespace Aletheia.Core;

/// <summary>
/// Identifies the exact dataset used to generate a calculation.
/// </summary>
public sealed record DatasetIdentity(
    string DataProvider,
    string DatasetFingerprintSha256,
    DateTimeOffset? DatasetTimestampUtc);
