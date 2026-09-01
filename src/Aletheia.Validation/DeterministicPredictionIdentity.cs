using System.Security.Cryptography;
using System.Text;

namespace Aletheia.Validation;

/// <summary>
/// Creates stable logical keys and GUIDs for idempotent prediction storage.
/// </summary>
public static class DeterministicPredictionIdentity
{
    /// <summary>
    /// Hashes a canonical logical identity into a stable GUID.
    /// </summary>
    /// <param name="logicalKey">The canonical logical prediction key.</param>
    /// <returns>A deterministic GUID derived from SHA-256.</returns>
    public static Guid CreateGuid(string logicalKey)
    {
        if (string.IsNullOrWhiteSpace(logicalKey))
        {
            throw new ArgumentException("Logical key cannot be empty.", nameof(logicalKey));
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(logicalKey));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, bytes.Length);
        return new Guid(bytes);
    }
}
