using System.Globalization;
using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.TimeSeries;

namespace Aletheia.Dynamics;

/// <summary>
/// Builds the Milestone 1.2 dynamic-state feature vector.
/// </summary>
/// <remarks>
/// The pipeline is the single source of truth for both current and historical
/// states. For a target index <c>i</c>, every feature is calculated from the
/// prefix <c>data[0..i]</c>. No future observations, full-history moments, or
/// future normalization can influence the reconstructed historical state.
/// </remarks>
public sealed class DynamicStateFeaturePipeline : IStateFeaturePipeline
{
    private readonly ReturnCalculator returnCalculator;
    private readonly RiskMetricsCalculator riskCalculator;
    private readonly TimeDomainFeatureCalculator featureCalculator;
    private readonly NumericalDerivativeCalculator derivativeCalculator;
    private readonly DynamicStateEstimatorOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicStateFeaturePipeline"/> class.
    /// </summary>
    /// <param name="returnCalculator">The return calculator.</param>
    /// <param name="riskCalculator">The risk calculator.</param>
    /// <param name="featureCalculator">The time-domain feature calculator.</param>
    /// <param name="derivativeCalculator">The numerical derivative calculator.</param>
    /// <param name="options">The estimator options.</param>
    public DynamicStateFeaturePipeline(
        ReturnCalculator? returnCalculator = null,
        RiskMetricsCalculator? riskCalculator = null,
        TimeDomainFeatureCalculator? featureCalculator = null,
        NumericalDerivativeCalculator? derivativeCalculator = null,
        DynamicStateEstimatorOptions? options = null)
    {
        this.returnCalculator = returnCalculator ?? new ReturnCalculator();
        this.riskCalculator = riskCalculator ?? new RiskMetricsCalculator();
        this.featureCalculator = featureCalculator ?? new TimeDomainFeatureCalculator();
        this.derivativeCalculator = derivativeCalculator ?? new NumericalDerivativeCalculator(this.featureCalculator);
        this.options = options ?? new DynamicStateEstimatorOptions();
        this.Schema = new StateSchemaDescriptor(
            "AletheiaStateSchema",
            "v1.2",
            [
                StandardStateDimensions.SimpleReturn,
                StandardStateDimensions.LogReturn,
                StandardStateDimensions.Trend,
                StandardStateDimensions.Momentum,
                StandardStateDimensions.Volatility,
                StandardStateDimensions.Drawdown,
                StandardStateDimensions.LogNavVelocityPerObservation,
                StandardStateDimensions.LogNavAccelerationPerObservationSquared,
            ],
            CreateFeatureConfiguration(this.options),
            "Milestone 1.2 state: explicit return semantics, frequency-aware volatility, and smoothed log-NAV observation derivatives.");
    }

    /// <inheritdoc />
    public StateSchemaDescriptor Schema { get; }

    /// <inheritdoc />
    public DynamicState Build(NavSeries navSeries, int targetIndex)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (targetIndex < 0 || targetIndex >= navSeries.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(targetIndex), targetIndex, "Target index is outside the NAV series.");
        }

        var prefix = new NavSeries(
            navSeries.Points.Take(targetIndex + 1),
            navSeries.ObservationFrequency);
        var simpleReturns = this.returnCalculator.CalculateSimpleReturns(prefix);
        var logReturns = this.returnCalculator.CalculateLogReturns(prefix);
        var recentSimpleReturn = simpleReturns.Count == 0 ? 0d : simpleReturns[simpleReturns.Count - 1].Value;
        var recentLogReturn = logReturns.Count == 0 ? 0d : logReturns[logReturns.Count - 1].Value;
        var volatilityWindow = logReturns.Count <= this.options.VolatilityLookback
            ? logReturns
            : logReturns.Slice(logReturns[logReturns.Count - this.options.VolatilityLookback].Date, null);
        var logNavSeries = ToLogNavSeries(prefix);
        var smoothingWindow = this.options.DerivativeSmoothingMethod == SmoothingMethod.None
            ? 1
            : this.options.DerivativeSmoothingWindow;
        var velocity = this.derivativeCalculator.CalculateFirstDerivativePerObservation(
            logNavSeries,
            smoothingWindow);
        var acceleration = this.derivativeCalculator.CalculateSecondDerivativePerObservationSquared(
            logNavSeries,
            smoothingWindow);

        var dimensions = new Dictionary<StateDimension, double>
        {
            [StandardStateDimensions.SimpleReturn] = recentSimpleReturn,
            [StandardStateDimensions.LogReturn] = recentLogReturn,
            [StandardStateDimensions.Trend] = this.featureCalculator.CalculateFirstOrderTrend(prefix, this.options.TrendLookback),
            [StandardStateDimensions.Momentum] = this.featureCalculator.CalculateMomentum(prefix, this.options.MomentumLookback),
            [StandardStateDimensions.Volatility] = this.riskCalculator.CalculateAnnualizedVolatility(volatilityWindow),
            [StandardStateDimensions.Drawdown] = this.riskCalculator.CalculateCurrentDrawdown(prefix),
            [StandardStateDimensions.LogNavVelocityPerObservation] = velocity.Count == 0 ? 0d : velocity[velocity.Count - 1].Value,
            [StandardStateDimensions.LogNavAccelerationPerObservationSquared] = acceleration.Count == 0 ? 0d : acceleration[acceleration.Count - 1].Value,
        };

        var dataAdequacy = Math.Clamp((double)prefix.Count / this.options.FullDataAdequacyObservationCount, 0d, 1d);
        return new DynamicState(prefix.EndDate, dimensions, dataAdequacy, this.Schema);
    }

    private static IReadOnlyDictionary<string, string> CreateFeatureConfiguration(DynamicStateEstimatorOptions options)
    {
        return new Dictionary<string, string>
        {
            ["TrendLookbackObservations"] = options.TrendLookback.ToString(CultureInfo.InvariantCulture),
            ["MomentumLookbackObservations"] = options.MomentumLookback.ToString(CultureInfo.InvariantCulture),
            ["VolatilityLookbackObservations"] = options.VolatilityLookback.ToString(CultureInfo.InvariantCulture),
            ["DerivativeSmoothingWindowObservations"] = options.DerivativeSmoothingWindow.ToString(CultureInfo.InvariantCulture),
            ["DerivativeSmoothingMethod"] = options.DerivativeSmoothingMethod.ToString(),
            ["DerivativeRepresentation"] = "LogNav",
            ["DerivativeUnit"] = "PerObservation",
            ["VolatilityAnnualization"] = "ObservationFrequencyConvention",
            ["FullDataAdequacyObservationCount"] = options.FullDataAdequacyObservationCount.ToString(CultureInfo.InvariantCulture),
            ["StateAlgorithmVersion"] = "1.2",
        };
    }

    private static TimeSeries<double> ToLogNavSeries(NavSeries navSeries)
    {
        var points = new List<TimeSeriesPoint<double>>(navSeries.Count);
        for (var index = 0; index < navSeries.Count; index++)
        {
            var nav = navSeries[index].Value;
            if (nav <= 0m)
            {
                throw new ArgumentException("Log-NAV features require positive NAV values.", nameof(navSeries));
            }

            points.Add(new TimeSeriesPoint<double>(navSeries[index].Date, Math.Log((double)nav)));
        }

        return new TimeSeries<double>(points, navSeries.ObservationFrequency);
    }
}
