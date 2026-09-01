namespace Aletheia.Core;

/// <summary>
/// Represents a fund's reconstructed state at a point in time.
/// </summary>
/// <remarks>
/// A dynamic state is a named vector rather than a rigid record. This keeps the
/// modelling engine open to future dimensions such as cycle phase, regime
/// probability, market-factor exposure, or PCA coordinates.
/// </remarks>
public sealed class DynamicState
{
    private readonly IReadOnlyDictionary<StateDimension, double> dimensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicState"/> class.
    /// </summary>
    /// <param name="date">The state date.</param>
    /// <param name="dimensions">The named state-vector coordinates.</param>
    /// <param name="dataAdequacy">A normalized data-adequacy score in the interval [0, 1].</param>
    /// <param name="schema">The schema that produced the state.</param>
    public DynamicState(
        DateOnly date,
        IReadOnlyDictionary<StateDimension, double> dimensions,
        double dataAdequacy,
        StateSchemaDescriptor? schema = null)
    {
        ArgumentNullException.ThrowIfNull(dimensions);

        if (double.IsNaN(dataAdequacy) || dataAdequacy < 0d || dataAdequacy > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(dataAdequacy), dataAdequacy, "Data adequacy must be in [0, 1].");
        }

        this.Date = date;
        this.dimensions = new Dictionary<StateDimension, double>(dimensions);
        this.DataAdequacy = dataAdequacy;
        this.Schema = schema;
    }

    /// <summary>
    /// Gets the state date.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the normalized data-adequacy score.
    /// </summary>
    public double DataAdequacy { get; }

    /// <summary>
    /// Gets the schema that produced this state, when known.
    /// </summary>
    public StateSchemaDescriptor? Schema { get; }

    /// <summary>
    /// Gets the named state-vector coordinates.
    /// </summary>
    public IReadOnlyDictionary<StateDimension, double> Dimensions => this.dimensions;

    /// <summary>
    /// Returns the value for a dimension when present.
    /// </summary>
    /// <param name="dimension">The requested dimension.</param>
    /// <param name="value">The dimension value.</param>
    /// <returns><see langword="true"/> when the dimension exists.</returns>
    public bool TryGetValue(StateDimension dimension, out double value)
    {
        return this.dimensions.TryGetValue(dimension, out value);
    }
}
