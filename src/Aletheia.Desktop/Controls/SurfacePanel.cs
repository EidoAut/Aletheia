using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Provides a rounded, bordered surface for dashboard cards and input groups.
/// </summary>
internal class SurfacePanel : Panel
{
    private int cornerRadius = 8;
    private int borderThickness = 1;
    private Color fillColor = ThemePalette.Surface;
    private Color borderColor = ThemePalette.Border;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurfacePanel"/> class.
    /// </summary>
    public SurfacePanel()
    {
        this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        this.BackColor = this.fillColor;
        this.ForeColor = ThemePalette.Text;
    }

    /// <summary>
    /// Gets or sets the corner radius.
    /// </summary>
    public int CornerRadius
    {
        get => this.cornerRadius;
        set
        {
            this.cornerRadius = Math.Max(0, value);
            this.UpdateRegion();
            this.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the border thickness.
    /// </summary>
    public int BorderThickness
    {
        get => this.borderThickness;
        set
        {
            this.borderThickness = Math.Max(0, value);
            this.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the surface fill color.
    /// </summary>
    public Color FillColor
    {
        get => this.fillColor;
        set
        {
            this.fillColor = value;
            this.BackColor = value;
            this.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the surface border color.
    /// </summary>
    public Color BorderColor
    {
        get => this.borderColor;
        set
        {
            this.borderColor = value;
            this.Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (this.borderThickness <= 0 || this.ClientSize.Width <= 1 || this.ClientSize.Height <= 1)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var inset = this.borderThickness / 2f;
        var bounds = new RectangleF(
            inset,
            inset,
            this.ClientSize.Width - this.borderThickness,
            this.ClientSize.Height - this.borderThickness);
        using var path = DrawingUtilities.CreateRoundedRectangle(bounds, this.cornerRadius);
        using var pen = new Pen(this.borderColor, this.borderThickness);
        e.Graphics.DrawPath(pen, path);
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        this.UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0)
        {
            return;
        }

        using var path = DrawingUtilities.CreateRoundedRectangle(
            new RectangleF(0f, 0f, this.ClientSize.Width, this.ClientSize.Height),
            this.cornerRadius);
        var nextRegion = new Region(path);
        var previousRegion = this.Region;
        this.Region = nextRegion;
        previousRegion?.Dispose();
    }
}
