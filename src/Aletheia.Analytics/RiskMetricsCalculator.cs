using Aletheia.Core;
using Aletheia.Mathematics;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics;

/// <summary>
/// Calculates risk metrics from return and NAV series.
/// </summary>
public sealed class RiskMetricsCalculator
{
    private readonly IAnnualizationConvention annualizationConvention;
    private readonly IIrregularAnnualizationEstimator? irregularAnnualizationEstimator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskMetricsCalculator"/> class.
    /// </summary>
    /// <param name="annualizationConvention">The convention used when periods per year are not supplied.</param>
    /// <param name="irregularAnnualizationEstimator">The optional elapsed-time policy for irregular timestamps.</param>
    public RiskMetricsCalculator(
        IAnnualizationConvention? annualizationConvention = null,
        IIrregularAnnualizationEstimator? irregularAnnualizationEstimator = null)
    {
        this.annualizationConvention = annualizationConvention ?? StandardAnnualizationConvention.Default;
        this.irregularAnnualizationEstimator = irregularAnnualizationEstimator;
    }

    /// <summary>
    /// Calculates realized volatility as sample standard deviation of returns.
    /// </summary>
    /// <param name="returns">The return series.</param>
    /// <returns>Sample standard deviation of returns.</returns>
    public double CalculateVolatility(TimeSeries<double> returns)
    {
        ArgumentNullException.ThrowIfNull(returns);

        if (returns.Count < 2)
        {
            return 0d;
        }

        return DescriptiveStatistics.SampleStandardDeviation(returns.ToValueArray());
    }

    /// <summary>
    /// Calculates annualized realized volatility.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="periodsPerYear">The optional explicit number of return periods per year.</param>
    /// <returns>Annualized volatility.</returns>
    public double CalculateAnnualizedVolatility(TimeSeries<double> returns, double? periodsPerYear = null)
    {
        ArgumentNullException.ThrowIfNull(returns);
        if (returns.Count < 2)
        {
            return 0d;
        }

        var resolvedPeriodsPerYear = this.ResolvePeriodsPerYear(returns, periodsPerYear);

        return this.CalculateVolatility(returns) * Math.Sqrt(resolvedPeriodsPerYear);
    }

    /// <summary>
    /// Calculates maximum drawdown from a NAV series.
    /// </summary>
    /// <remarks>
    /// Drawdown at time <c>t</c> is <c>P_t / max(P_0...P_t) - 1</c>. It is
    /// represented as a negative return, so a 20% loss is <c>-0.20</c>.
    /// </remarks>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>The maximum drawdown result.</returns>
    public DrawdownResult CalculateMaximumDrawdown(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count == 0)
        {
            return new DrawdownResult(0d, null, null, 0, null);
        }

        var peakValue = navSeries[0].Value;
        var peakDate = navSeries[0].Date;
        var worstDrawdown = 0d;
        DateOnly? worstPeakDate = null;
        DateOnly? troughDate = null;
        DateOnly? recoveryDate = null;

        for (var index = 1; index < navSeries.Count; index++)
        {
            var point = navSeries[index];
            if (point.Value >= peakValue)
            {
                if (troughDate.HasValue && recoveryDate is null && point.Value >= peakValue)
                {
                    recoveryDate = point.Date;
                }

                peakValue = point.Value;
                peakDate = point.Date;
                continue;
            }

            var drawdown = ((double)point.Value / (double)peakValue) - 1d;
            if (drawdown < worstDrawdown)
            {
                worstDrawdown = drawdown;
                worstPeakDate = peakDate;
                troughDate = point.Date;
                recoveryDate = null;
            }
        }

        var durationDays = worstPeakDate.HasValue && troughDate.HasValue
            ? troughDate.Value.DayNumber - worstPeakDate.Value.DayNumber
            : 0;

