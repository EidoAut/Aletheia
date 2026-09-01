#pragma warning disable SA1116 // Existing compact WinForms initializers are kept stable.
#pragma warning disable SA1117 // Existing compact WinForms initializers are kept stable.
#pragma warning disable SA1204 // Existing UI factory methods are grouped by workflow.

using Aletheia.Application;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Displays concise theory metadata for an analytical method.
/// </summary>
internal sealed class TheoryPanel : UserControl
{
    private static readonly Font ArticleTitleFont = new("Consolas", 14f, FontStyle.Bold);
    private static readonly Font EquationFont = new("Consolas", 10f, FontStyle.Regular);
    private static readonly Font SectionHeadingFont = new("Consolas", 7.5f, FontStyle.Bold);
    private static readonly Font SectionBodyFont = new("Segoe UI", 9f, FontStyle.Regular);

    private readonly ComboBox selector = new();
    private readonly RichTextBox content = new();
    private readonly TheoryCatalog catalog = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TheoryPanel"/> class.
    /// </summary>
    public TheoryPanel()
    {
        this.Dock = DockStyle.Fill;
        this.Margin = new Padding(7);
        this.BackColor = ThemePalette.Background;
        this.BuildLayout();
        this.UpdateContent();
    }

    private void BuildLayout()
    {
        var card = new SurfacePanel
        {
            Dock = DockStyle.Fill,
            FillColor = ThemePalette.Surface,
            BorderColor = ThemePalette.Border,
            CornerRadius = 8,
            Padding = new Padding(1),
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ThemePalette.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(BuildHeader(), 0, 0);
        layout.Controls.Add(this.BuildSelector(), 0, 1);
        layout.Controls.Add(this.BuildContentHost(), 0, 2);
        card.Controls.Add(layout);
        this.Controls.Add(card);
    }

    private static Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ThemePalette.Surface,
            Padding = new Padding(14, 8, 14, 4),
            Margin = new Padding(0),
        };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        header.Controls.Add(new Label
        {
            Text = "Theory reference",
            Dock = DockStyle.Fill,
            ForeColor = ThemePalette.TextStrong,
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);
        header.Controls.Add(new Label
        {
            Text = "Method purpose, assumptions, interpretation and limits",
            Dock = DockStyle.Fill,
            ForeColor = ThemePalette.SubtleText,
            Font = new Font("Segoe UI", 8f, FontStyle.Regular),
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true,
        }, 0, 1);
        return header;
    }

    private Control BuildSelector()
    {
        var host = new SurfacePanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(14, 3, 14, 7),
            FillColor = ThemePalette.Input,
            BorderColor = ThemePalette.BorderStrong,
            CornerRadius = 8,
            Padding = new Padding(9, 5, 9, 3),
        };
        this.selector.Dock = DockStyle.Fill;
        this.selector.DropDownStyle = ComboBoxStyle.DropDownList;
        this.selector.FlatStyle = FlatStyle.Flat;
        this.selector.BackColor = ThemePalette.Input;
        this.selector.ForeColor = ThemePalette.Text;
        this.selector.Font = new Font("Consolas", 9f, FontStyle.Bold);
        this.selector.DisplayMember = nameof(TheoryArticle.Name);
        this.selector.IntegralHeight = false;
        this.selector.DropDownHeight = 260;
        this.selector.DataSource = this.catalog.Articles.ToArray();
        this.selector.SelectedIndexChanged += (_, _) => this.UpdateContent();
        host.Controls.Add(this.selector);
        return host;
    }

    private Control BuildContentHost()
    {
        var host = new SurfacePanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(14, 4, 14, 14),
            FillColor = ThemePalette.ChartBackground,
            BorderColor = ThemePalette.Border,
            CornerRadius = 8,
            Padding = new Padding(15, 13, 10, 12),
        };
        this.content.Dock = DockStyle.Fill;
        this.content.ReadOnly = true;
        this.content.BorderStyle = BorderStyle.None;
        this.content.BackColor = ThemePalette.ChartBackground;
        this.content.ForeColor = ThemePalette.Text;
        this.content.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        this.content.ScrollBars = RichTextBoxScrollBars.Vertical;
        this.content.DetectUrls = false;
        this.content.WordWrap = true;
        this.content.TabStop = false;
        host.Controls.Add(this.content);
        return host;
    }

    private void UpdateContent()
    {
        if (this.selector.SelectedItem is not TheoryArticle article)
        {
            return;
        }

        this.content.SuspendLayout();
        try
        {
            this.content.Clear();
            this.AppendTitle(article.Name);
            this.AppendEquation(article.Equation);
            this.AppendSection("PURPOSE", article.Purpose);
            this.AppendSection("ASSUMPTIONS", article.Assumptions);
            this.AppendSection("INTERPRETATION", article.Interpretation);
            this.AppendSection("LIMITATIONS", article.Limitations);
            this.content.SelectionStart = 0;
            this.content.SelectionLength = 0;
            this.content.ScrollToCaret();
        }
        finally
        {
            this.content.ResumeLayout();
        }
    }

    private void AppendTitle(string text)
    {
        this.content.SelectionColor = ThemePalette.TextStrong;
        this.content.SelectionFont = ArticleTitleFont;
        this.content.AppendText(text);
        this.content.AppendText(Environment.NewLine + Environment.NewLine);
    }

    private void AppendEquation(string text)
    {
        this.content.SelectionColor = ThemePalette.Accent;
        this.content.SelectionFont = EquationFont;
        this.content.AppendText(text);
        this.content.AppendText(Environment.NewLine + Environment.NewLine);
    }

    private void AppendSection(string heading, string text)
    {
        this.content.SelectionColor = ThemePalette.SubtleText;
        this.content.SelectionFont = SectionHeadingFont;
        this.content.AppendText(heading);
        this.content.AppendText(Environment.NewLine);
        this.content.SelectionColor = ThemePalette.Text;
        this.content.SelectionFont = SectionBodyFont;
        this.content.AppendText(text);
        this.content.AppendText(Environment.NewLine + Environment.NewLine);
    }
}
