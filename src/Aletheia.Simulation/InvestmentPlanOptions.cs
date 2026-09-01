namespace Aletheia.Simulation;

/// <summary>
/// Configures a periodic-investment Monte Carlo scenario.
/// </summary>
public sealed record InvestmentPlanOptions
{
    /// <summary>
    /// Gets the capital invested at the simulation start.
    /// </summary>
    public double InitialInvestment { get; init; } = 1_800d;

    /// <summary>
    /// Gets the contribution added at the end of every simulated month.
    /// </summary>
    public double MonthlyContribution { get; init; } = 100d;

    /// <summary>
    /// Gets the number of simulated calendar months.
    /// </summary>
    public int HorizonMonths { get; init; } = 120;

    /// <summary>
    /// Gets the number of simulated paths.
    /// </summary>
    public int PathCount { get; init; } = 5_000;

    /// <summary>
    /// Gets the deterministic random seed.
    /// </summary>
    public int Seed { get; init; } = 161803;

    /// <summary>
    /// Gets the entry fee applied to each external contribution.
    /// </summary>
    public double EntryFeeRate { get; init; }

    /// <summary>
    /// Gets the exit fee applied to terminal portfolio value.
    /// </summary>
    public double ExitFeeRate { get; init; }

    /// <summary>
    /// Gets the annual external service cost, excluding costs already embedded in NAV.
    /// </summary>
    public double AnnualServiceCostRate { get; init; }

    /// <summary>
    /// Gets the annual inflation assumption used for real-value diagnostics.
    /// </summary>
    public double AnnualInflationRate { get; init; }
}
