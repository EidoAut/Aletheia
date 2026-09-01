using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Simulation;

/// <summary>
/// Simulates portfolio values for an initial investment plus end-of-month contributions.
/// </summary>
/// <remarks>
/// The simulator is a transparent baseline scenario generator. It assumes
/// independent Gaussian log returns whose historical per-observation moments
/// are scaled to a monthly interval. It is not a validated market model.
/// </remarks>
public sealed class InvestmentPlanSimulator
{
    private readonly InvestmentPlanOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvestmentPlanSimulator"/> class.
    /// </summary>
    /// <param name="options">The investment-plan options.</param>
    public InvestmentPlanSimulator(InvestmentPlanOptions? options = null)
    {
        this.options = options ?? new InvestmentPlanOptions();
    }

    /// <summary>
    /// Simulates portfolio-value paths from historical log-return moments.
    /// </summary>
    /// <param name="historicalLogReturns">Historical per-observation log returns.</param>
    /// <param name="observationFrequency">The historical observation cadence.</param>
    /// <param name="dataCutoffDate">The last historical observation date.</param>
    /// <param name="cancellationToken">A token used to cancel simulation.</param>
    /// <returns>The simulated plan result.</returns>
    public InvestmentPlanSimulationResult Simulate(
        IReadOnlyList<double> historicalLogReturns,
        ObservationFrequency observationFrequency,
        DateOnly dataCutoffDate,
        CancellationToken cancellationToken = default)
    {
        return this.Simulate(
            historicalLogReturns,
            observationFrequency,
            dataCutoffDate,
            null,
            cancellationToken);
    }

    /// <summary>
    /// Simulates portfolio-value paths with an optional explicit annual cadence.
    /// </summary>
    /// <param name="historicalLogReturns">Historical per-observation log returns.</param>
    /// <param name="observationFrequency">The historical observation cadence.</param>
    /// <param name="dataCutoffDate">The last historical observation date.</param>
    /// <param name="periodsPerYear">The optional explicit periods-per-year convention.</param>
    /// <param name="cancellationToken">A token used to cancel simulation.</param>
    /// <returns>The simulated plan result.</returns>
    public InvestmentPlanSimulationResult Simulate(
        IReadOnlyList<double> historicalLogReturns,
        ObservationFrequency observationFrequency,
        DateOnly dataCutoffDate,
        double? periodsPerYear,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(historicalLogReturns);
        cancellationToken.ThrowIfCancellationRequested();
        this.ValidateOptions();
        if (historicalLogReturns.Count < 2)
        {
            throw new InvalidOperationException("Investment simulation requires at least two historical log returns.");
        }

        if (historicalLogReturns.Any(value => !double.IsFinite(value)))
        {
            throw new InvalidOperationException("Historical log returns must contain only finite values.");
        }

        var periodsPerMonth = ResolvePeriodsPerYear(observationFrequency, periodsPerYear) / 12d;
        var historicalMean = DescriptiveStatistics.Mean(historicalLogReturns);
        var historicalStandardDeviation = DescriptiveStatistics.SampleStandardDeviation(historicalLogReturns);
        var monthlyMean = historicalMean * periodsPerMonth;
        var monthlyStandardDeviation = historicalStandardDeviation * Math.Sqrt(periodsPerMonth);
        if (!double.IsFinite(monthlyMean) || !double.IsFinite(monthlyStandardDeviation))
        {
            throw new InvalidOperationException("Historical log-return moments cannot be scaled to finite monthly values.");
        }

        var random = new Random(this.options.Seed);
        var investableInitial = this.options.InitialInvestment * (1d - this.options.EntryFeeRate);
        var investableMonthlyContribution = this.options.MonthlyContribution * (1d - this.options.EntryFeeRate);
        var monthlyServiceMultiplier = Math.Pow(1d - this.options.AnnualServiceCostRate, 1d / 12d);
        var balances = Enumerable.Repeat(investableInitial, this.options.PathCount).ToArray();
        var trajectory = new List<InvestmentPlanSimulationPoint>(this.options.HorizonMonths + 1)
        {
            CreatePoint(0, dataCutoffDate, this.options.InitialInvestment, balances),
        };

        for (var month = 1; month <= this.options.HorizonMonths; month++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var path = 0; path < balances.Length; path++)
            {
                if ((path & 255) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var monthlyLogReturn = monthlyMean + (monthlyStandardDeviation * NextGaussian(random));
                var nextBalance = (balances[path] * Math.Exp(monthlyLogReturn) * monthlyServiceMultiplier) + investableMonthlyContribution;
                if (!double.IsFinite(nextBalance))
                {
                    throw new InvalidOperationException("Investment simulation produced a non-finite portfolio value.");
                }

                balances[path] = nextBalance;
            }

            var contributed = this.options.InitialInvestment + (this.options.MonthlyContribution * month);
            trajectory.Add(CreatePoint(month, dataCutoffDate.AddMonths(month), contributed, balances));
        }

        if (this.options.ExitFeeRate > 0d)
        {
            for (var path = 0; path < balances.Length; path++)
            {
                balances[path] *= 1d - this.options.ExitFeeRate;
            }

            trajectory[^1] = CreatePoint(
                this.options.HorizonMonths,
                dataCutoffDate.AddMonths(this.options.HorizonMonths),
                this.options.InitialInvestment + (this.options.MonthlyContribution * this.options.HorizonMonths),
                balances);
        }

        var terminal = trajectory[^1];
        var probabilityBelowContributions = balances.Count(value => value < terminal.TotalContributed) / (double)balances.Length;
        var realDiscount = Math.Pow(1d + this.options.AnnualInflationRate, this.options.HorizonMonths / 12d);
        var realBalances = balances.Select(value => value / realDiscount).ToArray();
        Array.Sort(realBalances);
        var probabilityLoss = balances.Count(value => value < this.options.InitialInvestment) / (double)balances.Length;
        return new InvestmentPlanSimulationResult(
            this.options,
            observationFrequency,
            dataCutoffDate,
            dataCutoffDate.AddMonths(this.options.HorizonMonths),
            periodsPerMonth,
            historicalMean,
            historicalStandardDeviation,
            monthlyMean,
            monthlyStandardDeviation,
            terminal.TotalContributed,
            terminal.MeanValue,
            terminal.MedianValue,
            terminal.P10Value,
            terminal.P25Value,
            terminal.P75Value,
            terminal.P90Value,
            probabilityBelowContributions,
            trajectory.ToArray(),
            realBalances.Average(),
            PercentileFromSorted(realBalances, 50d),
            PercentileFromSorted(realBalances, 10d),
            PercentileFromSorted(realBalances, 90d),
            probabilityLoss);
    }

