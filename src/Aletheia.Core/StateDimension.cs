namespace Aletheia.Core;

/// <summary>
/// Names one coordinate in a dynamic state vector.
/// </summary>
/// <remarks>
/// Dimensions are values rather than enum members so the state vector can evolve
/// without changing the core model every time a new feature is introduced.
/// </remarks>
public readonly record struct StateDimension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StateDimension"/> struct.
    /// </summary>
    /// <param name="name">The stable dimension name.</param>
    public StateDimension(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A state dimension name cannot be empty.", nameof(name));
        }

        this.Name = name.Trim();
    }

    /// <summary>
    /// Gets the stable dimension name.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString() => this.Name;
}
