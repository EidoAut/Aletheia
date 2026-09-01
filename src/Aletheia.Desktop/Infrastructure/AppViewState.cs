namespace Aletheia.Desktop.Infrastructure;

/// <summary>
/// Describes the coarse state of the desktop shell.
/// </summary>
internal enum AppViewState
{
    /// <summary>
    /// No fund dataset has been loaded.
    /// </summary>
    NoDataset,

    /// <summary>
    /// A dataset is being loaded.
    /// </summary>
    Loading,

    /// <summary>
    /// Fund discovery is querying configured providers.
    /// </summary>
    Searching,

    /// <summary>
    /// Analysis is running.
    /// </summary>
    Analyzing,

    /// <summary>
    /// Analysis results are available.
    /// </summary>
    AnalysisAvailable,

    /// <summary>
    /// Model Arena is running.
    /// </summary>
    ArenaRunning,

    /// <summary>
    /// Model Arena results are available.
    /// </summary>
    ArenaAvailable,

    /// <summary>
    /// An ordinary user-facing error occurred.
    /// </summary>
    Error,
}
