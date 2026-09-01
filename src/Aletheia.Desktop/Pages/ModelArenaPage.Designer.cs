using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class ModelArenaPage
{
    private TableLayoutPanel layout = null!;
    private MetricStripControl metrics = null!;
    private DataGridCardControl coverageCard = null!;
    private DataGridCardControl pointCard = null!;
    private DataGridCardControl probabilityCard = null!;
    private DataGridCardControl quantileCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.metrics = new MetricStripControl();
        this.coverageCard = new DataGridCardControl();
        this.pointCard = new DataGridCardControl();
        this.probabilityCard = new DataGridCardControl();
        this.quantileCard = new DataGridCardControl();
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

        // coverageCard
        this.coverageCard.Dock = DockStyle.Fill;
        this.coverageCard.Name = "coverageCard";
        this.coverageCard.CardTitle = "Coverage";
        this.coverageCard.CardSubtitle = "Eligibility, success and claimed capabilities";

        // pointCard
        this.pointCard.Dock = DockStyle.Fill;
        this.pointCard.Name = "pointCard";
        this.pointCard.CardTitle = "Point forecasts";
        this.pointCard.CardSubtitle = "Errors, direction and baseline-relative skill";

        // probabilityCard
        this.probabilityCard.Dock = DockStyle.Fill;
        this.probabilityCard.Name = "probabilityCard";
        this.probabilityCard.CardTitle = "Probability forecasts";
        this.probabilityCard.CardSubtitle = "Brier score, calibration and skill";

        // quantileCard
        this.quantileCard.Dock = DockStyle.Fill;
        this.quantileCard.Name = "quantileCard";
        this.quantileCard.CardTitle = "Quantile forecasts";
        this.quantileCard.CardSubtitle = "Pinball loss and interval behaviour";

        this.layout.Controls.Add(this.metrics, 0, 0);
        this.layout.SetColumnSpan(this.metrics, 2);
        this.layout.Controls.Add(this.coverageCard, 0, 1);
        this.layout.Controls.Add(this.pointCard, 1, 1);
        this.layout.Controls.Add(this.probabilityCard, 0, 2);
        this.layout.Controls.Add(this.quantileCard, 1, 2);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "ModelArenaPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
