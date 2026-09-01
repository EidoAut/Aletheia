namespace Aletheia.Validation;

/// <summary>
/// Stores transparent baseline-relative skill values.
/// </summary>
public sealed record RelativeSkill(
    string? PointBaselineModelId,
    string? ProbabilityBaselineModelId,
    double? MeanAbsoluteErrorSkill,
    double? RootMeanSquaredErrorSkill,
    double? BrierScoreSkill);
