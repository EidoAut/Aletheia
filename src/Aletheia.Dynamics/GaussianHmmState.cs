namespace Aletheia.Dynamics;

/// <summary>
/// Describes one fitted Gaussian HMM state.
/// </summary>
/// <param name="Index">The state index.</param>
/// <param name="Mean">The Gaussian mean.</param>
/// <param name="Variance">The Gaussian variance.</param>
/// <param name="Label">The post-fit descriptive label.</param>
public sealed record GaussianHmmState(
    int Index,
    double Mean,
    double Variance,
    string Label);
