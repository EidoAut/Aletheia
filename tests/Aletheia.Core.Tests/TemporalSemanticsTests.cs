using Aletheia.Core;

namespace Aletheia.Core.Tests;

public sealed class TemporalSemanticsTests
{
    [Fact]
    public void ForecastHorizon_ToString_ExposesUnit()
    {
        Assert.Equal("30 calendar days", ForecastHorizon.CalendarDays(30).ToString());
        Assert.Equal("30 observations", ForecastHorizon.Observations(30).ToString());
    }

    [Fact]
    public void WeekdayCalendar_AdvancingThreeObservationsFromFriday_ReachesWednesday()
    {
        var calendar = new WeekdayObservationCalendar();

        var result = calendar.AdvanceObservations(new DateOnly(2024, 1, 5), 3);

        Assert.Equal(new DateOnly(2024, 1, 10), result);
    }

    [Fact]
    public void HorizonResolver_DoesNotTreatCalendarDaysAsObservations()
    {
        var resolver = new ForecastHorizonResolver(new WeekdayObservationCalendar());

        var result = resolver.Resolve(
            ForecastHorizon.CalendarDays(3),
            new DateOnly(2024, 1, 5),
            ObservationFrequency.BusinessDaily);

        Assert.Equal(1, result.EffectiveObservationCount);
        Assert.Equal(new DateOnly(2024, 1, 8), result.TargetDate);
        Assert.Equal(ObservationFrequency.BusinessDaily, result.ObservationFrequency);
        Assert.True(result.IsApproximation);
    }

    [Fact]
    public void HorizonResolver_WithFrequencySpecificCalendarHorizon_UsesFrequency()
    {
        var resolver = new ForecastHorizonResolver(new WeekdayObservationCalendar());
        var cutoff = new DateOnly(2024, 1, 1);

        var daily = resolver.Resolve(ForecastHorizon.CalendarDays(30), cutoff, ObservationFrequency.Daily);
        var businessDaily = resolver.Resolve(ForecastHorizon.CalendarDays(30), cutoff, ObservationFrequency.BusinessDaily);
        var weekly = resolver.Resolve(ForecastHorizon.CalendarDays(30), cutoff, ObservationFrequency.Weekly);
        var monthly = resolver.Resolve(ForecastHorizon.CalendarDays(30), cutoff, ObservationFrequency.Monthly);

        Assert.Equal(30, daily.EffectiveObservationCount);
        Assert.NotEqual(daily.EffectiveObservationCount, businessDaily.EffectiveObservationCount);
        Assert.Equal(4, weekly.EffectiveObservationCount);
        Assert.Equal(0, monthly.EffectiveObservationCount);
    }

    [Fact]
    public void HorizonResolver_WithExplicitIrregularCadence_ResolvesCalendarHorizon()
    {
        var resolver = new ForecastHorizonResolver(
            new WeekdayObservationCalendar(),
            irregularPeriodsPerYear: 24d);

        var result = resolver.Resolve(
            ForecastHorizon.CalendarDays(30),
            new DateOnly(2024, 1, 1),
            ObservationFrequency.Irregular);

        Assert.Equal(2, result.EffectiveObservationCount);
        Assert.Equal(new DateOnly(2024, 1, 31), result.TargetDate);
        Assert.True(result.IsApproximation);
        Assert.Equal("Elapsed-time effective cadence", result.ResolutionPolicyName);
    }

    [Fact]
    public void HorizonResolver_WithIrregularCalendarHorizon_Throws()
    {
        var resolver = new ForecastHorizonResolver(new WeekdayObservationCalendar());

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(
            ForecastHorizon.CalendarDays(30),
            new DateOnly(2024, 1, 1),
            ObservationFrequency.Irregular));
    }
}
