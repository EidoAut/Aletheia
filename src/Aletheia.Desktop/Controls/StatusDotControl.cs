using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Displays a compact colored application-state indicator.
/// </summary>
internal sealed class StatusDotControl : Control
{
    private Color indicatorColor = ThemePalette.Warning;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusDotControl"/> class.
    /// </summary>
    public StatusDotControl()
    {
        this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        this.Size = new Size(12, 12);
        this.Margin = new Padding(0);
    }

    /// <summary>
    /// Gets or sets the indicator color.
    /// </summary>
    public Color IndicatorColor
    {
        get => this.indicatorColor;
        set
        {
            this.indicatorColor = value;
            this.Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var size = Math.Max(4, Math.Min(this.ClientSize.Width, this.ClientSize.Height) - 4);
        var x = (this.ClientSize.Width - size) / 2;
        var y = (this.ClientSize.Height - size) / 2;
        using var glow = new SolidBrush(DrawingUtilities.Blend(this.indicatorColor, this.Parent?.BackColor ?? ThemePalette.Panel, 0.24d));
        using var core = new SolidBrush(this.indicatorColor);
        e.Graphics.FillEllipse(glow, x - 2, y - 2, size + 4, size + 4);
        e.Graphics.FillEllipse(core, x, y, size, size);
    }
}
