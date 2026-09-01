using System.Globalization;
using Aletheia.Core;

namespace Aletheia.Application;

/// <summary>
/// Centralizes deterministic quantitative formatting for presentation layers.
/// </summary>
public static class QuantitativeFormatter
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    /// <summary>
    /// Formats a nullable decimal return as a percentage.
    /// </summary>
    /// <param name="value">The decimal return value.</param>
    /// <returns>A formatted percentage or N/A.</returns>
    public static string FormatReturn(double? value) =>
        value.HasValue ? value.Value.ToString("P2", InvariantCulture) : "N/A";

    /// <summary>
    /// Formats a nullable ratio as a short percentage.
    /// </summary>
    /// <param name="value">The ratio value.</param>
    /// <returns>A formatted percentage or N/A.</returns>
    public static string FormatPercentShort(double? value) =>
        value.HasValue ? value.Value.ToString("P1", InvariantCulture) : "N/A";

    /// <summary>
    /// Formats a nullable scalar.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    /// <returns>A formatted number or N/A.</returns>
    public static string FormatNumber(double? value) =>
        value.HasValue ? value.Value.ToString("0.0000", InvariantCulture) : "N/A";

    /// <summary>
    /// Formats a nullable monetary amount with an explicit currency code.
    /// </summary>
    /// <param name="value">The monetary value.</param>
    /// <param name="currency">The ISO-like currency code, when known.</param>
    /// <returns>A formatted amount or N/A.</returns>
    public static string FormatCurrency(double? value, string? currency)
    {
        if (!value.HasValue)
        {
            return "N/A";
        }

        var code = string.IsNullOrWhiteSpace(currency) ? "CUR" : currency.ToUpperInvariant();
        return $"{code} {value.Value.ToString("#,##0.00", InvariantCulture)}";
    }

    /// <summary>
    /// Formats a nullable probability score.
    /// </summary>
    /// <param name="value">The score value.</param>
    /// <returns>A formatted probability score or N/A.</returns>
    public static string FormatScore(double? value) =>
        value.HasValue ? value.Value.ToString("0.000", InvariantCulture) : "N/A";

    /// <summary>
    /// Formats a date using Aletheia's unambiguous convention.
    /// </summary>
    /// <param name="date">The optional date.</param>
    /// <returns>The formatted date or N/A.</returns>
    public static string FormatDate(DateOnly? date) =>
        date.HasValue ? date.Value.ToString("yyyy-MM-dd", InvariantCulture) : "N/A";

    /// <summary>
    /// Formats a timestamp using an unambiguous UTC-friendly convention.
    /// </summary>
    /// <param name="timestamp">The optional timestamp.</param>
    /// <returns>The formatted timestamp or N/A.</returns>
    public static string FormatTimestamp(DateTimeOffset? timestamp) =>
        timestamp.HasValue ? timestamp.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", InvariantCulture) : "N/A";

    /// <summary>
    /// Formats a fingerprint for dense display.
    /// </summary>
    /// <param name="fingerprint">The full fingerprint.</param>
    /// <param name="length">The displayed prefix length.</param>
    /// <returns>The abbreviated fingerprint or N/A.</returns>
    public static string FormatFingerprint(string? fingerprint, int length = 12)
    {
        return string.IsNullOrWhiteSpace(fingerprint)
            ? "N/A"
            : $"{fingerprint[..Math.Min(length, fingerprint.Length)]}...";
    }

    /// <summary>
    /// Formats a forecast value only when its capability is supported.
    /// </summary>
    /// <param name="capabilities">The available forecast capabilities.</param>
    /// <param name="capability">The required capability.</param>
    /// <param name="value">The forecast value.</param>
    /// <returns>The formatted return or N/A.</returns>
    public static string FormatCapabilityReturn(
        ForecastCapabilities capabilities,
        ForecastCapabilities capability,
        double value)
    {
        return (capabilities & capability) == capability ? FormatReturn(value) : "N/A";
    }

    /// <summary>
    /// Formats a boolean value as a technical yes/no flag.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>YES or NO.</returns>
    public static string FormatYesNo(bool value) => value ? "YES" : "NO";
}
