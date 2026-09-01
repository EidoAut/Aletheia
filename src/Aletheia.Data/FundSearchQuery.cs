using System.Globalization;
using System.Text;

namespace Aletheia.Data;

/// <summary>
/// Represents a normalized fund-discovery query.
/// </summary>
public sealed record FundSearchQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FundSearchQuery"/> class.
    /// </summary>
    /// <param name="text">The free-text query.</param>
    /// <param name="isin">The optional ISIN or partial ISIN query.</param>
    public FundSearchQuery(string? text = null, string? isin = null)
    {
        this.Text = NormalizeSearchText(text);
        this.Isin = NormalizeIsin(isin);
    }

    /// <summary>
    /// Gets the normalized free-text query.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets the normalized ISIN or partial ISIN query.
    /// </summary>
    public string? Isin { get; }

    /// <summary>
    /// Gets a value indicating whether the query has no criteria.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(this.Text) && string.IsNullOrWhiteSpace(this.Isin);

    /// <summary>
    /// Creates a query from the user-entered search box text.
    /// </summary>
    /// <param name="value">The entered value.</param>
    /// <returns>The normalized query.</returns>
    public static FundSearchQuery FromUserText(string value)
    {
        var normalized = NormalizeIsin(value);
        if (normalized is not null && normalized.Length >= 2 && normalized.All(char.IsLetterOrDigit))
        {
            return new FundSearchQuery(text: value, isin: normalized);
        }

        return new FundSearchQuery(text: value);
    }

    /// <summary>
    /// Normalizes free-text search terms for deterministic provider filtering.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalized value, or <see langword="null"/> when empty.</returns>
    public static string? NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;
        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousWasSpace = false;
                continue;
            }

            if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        var normalized = builder.ToString().Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    /// <summary>
    /// Normalizes an ISIN or partial ISIN.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalized value, or <see langword="null"/> when empty.</returns>
    public static string? NormalizeIsin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return normalized.Length == 0 ? null : normalized;
    }
}
