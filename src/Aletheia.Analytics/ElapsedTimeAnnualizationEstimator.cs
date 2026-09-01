namespace Aletheia.Analytics;

/// <summary>
/// Estimates cadence from the number of observed intervals divided by actual elapsed calendar time.
/// </summary>
/// <remarks>
/// This estimator does not fill missing dates or pretend that irregular observations are regular.
/// It supplies an explicit elapsed-time convention for metrics that require an annual scale.
/// </remarks>
public sealed class ElapsedTimeAnnualizationEstimator : IIrregularAnnualizationEstimator
{
    private const double DaysPerYear = 365.25d;

    /// <inheritdoc />
    public double EstimatePeriodsPerYear(IReadOnlyList<DateOnly> observationDates)
    {
        ArgumentNullException.ThrowIfNull(observationDates);
        if (observationDates.Count < 2)
        {
            throw new InvalidOperationException(
                "At least two dated observations are required to estimate an annual cadence.");
        }

        var ordered = observationDates.OrderBy(date => date).Distinct().ToArray();
        if (ordered.Length < 2)
        {
            throw new InvalidOperationException(
                "At least two distinct observation dates are required to estimate an annual cadence.");
        }

        var elapsedDays = ordered[^1].DayNumber - ordered[0].DayNumber;
        if (elapsedDays <= 0)
        {
            throw new InvalidOperationException("Observation dates must span at least one calendar day.");
        }

        var periodsPerYear = (ordered.Length - 1d) * DaysPerYear / elapsedDays;
        if (!double.IsFinite(periodsPerYear) || periodsPerYear <= 0d)
        {
            throw new InvalidOperationException("The elapsed-time annualization estimate is not positive and finite.");
        }

        return periodsPerYear;
    }
}
