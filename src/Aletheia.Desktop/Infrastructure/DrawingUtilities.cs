using System.Drawing.Drawing2D;

namespace Aletheia.Desktop.Infrastructure;

/// <summary>
/// Provides small drawing helpers used by owner-drawn desktop controls.
/// </summary>
internal static class DrawingUtilities
{
    /// <summary>
    /// Creates a rounded rectangle path constrained to the supplied bounds.
    /// </summary>
    /// <param name="bounds">The rectangle bounds.</param>
    /// <param name="radius">The requested corner radius.</param>
    /// <returns>The rounded path.</returns>
    public static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var normalizedRadius = Math.Max(0f, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
        if (normalizedRadius <= 0.5f)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        var diameter = normalizedRadius * 2f;
        var arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180f, 90f);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270f, 90f);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0f, 90f);
        arc.X = bounds.X;
        path.AddArc(arc, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Blends one color over another using an opacity between zero and one.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="opacity">The foreground opacity.</param>
    /// <returns>The blended opaque color.</returns>
    public static Color Blend(Color foreground, Color background, double opacity)
    {
        var alpha = Math.Clamp(opacity, 0d, 1d);
        return Color.FromArgb(
            255,
            (int)Math.Round((foreground.R * alpha) + (background.R * (1d - alpha))),
            (int)Math.Round((foreground.G * alpha) + (background.G * (1d - alpha))),
            (int)Math.Round((foreground.B * alpha) + (background.B * (1d - alpha))));
    }
}
