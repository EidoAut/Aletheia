using System.Globalization;

namespace Aletheia.Desktop.Infrastructure;

/// <summary>
/// Writes sparse structured desktop diagnostics to a local log file.
/// </summary>
internal sealed class DesktopLog
{
    private readonly string logPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopLog"/> class.
    /// </summary>
    public DesktopLog()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aletheia",
            "logs");
        Directory.CreateDirectory(directory);
        this.logPath = Path.Combine(directory, "desktop.log");
    }

    /// <summary>
    /// Logs an informational event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="message">The event message.</param>
    public void Info(string eventName, string message) => this.Write("INFO", eventName, message);

    /// <summary>
    /// Logs an error event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="exception">The exception.</param>
    public void Error(string eventName, Exception exception) => this.Write("ERROR", eventName, exception.ToString());

    private void Write(string level, string eventName, string message)
    {
        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:O}\t{level}\t{eventName}\t{message}{Environment.NewLine}");
        File.AppendAllText(this.logPath, line);
    }
}
