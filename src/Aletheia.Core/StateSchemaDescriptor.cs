using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Aletheia.Core;

/// <summary>
/// Identifies the feature schema used to construct a dynamic state.
/// </summary>
/// <remarks>
/// Prediction records and analogue searches should not compare states produced
/// by incompatible feature definitions. The descriptor captures the stable
/// version plus enough human-readable semantics to make the schema auditable.
/// </remarks>
public sealed class StateSchemaDescriptor
{
    private readonly IReadOnlyList<StateDimension> dimensionOrder;
    private readonly IReadOnlyDictionary<string, string> featureConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateSchemaDescriptor"/> class.
    /// </summary>
    /// <param name="id">The stable schema identifier.</param>
    /// <param name="version">The schema version.</param>
    /// <param name="dimensionOrder">The ordered state dimensions.</param>
    /// <param name="featureConfiguration">The deterministic feature configuration.</param>
    /// <param name="description">The schema description.</param>
    public StateSchemaDescriptor(
        string id,
        string version,
        IReadOnlyList<StateDimension> dimensionOrder,
        IReadOnlyDictionary<string, string> featureConfiguration,
        string description)
    {
        this.Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Schema id cannot be empty.", nameof(id))
            : id;
        this.Version = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Schema version cannot be empty.", nameof(version))
            : version;
        ArgumentNullException.ThrowIfNull(dimensionOrder);
        ArgumentNullException.ThrowIfNull(featureConfiguration);

        this.dimensionOrder = dimensionOrder.ToArray();
        if (this.dimensionOrder.Count == 0)
        {
            throw new ArgumentException("Schema dimension order cannot be empty.", nameof(dimensionOrder));
        }

        if (this.dimensionOrder.Select(dimension => dimension.Name).Distinct(StringComparer.Ordinal).Count() != this.dimensionOrder.Count)
        {
            throw new ArgumentException("Schema dimension order cannot contain duplicate dimensions.", nameof(dimensionOrder));
        }

        var sortedConfiguration = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in featureConfiguration)
        {
            sortedConfiguration[pair.Key] = pair.Value;
        }

        this.featureConfiguration = sortedConfiguration;
        this.Description = string.IsNullOrWhiteSpace(description)
            ? throw new ArgumentException("Schema description cannot be empty.", nameof(description))
            : description;
        this.Fingerprint = CalculateFingerprint(
            this.Id,
            this.Version,
            this.dimensionOrder,
            this.featureConfiguration);
    }

    /// <summary>
    /// Gets the stable schema identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the schema version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the ordered state dimensions.
    /// </summary>
    public IReadOnlyList<StateDimension> DimensionOrder => this.dimensionOrder;

    /// <summary>
    /// Gets the deterministic feature configuration.
    /// </summary>
    public IReadOnlyDictionary<string, string> FeatureConfiguration => this.featureConfiguration;

    /// <summary>
    /// Gets the SHA-256 fingerprint of the full state definition.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>
    /// Gets the schema description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Determines whether two schemas are scientifically compatible.
    /// </summary>
    /// <param name="other">The other schema.</param>
    /// <returns><see langword="true"/> when fingerprints are identical.</returns>
    public bool IsCompatibleWith(StateSchemaDescriptor other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return string.Equals(this.Fingerprint, other.Fingerprint, StringComparison.Ordinal);
    }

    private static string CalculateFingerprint(
        string id,
        string version,
        IReadOnlyList<StateDimension> dimensionOrder,
        IReadOnlyDictionary<string, string> featureConfiguration)
    {
        var builder = new StringBuilder();
        builder
            .Append("SchemaId=").Append(id).Append('\n')
            .Append("SchemaVersion=").Append(version).Append('\n');

        for (var index = 0; index < dimensionOrder.Count; index++)
        {
            builder
                .Append("Dimension[")
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append("]=")
                .Append(dimensionOrder[index].Name)
                .Append('\n');
        }

        foreach (var pair in featureConfiguration.OrderBy(pair => pair.Key, StringComparer.Ordinal))
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
}
