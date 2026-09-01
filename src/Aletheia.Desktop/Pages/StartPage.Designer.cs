using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Pages;

partial class StartPage
{
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel heroLayout = null!;
    private TableLayoutPanel heroCopyLayout = null!;
    private Label eyebrowLabel = null!;
    private Label heroTitleLabel = null!;
    private Label heroDescriptionLabel = null!;
    private SurfacePanel principlesCard = null!;
    private TableLayoutPanel principlesLayout = null!;
    private Label sourceKeyLabel = null!;
    private Label sourceValueLabel = null!;
    private Label methodKeyLabel = null!;
    private Label methodValueLabel = null!;
    private Label outputKeyLabel = null!;
    private Label outputValueLabel = null!;
    private SurfacePanel searchCard = null!;
    private TableLayoutPanel searchLayout = null!;
    private SurfacePanel inputFrame = null!;
    private TextBox searchBox = null!;
    private AletheiaButton searchButton = null!;
    private AletheiaButton loadSelectedButton = null!;
    private Label stateLabel = null!;
    private DataGridCardControl resultsCard = null!;
    private TableLayoutPanel quickActionsLayout = null!;
    private SurfacePanel sampleCard = null!;
    private TableLayoutPanel sampleLayout = null!;
    private Label sampleTitleLabel = null!;
    private Label sampleDescriptionLabel = null!;
    private AletheiaButton loadSampleQuickButton = null!;
    private SurfacePanel csvCard = null!;
    private TableLayoutPanel csvLayout = null!;
    private Label csvTitleLabel = null!;
    private Label csvDescriptionLabel = null!;
    private AletheiaButton openCsvQuickButton = null!;
    private SurfacePanel disciplineCard = null!;
    private TableLayoutPanel disciplineLayout = null!;
    private Label disciplineTitleLabel = null!;
    private Label disciplineDescriptionLabel = null!;

