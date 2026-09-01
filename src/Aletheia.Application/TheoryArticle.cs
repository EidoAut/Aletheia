namespace Aletheia.Application;

/// <summary>
/// Stores concise theory metadata for an analytical method.
/// </summary>
public sealed record TheoryArticle(
    string Name,
    string Equation,
    string Purpose,
    string Assumptions,
    string Interpretation,
    string Limitations);
