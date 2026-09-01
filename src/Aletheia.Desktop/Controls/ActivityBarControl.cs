using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Displays a lightweight indeterminate activity bar without native progress-bar chrome.
/// </summary>
internal sealed class ActivityBarControl : Control
{
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 18 };
    private int offset;
    private bool active;
    private double? progressFraction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityBarControl"/> class.
    /// </summary>
    public ActivityBarControl()
    {
        this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        this.Height = 4;
        this.animationTimer.Tick += (_, _) =>
        {
            this.offset += 10;
            var resetAt = Math.Max(1, this.ClientSize.Width + 160);
            if (this.offset > resetAt)
            {
                this.offset = 0;
            }

            this.Invalidate();
        };
    }

    /// <summary>
    /// Gets or sets a value indicating whether the animation is active.
    /// </summary>
    public bool Active
    {
        get => this.active;
        set
        {
            if (this.active == value)
            {
                return;
            }

            this.active = value;
            this.Visible = value;
            this.offset = 0;
            if (value)
            {
                this.animationTimer.Start();
            }
            else
            {
                this.animationTimer.Stop();
            }

            this.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets determinate progress in the [0, 1] range, or null for indeterminate animation.
    /// </summary>
    public double? ProgressFraction
    {
        get => this.progressFraction;
        set
        {
            this.progressFraction = value.HasValue && double.IsFinite(value.Value)
                ? Math.Clamp(value.Value, 0d, 1d)
                : null;
            this.Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.animationTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var track = new SolidBrush(ThemePalette.Border))
        {
            e.Graphics.FillRectangle(track, this.ClientRectangle);
        }

        if (!this.active || this.ClientSize.Width <= 0)
        {
            return;
        }

        if (this.progressFraction.HasValue)
        {
            var width = Math.Max(1, (int)Math.Round(this.ClientSize.Width * this.progressFraction.Value));
            using var fill = new SolidBrush(ThemePalette.Accent);
            e.Graphics.FillRectangle(fill, new Rectangle(0, 0, width, Math.Max(1, this.ClientSize.Height)));
            return;
        }

        const int segmentWidth = 150;
        var left = this.offset - segmentWidth;
        var segment = new Rectangle(left, 0, segmentWidth, Math.Max(1, this.ClientSize.Height));
        using var gradient = new LinearGradientBrush(
            segment,
            DrawingUtilities.Blend(ThemePalette.Accent, ThemePalette.Border, 0.15d),
            ThemePalette.Accent,
            LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(gradient, segment);
    }
}
