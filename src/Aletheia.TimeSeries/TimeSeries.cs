using Aletheia.Core;

namespace Aletheia.TimeSeries;

/// <summary>
/// Immutable ordered collection of dated observations.
/// </summary>
/// <typeparam name="T">The observation value type.</typeparam>
/// <remarks>
/// The class enforces date ordering and uniqueness. Numerical algorithms can
/// then assume that index <c>i - 1</c> is the previous observation without
/// repeatedly revalidating the input.
/// </remarks>
public sealed class TimeSeries<T>
{
    private readonly TimeSeriesPoint<T>[] points;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSeries{T}"/> class.
    /// </summary>
    /// <param name="points">The observations to sort and validate.</param>
    /// <param name="observationFrequency">The declared observation frequency.</param>
    public TimeSeries(
        IEnumerable<TimeSeriesPoint<T>> points,
        ObservationFrequency observationFrequency = ObservationFrequency.Irregular)
    {
        ArgumentNullException.ThrowIfNull(points);

        this.points = points.OrderBy(point => point.Date).ToArray();
        this.ObservationFrequency = observationFrequency;
        for (var index = 1; index < this.points.Length; index++)
        {
            if (this.points[index].Date == this.points[index - 1].Date)
            {
                throw new ArgumentException("A time series cannot contain duplicate dates.", nameof(points));
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
    public DateOnly StartDate => this.RequireNotEmpty().points[0].Date;

    /// <summary>
    /// Gets the final observation date.
    /// </summary>
    public DateOnly EndDate => this.RequireNotEmpty().points[^1].Date;

    /// <summary>
    /// Gets the ordered observations.
    /// </summary>
    public IReadOnlyList<TimeSeriesPoint<T>> Points => this.points;

    /// <summary>
    /// Gets the declared sampling semantics of the series.
    /// </summary>
    public ObservationFrequency ObservationFrequency { get; }

    /// <summary>
    /// Gets the observation at the supplied zero-based index.
    /// </summary>
    /// <param name="index">The zero-based observation index.</param>
    /// <returns>The point at the requested index.</returns>
    public TimeSeriesPoint<T> this[int index] => this.points[index];

    /// <summary>
    /// Returns a date-bounded slice of the series.
    /// </summary>
    /// <param name="from">The optional inclusive start date.</param>
    /// <param name="to">The optional inclusive end date.</param>
    /// <returns>A new time series containing only points inside the requested range.</returns>
    public TimeSeries<T> Slice(DateOnly? from = null, DateOnly? to = null)
    {
        return new TimeSeries<T>(
            this.points.Where(point =>
                (!from.HasValue || point.Date >= from.Value) &&
                (!to.HasValue || point.Date <= to.Value)),
            this.ObservationFrequency);
    }

    /// <summary>
    /// Creates rolling windows with a fixed observation count.
    /// </summary>
    /// <param name="windowSize">The number of observations in each window.</param>
    /// <returns>Rolling windows in chronological order.</returns>
    public IReadOnlyList<TimeSeries<T>> RollingWindows(int windowSize)
    {
        if (windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Window size must be positive.");
        }

        if (this.points.Length < windowSize)
        {
            return Array.Empty<TimeSeries<T>>();
        }

        var windows = new List<TimeSeries<T>>(this.points.Length - windowSize + 1);
        for (var start = 0; start <= this.points.Length - windowSize; start++)
        {
            windows.Add(new TimeSeries<T>(this.points.Skip(start).Take(windowSize), this.ObservationFrequency));
        }

        return windows;
    }

    /// <summary>
    /// Projects the values into a newly allocated array.
    /// </summary>
    /// <returns>An array containing values in chronological order.</returns>
    public T[] ToValueArray()
    {
        var values = new T[this.points.Length];
        for (var index = 0; index < this.points.Length; index++)
        {
            values[index] = this.points[index].Value;
        }

        return values;
    }

    /// <summary>
    /// Projects the dates into a newly allocated array.
    /// </summary>
    /// <returns>An array containing dates in chronological order.</returns>
    public DateOnly[] ToDateArray()
    {
        var dates = new DateOnly[this.points.Length];
        for (var index = 0; index < this.points.Length; index++)
        {
            dates[index] = this.points[index].Date;
        }

        return dates;
    }

    private TimeSeries<T> RequireNotEmpty()
    {
        if (this.points.Length == 0)
        {
            throw new InvalidOperationException("The time series is empty.");
        }

        return this;
    }
}
