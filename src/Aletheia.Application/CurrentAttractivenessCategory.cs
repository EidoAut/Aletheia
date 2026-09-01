namespace Aletheia.Application;

/// <summary>
/// Describes current opportunity attractiveness as a research classification.
/// </summary>
public enum CurrentAttractivenessCategory
{
    /// <summary>
    /// Evidence points to an unfavorable current setup.
    /// </summary>
    VeryUnfavorable,

    /// <summary>
    /// Evidence is moderately unfavorable.
    /// </summary>
    Unfavorable,

    /// <summary>
    /// Evidence is balanced or insufficient.
    /// </summary>
    Neutral,

    /// <summary>
    /// Evidence is moderately favorable.
    /// </summary>
    Favorable,

    /// <summary>
    /// Evidence is strongly favorable.
    /// </summary>
    VeryFavorable,
}
