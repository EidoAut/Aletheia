using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class ForecastPage
{
    private TableLayoutPanel layout = null!;
    private DataGridCardControl forecastCard = null!;
    private DataGridCardControl detailCard = null!;
    private ForecastChartControl quantileChart = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.forecastCard = new DataGridCardControl();
        this.detailCard = new DataGridCardControl();
        this.quantileChart = new ForecastChartControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 2;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

        // forecastCard
        this.forecastCard.Dock = DockStyle.Fill;
        this.forecastCard.Name = "forecastCard";
        this.forecastCard.CardTitle = "01 / FORECAST_WINDOWS";
        this.forecastCard.CardSubtitle = "Current model outputs by requested horizon";

        // detailCard
        this.detailCard.Dock = DockStyle.Fill;
        this.detailCard.Name = "detailCard";
        this.detailCard.CardTitle = "02 / SELECTED_MODEL";
        this.detailCard.CardSubtitle = "Capabilities, horizon resolution and distribution summary";

        // quantileChart
        this.quantileChart.Dock = DockStyle.Fill;
        this.quantileChart.Name = "quantileChart";

        this.layout.Controls.Add(this.forecastCard, 0, 0);
        this.layout.SetRowSpan(this.forecastCard, 2);
        this.layout.Controls.Add(this.detailCard, 1, 0);
        this.layout.Controls.Add(this.quantileChart, 1, 1);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "ForecastPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
