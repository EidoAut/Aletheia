using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class OverviewPage
{
    private TableLayoutPanel layout = null!;
    private MetricStripControl metrics = null!;
    private TimeSeriesChartControl navChart = null!;
    private DrawdownChartControl drawdownChart = null!;
    private TimeSeriesChartControl rollingChart = null!;
    private DataGridCardControl detailsCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.metrics = new MetricStripControl();
        this.navChart = new TimeSeriesChartControl();
        this.drawdownChart = new DrawdownChartControl();
        this.rollingChart = new TimeSeriesChartControl();
        this.detailsCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 3;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 61F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 39F));

        // metrics
        this.metrics.Dock = DockStyle.Fill;
        this.metrics.Name = "metrics";

        // navChart
        this.navChart.Dock = DockStyle.Fill;
        this.navChart.Name = "navChart";

        // drawdownChart
        this.drawdownChart.Dock = DockStyle.Fill;
        this.drawdownChart.Name = "drawdownChart";

        // rollingChart
        this.rollingChart.Dock = DockStyle.Fill;
        this.rollingChart.Name = "rollingChart";

        // detailsCard
        this.detailsCard.Dock = DockStyle.Fill;
        this.detailsCard.Name = "detailsCard";
        this.detailsCard.CardTitle = "01 / INVESTOR_GUIDANCE";
        this.detailsCard.CardSubtitle = "Quality, actionability, provenance and current state";

        this.layout.Controls.Add(this.metrics, 0, 0);
        this.layout.SetColumnSpan(this.metrics, 2);
        this.layout.Controls.Add(this.navChart, 0, 1);
        this.layout.Controls.Add(this.detailsCard, 1, 1);
        this.layout.Controls.Add(this.drawdownChart, 0, 2);
        this.layout.Controls.Add(this.rollingChart, 1, 2);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "OverviewPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
