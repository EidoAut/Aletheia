using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Renders the EIDO-style horizontal technical header gradient.
/// </summary>
internal sealed class GradientPanel : Panel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GradientPanel"/> class.
    /// </summary>
    public GradientPanel()
    {
        this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    /// <inheritdoc />
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0)
        {
            base.OnPaintBackground(e);
            return;
        }

        using var brush = new LinearGradientBrush(
            this.ClientRectangle,
            ThemePalette.HeaderStart,
            ThemePalette.HeaderEnd,
            LinearGradientMode.Horizontal);
        brush.InterpolationColors = new ColorBlend
        {
            Colors =
            [
                ThemePalette.HeaderStart,
                ThemePalette.HeaderMiddle,
                ThemePalette.HeaderEnd,
            ],
            Positions =
            [
                0f,
                0.46f,
                1f,
            ],
        };
        e.Graphics.FillRectangle(brush, this.ClientRectangle);
    }
}
