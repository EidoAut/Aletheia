#pragma warning disable SA1204 // Causal helper grouping follows the pipeline workflow.

using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.Dynamics;
using Aletheia.Mathematics;

namespace Aletheia.Validation;

/// <summary>
/// Builds causal feature vectors for market-timing models.
/// </summary>
public sealed class MarketTimingFeaturePipeline
{
    private static readonly string[] OrderedFeatureNames =
    [
        "lag_return_1",
        "rolling_return_5",
        "rolling_return_20",
        "momentum_20_minus_60",
        "acceleration",
        "kalman_level",
        "kalman_trend",
        "kalman_trend_uncertainty",
        "kalman_innovation",
        "kalman_normalized_innovation",
        "rolling_volatility",
        "ewma_volatility",
        "garch_or_ewma_volatility",
        "volatility_acceleration",
        "volatility_percentile",
        "current_drawdown",
        "drawdown_duration",
        "distance_from_high",
        "recovery_velocity",
        "hmm_bull_probability",
        "hmm_bear_probability",
        "hmm_expected_return",
        "hmm_expected_volatility",
        "hmm_leave_current_probability",
        "analogue_expected_forward_return",
        "analogue_probability_positive",
        "analogue_probability_downside",
        "analogue_dispersion",
        "analogue_effective_sample_size",
        "spectral_phase",
        "spectral_period",
        "spectral_stability",
        "spectral_phase_derivative",
        "ensemble_expected_return",
        "ensemble_probability_positive",
        "ensemble_downside_probability",
        "forecast_dispersion",
        "model_disagreement",
        "ensemble_reliability",
        "change_point_probability",
    ];

    private readonly ReturnCalculator returnCalculator = new();
    private readonly Garch11Estimator garchEstimator = new();
    private readonly LocalLinearTrendKalmanModel kalmanModel = new();
    private readonly GaussianHiddenMarkovModel hmmModel = new();
    private readonly OnlineWindowChangePointDetector changePointDetector = new();

