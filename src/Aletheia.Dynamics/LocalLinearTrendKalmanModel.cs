using Aletheia.Mathematics;

namespace Aletheia.Dynamics;

/// <summary>
/// Implements a univariate local linear trend state-space model with Kalman filtering.
/// </summary>
public sealed class LocalLinearTrendKalmanModel
{
    private const double MinimumVariance = 1e-12d;

    /// <summary>
    /// Filters observations with a local linear trend model.
    /// </summary>
    /// <param name="observations">The finite observations in chronological order.</param>
    /// <param name="observationVariance">The optional observation noise variance.</param>
    /// <param name="levelVariance">The optional level process variance.</param>
    /// <param name="trendVariance">The optional trend process variance.</param>
    /// <returns>The filter result.</returns>
    public KalmanFilterResult Filter(
        IReadOnlyList<double> observations,
        double? observationVariance = null,
        double? levelVariance = null,
        double? trendVariance = null)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count == 0)
        {
            return new KalmanFilterResult(Array.Empty<KalmanStateEstimate>(), 0d, 0d, 0d, 0d);
        }

        ValidateFinite(observations);
        var scale = observations.Count < 2
            ? MinimumVariance
            : Math.Max(MinimumVariance, DescriptiveStatistics.SampleVariance(observations));
        var r = ValidateVariance(observationVariance ?? scale * 0.25d, nameof(observationVariance));
        var qLevel = ValidateVariance(levelVariance ?? scale * 0.05d, nameof(levelVariance));
        var qTrend = ValidateVariance(trendVariance ?? scale * 0.005d, nameof(trendVariance));

        var level = observations[0];
        var trend = observations.Count > 1 ? observations[1] - observations[0] : 0d;
        var p00 = scale;
        var p01 = 0d;
        var p10 = 0d;
        var p11 = scale;
        var estimates = new List<KalmanStateEstimate>(observations.Count);
        var logLikelihood = 0d;

        for (var index = 0; index < observations.Count; index++)
        {
            var predictedLevel = level + trend;
            var predictedTrend = trend;
            var pp00 = p00 + p01 + p10 + p11 + qLevel;
            var pp01 = p01 + p11;
            var pp10 = p10 + p11;
            var pp11 = p11 + qTrend;

            var innovation = observations[index] - predictedLevel;
            var innovationVariance = Math.Max(MinimumVariance, pp00 + r);
            var k0 = pp00 / innovationVariance;
            var k1 = pp10 / innovationVariance;

            level = predictedLevel + (k0 * innovation);
            trend = predictedTrend + (k1 * innovation);
            p00 = (1d - k0) * pp00;
            p01 = (1d - k0) * pp01;
            p10 = pp10 - (k1 * pp00);
            p11 = pp11 - (k1 * pp01);
            var levelTrendCovariance = 0.5d * (p01 + p10);
            p01 = levelTrendCovariance;
            p10 = levelTrendCovariance;

            logLikelihood += -0.5d * (Math.Log(2d * Math.PI) + Math.Log(innovationVariance) + (innovation * innovation / innovationVariance));
            estimates.Add(new KalmanStateEstimate(
                index,
                observations[index],
                level,
                trend,
                Math.Max(0d, p00),
                Math.Max(0d, p11),
                innovation,
                innovationVariance,
                levelTrendCovariance));
        }

        return new KalmanFilterResult(estimates, logLikelihood, r, qLevel, qTrend);
    }

    /// <summary>
    /// Forecasts future observations from a fitted filter result.
    /// </summary>
    /// <param name="filter">The fitted filter result.</param>
    /// <param name="steps">The positive number of steps to forecast.</param>
    /// <returns>Forecast points in input units.</returns>
    public IReadOnlyList<KalmanForecastPoint> Forecast(KalmanFilterResult filter, int steps)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (steps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), steps, "Forecast steps must be positive.");
        }

        var last = filter.LastEstimate;
        if (last is null)
        {
            return Array.Empty<KalmanForecastPoint>();
        }

        var level = last.Level;
        var trend = last.Trend;
        var p00 = last.LevelVariance;
        var p01 = last.LevelTrendCovariance;
        var p10 = last.LevelTrendCovariance;
        var p11 = last.TrendVariance;
        var forecasts = new List<KalmanForecastPoint>(steps);

        for (var step = 1; step <= steps; step++)
        {
            level += trend;
            var pp00 = p00 + p01 + p10 + p11 + filter.LevelVariance;
            var pp01 = p01 + p11;
            var pp10 = p10 + p11;
            var pp11 = p11 + filter.TrendVariance;
            p00 = pp00;
            p01 = pp01;
            p10 = pp10;
            p11 = pp11;

            var variance = Math.Max(MinimumVariance, p00 + filter.ObservationVariance);
            var standardDeviation = Math.Sqrt(variance);
            forecasts.Add(new KalmanForecastPoint(
                step,
                level,
                variance,
                level - (1.96d * standardDeviation),
                level + (1.96d * standardDeviation)));
        }

        return forecasts;
    }

    private static double ValidateVariance(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Variance must be finite and non-negative.");
        }

        return Math.Max(MinimumVariance, value);
    }

    private static void ValidateFinite(IReadOnlyList<double> observations)
    {
        for (var index = 0; index < observations.Count; index++)
        {
            if (!double.IsFinite(observations[index]))
            {
                throw new ArgumentException("Kalman filtering requires finite observations.", nameof(observations));
            }
        }
    }
}
