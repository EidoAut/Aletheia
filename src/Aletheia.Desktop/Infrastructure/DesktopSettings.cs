using System.Text.Json;

namespace Aletheia.Desktop.Infrastructure;

/// <summary>
/// Stores lightweight desktop settings such as recent CSV paths.
/// </summary>
internal sealed class DesktopSettings
{
    private const int MinimumArenaHorizonDays = 7;
    private const int MaximumArenaHorizonDays = 730;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

    /// <summary>
    /// Gets the recently opened CSV files.
    /// </summary>
    public List<string> RecentFiles { get; init; } = [];

    /// <summary>
    /// Gets or sets the user-selected calendar-day horizon for Model Arena validation.
    /// </summary>
    public int ArenaHorizonDays { get; set; } = 90;

    /// <summary>
    /// Loads desktop settings from local application data.
    /// </summary>
    /// <returns>The settings.</returns>
    public static DesktopSettings Load()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            return new DesktopSettings();
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<DesktopSettings>(json, JsonOptions) ?? new DesktopSettings();
            settings.Normalize();
            return settings;
        }
        catch (IOException)
        {
            return new DesktopSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new DesktopSettings();
        }
        catch (JsonException)
        {
            return new DesktopSettings();
        }
    }

    /// <summary>
    /// Saves desktop settings.
    /// </summary>
    public void Save()
    {
        this.Normalize();
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>
    /// Adds a recent file path.
    /// </summary>
    /// <param name="path">The path.</param>
    public void AddRecentFile(string path)
    {
        this.RecentFiles.RemoveAll(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
        this.RecentFiles.Insert(0, path);
        while (this.RecentFiles.Count > 8)
        {
            this.RecentFiles.RemoveAt(this.RecentFiles.Count - 1);
        }
    }

    private void Normalize()
    {
        this.ArenaHorizonDays = Math.Clamp(this.ArenaHorizonDays, MinimumArenaHorizonDays, MaximumArenaHorizonDays);
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aletheia",
            "desktop-settings.json");
    }
}
