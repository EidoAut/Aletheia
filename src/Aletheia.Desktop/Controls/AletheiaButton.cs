#pragma warning disable SA1118 // Existing compact object creation is kept stable.

using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Identifies the visual hierarchy of an Aletheia action button.
/// </summary>
internal enum AletheiaButtonKind
{
    /// <summary>Primary, high-emphasis action.</summary>
    Primary,

    /// <summary>Secondary bordered action.</summary>
    Secondary,

    /// <summary>Low-emphasis toolbar action.</summary>
    Ghost,

    /// <summary>Destructive or cancellation action.</summary>
    Danger,
}

/// <summary>
/// Renders a rounded owner-drawn button with consistent interaction states.
/// </summary>
internal class AletheiaButton : Button
{
    private bool mouseOver;
    private bool mouseDown;
    private AletheiaButtonKind kind = AletheiaButtonKind.Secondary;

    /// <summary>
    /// Initializes a new instance of the <see cref="AletheiaButton"/> class.
    /// </summary>
    public AletheiaButton()
    {
        this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
        this.FlatStyle = FlatStyle.Flat;
        this.FlatAppearance.BorderSize = 0;
        this.UseVisualStyleBackColor = false;
        this.BackColor = Color.Transparent;
        this.ForeColor = ThemePalette.Text;
        this.Cursor = Cursors.Hand;
        this.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
        this.Height = 36;
        this.Margin = new Padding(4);
        this.Padding = new Padding(10, 0, 10, 0);
        this.TabStop = true;
    }

    /// <summary>
    /// Gets or sets the visual button kind.
    /// </summary>
    public AletheiaButtonKind Kind
    {
        get => this.kind;
        set
        {
            this.kind = value;
            this.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the corner radius.
    /// </summary>
    public int CornerRadius { get; set; } = 6;

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(this.ResolveParentColor());

        var palette = this.ResolvePalette();
        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(0f, this.ClientSize.Width - 1f), Math.Max(0f, this.ClientSize.Height - 1f));
        using var path = DrawingUtilities.CreateRoundedRectangle(bounds, this.CornerRadius);
        using var fill = new SolidBrush(palette.Fill);
        using var border = new Pen(palette.Border, 1f);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);

        var textBounds = Rectangle.Inflate(this.ClientRectangle, -8, 0);
        TextRenderer.DrawText(
            graphics,
            this.Text.ToUpperInvariant(),
            this.Font,
            textBounds,
            palette.Text,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix);

        if (this.Focused && this.ShowFocusCues)
        {
            var focusBounds = Rectangle.Inflate(this.ClientRectangle, -4, -4);
            ControlPaint.DrawFocusRectangle(graphics, focusBounds, palette.Text, palette.Fill);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        this.mouseOver = true;
        this.Invalidate();
        base.OnMouseEnter(e);
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        this.mouseOver = false;
        this.mouseDown = false;
        this.Invalidate();
        base.OnMouseLeave(e);
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            this.mouseDown = true;
            this.Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        this.mouseDown = false;
        this.Invalidate();
        base.OnMouseUp(mevent);
    }

    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        this.mouseDown = false;
        this.Invalidate();
        base.OnMouseCaptureChanged(e);
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        this.Cursor = this.Enabled ? Cursors.Hand : Cursors.Default;
        this.Invalidate();
        base.OnEnabledChanged(e);
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        this.AccessibleName = this.Text;
        this.Invalidate();
        base.OnTextChanged(e);
    }

    private (Color Fill, Color Border, Color Text) ResolvePalette()
    {
        var parent = this.ResolveParentColor();
        if (!this.Enabled)
        {
            return (
                DrawingUtilities.Blend(ThemePalette.PanelAlt, parent, 0.55d),
                DrawingUtilities.Blend(ThemePalette.Border, parent, 0.55d),
                ThemePalette.MutedText);
        }

        var palette = this.kind switch
        {
            AletheiaButtonKind.Primary => (ThemePalette.SurfaceElevated, ThemePalette.Accent, ThemePalette.TextStrong),
            AletheiaButtonKind.Danger => (ThemePalette.NegativeSoft, ThemePalette.Negative, ThemePalette.Negative),
            AletheiaButtonKind.Ghost => (parent, parent, ThemePalette.MutedText),
            _ => (ThemePalette.PanelAlt, ThemePalette.BorderStrong, ThemePalette.Text),
        };

        if (this.mouseDown)
        {
            return this.kind == AletheiaButtonKind.Primary
                ? (ThemePalette.Selection, ThemePalette.AccentPressed, ThemePalette.TextStrong)
                : (DrawingUtilities.Blend(ThemePalette.Text, palette.Item1, 0.10d), palette.Item2, palette.Item3);
        }

        if (this.mouseOver)
        {
            return this.kind == AletheiaButtonKind.Primary
                ? (ThemePalette.SurfaceElevated, ThemePalette.AccentHover, ThemePalette.TextStrong)
                : (DrawingUtilities.Blend(ThemePalette.Text, palette.Item1, 0.07d), ThemePalette.AccentPressed, ThemePalette.TextStrong);
        }

        return palette;
    }

    private Color ResolveParentColor()
    {
        var parent = this.Parent;
        while (parent is not null)
        {
            if (parent.BackColor.A > 0)
            {
                return parent.BackColor;
            }

            parent = parent.Parent;
        }

        return ThemePalette.Panel;
    }
}
