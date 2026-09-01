using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Represents the currently loaded analytical workspace.
/// </summary>
public sealed record FundWorkspace(
    FundHistory History,
    ForecastEvaluationDataset EvaluationDataset,
    FundAnalysisResult Analysis,
    ModelArenaResult? Arena = null,
    IReadOnlyList<ModelArenaResult>? HorizonArenas = null)
{
    /// <summary>
    /// Gets all attached Model Arena results indexed by their evaluated horizon.
    /// </summary>
    public IReadOnlyList<ModelArenaResult> Arenas =>
        this.HorizonArenas ?? (this.Arena is null ? Array.Empty<ModelArenaResult>() : [this.Arena]);

    /// <summary>
    /// Creates a copy with Model Arena results attached.
    /// </summary>
    /// <param name="arena">The completed arena result.</param>
    /// <returns>A workspace with arena data.</returns>
    public FundWorkspace WithArena(ModelArenaResult arena) => this with { Arena = arena, HorizonArenas = [arena] };

    /// <summary>
    /// Creates a copy with horizon-indexed Model Arena results attached.
    /// </summary>
    /// <param name="arenas">The completed arena results.</param>
    /// <returns>A workspace with arena data.</returns>
    public FundWorkspace WithArenas(IReadOnlyList<ModelArenaResult> arenas)
    {
        return this.WithArenas(arenas, ForecastHorizon.CalendarDays(90));
    }

    /// <summary>
    /// Creates a copy with horizon-indexed Model Arena results attached and a preferred primary horizon.
    /// </summary>
    /// <param name="arenas">The completed arena results.</param>
    /// <param name="preferredHorizon">The preferred user-facing primary horizon.</param>
    /// <returns>A workspace with arena data.</returns>
    public FundWorkspace WithArenas(IReadOnlyList<ModelArenaResult> arenas, ForecastHorizon preferredHorizon)
    {
        ArgumentNullException.ThrowIfNull(arenas);
        var selected = arenas.FirstOrDefault(arena => arena.Horizon.Equals(preferredHorizon)) ??
            arenas.FirstOrDefault(arena =>
            arena.Horizon.Unit == ForecastHorizonUnit.CalendarDays &&
            arena.Horizon.Value == 90) ?? arenas.FirstOrDefault();
        return this with { Arena = selected, HorizonArenas = arenas.ToArray() };
    }
}
