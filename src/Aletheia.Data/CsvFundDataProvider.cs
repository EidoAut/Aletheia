using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Provides fund histories from configured CSV files.
/// </summary>
public sealed class CsvFundDataProvider : IProvenanceAwareFundDataProvider
{
    private readonly IReadOnlyDictionary<FundIdentifier, string> filePaths;
    private readonly CsvFundDataReader reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvFundDataProvider"/> class.
    /// </summary>
    /// <param name="filePaths">A map from fund identifiers to CSV file paths.</param>
    /// <param name="reader">The CSV reader.</param>
    public CsvFundDataProvider(
        IReadOnlyDictionary<FundIdentifier, string>? filePaths = null,
        CsvFundDataReader? reader = null)
    {
        this.filePaths = filePaths ?? new Dictionary<FundIdentifier, string>();
        this.reader = reader ?? new CsvFundDataReader();
    }

    /// <inheritdoc />
    public Task<Fund?> FindByIsinAsync(string isin, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Isin.IsValid(isin))
        {
            return Task.FromResult<Fund?>(null);
        }

        var identifier = new Isin(isin).ToFundIdentifier();
        return this.filePaths.ContainsKey(identifier)
            ? Task.FromResult<Fund?>(new Fund(identifier, identifier.Value, "CSV", null))
            : Task.FromResult<Fund?>(null);
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
        var filePath = fundIdentifier.Kind == FundIdentifierKind.Local
            ? fundIdentifier.Value
            : this.filePaths.GetValueOrDefault(fundIdentifier);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new KeyNotFoundException($"No CSV file is configured for fund '{fundIdentifier}'.");
        }

        var history = await this.reader.ReadAsync(filePath, cancellationToken).ConfigureAwait(false);
        return CreateResult(new FundHistory(history.Fund, history.NavSeries.Slice(from, to)), fundIdentifier, filePath, from, to);
    }

    /// <summary>
    /// Loads a CSV file directly without pre-registering it in the provider map.
    /// </summary>
    /// <param name="filePath">The CSV file path.</param>
    /// <param name="cancellationToken">A token used to cancel file I/O.</param>
    /// <returns>The fund history.</returns>
    public Task<FundHistory> GetHistoryFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return this.reader.ReadAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// Loads a CSV file directly with local-file provenance.
    /// </summary>
    /// <param name="filePath">The CSV file path.</param>
    /// <param name="cancellationToken">A token used to cancel file I/O.</param>
    /// <returns>The fund history and provenance.</returns>
    public async Task<FundHistoryResult> GetHistoryFromFileWithProvenanceAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var history = await this.reader.ReadAsync(filePath, cancellationToken).ConfigureAwait(false);
        return CreateResult(history, history.Fund.Identifier, filePath, null, null);
    }

    private static FundHistoryResult CreateResult(
        FundHistory history,
        FundIdentifier requestedIdentifier,
        string filePath,
        DateOnly? from,
        DateOnly? to)
    {
        var fingerprint = new DatasetFingerprintCalculator().CalculateSha256(history.NavSeries);
        var fullPath = Path.GetFullPath(filePath);
        var provenance = new FundDataProvenance(
            "local-csv",
            "Local CSV",
            DateTimeOffset.UtcNow,
            requestedIdentifier,
            requestedIdentifier.Kind == FundIdentifierKind.Isin ? requestedIdentifier.Value : null,
            new Uri(fullPath),
            fullPath,
            history.NavSeries.ObservationFrequency,
            from,
            to,
            history.NavSeries.Count == 0 ? null : history.NavSeries.StartDate,
            history.NavSeries.Count == 0 ? null : history.NavSeries.EndDate,
            history.NavSeries.Count,
            history.NavSeries.Count,
            fingerprint,
            false,
            null);
        return new FundHistoryResult(history, provenance);
    }
}