    private void InitializeComponent()
    {
        this.rootLayout = new TableLayoutPanel();
        this.heroLayout = new TableLayoutPanel();
        this.heroCopyLayout = new TableLayoutPanel();
        this.eyebrowLabel = new Label();
        this.heroTitleLabel = new Label();
        this.heroDescriptionLabel = new Label();
        this.principlesCard = new SurfacePanel();
        this.principlesLayout = new TableLayoutPanel();
        this.sourceKeyLabel = new Label();
        this.sourceValueLabel = new Label();
        this.methodKeyLabel = new Label();
        this.methodValueLabel = new Label();
        this.outputKeyLabel = new Label();
        this.outputValueLabel = new Label();
        this.searchCard = new SurfacePanel();
        this.searchLayout = new TableLayoutPanel();
        this.inputFrame = new SurfacePanel();
        this.searchBox = new TextBox();
        this.searchButton = new AletheiaButton();
        this.loadSelectedButton = new AletheiaButton();
        this.stateLabel = new Label();
        this.resultsCard = new DataGridCardControl();
        this.quickActionsLayout = new TableLayoutPanel();
        this.sampleCard = new SurfacePanel();
        this.sampleLayout = new TableLayoutPanel();
        this.sampleTitleLabel = new Label();
        this.sampleDescriptionLabel = new Label();
        this.loadSampleQuickButton = new AletheiaButton();
        this.csvCard = new SurfacePanel();
        this.csvLayout = new TableLayoutPanel();
        this.csvTitleLabel = new Label();
        this.csvDescriptionLabel = new Label();
        this.openCsvQuickButton = new AletheiaButton();
        this.disciplineCard = new SurfacePanel();
        this.disciplineLayout = new TableLayoutPanel();
        this.disciplineTitleLabel = new Label();
        this.disciplineDescriptionLabel = new Label();
        this.rootLayout.SuspendLayout();
        this.heroLayout.SuspendLayout();
        this.heroCopyLayout.SuspendLayout();
        this.principlesCard.SuspendLayout();
        this.principlesLayout.SuspendLayout();
        this.searchCard.SuspendLayout();
        this.searchLayout.SuspendLayout();
        this.inputFrame.SuspendLayout();
        this.quickActionsLayout.SuspendLayout();
        this.sampleCard.SuspendLayout();
        this.sampleLayout.SuspendLayout();
        this.csvCard.SuspendLayout();
        this.csvLayout.SuspendLayout();
        this.disciplineCard.SuspendLayout();
        this.disciplineLayout.SuspendLayout();
        this.SuspendLayout();

        // rootLayout
        this.rootLayout.BackColor = Color.FromArgb(0, 7, 12);
        this.rootLayout.ColumnCount = 1;
        this.rootLayout.Dock = DockStyle.Fill;
        this.rootLayout.Margin = new Padding(0);
        this.rootLayout.Name = "rootLayout";
        this.rootLayout.Padding = new Padding(0);
        this.rootLayout.RowCount = 4;
        this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
        this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
        this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));

        // heroLayout
        this.heroLayout.BackColor = Color.FromArgb(0, 7, 12);
        this.heroLayout.ColumnCount = 2;
        this.heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        this.heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        this.heroLayout.Controls.Add(this.heroCopyLayout, 0, 0);
        this.heroLayout.Controls.Add(this.principlesCard, 1, 0);
        this.heroLayout.Dock = DockStyle.Fill;
        this.heroLayout.Margin = new Padding(0);
        this.heroLayout.Name = "heroLayout";
        this.heroLayout.RowCount = 1;
        this.heroLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // heroCopyLayout
        this.heroCopyLayout.BackColor = Color.FromArgb(0, 7, 12);
        this.heroCopyLayout.ColumnCount = 1;
        this.heroCopyLayout.Controls.Add(this.eyebrowLabel, 0, 0);
        this.heroCopyLayout.Controls.Add(this.heroTitleLabel, 0, 1);
        this.heroCopyLayout.Controls.Add(this.heroDescriptionLabel, 0, 2);
        this.heroCopyLayout.Dock = DockStyle.Fill;
        this.heroCopyLayout.Margin = new Padding(0);
        this.heroCopyLayout.Name = "heroCopyLayout";
        this.heroCopyLayout.Padding = new Padding(8, 8, 12, 8);
        this.heroCopyLayout.RowCount = 3;
        this.heroCopyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        this.heroCopyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        this.heroCopyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // eyebrowLabel
        this.eyebrowLabel.Dock = DockStyle.Fill;
        this.eyebrowLabel.Font = new Font("Consolas", 7.75F, FontStyle.Bold);
        this.eyebrowLabel.ForeColor = Color.FromArgb(96, 192, 224);
        this.eyebrowLabel.Name = "eyebrowLabel";
        this.eyebrowLabel.Text = "ROOT / EIDO / MARKET_SIMULATOR";
        this.eyebrowLabel.TextAlign = ContentAlignment.BottomLeft;

        // heroTitleLabel
        this.heroTitleLabel.AutoEllipsis = true;
        this.heroTitleLabel.Dock = DockStyle.Fill;
        this.heroTitleLabel.Font = new Font("Consolas", 16F, FontStyle.Bold);
        this.heroTitleLabel.ForeColor = Color.FromArgb(255, 255, 255);
        this.heroTitleLabel.Name = "heroTitleLabel";
        this.heroTitleLabel.Text = "ALETHEIA MARKET LAB";
        this.heroTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        // heroDescriptionLabel
        this.heroDescriptionLabel.AutoEllipsis = true;
        this.heroDescriptionLabel.Dock = DockStyle.Fill;
        this.heroDescriptionLabel.Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
        this.heroDescriptionLabel.ForeColor = Color.FromArgb(186, 200, 209);
        this.heroDescriptionLabel.Name = "heroDescriptionLabel";
        this.heroDescriptionLabel.Text = "OFFICIAL FUND SEARCH / FORECAST / TIMING / VALIDATION";
        this.heroDescriptionLabel.TextAlign = ContentAlignment.TopLeft;

        // principlesCard
        this.principlesCard.BorderColor = Color.FromArgb(26, 42, 52);
        this.principlesCard.Controls.Add(this.principlesLayout);
        this.principlesCard.CornerRadius = 8;
        this.principlesCard.Dock = DockStyle.Fill;
        this.principlesCard.FillColor = Color.FromArgb(0, 10, 18);
        this.principlesCard.Margin = new Padding(7);
        this.principlesCard.Name = "principlesCard";
        this.principlesCard.Padding = new Padding(14, 10, 14, 10);

        // principlesLayout
        this.principlesLayout.BackColor = Color.FromArgb(0, 10, 18);
        this.principlesLayout.ColumnCount = 2;
        this.principlesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        this.principlesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this.principlesLayout.Controls.Add(this.sourceKeyLabel, 0, 0);
        this.principlesLayout.Controls.Add(this.sourceValueLabel, 1, 0);
        this.principlesLayout.Controls.Add(this.methodKeyLabel, 0, 1);
        this.principlesLayout.Controls.Add(this.methodValueLabel, 1, 1);
        this.principlesLayout.Controls.Add(this.outputKeyLabel, 0, 2);
        this.principlesLayout.Controls.Add(this.outputValueLabel, 1, 2);
        this.principlesLayout.Dock = DockStyle.Fill;
        this.principlesLayout.Margin = new Padding(0);
        this.principlesLayout.Name = "principlesLayout";
        this.principlesLayout.RowCount = 3;
        this.principlesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        this.principlesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        this.principlesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

        // principle labels
        this.sourceKeyLabel.Dock = DockStyle.Fill;
        this.sourceKeyLabel.Font = new Font("Consolas", 7.25F, FontStyle.Bold);
        this.sourceKeyLabel.ForeColor = Color.FromArgb(129, 147, 157);
        this.sourceKeyLabel.Name = "sourceKeyLabel";
        this.sourceKeyLabel.Text = "SOURCE";
        this.sourceKeyLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.sourceValueLabel.AutoEllipsis = true;
        this.sourceValueLabel.Dock = DockStyle.Fill;
        this.sourceValueLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
        this.sourceValueLabel.ForeColor = Color.FromArgb(238, 247, 251);
        this.sourceValueLabel.Name = "sourceValueLabel";
        this.sourceValueLabel.Text = "CNMV / CSV";
        this.sourceValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.methodKeyLabel.Dock = DockStyle.Fill;
        this.methodKeyLabel.Font = new Font("Consolas", 7.25F, FontStyle.Bold);
        this.methodKeyLabel.ForeColor = Color.FromArgb(129, 147, 157);
        this.methodKeyLabel.Name = "methodKeyLabel";
        this.methodKeyLabel.Text = "STACK";
        this.methodKeyLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.methodValueLabel.AutoEllipsis = true;
        this.methodValueLabel.Dock = DockStyle.Fill;
        this.methodValueLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
        this.methodValueLabel.ForeColor = Color.FromArgb(238, 247, 251);
        this.methodValueLabel.Name = "methodValueLabel";
        this.methodValueLabel.Text = "Forecast / Arena";
        this.methodValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.outputKeyLabel.Dock = DockStyle.Fill;
        this.outputKeyLabel.Font = new Font("Consolas", 7.25F, FontStyle.Bold);
        this.outputKeyLabel.ForeColor = Color.FromArgb(129, 147, 157);
        this.outputKeyLabel.Name = "outputKeyLabel";
        this.outputKeyLabel.Text = "OUTPUT";
        this.outputKeyLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.outputValueLabel.AutoEllipsis = true;
        this.outputValueLabel.Dock = DockStyle.Fill;
        this.outputValueLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
        this.outputValueLabel.ForeColor = Color.FromArgb(238, 247, 251);
        this.outputValueLabel.Name = "outputValueLabel";
        this.outputValueLabel.Text = "Buy / Hold / Reduce";
        this.outputValueLabel.TextAlign = ContentAlignment.MiddleLeft;

        // searchCard
        this.searchCard.BorderColor = Color.FromArgb(26, 42, 52);
        this.searchCard.Controls.Add(this.searchLayout);
        this.searchCard.CornerRadius = 8;
        this.searchCard.Dock = DockStyle.Fill;
        this.searchCard.FillColor = Color.FromArgb(0, 10, 18);
        this.searchCard.Margin = new Padding(7);
        this.searchCard.Name = "searchCard";
        this.searchCard.Padding = new Padding(14, 12, 14, 10);

        // searchLayout
        this.searchLayout.BackColor = Color.FromArgb(0, 10, 18);
        this.searchLayout.ColumnCount = 3;
        this.searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this.searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106F));
        this.searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
        this.searchLayout.Controls.Add(this.inputFrame, 0, 0);
        this.searchLayout.Controls.Add(this.searchButton, 1, 0);
        this.searchLayout.Controls.Add(this.loadSelectedButton, 2, 0);
        this.searchLayout.Controls.Add(this.stateLabel, 0, 1);
        this.searchLayout.Dock = DockStyle.Fill;
        this.searchLayout.Margin = new Padding(0);
        this.searchLayout.Name = "searchLayout";
        this.searchLayout.RowCount = 2;
        this.searchLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        this.searchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.searchLayout.SetColumnSpan(this.stateLabel, 3);

        // inputFrame
        this.inputFrame.BorderColor = Color.FromArgb(38, 57, 70);
        this.inputFrame.Controls.Add(this.searchBox);
        this.inputFrame.CornerRadius = 8;
        this.inputFrame.Dock = DockStyle.Fill;
        this.inputFrame.FillColor = Color.FromArgb(0, 10, 18);
        this.inputFrame.Margin = new Padding(0, 0, 8, 2);
        this.inputFrame.Name = "inputFrame";
        this.inputFrame.Padding = new Padding(12, 10, 12, 7);

        // searchBox
        this.searchBox.BackColor = Color.FromArgb(0, 10, 18);
        this.searchBox.BorderStyle = BorderStyle.None;
        this.searchBox.Dock = DockStyle.Fill;
        this.searchBox.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        this.searchBox.ForeColor = Color.FromArgb(238, 247, 251);
        this.searchBox.Name = "searchBox";
        this.searchBox.PlaceholderText = "Fund name, ISIN or manager";

        // searchButton
        this.searchButton.Dock = DockStyle.Fill;
        this.searchButton.Kind = AletheiaButtonKind.Primary;
        this.searchButton.Margin = new Padding(2, 0, 4, 2);
        this.searchButton.Name = "SearchButton";
        this.searchButton.Text = "Search";

        // loadSelectedButton
        this.loadSelectedButton.Dock = DockStyle.Fill;
        this.loadSelectedButton.Enabled = false;
        this.loadSelectedButton.Kind = AletheiaButtonKind.Secondary;
        this.loadSelectedButton.Margin = new Padding(4, 0, 0, 2);
        this.loadSelectedButton.Name = "LoadFundButton";
        this.loadSelectedButton.Text = "Analyze";

        // stateLabel
        this.stateLabel.Dock = DockStyle.Fill;
        this.stateLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
        this.stateLabel.ForeColor = Color.FromArgb(186, 200, 209);
        this.stateLabel.Name = "stateLabel";
        this.stateLabel.Text = "Search the official catalogue to begin the investor guidance workflow.";
        this.stateLabel.TextAlign = ContentAlignment.MiddleLeft;

        // resultsCard
        this.resultsCard.CardSubtitle = "Official provider discovery results";
        this.resultsCard.CardTitle = "01 / FUND_CATALOGUE";
        this.resultsCard.Dock = DockStyle.Fill;
        this.resultsCard.Name = "resultsCard";

        // quickActionsLayout
        this.quickActionsLayout.BackColor = Color.FromArgb(0, 7, 12);
        this.quickActionsLayout.ColumnCount = 3;
        this.quickActionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        this.quickActionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        this.quickActionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        this.quickActionsLayout.Controls.Add(this.sampleCard, 0, 0);
        this.quickActionsLayout.Controls.Add(this.csvCard, 1, 0);
        this.quickActionsLayout.Controls.Add(this.disciplineCard, 2, 0);
        this.quickActionsLayout.Dock = DockStyle.Fill;
        this.quickActionsLayout.Margin = new Padding(0);
        this.quickActionsLayout.Name = "quickActionsLayout";
        this.quickActionsLayout.Padding = new Padding(0, 1, 0, 5);
        this.quickActionsLayout.RowCount = 1;
        this.quickActionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // sampleCard
        this.sampleCard.BorderColor = Color.FromArgb(26, 42, 52);
        this.sampleCard.Controls.Add(this.sampleLayout);
        this.sampleCard.CornerRadius = 8;
        this.sampleCard.Dock = DockStyle.Fill;
        this.sampleCard.FillColor = Color.FromArgb(0, 10, 18);
        this.sampleCard.Margin = new Padding(7, 5, 7, 7);
        this.sampleCard.Name = "sampleCard";
        this.sampleCard.Padding = new Padding(13, 10, 12, 10);
        this.sampleLayout.BackColor = Color.FromArgb(0, 10, 18);
        this.sampleLayout.ColumnCount = 2;
        this.sampleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this.sampleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
        this.sampleLayout.Controls.Add(this.sampleTitleLabel, 0, 0);
        this.sampleLayout.Controls.Add(this.sampleDescriptionLabel, 0, 1);
        this.sampleLayout.Controls.Add(this.loadSampleQuickButton, 1, 0);
        this.sampleLayout.Dock = DockStyle.Fill;
        this.sampleLayout.Margin = new Padding(0);
        this.sampleLayout.Name = "sampleLayout";
        this.sampleLayout.RowCount = 2;
        this.sampleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        this.sampleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.sampleLayout.SetRowSpan(this.loadSampleQuickButton, 2);
        this.sampleTitleLabel.Dock = DockStyle.Fill;
        this.sampleTitleLabel.Font = new Font("Consolas", 7.25F, FontStyle.Bold);
        this.sampleTitleLabel.ForeColor = Color.FromArgb(96, 192, 224);
        this.sampleTitleLabel.Name = "sampleTitleLabel";
        this.sampleTitleLabel.Text = "02 / SAMPLE_DATASET";
        this.sampleTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.sampleDescriptionLabel.AutoEllipsis = true;
        this.sampleDescriptionLabel.Dock = DockStyle.Fill;
        this.sampleDescriptionLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        this.sampleDescriptionLabel.ForeColor = Color.FromArgb(186, 200, 209);
        this.sampleDescriptionLabel.Name = "sampleDescriptionLabel";
        this.sampleDescriptionLabel.Text = "Open the bundled reproducible example.";
        this.sampleDescriptionLabel.TextAlign = ContentAlignment.TopLeft;
        this.loadSampleQuickButton.Anchor = AnchorStyles.None;
        this.loadSampleQuickButton.Kind = AletheiaButtonKind.Secondary;
        this.loadSampleQuickButton.Margin = new Padding(8, 0, 0, 0);
        this.loadSampleQuickButton.Name = "loadSampleQuickButton";
        this.loadSampleQuickButton.Size = new Size(100, 36);
        this.loadSampleQuickButton.Text = "Load";

        // csvCard
        this.csvCard.BorderColor = Color.FromArgb(26, 42, 52);
        this.csvCard.Controls.Add(this.csvLayout);
        this.csvCard.CornerRadius = 8;
        this.csvCard.Dock = DockStyle.Fill;
        this.csvCard.FillColor = Color.FromArgb(0, 10, 18);
        this.csvCard.Margin = new Padding(7, 5, 7, 7);
        this.csvCard.Name = "csvCard";
        this.csvCard.Padding = new Padding(13, 10, 12, 10);
        this.csvLayout.BackColor = Color.FromArgb(0, 10, 18);
        this.csvLayout.ColumnCount = 2;
        this.csvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this.csvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
        this.csvLayout.Controls.Add(this.csvTitleLabel, 0, 0);
        this.csvLayout.Controls.Add(this.csvDescriptionLabel, 0, 1);
        this.csvLayout.Controls.Add(this.openCsvQuickButton, 1, 0);
        this.csvLayout.Dock = DockStyle.Fill;
        this.csvLayout.Margin = new Padding(0);
        this.csvLayout.Name = "csvLayout";
        this.csvLayout.RowCount = 2;
        this.csvLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        this.csvLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.csvLayout.SetRowSpan(this.openCsvQuickButton, 2);
        this.csvTitleLabel.Dock = DockStyle.Fill;
        this.csvTitleLabel.Font = new Font("Consolas", 7.25F, FontStyle.Bold);
        this.csvTitleLabel.ForeColor = Color.FromArgb(96, 192, 224);
        this.csvTitleLabel.Name = "csvTitleLabel";
        this.csvTitleLabel.Text = "03 / LOCAL_FILE";
        this.csvTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.csvDescriptionLabel.AutoEllipsis = true;
        this.csvDescriptionLabel.Dock = DockStyle.Fill;
        this.csvDescriptionLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        this.csvDescriptionLabel.ForeColor = Color.FromArgb(186, 200, 209);
        this.csvDescriptionLabel.Name = "csvDescriptionLabel";
        this.csvDescriptionLabel.Text = "Load Date,NAV observations from CSV.";
        this.csvDescriptionLabel.TextAlign = ContentAlignment.TopLeft;
        this.openCsvQuickButton.Anchor = AnchorStyles.None;
        this.openCsvQuickButton.Kind = AletheiaButtonKind.Secondary;
        this.openCsvQuickButton.Margin = new Padding(8, 0, 0, 0);
        this.openCsvQuickButton.Name = "openCsvQuickButton";
        this.openCsvQuickButton.Size = new Size(100, 36);
        this.openCsvQuickButton.Text = "Open";

        // disciplineCard
        this.disciplineCard.BorderColor = Color.FromArgb(96, 192, 224);
        this.disciplineCard.Controls.Add(this.disciplineLayout);
        this.disciplineCard.CornerRadius = 8;
        this.disciplineCard.Dock = DockStyle.Fill;
        this.disciplineCard.FillColor = Color.FromArgb(20, 56, 74);
        this.disciplineCard.Margin = new Padding(7, 5, 7, 7);
        this.disciplineCard.Name = "disciplineCard";
        this.disciplineCard.Padding = new Padding(14, 10, 14, 10);
        this.disciplineLayout.BackColor = Color.FromArgb(20, 56, 74);
        this.disciplineLayout.ColumnCount = 1;
        this.disciplineLayout.Controls.Add(this.disciplineTitleLabel, 0, 0);
        this.disciplineLayout.Controls.Add(this.disciplineDescriptionLabel, 0, 1);
        this.disciplineLayout.Dock = DockStyle.Fill;
        this.disciplineLayout.Margin = new Padding(0);
        this.disciplineLayout.Name = "disciplineLayout";
        this.disciplineLayout.RowCount = 2;
        this.disciplineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        this.disciplineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.disciplineTitleLabel.Dock = DockStyle.Fill;
        this.disciplineTitleLabel.Font = new Font("Consolas", 7.25F, FontStyle.Bold);
        this.disciplineTitleLabel.ForeColor = Color.FromArgb(96, 192, 224);
        this.disciplineTitleLabel.Name = "disciplineTitleLabel";
        this.disciplineTitleLabel.Text = "04 / DECISION_DISCIPLINE";
        this.disciplineTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.disciplineDescriptionLabel.Dock = DockStyle.Fill;
        this.disciplineDescriptionLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        this.disciplineDescriptionLabel.ForeColor = Color.FromArgb(238, 247, 251);
        this.disciplineDescriptionLabel.Name = "disciplineDescriptionLabel";
        this.disciplineDescriptionLabel.Text = "Aletheia can guide action only when evidence, freshness and validation survive checks.";
        this.disciplineDescriptionLabel.TextAlign = ContentAlignment.TopLeft;

        this.rootLayout.Controls.Add(this.heroLayout, 0, 0);
        this.rootLayout.Controls.Add(this.searchCard, 0, 1);
        this.rootLayout.Controls.Add(this.resultsCard, 0, 2);
        this.rootLayout.Controls.Add(this.quickActionsLayout, 0, 3);

        // StartPage
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(0, 7, 12);
        this.Controls.Add(this.rootLayout);
        this.Dock = DockStyle.Fill;
        this.Name = "StartPage";
        this.Size = new Size(1200, 760);
        this.disciplineLayout.ResumeLayout(false);
        this.disciplineCard.ResumeLayout(false);
        this.csvLayout.ResumeLayout(false);
        this.csvCard.ResumeLayout(false);
        this.sampleLayout.ResumeLayout(false);
        this.sampleCard.ResumeLayout(false);
        this.quickActionsLayout.ResumeLayout(false);
        this.inputFrame.ResumeLayout(false);
        this.inputFrame.PerformLayout();
        this.searchLayout.ResumeLayout(false);
        this.searchCard.ResumeLayout(false);
        this.principlesLayout.ResumeLayout(false);
        this.principlesCard.ResumeLayout(false);
        this.heroCopyLayout.ResumeLayout(false);
        this.heroLayout.ResumeLayout(false);
        this.rootLayout.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
