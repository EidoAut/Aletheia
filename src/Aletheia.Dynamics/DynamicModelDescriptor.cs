using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Describes a dynamic model implementation.
/// </summary>
public sealed class DynamicModelDescriptor
{
    private readonly IReadOnlyList<StateDimension> requiredStateDimensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicModelDescriptor"/> class.
    /// </summary>
    /// <param name="id">The stable model identifier.</param>
    /// <param name="name">The model name.</param>
    /// <param name="version">The model version.</param>
    /// <param name="description">The model description.</param>
    /// <param name="requiredStateDimensions">The required state dimensions.</param>
    public DynamicModelDescriptor(
        string id,
        string name,
        string version,
        string description,
        IReadOnlyList<StateDimension>? requiredStateDimensions = null)
    {
        this.Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Model id cannot be empty.", nameof(id))
            : id;
        this.Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Model name cannot be empty.", nameof(name))
            : name;
        this.Version = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Model version cannot be empty.", nameof(version))
            : version;
        this.Description = string.IsNullOrWhiteSpace(description)
            ? throw new ArgumentException("Model description cannot be empty.", nameof(description))
            : description;
        this.requiredStateDimensions = requiredStateDimensions is null
            ? Array.Empty<StateDimension>()
            : requiredStateDimensions.ToArray();
    }

    /// <summary>
    /// Gets the stable model identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the model name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the model version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the model description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the dimensions that must be present in a compatible state.
    /// </summary>
    public IReadOnlyList<StateDimension> RequiredStateDimensions => this.requiredStateDimensions;
}
