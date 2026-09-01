using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class PerformancePage
{
    private TableLayoutPanel layout = null!;
    private MetricStripControl metrics = null!;
    private TimeSeriesChartControl navChart = null!;
    private TimeSeriesChartControl cumulativeChart = null!;
    private TimeSeriesChartControl returnsChart = null!;
    private TimeSeriesChartControl rollingReturnChart = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.metrics = new MetricStripControl();
        this.navChart = new TimeSeriesChartControl();
        this.cumulativeChart = new TimeSeriesChartControl();
        this.returnsChart = new TimeSeriesChartControl();
        this.rollingReturnChart = new TimeSeriesChartControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 3;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        // metrics
        this.metrics.Dock = DockStyle.Fill;
        this.metrics.Name = "metrics";

        // navChart
        this.navChart.Dock = DockStyle.Fill;
        this.navChart.Name = "navChart";

        // cumulativeChart
        this.cumulativeChart.Dock = DockStyle.Fill;
        this.cumulativeChart.Name = "cumulativeChart";

        // returnsChart
        this.returnsChart.Dock = DockStyle.Fill;
        this.returnsChart.Name = "returnsChart";

        // rollingReturnChart
        this.rollingReturnChart.Dock = DockStyle.Fill;
        this.rollingReturnChart.Name = "rollingReturnChart";

        this.layout.Controls.Add(this.metrics, 0, 0);
        this.layout.SetColumnSpan(this.metrics, 2);
        this.layout.Controls.Add(this.navChart, 0, 1);
        this.layout.Controls.Add(this.cumulativeChart, 1, 1);
        this.layout.Controls.Add(this.returnsChart, 0, 2);
        this.layout.Controls.Add(this.rollingReturnChart, 1, 2);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "PerformancePage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
