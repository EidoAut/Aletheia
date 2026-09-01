using System.Globalization;
using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Reads dated NAV observations from a CSV file.
/// </summary>
/// <remarks>
/// The reader accepts either a header with date and NAV-like columns, or a
/// two-column headerless file where column 0 is the date and column 1 is NAV.
/// It is intentionally deterministic and culture-invariant.
/// </remarks>
public sealed class CsvFundDataReader
{
    private static readonly string[] DateColumnNames = ["date", "valuationdate", "navdate"];
    private static readonly string[] NavColumnNames = ["nav", "value", "price", "close"];

    /// <summary>
    /// Loads a fund history from a CSV file.
    /// </summary>
    /// <param name="filePath">The CSV file path.</param>
    /// <param name="cancellationToken">A token used to cancel file I/O.</param>
    /// <returns>The parsed fund history.</returns>
    public async Task<FundHistory> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("CSV path cannot be empty.", nameof(filePath));
        }

        var points = new List<NavPoint>();
        var dateColumnIndex = 0;
        var navColumnIndex = 1;
        var firstDataLineProcessed = false;
        var lineNumber = 0;

        await foreach (var line in File.ReadLinesAsync(filePath, cancellationToken))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = SplitCsvLine(line);
            if (!firstDataLineProcessed)
            {
                if (!TryParseDate(columns[0], out _))
                {
                    dateColumnIndex = FindColumn(columns, DateColumnNames, lineNumber);
                    navColumnIndex = FindColumn(columns, NavColumnNames, lineNumber);
                    firstDataLineProcessed = true;
                    continue;
                }

                firstDataLineProcessed = true;
            }

            if (columns.Count <= Math.Max(dateColumnIndex, navColumnIndex))
            {
                throw new FormatException($"CSV line {lineNumber} does not contain the required date and NAV columns.");
            }

            if (!TryParseDate(columns[dateColumnIndex], out var date))
            {
                throw new FormatException($"CSV line {lineNumber} contains an invalid date.");
            }

            if (!decimal.TryParse(
                columns[navColumnIndex],
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var nav))
            {
                throw new FormatException($"CSV line {lineNumber} contains an invalid NAV value.");
            }

            points.Add(new NavPoint(date, nav));
        }

        var name = Path.GetFileNameWithoutExtension(filePath);
        var identifier = new FundIdentifier(FundIdentifierKind.Local, Path.GetFullPath(filePath));
        var fund = new Fund(identifier, name, "CSV", null);

        return new FundHistory(fund, new NavSeries(points, ObservationFrequencyDetector.Detect(points)));
    }

    private static int FindColumn(IReadOnlyList<string> columns, IReadOnlyList<string> candidates, int lineNumber)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            var normalized = NormalizeColumnName(columns[index]);
            if (candidates.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        throw new FormatException($"CSV header on line {lineNumber} does not contain a required column.");
    }

    private static string NormalizeColumnName(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var columns = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Add('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                columns.Add(new string(current.ToArray()).Trim());
                current.Clear();
                continue;
            }

            current.Add(character);
        }

        columns.Add(new string(current.ToArray()).Trim());
        return columns;
    }
}
