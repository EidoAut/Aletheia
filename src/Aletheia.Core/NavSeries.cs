namespace Aletheia.Core;

/// <summary>
/// Represents an immutable, date-ordered NAV series.
/// </summary>
/// <remarks>
/// A NAV series is a core domain object rather than a loose list of values.
/// It guarantees chronological ordering and rejects duplicate dates so that
/// later return and drawdown calculations have unambiguous previous values.
/// </remarks>
public sealed class NavSeries
{
    private readonly NavPoint[] points;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavSeries"/> class.
    /// </summary>
    /// <param name="points">The observations to sort and validate.</param>
    /// <param name="observationFrequency">The declared observation frequency.</param>
    public NavSeries(IEnumerable<NavPoint> points, ObservationFrequency observationFrequency = ObservationFrequency.Irregular)
    {
        ArgumentNullException.ThrowIfNull(points);

        this.points = points.OrderBy(point => point.Date).ToArray();
        this.ObservationFrequency = observationFrequency;
        for (var index = 1; index < this.points.Length; index++)
        {
            if (this.points[index].Date == this.points[index - 1].Date)
            {
                throw new ArgumentException("A NAV series cannot contain duplicate dates.", nameof(points));
            }
        }
    }

    /// <summary>
    /// Gets the number of observations.
    /// </summary>
    public int Count => this.points.Length;

    /// <summary>
    /// Gets the first observation date.
    /// </summary>
    public DateOnly StartDate => this.EnsureNotEmpty().points[0].Date;

    /// <summary>
    /// Gets the last observation date.
    /// </summary>
    public DateOnly EndDate => this.EnsureNotEmpty().points[^1].Date;

    /// <summary>
    /// Gets the immutable observations as a read-only list.
    /// </summary>
    public IReadOnlyList<NavPoint> Points => this.points;

    /// <summary>
    /// Gets the declared sampling semantics of the series.
    /// </summary>
    public ObservationFrequency ObservationFrequency { get; }

    /// <summary>
    /// Gets the observation at the supplied zero-based index.
    /// </summary>
    /// <param name="index">The zero-based observation index.</param>
    /// <returns>The observation at <paramref name="index"/>.</returns>
    public NavPoint this[int index] => this.points[index];

    /// <summary>
    /// Creates a date-bounded slice while preserving chronological ordering.
    /// </summary>
    /// <param name="from">The optional inclusive start date.</param>
    /// <param name="to">The optional inclusive end date.</param>
    /// <returns>A new NAV series containing only observations in the requested range.</returns>
    public NavSeries Slice(DateOnly? from = null, DateOnly? to = null)
    {
        return new NavSeries(
            this.points.Where(point =>
                (!from.HasValue || point.Date >= from.Value) &&
                (!to.HasValue || point.Date <= to.Value)),
            this.ObservationFrequency);
    }

    /// <summary>
    /// Converts NAV values to double precision for numerical algorithms.
    /// </summary>
    /// <returns>A newly allocated array of NAV values in chronological order.</returns>
    public double[] ToDoubleValues()
    {
        var values = new double[this.points.Length];
        for (var index = 0; index < this.points.Length; index++)
        {
            values[index] = (double)this.points[index].Value;
        }

        return values;
    }

    /// <summary>
    /// Converts dates to an array for algorithms that need aligned values and timestamps.
    /// </summary>
    /// <returns>A newly allocated date array in chronological order.</returns>
    public DateOnly[] ToDates()
    {
        var dates = new DateOnly[this.points.Length];
        for (var index = 0; index < this.points.Length; index++)
        {
            dates[index] = this.points[index].Date;
        }

        return dates;
    }

    private NavSeries EnsureNotEmpty()
    {
        if (this.points.Length == 0)
        {
            throw new InvalidOperationException("The NAV series is empty.");
        }

        return this;
    }
}
