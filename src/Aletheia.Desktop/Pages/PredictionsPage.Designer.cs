using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class PredictionsPage
{
    private TableLayoutPanel layout = null!;
    private DataGridCardControl listCard = null!;
    private DataGridCardControl detailsCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.listCard = new DataGridCardControl();
        this.detailsCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 1;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // listCard
        this.listCard.Dock = DockStyle.Fill;
        this.listCard.Name = "listCard";
        this.listCard.CardTitle = "Prediction ledger";
        this.listCard.CardSubtitle = "Latest immutable forecast records";

        // detailsCard
        this.detailsCard.Dock = DockStyle.Fill;
        this.detailsCard.Name = "detailsCard";
        this.detailsCard.CardTitle = "Selected prediction";
        this.detailsCard.CardSubtitle = "Identity, capabilities and realized evaluation";

        this.layout.Controls.Add(this.listCard, 0, 0);
        this.layout.Controls.Add(this.detailsCard, 1, 0);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "PredictionsPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
