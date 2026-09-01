using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Represents a historical state vector used for analogue search.
/// </summary>
public sealed class StateObservation
{
    private readonly IReadOnlyDictionary<StateDimension, double> dimensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateObservation"/> class.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="dimensions">The state dimensions.</param>
    /// <param name="schema">The schema that produced the state dimensions.</param>
    public StateObservation(
        DateOnly date,
        IReadOnlyDictionary<StateDimension, double> dimensions,
        StateSchemaDescriptor? schema = null)
    {
        this.Date = date;
        this.dimensions = new Dictionary<StateDimension, double>(dimensions ?? throw new ArgumentNullException(nameof(dimensions)));
        this.Schema = schema;
    }

    /// <summary>
    /// Gets the observation date.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the state dimensions.
    /// </summary>
    public IReadOnlyDictionary<StateDimension, double> Dimensions => this.dimensions;

    /// <summary>
    /// Gets the state schema that produced this observation, when known.
    /// </summary>
    public StateSchemaDescriptor? Schema { get; }
}