    private static InvestmentPlanSimulationPoint CreatePoint(
        int monthOffset,
        DateOnly date,
        double totalContributed,
        IReadOnlyList<double> balances)
    {
        var sorted = balances.ToArray();
        Array.Sort(sorted);
        var mean = 0d;
        for (var index = 0; index < sorted.Length; index++)
        {
            mean += (sorted[index] - mean) / (index + 1d);
        }

        return new InvestmentPlanSimulationPoint(
            monthOffset,
            date,
            totalContributed,
            mean,
            PercentileFromSorted(sorted, 10d),
            PercentileFromSorted(sorted, 25d),
            PercentileFromSorted(sorted, 50d),
            PercentileFromSorted(sorted, 75d),
            PercentileFromSorted(sorted, 90d));
    }

    private static double PercentileFromSorted(IReadOnlyList<double> sorted, double percentile)
    {
        if (sorted.Count == 1)
        {
            return sorted[0];
        }

        var position = (percentile / 100d) * (sorted.Count - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        var weight = position - lowerIndex;
        return sorted[lowerIndex] + ((sorted[upperIndex] - sorted[lowerIndex]) * weight);
    }

    private static double ResolvePeriodsPerYear(
        ObservationFrequency frequency,
        double? periodsPerYear)
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

        return frequency switch
        {
            ObservationFrequency.Daily => 365.25d,
            ObservationFrequency.BusinessDaily => 252d,
            ObservationFrequency.Weekly => 52d,
            ObservationFrequency.Monthly => 12d,
            ObservationFrequency.Irregular => throw new InvalidOperationException(
                "Investment simulation requires a regular observation frequency or an explicit annual cadence."),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported observation frequency."),
        };
    }

    private static double NextGaussian(Random random)
    {
        var u1 = 1d - random.NextDouble();
        var u2 = 1d - random.NextDouble();
        return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
    }

    private void ValidateOptions()
    {
        if (!double.IsFinite(this.options.InitialInvestment) || this.options.InitialInvestment < 0d)
        {
            throw new InvalidOperationException("Initial investment must be a finite non-negative value.");
        }

        if (!double.IsFinite(this.options.MonthlyContribution) || this.options.MonthlyContribution < 0d)
        {
            throw new InvalidOperationException("Monthly contribution must be a finite non-negative value.");
        }

        if (this.options.InitialInvestment == 0d && this.options.MonthlyContribution == 0d)
        {
            throw new InvalidOperationException("At least one investment contribution must be positive.");
        }

        if (this.options.HorizonMonths is < 1 or > 600)
        {
            throw new InvalidOperationException("Simulation horizon must be between 1 and 600 months.");
        }

        if (this.options.PathCount is < 100 or > 100_000)
        {
            throw new InvalidOperationException("Simulation path count must be between 100 and 100,000.");
        }

        var totalContributed = this.options.InitialInvestment +
            (this.options.MonthlyContribution * this.options.HorizonMonths);
        if (!double.IsFinite(totalContributed))
        {
            throw new InvalidOperationException("Total contributed capital must remain finite over the simulation horizon.");
        }

        if ((long)this.options.PathCount * this.options.HorizonMonths > 12_000_000L)
        {
            throw new InvalidOperationException(
                "Simulation workload cannot exceed 12,000,000 path-months. Reduce the horizon or path count.");
        }

        ValidateRate(this.options.EntryFeeRate, nameof(this.options.EntryFeeRate));
        ValidateRate(this.options.ExitFeeRate, nameof(this.options.ExitFeeRate));
        ValidateRate(this.options.AnnualServiceCostRate, nameof(this.options.AnnualServiceCostRate));
        if (!double.IsFinite(this.options.AnnualInflationRate) || this.options.AnnualInflationRate <= -1d)
        {
            throw new InvalidOperationException("Annual inflation rate must be finite and greater than -100%.");
        }
    }

    private void ValidateRate(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0d || value >= 1d)
        {
            throw new InvalidOperationException($"{name} must be finite and in [0, 1).");
        }
    }
}
