namespace Aletheia.Application;

/// <summary>
/// Describes an interpretive research signal, not an automatic trading instruction.
/// </summary>
public enum DecisionSignalAction
{
    /// <summary>
    /// Strongly adverse interpretive signal.
    /// </summary>
    StrongReduce,

    /// <summary>
    /// Moderately adverse interpretive signal.
    /// </summary>
    Reduce,

    /// <summary>
    /// Balanced legacy action; insufficient evidence is exposed through signal qualification.
    /// </summary>
    Neutral,

    /// <summary>
    /// Moderately constructive interpretive signal.
    /// </summary>
    MildAccumulate,

    /// <summary>
    /// Strongly constructive interpretive signal.
    /// </summary>
    Accumulate,
}
