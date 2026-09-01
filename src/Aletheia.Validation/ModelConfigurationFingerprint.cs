using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Creates deterministic fingerprints for model configuration dictionaries.
/// </summary>
public static class ModelConfigurationFingerprint
{
    /// <summary>
    /// Hashes a model descriptor and sorted configuration key/value pairs.
    /// </summary>
    /// <param name="descriptor">The model descriptor.</param>
    /// <param name="configuration">The configuration dictionary.</param>
    /// <returns>A lowercase SHA-256 fingerprint.</returns>
    public static string Calculate(
        ModelDescriptor descriptor,
        IReadOnlyDictionary<string, string> configuration)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = new StringBuilder()
            .Append("ModelId=").Append(descriptor.Id).Append('\n')
            .Append("ModelVersion=").Append(descriptor.Version).Append('\n');

        foreach (var pair in configuration.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            builder
                .Append("Configuration.")
                .Append(pair.Key)
                .Append('=')
                .Append(pair.Value)
                .Append('\n');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Formats an integer configuration value with invariant culture.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant string representation.</returns>
    public static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
