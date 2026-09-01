namespace Aletheia.Core;

/// <summary>
/// Identifies a mathematical or forecasting model in a reproducible way.
/// </summary>
public sealed record ModelDescriptor(string Id, string Name, string Version);
