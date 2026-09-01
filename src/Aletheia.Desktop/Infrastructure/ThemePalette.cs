namespace Aletheia.Desktop.Infrastructure;

/// <summary>
/// Centralizes the EIDO Automation desktop color palette.
/// </summary>
internal static class ThemePalette
{
    /// <summary>
    /// Gets the primary application background.
    /// </summary>
    public static Color Background => Color.FromArgb(0, 7, 12);

    /// <summary>
    /// Gets the sidebar background.
    /// </summary>
    public static Color Sidebar => Color.FromArgb(0, 10, 18);

    /// <summary>
    /// Gets the panel and header background.
    /// </summary>
    public static Color Panel => Color.FromArgb(0, 12, 22);

    /// <summary>
    /// Gets the secondary panel background.
    /// </summary>
    public static Color PanelAlt => Color.FromArgb(7, 19, 29);

    /// <summary>
    /// Gets a raised neutral surface color.
    /// </summary>
    public static Color Surface => Color.FromArgb(0, 10, 18);

    /// <summary>
    /// Gets the most elevated neutral surface color.
    /// </summary>
    public static Color SurfaceElevated => Color.FromArgb(13, 32, 44);

    /// <summary>
    /// Gets the chart plotting background.
    /// </summary>
    public static Color ChartBackground => Color.FromArgb(0, 7, 12);

    /// <summary>
    /// Gets the input background.
    /// </summary>
    public static Color Input => Color.FromArgb(0, 10, 18);

    /// <summary>
    /// Gets the subtle border color.
    /// </summary>
    public static Color Border => Color.FromArgb(26, 42, 52);

    /// <summary>
    /// Gets the emphasized border color.
    /// </summary>
    public static Color BorderStrong => Color.FromArgb(38, 57, 70);

    /// <summary>
    /// Gets the chart grid color.
    /// </summary>
    public static Color Grid => Color.FromArgb(38, 57, 70);

    /// <summary>
    /// Gets the primary text color.
    /// </summary>
    public static Color Text => Color.FromArgb(238, 247, 251);

    /// <summary>
    /// Gets the strongest text color.
    /// </summary>
    public static Color TextStrong => Color.FromArgb(255, 255, 255);

    /// <summary>
    /// Gets the secondary text color.
    /// </summary>
    public static Color MutedText => Color.FromArgb(186, 200, 209);

    /// <summary>
    /// Gets the tertiary text color.
    /// </summary>
    public static Color SubtleText => Color.FromArgb(129, 147, 157);

    /// <summary>
    /// Gets the main technical accent color.
    /// </summary>
    public static Color Accent => Color.FromArgb(96, 192, 224);

    /// <summary>
    /// Gets the hover state of the main accent.
    /// </summary>
    public static Color AccentHover => Color.FromArgb(128, 220, 255);

    /// <summary>
    /// Gets the pressed state of the main accent.
    /// </summary>
    public static Color AccentPressed => Color.FromArgb(77, 145, 168);

    /// <summary>
    /// Gets text displayed on top of the main accent.
    /// </summary>
    public static Color AccentText => Color.FromArgb(0, 7, 12);

    /// <summary>
    /// Gets the low-emphasis accent surface.
    /// </summary>
    public static Color AccentSoft => Color.FromArgb(20, 56, 74);

    /// <summary>
    /// Gets the secondary accent color.
    /// </summary>
    public static Color AccentSecondary => Color.FromArgb(128, 220, 255);

    /// <summary>
    /// Gets the positive value color.
    /// </summary>
    public static Color Positive => Color.FromArgb(55, 255, 139);

    /// <summary>
    /// Gets the low-emphasis positive surface.
    /// </summary>
    public static Color PositiveSoft => Color.FromArgb(7, 47, 34);

    /// <summary>
    /// Gets the negative value color.
    /// </summary>
    public static Color Negative => Color.FromArgb(255, 94, 122);

    /// <summary>
    /// Gets the low-emphasis negative surface.
    /// </summary>
    public static Color NegativeSoft => Color.FromArgb(53, 18, 28);

    /// <summary>
    /// Gets the warning color.
    /// </summary>
    public static Color Warning => Color.FromArgb(255, 230, 107);

    /// <summary>
    /// Gets the selected-row color.
    /// </summary>
    public static Color Selection => Color.FromArgb(20, 56, 74);

    /// <summary>
    /// Gets the left stop of the technical header gradient.
    /// </summary>
    public static Color HeaderStart => Color.FromArgb(0, 7, 12);

    /// <summary>
    /// Gets the middle stop of the technical header gradient.
    /// </summary>
    public static Color HeaderMiddle => Color.FromArgb(0, 12, 22);

    /// <summary>
    /// Gets the right stop of the technical header gradient.
    /// </summary>
    public static Color HeaderEnd => Color.FromArgb(0, 16, 32);
}
