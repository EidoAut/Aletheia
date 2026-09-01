using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Renders a sidebar destination with a monogram and active-state indicator.
/// </summary>
internal sealed class NavigationButton : Control
{
    private bool mouseOver;
    private bool mouseDown;
    private bool selected;
    private string monogram = "--";

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationButton"/> class.
    /// </summary>
    public NavigationButton()
    {
        this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable |
            ControlStyles.StandardClick,
            true);
        this.Height = 36;
        this.Width = 222;
        this.Margin = new Padding(10, 1, 10, 1);
        this.Cursor = Cursors.Hand;
        this.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
        this.TabStop = true;
        this.AccessibleRole = AccessibleRole.PushButton;
    }

    /// <summary>
    /// Gets or sets the compact destination monogram.
    /// </summary>
    public string Monogram
    {
        get => this.monogram;
        set
        {
            this.monogram = string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
            this.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the destination is active.
    /// </summary>
    public bool Selected
    {
        get => this.selected;
        set
        {
            this.selected = value;
            this.Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var parentColor = this.Parent?.BackColor ?? ThemePalette.Sidebar;
        graphics.Clear(parentColor);

        var background = this.ResolveBackground(parentColor);
        var bounds = new RectangleF(4f, 1f, Math.Max(0f, this.ClientSize.Width - 8f), Math.Max(0f, this.ClientSize.Height - 2f));
        using (var path = DrawingUtilities.CreateRoundedRectangle(bounds, 6f))
        using (var brush = new SolidBrush(background))
        {
            graphics.FillPath(brush, path);
        }

        using (var borderPath = DrawingUtilities.CreateRoundedRectangle(bounds, 6f))
        using (var borderPen = new Pen(this.selected ? ThemePalette.AccentPressed : ThemePalette.Border, 1f))
        {
            graphics.DrawPath(borderPen, borderPath);
        }

        if (this.selected)
        {
            using var accentBrush = new SolidBrush(ThemePalette.Accent);
            using var accentPath = DrawingUtilities.CreateRoundedRectangle(new RectangleF(4f, 9f, 3f, 24f), 1.5f);
            graphics.FillPath(accentBrush, accentPath);
        }

        var iconBounds = new Rectangle(17, 7, 24, 24);
        var iconFill = this.selected
            ? ThemePalette.Selection
            : DrawingUtilities.Blend(ThemePalette.Text, ThemePalette.SurfaceElevated, this.mouseOver ? 0.12d : 0.06d);
        var iconText = this.selected ? ThemePalette.AccentHover : ThemePalette.MutedText;
        using (var iconPath = DrawingUtilities.CreateRoundedRectangle(iconBounds, 5f))
        using (var iconBrush = new SolidBrush(iconFill))
        {
            graphics.FillPath(iconBrush, iconPath);
        }

        using (var iconFont = new Font("Consolas", 7.5f, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                graphics,
                this.monogram,
                iconFont,
                iconBounds,
                iconText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        var textColor = !this.Enabled
            ? ThemePalette.SubtleText
            : this.selected ? ThemePalette.TextStrong : ThemePalette.MutedText;
        var textBounds = new Rectangle(53, 0, Math.Max(0, this.ClientSize.Width - 63), this.ClientSize.Height);
        TextRenderer.DrawText(
            graphics,
            this.Text.ToUpperInvariant(),
            this.Font,
            textBounds,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

        if (this.Focused && this.ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(this.ClientRectangle, -5, -4), textColor, background);
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
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            this.mouseDown = true;
            this.Focus();
            this.Invalidate();
        }

        base.OnMouseDown(e);
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        this.mouseDown = false;
        this.Invalidate();
        base.OnMouseUp(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            e.Handled = true;
            this.OnClick(EventArgs.Empty);
        }

        base.OnKeyDown(e);
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

    private Color ResolveBackground(Color parentColor)
    {
        if (this.selected)
        {
            return ThemePalette.SurfaceElevated;
        }

        if (this.mouseDown)
        {
            return ThemePalette.Selection;
        }

        return this.mouseOver ? ThemePalette.PanelAlt : parentColor;
    }
}
