namespace Aletheia.Application;

/// <summary>
/// Stores chart-ready dynamic-state coordinates.
/// </summary>
public sealed record StateProjectionPoint(
    DateOnly Date,
    double Momentum,
    double Volatility,
    double Velocity,
    double Acceleration,
    bool IsCurrent);
