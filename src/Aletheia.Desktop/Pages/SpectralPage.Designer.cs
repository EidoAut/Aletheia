using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class SpectralPage
{
    private TableLayoutPanel layout = null!;
    private SpectrumChartControl spectrumChart = null!;
    private DataGridCardControl diagnosticsCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.spectrumChart = new SpectrumChartControl();
        this.diagnosticsCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 1;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // spectrumChart
        this.spectrumChart.Dock = DockStyle.Fill;
        this.spectrumChart.Name = "spectrumChart";

        // diagnosticsCard
        this.diagnosticsCard.Dock = DockStyle.Fill;
        this.diagnosticsCard.Name = "diagnosticsCard";
        this.diagnosticsCard.CardTitle = "Spectral diagnostics";
        this.diagnosticsCard.CardSubtitle = "Peak structure, persistence and transform metadata";

        this.layout.Controls.Add(this.spectrumChart, 0, 0);
        this.layout.Controls.Add(this.diagnosticsCard, 1, 0);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "SpectralPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
