using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Creates causal training targets with triple-barrier labeling.
/// </summary>
public sealed class TripleBarrierLabeler
{
    private readonly ForecastHorizonResolver horizonResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="TripleBarrierLabeler"/> class.
    /// </summary>
    /// <param name="horizonResolver">The horizon resolver.</param>
    public TripleBarrierLabeler(ForecastHorizonResolver? horizonResolver = null)
    {
        this.horizonResolver = horizonResolver ?? new ForecastHorizonResolver();
    }

    /// <summary>
    /// Labels every eligible start point in a NAV series.
    /// </summary>
    /// <param name="navSeries">The historical NAV series.</param>
    /// <param name="definition">The triple-barrier definition.</param>
    /// <param name="causalVolatility">Optional volatility estimates aligned to NAV observations.</param>
    /// <returns>Triple-barrier outcomes.</returns>
    public IReadOnlyList<TripleBarrierOutcome> Label(
        NavSeries navSeries,
        TripleBarrierDefinition definition,
        IReadOnlyList<double>? causalVolatility = null)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(definition);
        ValidateDefinition(definition);
        if (causalVolatility is not null && causalVolatility.Count < navSeries.Count)
        {
            throw new ArgumentException("Volatility estimates must be aligned to the NAV series.", nameof(causalVolatility));
        }

        var results = new List<TripleBarrierOutcome>();
        for (var index = 0; index < navSeries.Count - 1; index++)
        {
            var valuation = this.ResolveValuation(navSeries, index, definition.Horizon);
            if (valuation.EndIndex <= index ||
                (!valuation.IsComplete && definition.Horizon.Unit == ForecastHorizonUnit.CalendarDays))
            {
                continue;
            }

            var startValue = (double)navSeries[index].Value;
            if (startValue <= 0d)
            {
                continue;
            }

            var (upperThreshold, lowerThreshold) = ResolveThresholds(definition, causalVolatility, index);
            if (!double.IsFinite(upperThreshold) ||
                !double.IsFinite(lowerThreshold) ||
                upperThreshold <= 0d ||
                lowerThreshold <= 0d)
            {
                continue;
            }

            var label = LabelPoint(navSeries, index, valuation, startValue, upperThreshold, lowerThreshold);
            if (label.IsHorizonComplete)
            {
                results.Add(label);
            }
        }