    /// <summary>
    /// Builds causal feature vectors for all observations from <paramref name="minimumIndex"/> through the end.
    /// </summary>
    /// <param name="navSeries">The historical NAV series.</param>
    /// <param name="minimumIndex">The first eligible observation index.</param>
    /// <param name="externalEvidence">Optional validation-gated external evidence.</param>
    /// <param name="enableStateModelFeatures">A value indicating whether GARCH, Kalman, and HMM state features should be fitted.</param>
    /// <param name="hmmMaximumIterations">The maximum number of HMM training iterations when state features are enabled.</param>
    /// <returns>The feature pipeline result.</returns>
    public MarketTimingFeaturePipelineResult Build(
        NavSeries navSeries,
        int minimumIndex = 120,
        MarketTimingExternalEvidence? externalEvidence = null,
        bool enableStateModelFeatures = true,
        int hmmMaximumIterations = 100)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        if (hmmMaximumIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hmmMaximumIterations), hmmMaximumIterations, "HMM iterations must be positive.");
        }

        if (navSeries.Count < 2)
        {
            return new MarketTimingFeaturePipelineResult(
                Array.Empty<MarketTimingFeatureVector>(),
                OrderedFeatureNames,
                false,
                "Insufficient NAV history for timing features.",
                0d,
                0d);
        }

        var simpleReturns = this.returnCalculator.CalculateSimpleReturns(navSeries).ToValueArray();
        var logReturns = this.returnCalculator.CalculateLogReturns(navSeries).ToValueArray();
        var ewmaVariancePath = BuildCausalEwmaVariancePath(logReturns);
        var currentEwmaVolatility = ewmaVariancePath.Length == 0
            ? 0d
            : Math.Sqrt(Math.Max(0d, ewmaVariancePath[^1]));
        var rollingVolatility = RollingStandardDeviation(logReturns, 20);
        var changePoints = this.changePointDetector.Estimate(logReturns);
        var stateFeatures = enableStateModelFeatures
            ? this.BuildCausalStateFeatures(logReturns, ewmaVariancePath, hmmMaximumIterations)
            : BuildEwmaOnlyStateFeatures(logReturns, ewmaVariancePath);
        var currentStateFeatures = stateFeatures.Length == 0
            ? StateFeatureSet.Empty(currentEwmaVolatility)
            : stateFeatures[^1];
        var currentGarchOrEwma = currentStateFeatures.GarchOrEwmaVolatility;
        var volatilityDiagnostic = !enableStateModelFeatures
            ? "Automatic timing profile uses causal EWMA volatility; GARCH, Kalman, and HMM features are skipped."
            : currentStateFeatures.GarchConverged
            ? "Causal GARCH conditional volatility used for the current state."
            : $"Causal GARCH fallback to EWMA: {currentStateFeatures.GarchDiagnostic}";
        var features = new List<MarketTimingFeatureVector>();
        var peak = (double)navSeries[0].Value;
        var drawdownDuration = 0;
        var previousDrawdown = 0d;
        var startIndex = Math.Max(1, Math.Min(navSeries.Count - 1, minimumIndex));

        for (var index = 1; index < navSeries.Count; index++)
        {
            var nav = (double)navSeries[index].Value;
            if (nav >= peak)
            {
                peak = nav;
                drawdownDuration = 0;
            }
            else
            {
                drawdownDuration++;
            }

            if (index < startIndex)
            {
                previousDrawdown = (nav / peak) - 1d;
                continue;
            }

            var returnIndex = index - 1;
            var drawdown = (nav / peak) - 1d;
            var volatility = returnIndex < rollingVolatility.Length ? rollingVolatility[returnIndex] : 0d;
            var ewmaVolatility = returnIndex < ewmaVariancePath.Length
                ? Math.Sqrt(Math.Max(0d, ewmaVariancePath[returnIndex]))
                : currentEwmaVolatility;
            var state = returnIndex < stateFeatures.Length
                ? stateFeatures[returnIndex]
                : StateFeatureSet.Empty(ewmaVolatility);
            var featureValues = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["lag_return_1"] = Safe(simpleReturns[returnIndex]),
                ["rolling_return_5"] = CumulativeReturn(logReturns, returnIndex, 5),
                ["rolling_return_20"] = CumulativeReturn(logReturns, returnIndex, 20),
                ["momentum_20_minus_60"] = CumulativeReturn(logReturns, returnIndex, 20) - CumulativeReturn(logReturns, returnIndex, 60),
                ["acceleration"] = Acceleration(logReturns, returnIndex),
                ["kalman_level"] = Safe(state.KalmanLevel),
                ["kalman_trend"] = Safe(state.KalmanTrend),
                ["kalman_trend_uncertainty"] = Safe(state.KalmanTrendUncertainty),
                ["kalman_innovation"] = Safe(state.KalmanInnovation),
                ["kalman_normalized_innovation"] = Safe(state.KalmanNormalizedInnovation),
                ["rolling_volatility"] = Safe(volatility),
                ["ewma_volatility"] = Safe(ewmaVolatility),
                ["garch_or_ewma_volatility"] = Safe(state.GarchOrEwmaVolatility),
                ["volatility_acceleration"] = Safe(returnIndex <= 0 || returnIndex >= rollingVolatility.Length ? 0d : rollingVolatility[returnIndex] - rollingVolatility[returnIndex - 1]),
                ["volatility_percentile"] = PercentileRank(rollingVolatility, returnIndex, volatility),
                ["current_drawdown"] = Safe(drawdown),
                ["drawdown_duration"] = drawdownDuration,
                ["distance_from_high"] = Safe(Math.Abs(drawdown)),
                ["recovery_velocity"] = Safe(drawdown - previousDrawdown),
                ["hmm_bull_probability"] = state.Hmm.BullProbability,
                ["hmm_bear_probability"] = state.Hmm.BearProbability,
                ["hmm_expected_return"] = state.Hmm.ExpectedReturn,
                ["hmm_expected_volatility"] = state.Hmm.ExpectedVolatility,
                ["hmm_leave_current_probability"] = state.Hmm.LeaveCurrentProbability,
                ["change_point_probability"] = returnIndex < changePoints.Count ? changePoints[returnIndex].ProbabilityChangePoint : 0d,
            };
            if (index == navSeries.Count - 1)
            {
                AddCurrentExternalEvidence(featureValues, externalEvidence);
            }

            features.Add(new MarketTimingFeatureVector(navSeries[index].Date, index, featureValues));
            previousDrawdown = drawdown;
        }

        var availableFeatureNames = features
            .SelectMany(feature => feature.Values.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => Array.IndexOf(OrderedFeatureNames, name) < 0 ? int.MaxValue : Array.IndexOf(OrderedFeatureNames, name))
            .ThenBy(name => name, StringComparer.Ordinal)
            .ToArray();
        return new MarketTimingFeaturePipelineResult(
            features,
            availableFeatureNames,
            currentStateFeatures.GarchConverged,
            volatilityDiagnostic,
            currentGarchOrEwma,
            currentEwmaVolatility);
    }

    private StateFeatureSet[] BuildCausalStateFeatures(
        IReadOnlyList<double> logReturns,
        IReadOnlyList<double> ewmaVariancePath,
        int hmmMaximumIterations)
    {
        var result = new StateFeatureSet[logReturns.Count];
        GaussianHmmResult? lastHmm = null;
        IReadOnlyList<double>? lastHmmFiltered = null;
        Garch11FitResult? lastGarch = null;
        double? lastGarchVariance = null;
        for (var returnIndex = 0; returnIndex < logReturns.Count; returnIndex++)
        {
            var ewmaVolatility = returnIndex < ewmaVariancePath.Count
                ? Math.Sqrt(Math.Max(0d, ewmaVariancePath[returnIndex]))
                : 0d;
            var prefix = Prefix(logReturns, returnIndex + 1);
            var kalmanEstimate = this.kalmanModel.Filter(prefix).LastEstimate;

            if (prefix.Length >= 30 && (returnIndex == logReturns.Count - 1 || returnIndex % 20 == 0 || lastGarch is null))
            {
                lastGarch = this.garchEstimator.Fit(prefix);
                lastGarchVariance = lastGarch.Converged && lastGarch.ConditionalVariances.Count > 0
                    ? lastGarch.ConditionalVariances[^1]
                    : null;
            }
            else if (lastGarch?.Converged == true && lastGarchVariance.HasValue && returnIndex > 0)
            {
                lastGarchVariance = lastGarch.NextConditionalVariance(logReturns[returnIndex - 1], lastGarchVariance.Value);
            }

            if (prefix.Length >= 60 && (returnIndex == logReturns.Count - 1 || returnIndex % 20 == 0 || lastHmm is null))
            {
                lastHmm = this.hmmModel.Fit(prefix, prefix.Length >= 300 ? 3 : 2, hmmMaximumIterations);
                lastHmmFiltered = lastHmm.LatestProbabilities;
            }
            else if (lastHmm is not null && lastHmmFiltered is not null)
            {
                lastHmmFiltered = this.hmmModel.FilterNext(lastHmm, lastHmmFiltered, logReturns[returnIndex]);
            }

            var garchVolatility = lastGarch?.Converged == true && lastGarchVariance.HasValue
                ? Math.Sqrt(Math.Max(0d, lastGarchVariance.Value))
                : ewmaVolatility;
            result[returnIndex] = new StateFeatureSet(
                Safe(kalmanEstimate?.Level ?? 0d),
                Safe(kalmanEstimate?.Trend ?? 0d),
                Safe(kalmanEstimate is null ? 0d : Math.Sqrt(Math.Max(0d, kalmanEstimate.TrendVariance))),
                Safe(kalmanEstimate?.Innovation ?? 0d),
                Safe(kalmanEstimate is null ? 0d : kalmanEstimate.Innovation / Math.Sqrt(Math.Max(1e-12d, kalmanEstimate.InnovationVariance))),
                Safe(garchVolatility),
                lastGarch?.Converged == true,
                lastGarch?.Diagnostic ?? "GARCH(1,1) has not yet reached its minimum causal sample count.",
                BuildHmmFeatures(lastHmm, lastHmmFiltered));
        }

        return result;
    }

    private static StateFeatureSet[] BuildEwmaOnlyStateFeatures(
        IReadOnlyList<double> logReturns,
        IReadOnlyList<double> ewmaVariancePath)
    {
        var result = new StateFeatureSet[logReturns.Count];
        for (var returnIndex = 0; returnIndex < logReturns.Count; returnIndex++)
        {
            var volatility = returnIndex < ewmaVariancePath.Count
                ? Math.Sqrt(Math.Max(0d, ewmaVariancePath[returnIndex]))
                : 0d;
            result[returnIndex] = StateFeatureSet.Empty(volatility);
        }

        return result;
    }

    private static double[] Prefix(IReadOnlyList<double> values, int count)
    {
        var result = new double[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = values[index];
        }

        return result;
    }

    private static double Safe(double value)
    {
        return double.IsFinite(value) ? value : 0d;
    }

    private static void AddCurrentExternalEvidence(
        IDictionary<string, double> featureValues,
        MarketTimingExternalEvidence? externalEvidence)
    {
        if (externalEvidence is null)
        {
            return;
        }

        if (externalEvidence.SpectralReliability >= 0.4d)
        {
            AddIfFinite(featureValues, "spectral_phase", externalEvidence.SpectralPhase);
            AddIfFinite(featureValues, "spectral_stability", externalEvidence.SpectralStability);
        }

        AddIfFinite(featureValues, "ensemble_expected_return", externalEvidence.EnsembleExpectedReturn);
        AddIfFinite(featureValues, "ensemble_probability_positive", externalEvidence.EnsembleProbabilityPositive);
        AddIfFinite(featureValues, "ensemble_downside_probability", externalEvidence.EnsembleDownsideProbability);
        AddIfFinite(featureValues, "forecast_dispersion", externalEvidence.EnsembleDisagreement);
        AddIfFinite(featureValues, "model_disagreement", externalEvidence.EnsembleDisagreement);
        AddIfFinite(featureValues, "ensemble_reliability", externalEvidence.EnsembleReliability);
    }

    private static void AddIfFinite(
        IDictionary<string, double> featureValues,
        string name,
        double? value)
    {
        if (value.HasValue && double.IsFinite(value.Value))
        {
            featureValues[name] = value.Value;
        }
    }

    private static double CumulativeReturn(IReadOnlyList<double> logReturns, int endIndex, int window)
    {
        if (endIndex < 0 || logReturns.Count == 0)
        {
            return 0d;
        }

        var start = Math.Max(0, endIndex - window + 1);
        var sum = 0d;
        for (var index = start; index <= endIndex && index < logReturns.Count; index++)
        {
            sum += logReturns[index];
        }

        return Math.Exp(sum) - 1d;
    }

    private static double Acceleration(IReadOnlyList<double> logReturns, int index)
    {
        if (index < 2)
        {
            return 0d;
        }

        return (logReturns[index] - logReturns[index - 1]) - (logReturns[index - 1] - logReturns[index - 2]);
    }

    private static double[] BuildCausalEwmaVariancePath(IReadOnlyList<double> values, double lambda = 0.94d)
    {
        var variances = new double[values.Count];
        if (values.Count == 0)
        {
            return variances;
        }

        variances[0] = values[0] * values[0];
        for (var index = 1; index < values.Count; index++)
        {
            variances[index] = (lambda * variances[index - 1]) + ((1d - lambda) * values[index - 1] * values[index - 1]);
        }

        return variances;
    }

    private static double[] RollingStandardDeviation(IReadOnlyList<double> values, int window)
    {
        var result = new double[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            var start = Math.Max(0, index - window + 1);
            var count = index - start + 1;
            if (count < 2)
            {
                result[index] = 0d;
                continue;
            }

            var slice = new double[count];
            for (var offset = 0; offset < count; offset++)
            {
                slice[offset] = values[start + offset];
            }

            result[index] = DescriptiveStatistics.SampleStandardDeviation(slice);
        }

        return result;
    }

    private static double PercentileRank(IReadOnlyList<double> values, int endIndex, double currentValue)
    {
        if (endIndex <= 0 || !double.IsFinite(currentValue))
        {
            return 0.5d;
        }

        var finite = 0;
        var lessOrEqual = 0;
        for (var index = 0; index <= endIndex && index < values.Count; index++)
        {
            if (!double.IsFinite(values[index]))
            {
                continue;
            }

            finite++;
            if (values[index] <= currentValue)
            {
                lessOrEqual++;
            }
        }

        return finite == 0 ? 0.5d : lessOrEqual / (double)finite;
    }

    private static HmmFeatureSet BuildHmmFeatures(GaussianHmmResult? hmm, IReadOnlyList<double>? filteredProbabilities)
    {
        if (hmm is null || hmm.States.Count == 0 || filteredProbabilities is null || filteredProbabilities.Count != hmm.States.Count)
        {
            return new HmmFeatureSet(0.5d, 0.5d, 0d, 0d, 0d);
        }

        var stateCount = hmm.States.Count;
        var expectedReturn = 0d;
        var expectedVolatility = 0d;
        var bullProbability = 0d;
        var bearProbability = 0d;
        var currentState = 0;
        var currentProbability = 0d;
        for (var state = 0; state < stateCount; state++)
        {
            var probability = filteredProbabilities[state];
            expectedReturn += probability * hmm.States[state].Mean;
            expectedVolatility += probability * Math.Sqrt(Math.Max(0d, hmm.States[state].Variance));
            if (hmm.States[state].Mean >= 0d)
            {
                bullProbability += probability;
            }

            if (hmm.States[state].Label.Contains("Bear", StringComparison.OrdinalIgnoreCase) ||
                hmm.States[state].Variance >= hmm.States.Max(item => item.Variance))
            {
                bearProbability += probability;
            }

            if (probability > currentProbability)
            {
                currentState = state;
                currentProbability = probability;
            }
        }

        var stayProbability = hmm.TransitionMatrix[currentState, currentState];
        return new HmmFeatureSet(
            Math.Clamp(bullProbability, 0d, 1d),
            Math.Clamp(bearProbability, 0d, 1d),
            expectedReturn,
            expectedVolatility,
            Math.Clamp(1d - stayProbability, 0d, 1d));
    }

    private sealed record HmmFeatureSet(
        double BullProbability,
        double BearProbability,
        double ExpectedReturn,
        double ExpectedVolatility,
        double LeaveCurrentProbability);

    private sealed record StateFeatureSet(
        double KalmanLevel,
        double KalmanTrend,
        double KalmanTrendUncertainty,
        double KalmanInnovation,
        double KalmanNormalizedInnovation,
        double GarchOrEwmaVolatility,
        bool GarchConverged,
        string GarchDiagnostic,
        HmmFeatureSet Hmm)
    {
        public static StateFeatureSet Empty(double volatility) =>
            new(0d, 0d, 0d, 0d, 0d, Safe(volatility), false, "EWMA volatility fallback.", new HmmFeatureSet(0.5d, 0.5d, 0d, 0d, 0d));
    }
}
