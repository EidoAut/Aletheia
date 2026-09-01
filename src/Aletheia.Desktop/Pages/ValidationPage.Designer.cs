using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class ValidationPage
{
    private TableLayoutPanel layout = null!;
    private CalibrationChartControl calibration = null!;
    private ForecastChartControl predictedActual = null!;
    private ForecastChartControl errors = null!;
    private DataGridCardControl configurationCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.calibration = new CalibrationChartControl();
        this.predictedActual = new ForecastChartControl();
        this.errors = new ForecastChartControl();
        this.configurationCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 2;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        // calibration
        this.calibration.Dock = DockStyle.Fill;
        this.calibration.Name = "calibration";

        // predictedActual
        this.predictedActual.Dock = DockStyle.Fill;
        this.predictedActual.Name = "predictedActual";

        // errors
        this.errors.Dock = DockStyle.Fill;
        this.errors.Name = "errors";

        // configurationCard
        this.configurationCard.Dock = DockStyle.Fill;
        this.configurationCard.Name = "configurationCard";
        this.configurationCard.CardTitle = "Validation configuration";
        this.configurationCard.CardSubtitle = "Walk-forward support and ranking context";

        this.layout.Controls.Add(this.calibration, 0, 0);
        this.layout.Controls.Add(this.predictedActual, 1, 0);
        this.layout.Controls.Add(this.errors, 0, 1);
        this.layout.Controls.Add(this.configurationCard, 1, 1);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "ValidationPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
