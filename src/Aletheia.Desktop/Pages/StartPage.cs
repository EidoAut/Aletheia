#pragma warning disable SA1204 // Existing designer-backed helper ordering is kept stable.
#pragma warning disable SA1642 // Existing constructor summaries are kept stable.

using System.Globalization;
using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays the fund discovery start page. The visual hierarchy is declared in
/// <c>StartPage.Designer.cs</c>; this partial contains provider and interaction logic.
/// </summary>
internal sealed partial class StartPage : UserControl
{
    private Func<string, Task> searchFunds = _ => Task.CompletedTask;
    private Func<FundSearchResultSummary, Task> loadFund = _ => Task.CompletedTask;

    /// <summary>
    /// Initializes a designer-safe instance of the start page.
    /// </summary>
    public StartPage()
    {
        this.InitializeComponent();
        this.ConfigureResultsGrid();
        this.searchBox.KeyDown += this.SearchBoxKeyDown;
        this.searchButton.Click += async (_, _) => await this.SearchAsync().ConfigureAwait(true);
        this.loadSelectedButton.Click += async (_, _) => await this.LoadSelectedAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Initializes a new runtime instance of the start page.
    /// </summary>
    /// <param name="searchFunds">The provider search action.</param>
    /// <param name="loadFund">The selected fund load action.</param>
    /// <param name="loadSample">The sample-load action.</param>
    /// <param name="openCsv">The CSV-open action.</param>
    public StartPage(
        Func<string, Task> searchFunds,
        Func<FundSearchResultSummary, Task> loadFund,
        EventHandler loadSample,
        EventHandler openCsv)
        : this()
    {
        this.searchFunds = searchFunds ?? throw new ArgumentNullException(nameof(searchFunds));
        this.loadFund = loadFund ?? throw new ArgumentNullException(nameof(loadFund));
        ArgumentNullException.ThrowIfNull(loadSample);
        ArgumentNullException.ThrowIfNull(openCsv);
        this.loadSampleQuickButton.Click += loadSample;
        this.openCsvQuickButton.Click += openCsv;
    }

    private DataGridView ResultsGrid => this.resultsCard.Grid;

    private FundSearchResultSummary? SelectedResult
    {
        get
        {
            if (this.ResultsGrid.SelectedRows.Count == 0)
            {
                return null;
            }

            return this.ResultsGrid.SelectedRows[0].Tag as FundSearchResultSummary;
        }
    }

    /// <summary>
    /// Updates the visual search state.
    /// </summary>
    /// <param name="message">The status message.</param>
    /// <param name="isBusy">Whether search is running.</param>
    public void SetSearchState(string message, bool isBusy)
    {
        this.stateLabel.Text = message;
        this.stateLabel.ForeColor = isBusy ? ThemePalette.Accent : ThemePalette.MutedText;
        this.searchButton.Enabled = !isBusy;
        this.searchBox.Enabled = !isBusy;
        this.loadSelectedButton.Enabled = !isBusy && this.SelectedResult is not null;
    }

    /// <summary>
    /// Replaces displayed fund search results.
    /// </summary>
    /// <param name="funds">The discovered funds.</param>
    public void SetSearchResults(IReadOnlyList<FundSearchResultSummary> funds)
    {
        this.searchButton.Enabled = true;
        this.searchBox.Enabled = true;
        this.ResultsGrid.SuspendLayout();
        try
        {
            this.ResultsGrid.Rows.Clear();
            foreach (var fund in funds)
            {
                var rowIndex = this.ResultsGrid.Rows.Add(
                    fund.FundName,
                    fund.Isin ?? "n/a",
                    fund.ManagementCompany ?? "n/a",
                    FormatDate(fund.LatestAvailableObservation),
                    fund.ProviderDisplayName);
                this.ResultsGrid.Rows[rowIndex].Tag = fund;
            }

            if (this.ResultsGrid.Rows.Count > 0)
            {
                this.ResultsGrid.ClearSelection();
                this.ResultsGrid.CurrentCell = this.ResultsGrid.Rows[0].Cells[0];
                this.ResultsGrid.Rows[0].Selected = true;
            }
        }
        finally
        {
            this.ResultsGrid.ResumeLayout();
        }

        this.stateLabel.Text = funds.Count == 0
            ? "No funds matched the current query."
            : $"{funds.Count.ToString(CultureInfo.InvariantCulture)} fund(s) available. Select one to begin the analysis.";
        this.stateLabel.ForeColor = funds.Count == 0 ? ThemePalette.Warning : ThemePalette.Positive;
        this.loadSelectedButton.Enabled = this.SelectedResult is not null;
    }

    private void ConfigureResultsGrid()
    {
        this.ResultsGrid.Columns.Clear();
        this.ResultsGrid.Columns.Add("FundName", "Fund");
        this.ResultsGrid.Columns.Add("Isin", "ISIN");
        this.ResultsGrid.Columns.Add("Manager", "Management company");
        this.ResultsGrid.Columns.Add("Latest", "Latest NAV");
        this.ResultsGrid.Columns.Add("Provider", "Provider");
        this.ResultsGrid.Columns["FundName"]!.FillWeight = 220;
        this.ResultsGrid.Columns["Isin"]!.FillWeight = 82;
        this.ResultsGrid.Columns["Manager"]!.FillWeight = 132;
        this.ResultsGrid.Columns["Latest"]!.FillWeight = 76;
        this.ResultsGrid.Columns["Provider"]!.FillWeight = 84;
        this.ResultsGrid.SelectionChanged += (_, _) => this.loadSelectedButton.Enabled = this.SelectedResult is not null;
        this.ResultsGrid.CellDoubleClick += async (_, eventArgs) =>
        {
            if (eventArgs.RowIndex >= 0)
            {
                await this.LoadSelectedAsync().ConfigureAwait(true);
            }
        };
    }

    private async void SearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        await this.SearchAsync().ConfigureAwait(true);
    }

    private async Task SearchAsync()
    {
        var query = this.searchBox.Text.Trim();
        if (query.Length == 0)
        {
            this.SetSearchResults(Array.Empty<FundSearchResultSummary>());
            this.SetSearchState("Enter a fund name, ISIN or management company.", false);
            return;
        }

        await this.searchFunds(query).ConfigureAwait(true);
    }

    private async Task LoadSelectedAsync()
    {
        var selected = this.SelectedResult;
        if (selected is null)
        {
            return;
        }

        await this.loadFund(selected).ConfigureAwait(true);
    }

    private static string FormatDate(DateOnly? value)
    {
        return value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "n/a";
    }
}
