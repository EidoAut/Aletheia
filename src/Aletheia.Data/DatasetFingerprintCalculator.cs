using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Calculates deterministic fingerprints for NAV datasets.
/// </summary>
/// <remarks>
/// The canonical payload is one ordered line per observation:
/// <c>yyyy-MM-dd|NAV</c>, where NAV uses invariant-culture decimal formatting.
/// The resulting SHA-256 hash identifies the exact data used by a prediction.
/// </remarks>
public sealed class DatasetFingerprintCalculator
{
    /// <summary>
    /// Calculates a SHA-256 fingerprint over ordered NAV observations.
    /// </summary>
    /// <param name="navSeries">The NAV series.</param>
    /// <returns>A lowercase hexadecimal SHA-256 fingerprint.</returns>
    public string CalculateSha256(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        var builder = new StringBuilder();
        for (var index = 0; index < navSeries.Count; index++)
        {
            var point = navSeries[index];
            builder
                .Append(point.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .Append('|')
                .Append(point.Value.ToString("0.#############################", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
