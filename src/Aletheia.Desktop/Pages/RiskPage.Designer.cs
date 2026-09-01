using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class RiskPage
{
    private TableLayoutPanel layout = null!;
    private MetricStripControl metrics = null!;
    private DrawdownChartControl drawdown = null!;
    private TimeSeriesChartControl volatility = null!;
    private DistributionChartControl histogram = null!;
    private DataGridCardControl statsCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.metrics = new MetricStripControl();
        this.drawdown = new DrawdownChartControl();
        this.volatility = new TimeSeriesChartControl();
        this.histogram = new DistributionChartControl();
        this.statsCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 3;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        // metrics
        this.metrics.Dock = DockStyle.Fill;
        this.metrics.Name = "metrics";

        // drawdown
        this.drawdown.Dock = DockStyle.Fill;
        this.drawdown.Name = "drawdown";

        // volatility
        this.volatility.Dock = DockStyle.Fill;
        this.volatility.Name = "volatility";

        // histogram
        this.histogram.Dock = DockStyle.Fill;
        this.histogram.Name = "histogram";

        // statsCard
        this.statsCard.Dock = DockStyle.Fill;
        this.statsCard.Name = "statsCard";
        this.statsCard.CardTitle = "Distribution statistics";
        this.statsCard.CardSubtitle = "Summary of realized simple returns";

        this.layout.Controls.Add(this.metrics, 0, 0);
        this.layout.SetColumnSpan(this.metrics, 2);
        this.layout.Controls.Add(this.drawdown, 0, 1);
        this.layout.Controls.Add(this.volatility, 1, 1);
        this.layout.Controls.Add(this.histogram, 0, 2);
        this.layout.Controls.Add(this.statsCard, 1, 2);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "RiskPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
