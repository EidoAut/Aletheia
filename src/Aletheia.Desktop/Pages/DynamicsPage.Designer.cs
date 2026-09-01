using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class DynamicsPage
{
    private TableLayoutPanel layout = null!;
    private ScatterChartControl momentumVolatility = null!;
    private ScatterChartControl velocityAcceleration = null!;
    private DataGridCardControl stateCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.momentumVolatility = new ScatterChartControl();
        this.velocityAcceleration = new ScatterChartControl();
        this.stateCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 2;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 56F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 44F));

        // momentumVolatility
        this.momentumVolatility.Dock = DockStyle.Fill;
        this.momentumVolatility.Name = "momentumVolatility";

        // velocityAcceleration
        this.velocityAcceleration.Dock = DockStyle.Fill;
        this.velocityAcceleration.Name = "velocityAcceleration";

        // stateCard
        this.stateCard.Dock = DockStyle.Fill;
        this.stateCard.Name = "stateCard";
        this.stateCard.CardTitle = "Current dynamic state";
        this.stateCard.CardSubtitle = "Schema-aware features at the latest observation";

        this.layout.Controls.Add(this.momentumVolatility, 0, 0);
        this.layout.Controls.Add(this.velocityAcceleration, 1, 0);
        this.layout.Controls.Add(this.stateCard, 0, 1);
        this.layout.SetColumnSpan(this.stateCard, 2);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "DynamicsPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
