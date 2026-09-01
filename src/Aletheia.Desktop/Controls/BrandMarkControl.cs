using System.Drawing.Drawing2D;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Renders the Aletheia and EIDO Automation wordmark.
/// </summary>
internal sealed class BrandMarkControl : UserControl
{
    private const string ProductCaption = "ALETHEIA";
    private const string KickerCaption = "ROOT / EIDO / MARKET_SIMULATOR";
    private const string CompanyCaption = "INDUSTRIAL FINANCIAL CONTROL";
    private const float DesignDpi = 96f;
    private readonly Image? logo;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrandMarkControl"/> class.
    /// </summary>
    public BrandMarkControl()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemePalette.Sidebar;
        this.DoubleBuffered = true;
        this.MinimumSize = new Size(224, 72);
        this.logo = LoadLogo();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.logo?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.Clear(ThemePalette.Sidebar);

        var logoHeight = Math.Min(this.ScaleLogical(92), Math.Max(this.ScaleLogical(62), this.ClientSize.Height - this.ScaleLogical(42)));
        var logoWidth = this.logo is null
            ? logoHeight
            : Math.Max(this.ScaleLogical(46), (int)Math.Round(logoHeight * (double)this.logo.Width / this.logo.Height));
        var logoBounds = new Rectangle(
            this.ScaleLogical(14),
            Math.Max(this.ScaleLogical(12), (this.ClientSize.Height - logoHeight) / 2),
            logoWidth,
            logoHeight);
        if (this.logo is null)
        {
            DrawFallbackMark(graphics, logoBounds);
        }
        else
        {
            graphics.DrawImage(this.logo, logoBounds);
        }

        var textLeft = logoBounds.Right + this.ScaleLogical(12);
        var textWidth = Math.Max(0, this.ClientSize.Width - textLeft - this.ScaleLogical(8));
        if (textWidth <= 0)
        {
            return;
        }

        using var kickerFont = CreateFittedFont(KickerCaption, textWidth, 7.25f, 5.75f);
        using var productFont = CreateFittedFont(ProductCaption, textWidth, 17f, 11f);
        using var companyFont = CreateFittedFont(CompanyCaption, textWidth, 7.75f, 5.75f);
        var kickerBounds = new Rectangle(
            textLeft,
            logoBounds.Top + this.ScaleLogical(7),
            textWidth,
            this.ScaleLogical(18));
        var productBounds = new Rectangle(
            textLeft,
            kickerBounds.Bottom,
            textWidth,
            this.ScaleLogical(33));
        var companyBounds = new Rectangle(
            textLeft,
            productBounds.Bottom,
            textWidth,
            this.ScaleLogical(22));
        var textFlags = TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(graphics, KickerCaption, kickerFont, kickerBounds, ThemePalette.AccentSecondary, textFlags);
        TextRenderer.DrawText(graphics, ProductCaption, productFont, productBounds, ThemePalette.TextStrong, textFlags);
        TextRenderer.DrawText(graphics, CompanyCaption, companyFont, companyBounds, ThemePalette.MutedText, textFlags);
    }

    private static Font CreateFittedFont(string text, int maximumWidth, float preferredSize, float minimumSize)
    {
        const float step = 0.25f;
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        for (var size = preferredSize; size >= minimumSize; size -= step)
        {
            var font = new Font("Consolas", size, FontStyle.Bold);
            var measured = TextRenderer.MeasureText(text, font, new Size(10_000, 1_000), flags);
            if (measured.Width <= maximumWidth)
            {
                return font;
            }

            font.Dispose();
        }

        return new Font("Consolas", minimumSize, FontStyle.Bold);
    }

    private static Image? LoadLogo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Branding", "eido_logo.png");
        return File.Exists(path) ? Image.FromFile(path) : null;
    }

    private static void DrawFallbackMark(Graphics graphics, Rectangle bounds)
    {
        using (var markPath = DrawingUtilities.CreateRoundedRectangle(bounds, 6f))
        using (var markBrush = new SolidBrush(ThemePalette.AccentSoft))
        using (var markBorder = new Pen(ThemePalette.Accent, 1.25f))
        {
            graphics.FillPath(markBrush, markPath);
            graphics.DrawPath(markBorder, markPath);
        }

        var left = bounds.Left + 10f;
        var center = bounds.Left + (bounds.Width / 2f);
        var right = bounds.Right - 10f;
        var top = bounds.Top + 11f;
        var bottom = bounds.Bottom - 11f;
        using var glyphPen = new Pen(ThemePalette.TextStrong, 2.35f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        graphics.DrawLine(glyphPen, left, center, center, top);
        graphics.DrawLine(glyphPen, center, top, right, center);
        graphics.DrawLine(glyphPen, right, center, center, bottom);
        graphics.DrawLine(glyphPen, center, bottom, left, center);
    }

    private int ScaleLogical(int value)
    {
        return Math.Max(1, (int)Math.Round(value * (this.DeviceDpi / DesignDpi), MidpointRounding.AwayFromZero));
    }

    private float ScaleLogical(float value)
    {
        return value * (this.DeviceDpi / DesignDpi);
    }
}
