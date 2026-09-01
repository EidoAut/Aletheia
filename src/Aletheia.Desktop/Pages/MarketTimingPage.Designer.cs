using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class MarketTimingPage
{
    private TableLayoutPanel layout = null!;
    private MetricStripControl metrics = null!;
    private DataGridCardControl summaryCard = null!;
    private DataGridCardControl horizonsCard = null!;
    private DataGridCardControl whyCard = null!;
    private DataGridCardControl advancedCard = null!;
    private DataGridCardControl economicCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.metrics = new MetricStripControl();
        this.summaryCard = new DataGridCardControl();
        this.horizonsCard = new DataGridCardControl();
        this.whyCard = new DataGridCardControl();
        this.advancedCard = new DataGridCardControl();
        this.economicCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 4;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));

        // metrics
        this.metrics.Dock = DockStyle.Fill;
        this.metrics.Name = "metrics";

        // summaryCard
        this.summaryCard.Dock = DockStyle.Fill;
        this.summaryCard.Name = "summaryCard";
        this.summaryCard.CardTitle = "01 / ACTION_GUIDE";
        this.summaryCard.CardSubtitle = "Buy, hold or reduce guidance gated by evidence";

        // horizonsCard
        this.horizonsCard.Dock = DockStyle.Fill;
        this.horizonsCard.Name = "horizonsCard";
        this.horizonsCard.CardTitle = "02 / TIMING_WINDOWS";
        this.horizonsCard.CardSubtitle = "Triple-barrier probabilities by configurable horizon";

        // whyCard
        this.whyCard.Dock = DockStyle.Fill;
        this.whyCard.Name = "whyCard";
        this.whyCard.CardTitle = "03 / WHY";
        this.whyCard.CardSubtitle = "Evidence, counter-evidence, warnings and alerts";

        // advancedCard
        this.advancedCard.Dock = DockStyle.Fill;
        this.advancedCard.Name = "advancedCard";
        this.advancedCard.CardTitle = "04 / ADVANCED_DIAGNOSTICS";
        this.advancedCard.CardSubtitle = "Model weights, calibration and hazard checks";

        // economicCard
        this.economicCard.Dock = DockStyle.Fill;
        this.economicCard.Name = "economicCard";
        this.economicCard.CardTitle = "05 / ECONOMIC_BACKTEST";
        this.economicCard.CardSubtitle = "Historical OOS decisions, delayed execution and comparable outcomes";

        this.layout.Controls.Add(this.metrics, 0, 0);
        this.layout.SetColumnSpan(this.metrics, 2);
        this.layout.Controls.Add(this.summaryCard, 0, 1);
        this.layout.Controls.Add(this.horizonsCard, 1, 1);
        this.layout.Controls.Add(this.whyCard, 0, 2);
        this.layout.Controls.Add(this.advancedCard, 1, 2);
        this.layout.Controls.Add(this.economicCard, 0, 3);
        this.layout.SetColumnSpan(this.economicCard, 2);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "MarketTimingPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
