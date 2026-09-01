using System.ComponentModel;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Hosts a styled analytical grid inside a titled dashboard card.
/// </summary>
internal sealed class DataGridCardControl : UserControl
{
    private readonly Label countLabel = new();
    private readonly Label titleLabel = new();
    private readonly Label subtitleLabel = new();
    private readonly TableLayoutPanel cardLayout = new();
    private readonly TableLayoutPanel textPanel = new();
    private string cardTitle = "Data";
    private string? cardSubtitle;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridCardControl"/> class for the WinForms designer.
    /// </summary>
    public DataGridCardControl()
        : this("Data")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridCardControl"/> class.
    /// </summary>
    /// <param name="title">The card title.</param>
    /// <param name="subtitle">The optional card subtitle.</param>
    public DataGridCardControl(string title, string? subtitle = null)
    {
        this.Dock = DockStyle.Fill;
        this.Margin = new Padding(7);
        this.BackColor = ThemePalette.Background;
        this.Grid = GridFactory.Create();
        this.Grid.Margin = new Padding(0);
        this.Grid.RowsAdded += (_, _) => this.UpdateCount();
        this.Grid.RowsRemoved += (_, _) => this.UpdateCount();
        this.Grid.DataBindingComplete += (_, _) => this.UpdateCount();

        var card = new SurfacePanel
        {
            Dock = DockStyle.Fill,
            FillColor = ThemePalette.Surface,
            BorderColor = ThemePalette.Border,
            CornerRadius = 8,
            Padding = new Padding(1),
        };
        this.cardLayout.Dock = DockStyle.Fill;
        this.cardLayout.ColumnCount = 1;
        this.cardLayout.RowCount = 2;
        this.cardLayout.BackColor = ThemePalette.Surface;
        this.cardLayout.Margin = new Padding(0);
        this.cardLayout.Padding = new Padding(0);
        this.cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        this.cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        this.cardLayout.Controls.Add(this.BuildHeader(), 0, 0);
        this.cardLayout.Controls.Add(this.Grid, 0, 1);
        card.Controls.Add(this.cardLayout);
        this.Controls.Add(card);

        this.CardTitle = title;
        this.CardSubtitle = subtitle;
        this.UpdateCount();
    }

    /// <summary>
    /// Gets the hosted analytical grid.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DataGridView Grid { get; }

    /// <summary>
    /// Gets or sets the title displayed by the card.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue("Data")]
    public string CardTitle
    {
        get => this.cardTitle;
        set
        {
            this.cardTitle = string.IsNullOrWhiteSpace(value) ? "Data" : value;
            this.titleLabel.Text = this.cardTitle;
        }
    }

    /// <summary>
    /// Gets or sets the optional explanatory subtitle displayed below the title.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public string? CardSubtitle
    {
        get => this.cardSubtitle;
        set
        {
            this.cardSubtitle = string.IsNullOrWhiteSpace(value) ? null : value;
            this.subtitleLabel.Text = this.cardSubtitle ?? string.Empty;
            this.subtitleLabel.Visible = this.cardSubtitle is not null;
            this.textPanel.RowCount = 2;
            this.textPanel.RowStyles.Clear();
            if (this.cardSubtitle is null)
            {
                this.textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                this.textPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
                this.cardLayout.RowStyles[0].Height = 45;
            }
            else
            {
                this.textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
                this.textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
                this.cardLayout.RowStyles[0].Height = 54;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the row counter is visible.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowCount
    {
        get => this.countLabel.Visible;
        set => this.countLabel.Visible = value;
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ThemePalette.Surface,
            Padding = new Padding(14, 7, 12, 5),
            Margin = new Padding(0),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

        this.textPanel.Dock = DockStyle.Fill;
        this.textPanel.ColumnCount = 1;
        this.textPanel.RowCount = 2;
        this.textPanel.BackColor = ThemePalette.Surface;
        this.textPanel.Margin = new Padding(0);
        this.textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        this.textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

        this.titleLabel.Dock = DockStyle.Fill;
        this.titleLabel.ForeColor = ThemePalette.TextStrong;
        this.titleLabel.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
        this.titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.titleLabel.AutoEllipsis = true;

        this.subtitleLabel.Dock = DockStyle.Fill;
        this.subtitleLabel.ForeColor = ThemePalette.SubtleText;
        this.subtitleLabel.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
        this.subtitleLabel.TextAlign = ContentAlignment.TopLeft;
        this.subtitleLabel.AutoEllipsis = true;

        this.textPanel.Controls.Add(this.titleLabel, 0, 0);
        this.textPanel.Controls.Add(this.subtitleLabel, 0, 1);

        this.countLabel.Dock = DockStyle.Fill;
        this.countLabel.ForeColor = ThemePalette.SubtleText;
        this.countLabel.Font = new Font("Consolas", 8f, FontStyle.Bold);
        this.countLabel.TextAlign = ContentAlignment.MiddleRight;
        header.Controls.Add(this.textPanel, 0, 0);
        header.Controls.Add(this.countLabel, 1, 0);
        return header;
    }

    private void UpdateCount()
    {
        this.countLabel.Text = this.Grid.Rows.Count == 1
            ? "1 ROW"
            : $"{this.Grid.Rows.Count} ROWS";
    }
}