        return new DrawdownResult(worstDrawdown, worstPeakDate, troughDate, durationDays, recoveryDate);
    }

    /// <summary>
    /// Calculates the current drawdown from the running high-water mark.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>The current drawdown as a negative return.</returns>
    public double CalculateCurrentDrawdown(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count == 0)
        {
            return 0d;
        }

        var peak = navSeries[0].Value;
        for (var index = 1; index < navSeries.Count; index++)
        {
            if (navSeries[index].Value > peak)
            {
                peak = navSeries[index].Value;
            }
        }

        return ((double)navSeries[navSeries.Count - 1].Value / (double)peak) - 1d;
    }

    /// <summary>
    /// Calculates downside deviation relative to a target return.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="targetReturn">The target return per period.</param>
    /// <returns>Downside deviation.</returns>
    public double CalculateDownsideDeviation(TimeSeries<double> returns, double targetReturn = 0d)
    {
        ArgumentNullException.ThrowIfNull(returns);

        if (returns.Count == 0)
        {
            return 0d;
        }

        var sumSquaredShortfall = 0d;
        var values = returns.ToValueArray();
        for (var index = 0; index < values.Length; index++)
        {
            var shortfall = Math.Min(0d, values[index] - targetReturn);
            sumSquaredShortfall += shortfall * shortfall;
        }

        return Math.Sqrt(sumSquaredShortfall / values.Length);
    }

    /// <summary>
    /// Calculates semideviation below the arithmetic mean return.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <returns>Downside semideviation.</returns>
    public double CalculateSemideviation(TimeSeries<double> returns)
    {
        ArgumentNullException.ThrowIfNull(returns);

        if (returns.Count == 0)
        {
            return 0d;
        }

        return this.CalculateDownsideDeviation(returns, DescriptiveStatistics.Mean(returns.ToValueArray()));
    }

    /// <summary>
    /// Calculates historical value-at-risk as a positive loss threshold.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="confidenceLevel">The confidence level in [0, 1].</param>
    /// <returns>The positive historical VaR loss.</returns>
    public double CalculateHistoricalValueAtRisk(TimeSeries<double> returns, double confidenceLevel = 0.95d)
    {
        ArgumentNullException.ThrowIfNull(returns);
        ValidateProbability(confidenceLevel, nameof(confidenceLevel));

        if (returns.Count == 0)
        {
            return 0d;
        }

        var tailProbability = 1d - confidenceLevel;
        var quantile = DescriptiveStatistics.Quantile(returns.ToValueArray(), tailProbability);
        return Math.Max(0d, -quantile);
    }

    /// <summary>
    /// Calculates parametric Gaussian value-at-risk as a positive loss threshold.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="confidenceLevel">The confidence level in [0, 1].</param>
    /// <returns>The positive parametric VaR loss.</returns>
    public double CalculateParametricGaussianValueAtRisk(TimeSeries<double> returns, double confidenceLevel = 0.95d)
    {
        ArgumentNullException.ThrowIfNull(returns);
        ValidateProbability(confidenceLevel, nameof(confidenceLevel));

        if (returns.Count < 2)
        {
            return 0d;
        }

        var values = returns.ToValueArray();
        var mean = DescriptiveStatistics.Mean(values);
        var standardDeviation = DescriptiveStatistics.SampleStandardDeviation(values);
        if (standardDeviation == 0d)
        {
            return Math.Max(0d, -mean);
        }

        var z = InverseStandardNormal(1d - confidenceLevel);
        return Math.Max(0d, -(mean + (standardDeviation * z)));
    }

    /// <summary>
    /// Calculates historical expected shortfall as a positive average tail loss.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="confidenceLevel">The confidence level in [0, 1].</param>
    /// <returns>The positive expected shortfall loss.</returns>
    public double CalculateExpectedShortfall(TimeSeries<double> returns, double confidenceLevel = 0.95d)
    {
        ArgumentNullException.ThrowIfNull(returns);
        ValidateProbability(confidenceLevel, nameof(confidenceLevel));

        if (returns.Count == 0)
        {
            return 0d;
        }

        var values = returns.ToValueArray();
        var cutoff = DescriptiveStatistics.Quantile(values, 1d - confidenceLevel);
        var tail = values.Where(value => value <= cutoff).ToArray();
        if (tail.Length == 0)
        {
            return 0d;
        }

        return Math.Max(0d, -DescriptiveStatistics.Mean(tail));
    }

    /// <summary>
    /// Calculates the ulcer index from a NAV series.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>The root mean square drawdown depth.</returns>
    public double CalculateUlcerIndex(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count == 0)
        {
            return 0d;
        }

        var peak = navSeries[0].Value;
        var sumSquares = 0d;
        for (var index = 0; index < navSeries.Count; index++)
        {
            if (navSeries[index].Value > peak)
            {
                peak = navSeries[index].Value;
            }

            var drawdown = ((double)navSeries[index].Value / (double)peak) - 1d;
            sumSquares += drawdown * drawdown;
        }

        return Math.Sqrt(sumSquares / navSeries.Count);
    }

    /// <summary>
    /// Calculates average and duration statistics for completed drawdown episodes.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>Drawdown statistics.</returns>
    public DrawdownStatistics CalculateDrawdownStatistics(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count == 0)
        {
            return new DrawdownStatistics(0d, 0d, 0, 0);
        }

        var peak = navSeries[0].Value;
        DateOnly? underwaterStart = null;
        var completedDurations = new List<int>();
        var underwaterDrawdowns = new List<double>();
        var maximumDuration = 0;

        for (var index = 0; index < navSeries.Count; index++)
        {
            var point = navSeries[index];
            if (point.Value >= peak)
            {
                if (underwaterStart.HasValue)
                {
                    var duration = point.Date.DayNumber - underwaterStart.Value.DayNumber;
                    completedDurations.Add(duration);
                    maximumDuration = Math.Max(maximumDuration, duration);
                    underwaterStart = null;
                }

                peak = point.Value;
                continue;
            }

            underwaterStart ??= point.Date;
            var drawdown = ((double)point.Value / (double)peak) - 1d;
            underwaterDrawdowns.Add(drawdown);
            maximumDuration = Math.Max(maximumDuration, point.Date.DayNumber - underwaterStart.Value.DayNumber);
        }

        var averageDrawdown = underwaterDrawdowns.Count == 0
            ? 0d
            : DescriptiveStatistics.Mean(underwaterDrawdowns);
        var averageDuration = completedDurations.Count == 0
            ? 0d
            : DescriptiveStatistics.Mean(completedDurations.Select(value => (double)value).ToArray());

        return new DrawdownStatistics(
            averageDrawdown,
            averageDuration,
            maximumDuration,
            completedDurations.Count);
    }

    /// <summary>
    /// Calculates the annualized Sharpe ratio.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="annualRiskFreeRate">The annual risk-free rate.</param>
    /// <param name="periodsPerYear">The optional explicit number of return periods per year.</param>
    /// <returns>The annualized Sharpe ratio, or 0 when volatility is zero.</returns>
    public double CalculateSharpeRatio(
        TimeSeries<double> returns,
        double annualRiskFreeRate = 0d,
        double? periodsPerYear = null)
    {
        ArgumentNullException.ThrowIfNull(returns);

        if (returns.Count < 2)
        {
            return 0d;
        }

        var resolvedPeriodsPerYear = this.ResolvePeriodsPerYear(returns, periodsPerYear);
        var periodicRiskFreeRate = annualRiskFreeRate / resolvedPeriodsPerYear;
        var excessMean = DescriptiveStatistics.Mean(returns.ToValueArray()) - periodicRiskFreeRate;
        var volatility = this.CalculateVolatility(returns);

        return volatility == 0d ? 0d : (excessMean / volatility) * Math.Sqrt(resolvedPeriodsPerYear);
    }

    /// <summary>
    /// Calculates the annualized Sortino ratio.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="annualTargetReturn">The annual target return.</param>
    /// <param name="periodsPerYear">The optional explicit number of return periods per year.</param>
    /// <returns>The annualized Sortino ratio, or 0 when downside deviation is zero.</returns>
    public double CalculateSortinoRatio(
        TimeSeries<double> returns,
        double annualTargetReturn = 0d,
        double? periodsPerYear = null)
    {
        ArgumentNullException.ThrowIfNull(returns);

        if (returns.Count == 0)
        {
            return 0d;
        }

        var resolvedPeriodsPerYear = this.ResolvePeriodsPerYear(returns, periodsPerYear);
        var periodicTarget = annualTargetReturn / resolvedPeriodsPerYear;
        var excessMean = DescriptiveStatistics.Mean(returns.ToValueArray()) - periodicTarget;
        var downsideDeviation = this.CalculateDownsideDeviation(returns, periodicTarget);

        return downsideDeviation == 0d ? 0d : (excessMean / downsideDeviation) * Math.Sqrt(resolvedPeriodsPerYear);
    }

    /// <summary>
    /// Calculates the Calmar ratio.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>The CAGR divided by absolute maximum drawdown, or 0 when drawdown is zero.</returns>
    public double CalculateCalmarRatio(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        var maximumDrawdown = Math.Abs(this.CalculateMaximumDrawdown(navSeries).MaximumDrawdown);
        if (maximumDrawdown == 0d)
        {
            return 0d;
        }

        return new ReturnCalculator().CalculateCagr(navSeries) / maximumDrawdown;
    }

    /// <summary>
    /// Calculates the Omega ratio at a periodic threshold.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="thresholdReturn">The periodic threshold return.</param>
    /// <returns>The Omega ratio, or 0 when downside mass is zero.</returns>
    public double CalculateOmegaRatio(TimeSeries<double> returns, double thresholdReturn = 0d)
    {
        ArgumentNullException.ThrowIfNull(returns);

        if (returns.Count == 0)
        {
            return 0d;
        }

        var gains = 0d;
        var losses = 0d;
        foreach (var value in returns.ToValueArray())
        {
            if (!double.IsFinite(value))
            {
                throw new ArgumentException("Return values must be finite.", nameof(returns));
            }

            gains += Math.Max(0d, value - thresholdReturn);
            losses += Math.Max(0d, thresholdReturn - value);
        }

        return losses == 0d ? 0d : gains / losses;
    }

    /// <summary>
    /// Calculates rolling annualized volatility.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="windowSize">The number of observations in each window.</param>
    /// <param name="periodsPerYear">The optional explicit number of return periods per year.</param>
    /// <returns>Rolling annualized volatility dated at each window end.</returns>
    public TimeSeries<double> CalculateRollingVolatility(
        TimeSeries<double> returns,
        int windowSize,
        double? periodsPerYear = null)
    {
        ArgumentNullException.ThrowIfNull(returns);

        var points = new List<TimeSeriesPoint<double>>();
        foreach (var window in returns.RollingWindows(windowSize))
        {
            points.Add(new TimeSeriesPoint<double>(
                window.EndDate,
                this.CalculateAnnualizedVolatility(window, periodsPerYear)));
        }

        return new TimeSeries<double>(points, returns.ObservationFrequency);
    }

    private double ResolvePeriodsPerYear(TimeSeries<double> returns, double? periodsPerYear)
    {
        if (periodsPerYear.HasValue)
        {
            if (!double.IsFinite(periodsPerYear.Value) || periodsPerYear.Value <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(periodsPerYear),
                    periodsPerYear,
                    "Periods per year must be positive and finite.");
            }

            return periodsPerYear.Value;
        }

        if (returns.ObservationFrequency == ObservationFrequency.Irregular &&
            this.irregularAnnualizationEstimator is not null)
        {
            return this.irregularAnnualizationEstimator.EstimatePeriodsPerYear(returns.ToDateArray());
        }

        return this.annualizationConvention.ResolvePeriodsPerYear(returns.ObservationFrequency);
    }

    private void ValidateProbability(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value >= 1d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Probability must be finite and strictly between 0 and 1.");
        }
    }

    private double InverseStandardNormal(double probability)
    {
        ValidateProbability(probability, nameof(probability));

        var a = new[]
        {
            -3.969683028665376e+01,
            2.209460984245205e+02,
            -2.759285104469687e+02,
            1.383577518672690e+02,
            -3.066479806614716e+01,
            2.506628277459239e+00,
        };
        var b = new[]
        {
            -5.447609879822406e+01,
            1.615858368580409e+02,
            -1.556989798598866e+02,
            6.680131188771972e+01,
            -1.328068155288572e+01,
        };
        var c = new[]
        {
            -7.784894002430293e-03,
            -3.223964580411365e-01,
            -2.400758277161838e+00,
            -2.549732539343734e+00,
            4.374664141464968e+00,
            2.938163982698783e+00,
        };
        var d = new[]
        {
            7.784695709041462e-03,
            3.224671290700398e-01,
            2.445134137142996e+00,
            3.754408661907416e+00,
        };

        const double low = 0.02425d;
        const double high = 1d - low;
        if (probability < low)
        {
            var q = Math.Sqrt(-2d * Math.Log(probability));
            return EvaluatePolynomial(c, q) / EvaluatePolynomial([d[0], d[1], d[2], d[3], 1d], q);
        }

        if (probability > high)
        {
            var q = Math.Sqrt(-2d * Math.Log(1d - probability));
            return -(EvaluatePolynomial(c, q) / EvaluatePolynomial([d[0], d[1], d[2], d[3], 1d], q));
        }

        var qCentral = probability - 0.5d;
        var r = qCentral * qCentral;
        return (EvaluatePolynomial(a, r) * qCentral) / EvaluatePolynomial([b[0], b[1], b[2], b[3], b[4], 1d], r);
    }

    private double EvaluatePolynomial(IReadOnlyList<double> coefficients, double value)
    {
        var result = 0d;
        for (var index = 0; index < coefficients.Count; index++)
        {
            result = (result * value) + coefficients[index];
        }

        return result;
    }
}
