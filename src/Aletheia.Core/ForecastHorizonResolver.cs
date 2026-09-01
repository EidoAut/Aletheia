namespace Aletheia.Core;

/// <summary>
/// Resolves requested horizons into observation-count semantics.
/// </summary>
public sealed class ForecastHorizonResolver
{
    private const double DaysPerYear = 365.25d;
    private readonly IObservationCalendar calendar;
    private readonly double? irregularPeriodsPerYear;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastHorizonResolver"/> class.
    /// </summary>
    /// <param name="calendar">The calendar used for calendar-day horizons.</param>
    /// <param name="irregularPeriodsPerYear">An optional explicit cadence for irregular observations.</param>
    public ForecastHorizonResolver(
        IObservationCalendar? calendar = null,
        double? irregularPeriodsPerYear = null)
    {
        if (irregularPeriodsPerYear.HasValue &&
            (!double.IsFinite(irregularPeriodsPerYear.Value) || irregularPeriodsPerYear.Value <= 0d))
        {
            throw new ArgumentOutOfRangeException(
                nameof(irregularPeriodsPerYear),
                irregularPeriodsPerYear,
                "Irregular periods per year must be positive and finite.");
        }

        this.calendar = calendar ?? new WeekdayObservationCalendar();
        this.irregularPeriodsPerYear = irregularPeriodsPerYear;
    }

    /// <summary>
    /// Resolves a horizon from a known data cutoff date.
    /// </summary>
    /// <param name="horizon">The requested horizon.</param>
    /// <param name="dataCutoffDate">The date of the last available observation.</param>
    /// <param name="observationFrequency">The cadence used to convert calendar time into observations.</param>
    /// <returns>The resolved horizon metadata.</returns>
    public ForecastHorizonResolution Resolve(
        ForecastHorizon horizon,
        DateOnly dataCutoffDate,
        ObservationFrequency observationFrequency)
    {
        if (horizon.Unit == ForecastHorizonUnit.Observations)
        {
            return this.ResolveObservationHorizon(horizon, dataCutoffDate, observationFrequency);
        }

        return this.ResolveCalendarHorizon(horizon, dataCutoffDate, observationFrequency);
    }

    private ForecastHorizonResolution ResolveObservationHorizon(
        ForecastHorizon horizon,
        DateOnly dataCutoffDate,
        ObservationFrequency observationFrequency)
    {
        return observationFrequency switch
        {
            ObservationFrequency.Daily => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                horizon.Value,
                dataCutoffDate.AddDays(horizon.Value),
                "Daily observation cadence",
                false),
            ObservationFrequency.BusinessDaily => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                horizon.Value,
                this.calendar.AdvanceObservations(dataCutoffDate, horizon.Value),
                this.calendar.Name,
                true),
            ObservationFrequency.Weekly => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                horizon.Value,
                dataCutoffDate.AddDays(7 * horizon.Value),
                "Weekly observation cadence",
                true),
            ObservationFrequency.Monthly => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                horizon.Value,
                dataCutoffDate.AddMonths(horizon.Value),
                "Monthly observation cadence",
                true),
            ObservationFrequency.Irregular => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                horizon.Value,
                null,
                "Observation-count horizon",
                false),
            _ => throw new ArgumentOutOfRangeException(
                nameof(observationFrequency),
                observationFrequency,
                "Unsupported observation frequency."),
        };
    }

    private ForecastHorizonResolution ResolveCalendarHorizon(
        ForecastHorizon horizon,
        DateOnly dataCutoffDate,
        ObservationFrequency observationFrequency)
    {
        var targetDate = dataCutoffDate.AddDays(horizon.Value);
        return observationFrequency switch
        {
            ObservationFrequency.Daily => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                horizon.Value,
                targetDate,
                "Daily observation cadence",
                false),
            ObservationFrequency.BusinessDaily => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                this.calendar.CountObservations(dataCutoffDate, targetDate),
                targetDate,
                this.calendar.Name,
                true),
            ObservationFrequency.Weekly => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                this.CountRegularObservationsOnOrBefore(dataCutoffDate, targetDate, date => date.AddDays(7)),
                targetDate,
                "Weekly observation cadence",
                true),
            ObservationFrequency.Monthly => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                this.CountRegularObservationsOnOrBefore(dataCutoffDate, targetDate, date => date.AddMonths(1)),
                targetDate,
                "Monthly observation cadence",
                true),
            ObservationFrequency.Irregular when this.irregularPeriodsPerYear.HasValue => new ForecastHorizonResolution(
                horizon,
                observationFrequency,
                this.ResolveIrregularCalendarObservationCount(horizon),
                targetDate,
                "Elapsed-time effective cadence",
                true),
            ObservationFrequency.Irregular => throw new InvalidOperationException(
                "Calendar-day horizons cannot be converted for irregular observations without an explicit historical cadence."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(observationFrequency),
                observationFrequency,
                "Unsupported observation frequency."),
        };
    }

    private int CountRegularObservationsOnOrBefore(
        DateOnly start,
        DateOnly end,
        Func<DateOnly, DateOnly> advance)
    {
        var count = 0;
        var current = advance(start);
        while (current <= end)
        {
            count++;
            current = advance(current);
        }

        return count;
    }

    private int ResolveIrregularCalendarObservationCount(ForecastHorizon horizon)
    {
        var count = (int)Math.Round(
            horizon.Value * this.irregularPeriodsPerYear!.Value / DaysPerYear,
            MidpointRounding.AwayFromZero);
        return Math.Max(1, count);
    }
}
