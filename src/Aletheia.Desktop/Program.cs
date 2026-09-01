namespace Aletheia.Desktop;

/// <summary>
/// Provides the WinForms entry point.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs the desktop analytical shell.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.Run(new MainForm());
    }
}
