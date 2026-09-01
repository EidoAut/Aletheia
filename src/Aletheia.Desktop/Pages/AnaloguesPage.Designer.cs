using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class AnaloguesPage
{
    private TableLayoutPanel layout = null!;
    private MetricStripControl metrics = null!;
    private ForecastChartControl paths = null!;
    private DataGridCardControl matchesCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.metrics = new MetricStripControl();
        this.paths = new ForecastChartControl();
        this.matchesCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 1;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 3;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

        // metrics
        this.metrics.Dock = DockStyle.Fill;
        this.metrics.Name = "metrics";

        // paths
        this.paths.Dock = DockStyle.Fill;
        this.paths.Name = "paths";

        // matchesCard
        this.matchesCard.Dock = DockStyle.Fill;
        this.matchesCard.Name = "matchesCard";
        this.matchesCard.CardTitle = "Nearest historical states";
        this.matchesCard.CardSubtitle = "Schema-compatible analogue matches and realized forward returns";

        this.layout.Controls.Add(this.metrics, 0, 0);
        this.layout.Controls.Add(this.paths, 0, 1);
        this.layout.Controls.Add(this.matchesCard, 0, 2);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "AnaloguesPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
