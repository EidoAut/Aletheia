namespace Aletheia.Application;

/// <summary>
/// Defines a user-facing periodic-investment simulation request.
/// </summary>
public sealed record InvestmentSimulationRequest(
    double InitialInvestment,
    double MonthlyContribution,
    int HorizonYears,
    int PathCount,
    int Seed = 161803);