        return results;
    }

    /// <summary>
    /// Labels a single start index.
    /// </summary>
    /// <param name="navSeries">The historical NAV series.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="definition">The triple-barrier definition.</param>
    /// <param name="causalVolatility">Optional volatility estimates aligned to NAV observations.</param>
    /// <returns>The label, or <see langword="null"/> when no future horizon exists.</returns>
    public TripleBarrierOutcome? LabelAt(
        NavSeries navSeries,
        int startIndex,
        TripleBarrierDefinition definition,
        IReadOnlyList<double>? causalVolatility = null)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(definition);
        ValidateDefinition(definition);
        if (startIndex < 0 || startIndex >= navSeries.Count - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "Start index must have future observations.");
        }

        if (causalVolatility is not null && causalVolatility.Count < navSeries.Count)
        {
            throw new ArgumentException("Volatility estimates must be aligned to the NAV series.", nameof(causalVolatility));
        }

        var valuation = this.ResolveValuation(navSeries, startIndex, definition.Horizon);
        if (valuation.EndIndex <= startIndex ||
            (!valuation.IsComplete && definition.Horizon.Unit == ForecastHorizonUnit.CalendarDays))
        {
            return null;
        }

        var startValue = (double)navSeries[startIndex].Value;
        var (upperThreshold, lowerThreshold) = ResolveThresholds(definition, causalVolatility, startIndex);
        if (!double.IsFinite(upperThreshold) ||
            !double.IsFinite(lowerThreshold) ||
            upperThreshold <= 0d ||
            lowerThreshold <= 0d)
        {
            return null;
        }

        var label = LabelPoint(navSeries, startIndex, valuation, startValue, upperThreshold, lowerThreshold);
        return label.IsHorizonComplete ? label : null;
    }

    private static void ValidateDefinition(TripleBarrierDefinition definition)
    {
        if (!double.IsFinite(definition.UpsideThreshold) || definition.UpsideThreshold <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), definition.UpsideThreshold, "Upside threshold must be positive and finite.");
        }

        if (!double.IsFinite(definition.DownsideThreshold) || definition.DownsideThreshold <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), definition.DownsideThreshold, "Downside threshold must be positive and finite.");
        }

        if (!double.IsFinite(definition.UpsideVolatilityMultiplier) || definition.UpsideVolatilityMultiplier <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), definition.UpsideVolatilityMultiplier, "Upside volatility multiplier must be positive and finite.");
        }

        if (!double.IsFinite(definition.DownsideVolatilityMultiplier) || definition.DownsideVolatilityMultiplier <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), definition.DownsideVolatilityMultiplier, "Downside volatility multiplier must be positive and finite.");
        }
    }

    private static (double UpperThreshold, double LowerThreshold) ResolveThresholds(
        TripleBarrierDefinition definition,
        IReadOnlyList<double>? causalVolatility,
        int index)
    {
        if (definition.Policy == BarrierThresholdPolicy.FixedPercentage)
        {
            return (definition.UpsideThreshold, definition.DownsideThreshold);
        }

        if (causalVolatility is null)
        {
            throw new ArgumentException("Volatility-scaled barriers require causal volatility estimates.", nameof(causalVolatility));
        }

        var volatility = causalVolatility[index];
        if (!double.IsFinite(volatility) || volatility <= 0d)
        {
            return (double.NaN, double.NaN);
        }

        return (
            Math.Max(0.0001d, definition.UpsideVolatilityMultiplier * volatility),
            Math.Max(0.0001d, definition.DownsideVolatilityMultiplier * volatility));
    }

    private static TripleBarrierOutcome LabelPoint(
        NavSeries navSeries,
        int startIndex,
        HorizonValuation valuation,
        double startValue,
        double upperThreshold,
        double lowerThreshold)
    {
        var outcome = TripleBarrierOutcomeType.NoBarrierHit;
        var timeToEvent = valuation.EndIndex - startIndex;
        var terminalIndex = valuation.EndIndex;
        var maximumFavorable = double.NegativeInfinity;
        var maximumAdverse = double.PositiveInfinity;

        for (var futureIndex = startIndex + 1; futureIndex <= valuation.EndIndex; futureIndex++)
        {
            var value = (double)navSeries[futureIndex].Value;
            var simpleReturn = (value / startValue) - 1d;
            maximumFavorable = Math.Max(maximumFavorable, simpleReturn);
            maximumAdverse = Math.Min(maximumAdverse, simpleReturn);
            if (simpleReturn >= upperThreshold)
            {
                outcome = TripleBarrierOutcomeType.UpperHitFirst;
                timeToEvent = futureIndex - startIndex;
                terminalIndex = futureIndex;
                break;
            }

            if (simpleReturn <= -lowerThreshold)
            {
                outcome = TripleBarrierOutcomeType.LowerHitFirst;
                timeToEvent = futureIndex - startIndex;
                terminalIndex = futureIndex;
                break;
            }
        }

        var realizedReturn = ((double)navSeries[terminalIndex].Value / startValue) - 1d;
        var isEvaluable = valuation.IsComplete || outcome != TripleBarrierOutcomeType.NoBarrierHit;
        return new TripleBarrierOutcome(
            navSeries[startIndex].Date,
            startIndex,
            outcome,
            timeToEvent,
            realizedReturn,
            double.IsNegativeInfinity(maximumFavorable) ? 0d : maximumFavorable,
            double.IsPositiveInfinity(maximumAdverse) ? 0d : maximumAdverse,
            upperThreshold,
            lowerThreshold,
            valuation.RequestedTargetDate,
            navSeries[terminalIndex].Date,
            valuation.IsCalendarApproximation,
            isEvaluable);
    }

    private HorizonValuation ResolveValuation(NavSeries navSeries, int startIndex, ForecastHorizon horizon)
    {
        if (horizon.Unit == ForecastHorizonUnit.Observations)
        {
            var endIndex = startIndex + horizon.Value;
            return endIndex < navSeries.Count
                ? new HorizonValuation(endIndex, null, false, true)
                : new HorizonValuation(navSeries.Count - 1, null, false, false);
        }

        var resolution = this.horizonResolver.Resolve(
            horizon,
            navSeries[startIndex].Date,
            navSeries.ObservationFrequency);
        if (resolution.TargetDate is null)
        {
            var endIndex = startIndex + resolution.EffectiveObservationCount;
            return endIndex < navSeries.Count
                ? new HorizonValuation(endIndex, null, resolution.IsApproximation, true)
                : new HorizonValuation(navSeries.Count - 1, null, resolution.IsApproximation, false);
        }

        for (var index = startIndex + 1; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= resolution.TargetDate.Value)
            {
                return new HorizonValuation(
                    index,
                    resolution.TargetDate,
                    resolution.IsApproximation || navSeries[index].Date != resolution.TargetDate.Value,
                    true);
            }
        }

        return new HorizonValuation(
            navSeries.Count - 1,
            resolution.TargetDate,
            resolution.IsApproximation,
            false);
    }

    private readonly record struct HorizonValuation(
        int EndIndex,
        DateOnly? RequestedTargetDate,
        bool IsCalendarApproximation,
        bool IsComplete);
}
