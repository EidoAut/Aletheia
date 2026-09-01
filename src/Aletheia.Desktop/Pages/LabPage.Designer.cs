using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class LabPage
{
    private TableLayoutPanel layout = null!;
    private TheoryPanel theory = null!;
    private DataGridCardControl sectionsCard = null!;

    private void InitializeComponent()
    {
        this.layout = new TableLayoutPanel();
        this.theory = new TheoryPanel();
        this.sectionsCard = new DataGridCardControl();
        this.layout.SuspendLayout();
        this.SuspendLayout();

        // layout
        this.layout.BackColor = Color.FromArgb(0, 7, 12);
        this.layout.ColumnCount = 2;
        this.layout.Dock = DockStyle.Fill;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowCount = 1;
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // theory
        this.theory.Dock = DockStyle.Fill;
        this.theory.Name = "theory";

        // sectionsCard
        this.sectionsCard.Dock = DockStyle.Fill;
        this.sectionsCard.Name = "sectionsCard";
        this.sectionsCard.CardTitle = "Research map";
        this.sectionsCard.CardSubtitle = "How the analytical modules fit together";

        this.layout.Controls.Add(this.sectionsCard, 0, 0);
        this.layout.Controls.Add(this.theory, 1, 0);

        // page
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.layout);
        this.Dock = DockStyle.Fill;
        this.Name = "LabPage";
        this.Size = new Size(1200, 760);
        this.layout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
