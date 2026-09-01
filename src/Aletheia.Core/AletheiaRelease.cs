namespace Aletheia.Core;

/// <summary>
/// Centralizes product and scientific-version identifiers used across Aletheia surfaces.
/// </summary>
public static class AletheiaRelease
{
    /// <summary>
    /// Gets the user-facing product version.
    /// </summary>
    public const string ProductVersion = "2.7.3";

    /// <summary>
    /// Gets the version stamped into reproducibility metadata and prediction content.
    /// </summary>
    public const string ScientificVersion = "2.12.0-causal-horizon-integrity";
}
