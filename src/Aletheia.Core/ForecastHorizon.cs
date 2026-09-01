namespace Aletheia.Core;

/// <summary>
/// Represents a forecast horizon with explicit temporal semantics.
/// </summary>
public readonly record struct ForecastHorizon
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastHorizon"/> struct.
    /// </summary>
    /// <param name="value">The positive horizon value.</param>
    /// <param name="unit">The unit in which <paramref name="value"/> is expressed.</param>
    public ForecastHorizon(int value, ForecastHorizonUnit unit)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A forecast horizon must be positive.");
        }

        this.Value = value;
        this.Unit = unit;
    }

    /// <summary>
    /// Gets the positive horizon value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Gets the unit in which <see cref="Value"/> is expressed.
    /// </summary>
    public ForecastHorizonUnit Unit { get; }

    /// <summary>
    /// Gets the standard calendar-time horizons used for user-facing forecasts.
    /// </summary>
    public static IReadOnlyList<ForecastHorizon> StandardCalendarHorizons { get; } =
    [
        CalendarDays(7),
        CalendarDays(30),
        CalendarDays(90),
        CalendarDays(180),
        CalendarDays(365),
    ];

    /// <summary>
    /// Gets standard observation-count horizons often used with business-daily fund data.
    /// </summary>
    public static IReadOnlyList<ForecastHorizon> StandardObservationHorizons { get; } =
    [
        Observations(5),
        Observations(21),
        Observations(63),
        Observations(126),
        Observations(252),
    ];

    /// <summary>
    /// Creates a calendar-day horizon.
    /// </summary>
    /// <param name="calendarDays">The number of elapsed calendar days.</param>
    /// <returns>A calendar-day forecast horizon.</returns>
    public static ForecastHorizon CalendarDays(int calendarDays) =>
        new(calendarDays, ForecastHorizonUnit.CalendarDays);

    /// <summary>
    /// Creates an observation-count horizon.
    /// </summary>
    /// <param name="observations">The number of future observations.</param>
    /// <returns>An observation-count forecast horizon.</returns>
    public static ForecastHorizon Observations(int observations) =>
        new(observations, ForecastHorizonUnit.Observations);

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Unit switch
        {
            ForecastHorizonUnit.CalendarDays => $"{this.Value} calendar days",
            ForecastHorizonUnit.Observations => $"{this.Value} observations",
            _ => $"{this.Value} {this.Unit}",
        };
    }
}
