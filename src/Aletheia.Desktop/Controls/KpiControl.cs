using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Displays one compact analytical metric in a dashboard card.
/// </summary>
internal sealed class KpiControl : SurfacePanel
{
    private static readonly Font ValueLargeFont = new("Consolas", 14f, FontStyle.Bold);
    private static readonly Font ValueMediumFont = new("Consolas", 12f, FontStyle.Bold);
    private static readonly Font ValueCompactFont = new("Consolas", 10.5f, FontStyle.Bold);
    private static readonly Font ValueSmallFont = new("Consolas", 9.25f, FontStyle.Bold);

    private readonly Label nameLabel = new();
    private readonly Label valueLabel = new();
    private Color accent = ThemePalette.Accent;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpiControl"/> class.
    /// </summary>
    public KpiControl()
    {
        this.Height = 80;
        this.Margin = new Padding(5);
        this.Padding = new Padding(13, 10, 13, 8);
        this.FillColor = ThemePalette.Surface;
        this.BorderColor = ThemePalette.Border;
        this.CornerRadius = 8;
        this.nameLabel.Dock = DockStyle.Top;
        this.nameLabel.Height = 20;
        this.nameLabel.ForeColor = ThemePalette.SubtleText;
        this.nameLabel.Font = new Font("Consolas", 7.5f, FontStyle.Bold);
        this.nameLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.nameLabel.AutoEllipsis = true;
        this.valueLabel.Dock = DockStyle.Fill;
        this.valueLabel.ForeColor = ThemePalette.TextStrong;
        this.valueLabel.Font = ValueLargeFont;
        this.valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.valueLabel.AutoEllipsis = true;
        this.Controls.Add(this.valueLabel);
        this.Controls.Add(this.nameLabel);
    }

    /// <summary>
    /// Sets the metric content.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The formatted value.</param>
    /// <param name="accent">The optional value color.</param>
    public void SetMetric(string name, string value, Color? accent = null)
    {
        this.nameLabel.Text = name.ToUpperInvariant();
        this.valueLabel.Text = value;
        this.accent = accent ?? ThemePalette.Accent;
        this.valueLabel.ForeColor = accent ?? ThemePalette.TextStrong;
        this.valueLabel.Font = value.Length switch
        {
            > 24 => ValueSmallFont,
            > 17 => ValueCompactFont,
            > 12 => ValueMediumFont,
            _ => ValueLargeFont,
        };
        this.Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(this.accent);
        using var path = DrawingUtilities.CreateRoundedRectangle(new RectangleF(13f, 7f, 28f, 3f), 1.5f);
        e.Graphics.FillPath(brush, path);
    }
}
