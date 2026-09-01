#pragma warning disable SA1204 // Existing shell helper ordering is kept stable.

using System.Globalization;
using System.Text;
using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;
using Aletheia.Desktop.Pages;
using Aletheia.Validation;

namespace Aletheia.Desktop;

/// <summary>
/// Hosts the Aletheia WinForms analytical shell. The static visual hierarchy lives in
/// <c>MainForm.Designer.cs</c>; this partial contains navigation and application behavior only.
/// </summary>
internal sealed partial class MainForm : Form
{
    private readonly AletheiaApplicationService application = new(CreateApplicationOptions());
    private readonly DesktopLog log = new();
    private readonly DesktopSettings settings = DesktopSettings.Load();
    private readonly Dictionary<PageKey, WorkspacePageBase> pages;
    private readonly Dictionary<PageKey, NavigationButton> navigationButtons = [];
    private CancellationTokenSource? operationCancellation;
    private FundWorkspace? workspace;
    private StartPage? startPage;
    private PageKey selectedPage = PageKey.Overview;
    private AppViewState state = AppViewState.NoDataset;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm()
    {
        this.InitializeComponent();
        this.versionSidebarLabel.Text = $"v{AletheiaRelease.ProductVersion}";
        this.versionStatusLabel.Text = $"Aletheia {AletheiaRelease.ProductVersion} · EIDO MARKET LAB";
        this.Text = $"Aletheia {AletheiaRelease.ProductVersion} - EIDO Market Simulator";
        this.pages = new Dictionary<PageKey, WorkspacePageBase>
        {
            [PageKey.Overview] = new OverviewPage(),
            [PageKey.Performance] = new PerformancePage(),
            [PageKey.Risk] = new RiskPage(),
            [PageKey.Simulation] = new SimulationPage(this.RunInvestmentSimulationAsync),
            [PageKey.Dynamics] = new DynamicsPage(),
            [PageKey.Spectral] = new SpectralPage(),
            [PageKey.Analogues] = new AnaloguesPage(),
            [PageKey.Forecast] = new ForecastPage(),
            [PageKey.MarketTiming] = new MarketTimingPage(),
            [PageKey.ModelArena] = new ModelArenaPage(),
            [PageKey.Validation] = new ValidationPage(),
            [PageKey.Predictions] = new PredictionsPage(this.application),
            [PageKey.Lab] = new LabPage(),
        };

        this.ConfigureNavigation();
        this.ConfigureShellActions();
        this.ConfigureHorizonSelector();
        this.SetState(AppViewState.NoDataset, "SYSTEM_READY · Load a fund dataset.");
        this.ShowStartPage();
        this.log.Info("DesktopStarted", "Designer-backed desktop shell started.");
    }

    /// <inheritdoc />
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        this.CancelCurrentOperation();
        base.OnFormClosing(e);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.CancelCurrentOperation();
            foreach (var page in this.pages.Values)
            {
                page.Dispose();
            }

            this.startPage?.Dispose();
            this.components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ConfigureNavigation()
    {
        this.ConfigureNavigationButton(PageKey.Overview, this.overviewNavigationButton);
        this.ConfigureNavigationButton(PageKey.Performance, this.performanceNavigationButton);
        this.ConfigureNavigationButton(PageKey.Risk, this.riskNavigationButton);
        this.ConfigureNavigationButton(PageKey.Simulation, this.simulationNavigationButton);
        this.ConfigureNavigationButton(PageKey.Dynamics, this.dynamicsNavigationButton);
        this.ConfigureNavigationButton(PageKey.Spectral, this.spectralNavigationButton);
        this.ConfigureNavigationButton(PageKey.Analogues, this.analoguesNavigationButton);
        this.ConfigureNavigationButton(PageKey.Forecast, this.forecastNavigationButton);
        this.ConfigureNavigationButton(PageKey.MarketTiming, this.marketTimingNavigationButton);
        this.ConfigureNavigationButton(PageKey.ModelArena, this.modelArenaNavigationButton);
        this.ConfigureNavigationButton(PageKey.Validation, this.validationNavigationButton);
        this.ConfigureNavigationButton(PageKey.Predictions, this.predictionsNavigationButton);
        this.ConfigureNavigationButton(PageKey.Lab, this.labNavigationButton);
    }

    private void ConfigureNavigationButton(PageKey key, NavigationButton button)
    {
        button.Selected = key == this.selectedPage;
        button.Click += async (_, _) => await this.ShowPageAsync(key).ConfigureAwait(true);
        this.navigationButtons[key] = button;
    }

    private void ConfigureShellActions()
    {
        this.changeFundButton.Click += (_, _) => this.ShowStartPage();
        this.loadSampleButton.Click += async (_, _) => await this.LoadSampleAsync().ConfigureAwait(true);
        this.openCsvButton.Click += async (_, _) => await this.OpenCsvAsync().ConfigureAwait(true);
        this.generateReportButton.Click += async (_, _) => await this.GenerateReportAsync().ConfigureAwait(true);
        this.runArenaButton.Click += async (_, _) => await this.RunArenaAsync().ConfigureAwait(true);
        this.cancelButton.Click += (_, _) => this.operationCancellation?.Cancel();
        this.toolTip.SetToolTip(this.changeFundButton, "Open fund discovery");
        this.toolTip.SetToolTip(this.loadSampleButton, "Load the bundled demonstration dataset");
        this.toolTip.SetToolTip(this.openCsvButton, "Open CSV (Ctrl+O)");
        this.toolTip.SetToolTip(this.generateReportButton, "Generate a Markdown research report");
        this.toolTip.SetToolTip(this.cancelButton, "Cancel the current operation");
        this.KeyDown += this.MainFormKeyDown;
    }

    private void ConfigureHorizonSelector()
    {
        var value = Math.Clamp(
            this.settings.ArenaHorizonDays,
            decimal.ToInt32(this.horizonDaysInput.Minimum),
            decimal.ToInt32(this.horizonDaysInput.Maximum));
        this.horizonDaysInput.Value = value;
        this.UpdateArenaHorizonText();
        this.horizonDaysInput.ValueChanged += (_, _) =>
        {
            this.settings.ArenaHorizonDays = decimal.ToInt32(this.horizonDaysInput.Value);
            this.settings.Save();
            this.UpdateArenaHorizonText();
        };
        this.toolTip.SetToolTip(this.horizonSettingLabel, "Calendar-day window used for the primary validation run.");
        this.toolTip.SetToolTip(this.horizonDaysInput, "Calendar-day window used for Model Arena validation.");
        this.toolTip.SetToolTip(this.horizonDaysUnitLabel, "Calendar days.");
    }

    private async Task LoadSampleAsync()
    {
        await this.RunWorkspaceOperationAsync(
            "Loading sample dataset...",
            "SampleLoaded",
            async (progress, token) => await this.application.LoadSampleWorkspaceAsync(token, progress).ConfigureAwait(false),
            null).ConfigureAwait(true);
    }

    private async Task OpenCsvAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Open Fund Dataset",
        };
        if (this.settings.RecentFiles.Count > 0)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(this.settings.RecentFiles[0]);
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await this.RunWorkspaceOperationAsync(
            "Loading CSV dataset...",
            "CsvLoaded",
            async (progress, token) => await this.application.LoadCsvWorkspaceAsync(dialog.FileName, token, progress).ConfigureAwait(false),
            dialog.FileName).ConfigureAwait(true);
    }

    private async Task RunWorkspaceOperationAsync(
        string status,
        string logEvent,
        Func<IProgress<string>, CancellationToken, Task<FundWorkspace>> operation,
        string? recentPath)
    {
        this.operationCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        this.operationCancellation = cancellation;
        this.SetState(AppViewState.Loading, status);
        this.startPage?.SetSearchState(status, true);
        var progress = new Progress<string>(this.ReportAnalysisProgress);
        try
        {
            var result = await Task.Run(async () => await operation(progress, cancellation.Token).ConfigureAwait(false), cancellation.Token).ConfigureAwait(true);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!this.IsCurrentOperation(cancellation))
            {
                return;
            }

            this.workspace = result;
            if (!string.IsNullOrWhiteSpace(recentPath))
            {
                this.settings.AddRecentFile(recentPath);
                this.settings.Save();
            }

            this.ApplyWorkspace();
            this.log.Info(logEvent, result.Analysis.Dataset.FundName);
            this.SetState(AppViewState.AnalysisAvailable, "Analysis available.");
            await this.ShowPageAsync(PageKey.Overview).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            this.log.Info("OperationCancelled", status);
            if (this.IsCurrentOperation(cancellation))
            {
                this.startPage?.SetSearchState("Operation cancelled.", false);
                if (this.workspace is null)
                {
                    this.SetState(AppViewState.NoDataset, "Operation cancelled.");
                    this.ShowStartPage();
                }
                else
                {
                    this.SetState(this.ResolveAvailableState(), "Operation cancelled.");
                }
            }
        }
        catch (Exception exception)
        {
            this.log.Error("UnexpectedError", exception);
            if (this.IsCurrentOperation(cancellation))
            {
                this.startPage?.SetSearchState(exception.Message, false);
                this.SetState(
                    this.workspace is null ? AppViewState.Error : this.ResolveAvailableState(),
                    $"Dataset load failed: {exception.Message}");
                MessageBox.Show(this, exception.Message, "Aletheia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (ReferenceEquals(this.operationCancellation, cancellation))
            {
                this.operationCancellation = null;
            }
        }
    }

    private async Task<InvestmentSimulationSummary?> RunInvestmentSimulationAsync(InvestmentSimulationRequest request)
    {
        if (this.workspace is null)
        {
            return null;
        }

        this.operationCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        this.operationCancellation = cancellation;
        var activeWorkspace = this.workspace;
        this.SetState(AppViewState.Analyzing, "Running investment scenario...");
        this.log.Info("SimulationStarted", activeWorkspace.Analysis.Dataset.FundName);
        try
        {
            var result = await Task.Run(
                () => this.application.RunInvestmentSimulation(activeWorkspace, request, cancellation.Token),
                cancellation.Token).ConfigureAwait(true);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!this.IsCurrentOperation(cancellation) || !ReferenceEquals(this.workspace, activeWorkspace))
            {
                return null;
            }

            this.log.Info("SimulationCompleted", $"{request.PathCount} paths over {request.HorizonYears} years.");
            this.SetState(this.ResolveAvailableState(), "Investment scenario available.");
            return result;
        }
        catch (OperationCanceledException)
        {
            this.log.Info("OperationCancelled", "Investment simulation cancelled.");
            if (this.IsCurrentOperation(cancellation))
            {
                this.SetState(this.ResolveAvailableState(), "Investment simulation cancelled.");
            }

            return null;
        }
        catch (Exception exception)
        {
            this.log.Error("UnexpectedError", exception);
            if (this.IsCurrentOperation(cancellation))
            {
                this.SetState(this.ResolveAvailableState(), $"Investment simulation failed: {exception.Message}");
                MessageBox.Show(this, exception.Message, "Aletheia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }
        finally
        {
            if (ReferenceEquals(this.operationCancellation, cancellation))
            {
                this.operationCancellation = null;
            }
        }
    }

    private async Task RunArenaAsync()
    {
        if (this.workspace is null)
        {
            return;
        }

        this.operationCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        this.operationCancellation = cancellation;
        var activeWorkspace = this.workspace;
        var selectedHorizon = this.CreateSelectedArenaHorizon();
        this.SetState(AppViewState.ArenaRunning, $"Running Model Arena for {selectedHorizon}...");
        this.log.Info("ArenaStarted", $"{activeWorkspace.Analysis.Dataset.FundName} · {selectedHorizon}");
        try
        {
            var arenas = await Task.Run(
                async () => await this.application.RunModelArenasAsync(activeWorkspace, selectedHorizon, cancellation.Token).ConfigureAwait(false),
                cancellation.Token).ConfigureAwait(true);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!this.IsCurrentOperation(cancellation) || !ReferenceEquals(this.workspace, activeWorkspace))
            {
                return;
            }

            var workspaceWithArena = activeWorkspace.WithArenas(arenas, selectedHorizon);
            var preliminaryReport = this.application.BuildResearchReport(workspaceWithArena);
            var preliminaryWorkspace = workspaceWithArena with
            {
                Analysis = workspaceWithArena.Analysis with
                {
                    ResearchReport = preliminaryReport,
                },
            };
            var timing = this.application.BuildMarketTimingAssessment(preliminaryWorkspace);
            var finalWorkspace = preliminaryWorkspace with
            {
                Analysis = preliminaryWorkspace.Analysis with
                {
                    MarketTiming = timing,
                },
            };
            var finalReport = this.application.BuildResearchReport(finalWorkspace);
            this.workspace = finalWorkspace with
            {
                Analysis = finalWorkspace.Analysis with
                {
                    ResearchReport = finalReport,
                },
            };
            this.ApplyWorkspace();
            this.log.Info("ArenaCompleted", $"{arenas.Count} horizon(s), {workspaceWithArena.Arena?.Models.Count ?? 0} primary models.");
            this.SetState(AppViewState.ArenaAvailable, $"Model Arena available for {selectedHorizon}.");
            await this.ShowPageAsync(PageKey.ModelArena).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            this.log.Info("OperationCancelled", "Model Arena cancelled.");
            if (this.IsCurrentOperation(cancellation))
            {
                this.SetState(this.ResolveAvailableState(), "Model Arena cancelled.");
            }
        }
        catch (Exception exception)
        {
            this.log.Error("UnexpectedError", exception);
            if (this.IsCurrentOperation(cancellation))
            {
                this.SetState(this.ResolveAvailableState(), $"Model Arena failed: {exception.Message}");
                MessageBox.Show(this, exception.Message, "Aletheia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (ReferenceEquals(this.operationCancellation, cancellation))
            {
                this.operationCancellation = null;
            }
        }
    }

    private async Task GenerateReportAsync()
    {
        if (this.workspace is null)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = "md",
            FileName = BuildDefaultReportFileName(this.workspace.Analysis.Dataset),
            Filter = "Markdown report (*.md)|*.md|Text report (*.txt)|*.txt|All files (*.*)|*.*",
            OverwritePrompt = true,
            Title = "Generate Aletheia Report",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var outputPath = dialog.FileName;
        this.operationCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        this.operationCancellation = cancellation;
        var activeWorkspace = this.workspace;
        this.SetState(AppViewState.Analyzing, "Generating research report...");
        this.log.Info("ReportStarted", activeWorkspace.Analysis.Dataset.FundName);
        try
        {
            var result = await Task.Run(
                () =>
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    var preliminaryReport = activeWorkspace.Analysis.ResearchReport ??
                        this.application.BuildResearchReport(activeWorkspace);
                    var workspaceWithReport = activeWorkspace with
                    {
                        Analysis = activeWorkspace.Analysis with
                        {
                            ResearchReport = preliminaryReport,
                        },
                    };
                    var timing = workspaceWithReport.Analysis.MarketTiming ??
                        this.application.BuildMarketTimingAssessment(workspaceWithReport);
                    var finalWorkspace = workspaceWithReport with
                    {
                        Analysis = workspaceWithReport.Analysis with
                        {
                            MarketTiming = timing,
                        },
                    };
                    var finalReport = this.application.BuildResearchReport(finalWorkspace);
                    finalWorkspace = finalWorkspace with
                    {
                        Analysis = finalWorkspace.Analysis with
                        {
                            ResearchReport = finalReport,
                        },
                    };
                    var markdown = BuildMarkdownReport(finalWorkspace, finalReport, timing);
                    File.WriteAllText(outputPath, markdown, Encoding.UTF8);
                    return finalWorkspace;
                },
                cancellation.Token).ConfigureAwait(true);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!this.IsCurrentOperation(cancellation) || !ReferenceEquals(this.workspace, activeWorkspace))
            {
                return;
            }

            this.workspace = result;
            this.ApplyWorkspace();
            this.log.Info("ReportGenerated", outputPath);
            this.SetState(this.ResolveAvailableState(), $"Report saved: {Path.GetFileName(outputPath)}");
            MessageBox.Show(
                this,
                $"Report saved to:{Environment.NewLine}{outputPath}",
                "Aletheia report",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            this.log.Info("OperationCancelled", "Report generation cancelled.");
            if (this.IsCurrentOperation(cancellation))
            {
                this.SetState(this.ResolveAvailableState(), "Report generation cancelled.");
            }
        }
        catch (Exception exception)
        {
            this.log.Error("UnexpectedError", exception);
            if (this.IsCurrentOperation(cancellation))
            {
                this.SetState(this.ResolveAvailableState(), $"Report generation failed: {exception.Message}");
                MessageBox.Show(this, exception.Message, "Aletheia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (ReferenceEquals(this.operationCancellation, cancellation))
            {
                this.operationCancellation = null;
            }
        }
    }

    private void ApplyWorkspace()
    {
        if (this.workspace is null)
        {
            return;
        }

        var dataset = this.workspace.Analysis.Dataset;
        this.Text = $"Aletheia {AletheiaRelease.ProductVersion} - {dataset.FundName}";
        this.datasetNameLabel.Text = dataset.FundName;
        this.datasetMetaLabel.Text = BuildDatasetHeaderMeta(dataset);
        this.RefreshVisiblePage();
    }

    private void ShowStartPage()
    {
        foreach (var button in this.navigationButtons.Values)
        {
            button.Selected = false;
        }

        this.UpdatePageHeader("Market Simulator", "Search official funds, load CSV data and build investor guidance");
        var previousStartPage = this.startPage;
        this.contentPanel.SuspendLayout();
        this.contentPanel.Controls.Clear();
        previousStartPage?.Dispose();
        this.startPage = new StartPage(
            this.SearchFundsAsync,
            this.LoadDiscoveredFundAsync,
            async (_, _) => await this.LoadSampleAsync().ConfigureAwait(true),
            async (_, _) => await this.OpenCsvAsync().ConfigureAwait(true));
        this.contentPanel.Controls.Add(this.startPage);
        this.contentPanel.ResumeLayout(true);
        this.SetState(this.workspace is null ? AppViewState.NoDataset : this.ResolveAvailableState(), "Ready.");
    }

    private async Task ShowPageAsync(PageKey key)
    {
        if (this.workspace is null)
        {
            this.ShowStartPage();
            return;
        }

        this.selectedPage = key;
        var previousStartPage = this.startPage;
        this.startPage = null;
        foreach (var pair in this.navigationButtons)
        {
            pair.Value.Selected = pair.Key == key;
        }

        var page = this.pages[key];
        this.UpdatePageHeader(page.PageTitle, GetPageSubtitle(key));
        this.contentPanel.SuspendLayout();
        this.contentPanel.Controls.Clear();
        previousStartPage?.Dispose();
        this.contentPanel.Controls.Add(page);
        page.BringToFront();
        this.contentPanel.ResumeLayout(true);
        page.SetWorkspace(this.workspace);
        page.SetArena(this.workspace.Arena);
        page.PerformLayout();
        page.Invalidate(true);
        if (page is PredictionsPage predictionsPage)
        {
            try
            {
                this.SetState(this.state, "Opening prediction ledger...");
                await predictionsPage.RefreshPredictionsAsync().ConfigureAwait(true);
                this.SetState(this.state, "Prediction ledger loaded.");
                this.log.Info("LedgerOpened", "Prediction ledger opened.");
            }
            catch (Exception exception)
            {
                this.log.Error("UnexpectedError", exception);
                this.SetState(this.ResolveAvailableState(), $"Prediction ledger failed: {exception.Message}");
                MessageBox.Show(this, exception.Message, "Aletheia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void SetState(AppViewState nextState, string message)
    {
        this.state = nextState;
        this.statusLabel.Text = message;
        var busy = nextState is AppViewState.Loading or AppViewState.Searching or AppViewState.Analyzing or AppViewState.ArenaRunning;
        this.operationProgress.Active = busy;
        if (!busy)
        {
            this.operationProgress.ProgressFraction = null;
        }

        this.cancelButton.Visible = busy;
        this.cancelButton.Enabled = busy;
        this.runArenaButton.Visible = !busy;
        this.changeFundButton.Enabled = !busy;
        this.loadSampleButton.Enabled = !busy;
        this.openCsvButton.Enabled = !busy;
        this.generateReportButton.Enabled = !busy && this.workspace is not null;
        this.runArenaButton.Enabled = !busy && this.workspace is not null;
        this.contentPanel.Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        this.statusIndicator.IndicatorColor = nextState switch
        {
            AppViewState.Error => ThemePalette.Negative,
            AppViewState.Loading or AppViewState.Searching or AppViewState.Analyzing or AppViewState.ArenaRunning => ThemePalette.Accent,
            AppViewState.AnalysisAvailable or AppViewState.ArenaAvailable => ThemePalette.Positive,
            _ => ThemePalette.Warning,
        };
        this.statusLabel.ForeColor = nextState == AppViewState.Error ? ThemePalette.Negative : ThemePalette.MutedText;
        foreach (var button in this.navigationButtons.Values)
        {
            button.Enabled = !busy && this.workspace is not null;
        }
    }

    private void ReportAnalysisProgress(string stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
        {
            return;
        }

        var stages = new[]
        {
            "Loading dataset",
            "Preparing time series",
            "Computing risk metrics",
            "Computing dynamic state",
            "Computing spectral diagnostics",
            "Running current forecasts",
            "Preparing report",
            "Validating market timing",
            "Analysis complete",
        };
        var index = Array.FindIndex(stages, item => string.Equals(item, stage, StringComparison.Ordinal));
        this.operationProgress.ProgressFraction = index < 0
            ? null
            : (index + 1d) / stages.Length;
        this.statusLabel.Text = stage;
        this.startPage?.SetSearchState(stage, true);
    }

    private void RefreshVisiblePage()
    {
        if (this.workspace is null || !this.pages.TryGetValue(this.selectedPage, out var page) || !this.contentPanel.Controls.Contains(page))
        {
            return;
        }

        page.SetWorkspace(this.workspace);
        page.SetArena(this.workspace.Arena);
        page.PerformLayout();
        page.Invalidate(true);
    }

    private async Task SearchFundsAsync(string query)
    {
        this.operationCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        this.operationCancellation = cancellation;
        this.SetState(AppViewState.Searching, "Searching funds...");
        this.startPage?.SetSearchState("Searching official fund catalogue...", true);
        try
        {
            var results = await Task.Run(
                async () => await this.application.SearchFundsAsync(query, 50, cancellation.Token).ConfigureAwait(false),
                cancellation.Token).ConfigureAwait(true);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!this.IsCurrentOperation(cancellation))
            {
                return;
            }

            this.startPage?.SetSearchResults(results);
            this.log.Info("FundsSearched", $"{query}: {results.Count} result(s).");
            this.SetState(this.ResolveAvailableState(), $"{results.Count} fund(s) found.");
        }
        catch (OperationCanceledException)
        {
            if (this.IsCurrentOperation(cancellation))
            {
                this.startPage?.SetSearchState("Search cancelled.", false);
                this.SetState(this.ResolveAvailableState(), "Search cancelled.");
            }
        }
        catch (Exception exception)
        {
            this.log.Error("UnexpectedError", exception);
            if (this.IsCurrentOperation(cancellation))
            {
                this.startPage?.SetSearchState(exception.Message, false);
                this.SetState(
                    this.workspace is null ? AppViewState.Error : this.ResolveAvailableState(),
                    $"Fund search failed: {exception.Message}");
                MessageBox.Show(this, exception.Message, "Aletheia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (ReferenceEquals(this.operationCancellation, cancellation))
            {
                this.operationCancellation = null;
            }
        }
    }

    private async Task LoadDiscoveredFundAsync(FundSearchResultSummary fund)
    {
        await this.RunWorkspaceOperationAsync(
            $"Loading {fund.FundName} from {fund.ProviderDisplayName}...",
            "ProviderFundLoaded",
            async (progress, token) => await this.application.LoadProviderWorkspaceAsync(
                fund.ProviderId,
                fund.FundIdentifier,
                null,
                null,
                token,
                progress).ConfigureAwait(false),
            null).ConfigureAwait(true);
    }

    private void CancelCurrentOperation()
    {
        var cancellation = this.operationCancellation;
        this.operationCancellation = null;
        cancellation?.Cancel();
    }

    private bool IsCurrentOperation(CancellationTokenSource cancellation)
    {
        return ReferenceEquals(this.operationCancellation, cancellation);
    }

    private AppViewState ResolveAvailableState()
    {
        if (this.workspace is null)
        {
            return AppViewState.NoDataset;
        }

        return this.workspace.Arena is null ? AppViewState.AnalysisAvailable : AppViewState.ArenaAvailable;
    }

    private void UpdatePageHeader(string title, string subtitle)
    {
        this.pageTitleLabel.Text = title;
        this.pageSubtitleLabel.Text = subtitle;
        this.toolTip.SetToolTip(this.pageTitleLabel, title);
        this.toolTip.SetToolTip(this.pageSubtitleLabel, subtitle);
    }

    private static string GetPageSubtitle(PageKey key)
    {
        return key switch
        {
            PageKey.Overview => "Investor guidance, fund health and data provenance",
            PageKey.Performance => "Realized returns across time and rolling horizons",
            PageKey.Risk => "Drawdown, volatility and return-distribution diagnostics",
            PageKey.Simulation => "Periodic-investment reference scenarios",
            PageKey.Dynamics => "Current state-space position and regime features",
            PageKey.Spectral => "Frequency-domain structure and persistence",
            PageKey.Analogues => "Historical states most similar to the current fund",
            PageKey.Forecast => "Configurable prediction windows and uncertainty",
            PageKey.MarketTiming => "Buy, hold or reduce guidance from validated timing evidence",
            PageKey.ModelArena => "Walk-forward model comparison on common support",
            PageKey.Validation => "Calibration, errors and scientific diagnostics",
            PageKey.Predictions => "Immutable forecast ledger and realized evaluations",
            PageKey.Lab => "Methods, assumptions and research documentation",
            _ => "Quantitative fund research",
        };
    }

    private ForecastHorizon CreateSelectedArenaHorizon()
    {
        return ForecastHorizon.CalendarDays(decimal.ToInt32(this.horizonDaysInput.Value));
    }

    private void UpdateArenaHorizonText()
    {
        var days = decimal.ToInt32(this.horizonDaysInput.Value);
        this.runArenaButton.Text = $"RUN {days}D";
        this.runArenaButton.AccessibleName = $"Run Model Arena for {days} calendar days";
        this.toolTip.SetToolTip(this.runArenaButton, $"Run walk-forward Model Arena with a {days}-calendar-day primary horizon.");
    }

    private static AletheiaApplicationOptions CreateApplicationOptions()
    {
        var applicationDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aletheia");
        return new AletheiaApplicationOptions
        {
            LedgerPath = Path.Combine(applicationDirectory, "aletheia.db"),
        };
    }

    private static string BuildDatasetHeaderMeta(DatasetSummary dataset)
    {
        var provider = dataset.Provenance?.ProviderDisplayName ?? dataset.Provider ?? "Unknown source";
        var identifier = dataset.Provenance?.Isin ?? dataset.Identifier.Value;
        var currency = string.IsNullOrWhiteSpace(dataset.Currency) ? "n/a" : dataset.Currency;
        return $"{identifier} · {currency} · {provider} · {dataset.ObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture)} obs · {dataset.ObservationFrequency}";
    }

    private static string BuildDatasetCaption(DatasetSummary dataset)
    {
        var provider = dataset.Provenance?.ProviderDisplayName ?? dataset.Provider ?? "Unknown source";
        var identifier = dataset.Provenance?.Isin ?? dataset.Identifier.Value;
        return $"{Shorten(dataset.FundName, 36)} · {identifier} · {provider} · {dataset.StartDate:yyyy-MM-dd} → {dataset.EndDate:yyyy-MM-dd}";
    }

    private static string BuildDefaultReportFileName(DatasetSummary dataset)
    {
        var name = SanitizeFileName(dataset.FundName);
        return $"{name}-aletheia-report-{DateTime.Now:yyyyMMdd-HHmm}.md";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalid.Contains(character) ? '-' : character);
        }

        var result = builder.ToString().Trim(' ', '.', '-');
        return string.IsNullOrWhiteSpace(result) ? "aletheia-fund" : Shorten(result, 80);
    }

    private static string BuildMarkdownReport(
        FundWorkspace workspace,
        FundResearchReport report,
        MarketTimingAssessment? timing)
    {
        var builder = new StringBuilder();
        var dataset = report.Dataset;
        var sourceObservationCount = dataset.SourceObservationCount == 0
            ? dataset.ObservationCount
            : dataset.SourceObservationCount;
        var sourceStartDate = dataset.SourceStartDate ?? dataset.StartDate;
        var sourceEndDate = dataset.SourceEndDate ?? dataset.EndDate;
        builder.AppendLine("# Aletheia Fund Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {report.DataFreshness.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm zzz}");
        builder.AppendLine($"Fund: {EscapeMarkdown(dataset.FundName)}");
        builder.AppendLine($"Scientific version: `{EscapeMarkdown(report.ScientificVersion)}`");
        builder.AppendLine();
        builder.AppendLine("## Dataset");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Identifier | {EscapeMarkdown(dataset.Identifier.Value)} |");
        builder.AppendLine($"| Provider | {EscapeMarkdown(dataset.Provenance?.ProviderDisplayName ?? dataset.Provider ?? "Unknown")} |");
        builder.AppendLine($"| Currency | {EscapeMarkdown(string.IsNullOrWhiteSpace(dataset.Currency) ? "n/a" : dataset.Currency)} |");
        builder.AppendLine($"| Source period | {sourceStartDate:yyyy-MM-dd} to {sourceEndDate:yyyy-MM-dd} |");
        builder.AppendLine($"| Effective period | {dataset.StartDate:yyyy-MM-dd} to {dataset.EndDate:yyyy-MM-dd} |");
        builder.AppendLine($"| Source observations | {sourceObservationCount.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Effective observations | {dataset.ObservationCount.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Synthetic/carry-forward rows excluded | {dataset.SyntheticObservationCount.ToString(CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Effective frequency | {dataset.ObservationFrequency} |");
        builder.AppendLine($"| Effective policy | {EscapeMarkdown(dataset.EffectiveObservationPolicy)} |");
        builder.AppendLine($"| Latest effective observation | {report.DataFreshness.LastEffectiveObservationDate:yyyy-MM-dd} |");
        builder.AppendLine($"| Data age | {report.DataFreshness.DataAgeDays.ToString(CultureInfo.InvariantCulture)} calendar days |");
        builder.AppendLine($"| Freshness status | {report.DataFreshness.Status} |");
        builder.AppendLine($"| Dataset fingerprint | `{EscapeMarkdown(Shorten(dataset.DatasetFingerprint, 24))}` |");
        builder.AppendLine();

        builder.AppendLine("## Research Overview");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Fund quality | {FormatNumber(report.FundScore.Score)} / 10 ({report.FundScore.Confidence}) |");
        builder.AppendLine($"| Strategic attractiveness as of {report.Actionability.EffectiveDate:yyyy-MM-dd} | {FormatNumber(report.CurrentAttractiveness.Score)} / 10 - {report.CurrentAttractiveness.Category} ({report.CurrentAttractiveness.Confidence}) |");
        builder.AppendLine($"| Strategic decision signal | {report.DecisionSignal.DisplayLabel} ({report.DecisionSignal.Qualification}, {report.DecisionSignal.Confidence}) |");
        builder.AppendLine($"| Actionability status | {EscapeMarkdown(report.Actionability.Status)} ({report.Actionability.Level}, {report.Actionability.Confidence}) |");
        builder.AppendLine($"| Data freshness | {report.DataFreshness.Status} ({report.DataFreshness.DataAgeDays.ToString(CultureInfo.InvariantCulture)} calendar days) |");
        builder.AppendLine($"| Latest effective regime | {EscapeMarkdown(report.CurrentRegimeLabel ?? "N/A")} |");
        builder.AppendLine($"| Regime probability | {FormatNullablePercent(report.CurrentRegimeProbability)} |");
        builder.AppendLine($"| CAGR | {FormatPercent(report.Performance.Cagr)} |");
        builder.AppendLine($"| Cumulative return | {FormatPercent(report.Performance.CumulativeReturn)} |");
        builder.AppendLine($"| Annualized volatility | {FormatPercent(report.Performance.AnnualizedVolatility)} |");
        builder.AppendLine($"| Maximum drawdown | {FormatPercent(report.Performance.MaximumDrawdown.MaximumDrawdown)} |");
        builder.AppendLine($"| Sharpe ratio | {FormatNumber(report.Performance.SharpeRatio)} |");
        builder.AppendLine($"| Ensemble ReliabilityIndex | {FormatNullablePercent(report.Ensemble?.Reliability)} |");
        builder.AppendLine($"| Ensemble disagreement | {FormatNullablePercent(report.Ensemble?.ModelDisagreement)} |");
        builder.AppendLine();

        AppendScore(builder, report.FundScore);
        AppendDecisionSignal(builder, report.DecisionSignal);
        AppendActionability(builder, report.Actionability);
        AppendMarketTiming(builder, timing);
        AppendForecasts(builder, report);
        AppendEnsembleAudit(builder, report);
        AppendStressScenarios(builder, report);
        AppendWarnings(builder, report, timing);
        AppendProvenance(builder, report, workspace);
        return builder.ToString();
    }

    private static void AppendScore(StringBuilder builder, FundScore score)
    {
        builder.AppendLine("## Fund Score");
        builder.AppendLine();
        builder.AppendLine("| Component | Score | Weight | Reason |");
        builder.AppendLine("| --- | ---: | ---: | --- |");
        foreach (var component in score.Components)
        {
            builder.AppendLine(
                $"| {EscapeMarkdown(component.Name)} | {FormatNumber(component.Score)} | {FormatPercent(component.Weight)} | {EscapeMarkdown(component.Reason)} |");
        }

        builder.AppendLine();
    }

    private static void AppendDecisionSignal(StringBuilder builder, DecisionSignal signal)
    {
        builder.AppendLine("## Decision Signal");
        builder.AppendLine();
        builder.AppendLine($"Signal: **{signal.DisplayLabel}**");
        builder.AppendLine($"Direction: {signal.Direction}");
        builder.AppendLine($"Qualification: {signal.Qualification}");
        builder.AppendLine($"Directional strength: {FormatPercent(signal.DirectionalStrength)}");
        builder.AppendLine($"Validation strength: {FormatPercent(signal.ValidationStrength)}");
        builder.AppendLine($"Legacy action: {signal.Action}");
        builder.AppendLine($"Confidence: {signal.Confidence}");
        if (signal.PrimaryHorizon is not null)
        {
            builder.AppendLine($"Primary horizon: {signal.PrimaryHorizon}");
        }

        AppendBullets(builder, "Reasons", signal.Reasons);
        AppendBullets(builder, "Evidence", signal.Evidence);
        AppendBullets(builder, "Counter-evidence", signal.CounterEvidence);
    }

    private static void AppendActionability(StringBuilder builder, ActionabilityAssessment actionability)
    {
        builder.AppendLine("## Actionability");
        builder.AppendLine();
        builder.AppendLine($"Status: **{EscapeMarkdown(actionability.Status)}**");
        builder.AppendLine($"Level: {actionability.Level}");
        builder.AppendLine($"Confidence: {actionability.Confidence}");
        builder.AppendLine($"Effective date: {actionability.EffectiveDate:yyyy-MM-dd}");
        AppendBullets(builder, "Actionability reasons", actionability.Reasons);
    }

    private static void AppendMarketTiming(StringBuilder builder, MarketTimingAssessment? timing)
    {
        builder.AppendLine("## Market Timing");
        builder.AppendLine();
        if (timing is null)
        {
            builder.AppendLine("No market-timing assessment is available.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"Current zone: **{timing.CurrentTimingZone}**");
        builder.AppendLine($"Timing signal: **{timing.Decision.DisplayLabel}** ({timing.Decision.Qualification}, {timing.Decision.Confidence})");
        builder.AppendLine($"Timing direction: {timing.Decision.Direction}");
        builder.AppendLine($"Signal strength: {FormatPercent(timing.Decision.Strength)}");
        builder.AppendLine($"Validation strength: {FormatPercent(timing.Decision.ValidationStrength)}");
        if (timing.Decision.PrimaryHorizon is not null)
        {
            builder.AppendLine($"Primary horizon: {timing.Decision.PrimaryHorizon}");
        }

        var primaryArena = FindPrimaryTimingArena(timing);
        if (primaryArena is not null)
        {
            builder.AppendLine($"OOD status: {primaryArena.OutOfDistribution.Level}");
            builder.AppendLine($"OOD robust distance: {FormatNumber(primaryArena.OutOfDistribution.RobustDistance)} / {FormatNumber(primaryArena.OutOfDistribution.Threshold)}");
        }

        builder.AppendLine($"Primary horizon reason: {EscapeMarkdown(timing.PrimaryHorizonSelectionReason)}");
        builder.AppendLine($"Training cutoff: {timing.TrainingCutoff:yyyy-MM-dd}");
        builder.AppendLine();
        builder.AppendLine(EscapeMarkdown(timing.Narrative.Summary));
        builder.AppendLine(EscapeMarkdown(timing.Narrative.DirectionExplanation));
        builder.AppendLine();
        AppendBullets(builder, "Timing decision reasons", timing.Decision.Reasons);
        builder.AppendLine("| Horizon | P up | P down | P neutral | Expected return | Barrier payoff | ReliabilityIndex | Evidence | Zone |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |");
        foreach (var horizon in timing.Horizons)
        {
            builder.AppendLine(
                $"| {horizon.Horizon} | {FormatPercent(horizon.ProbabilityUp)} | {FormatPercent(horizon.ProbabilityDown)} | {FormatPercent(horizon.ProbabilityNeutral)} | {FormatNullablePercent(horizon.ForecastExpectedReturn)} | {FormatPercent(horizon.ExpectedBarrierPayoff)} | {FormatPercent(horizon.ReliabilityIndex)} | {horizon.EvidenceStrength} | {horizon.Zone} |");
        }

        builder.AppendLine();
        builder.AppendLine("ReliabilityIndex is a validation-quality index; it is not a probability of a correct timing call.");
        builder.AppendLine();
    }

    private static void AppendForecasts(StringBuilder builder, FundResearchReport report)
    {
        builder.AppendLine("## Current Forecasts");
        builder.AppendLine();
        builder.AppendLine("| Model | Horizon | Status | Expected return | P positive |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: |");
        foreach (var run in report.Forecasts.Runs)
        {
            var expected = run.Distribution?.ExpectedReturnOrNull;
            var probabilityPositive = run.Distribution?.ProbabilityPositiveOrNull;
            builder.AppendLine(
                $"| {EscapeMarkdown(run.Model.Name)} | {run.RequestedHorizon} | {run.Status} | {FormatNullablePercent(expected)} | {FormatNullablePercent(probabilityPositive)} |");
        }

        builder.AppendLine();
    }

    private static void AppendEnsembleAudit(StringBuilder builder, FundResearchReport report)
    {
        builder.AppendLine("## Ensemble Audit");
        builder.AppendLine();
        if (report.EnsembleAudit.Count == 0)
        {
            builder.AppendLine("No ensemble audit entries are available.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Model | Validation status | Arena rank | Ensemble weight | Validation score | Included | Reason |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | --- | --- |");
        foreach (var entry in report.EnsembleAudit)
        {
            builder.AppendLine(
                $"| {EscapeMarkdown(entry.Model.Name)} | {EscapeMarkdown(entry.ValidationStatus)} | {entry.ArenaRank?.ToString(CultureInfo.InvariantCulture) ?? "N/A"} | {FormatPercent(entry.EnsembleWeight)} | {FormatNullablePercent(entry.ValidationScore)} | {(entry.Included ? "Yes" : "No")} | {EscapeMarkdown(entry.ExclusionReason)} |");
        }

        builder.AppendLine();
    }

    private static void AppendStressScenarios(StringBuilder builder, FundResearchReport report)
    {
        builder.AppendLine("## Stress Scenarios");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Start | End | Window obs | Peak loss | Terminal return | Selection | Diagnostic |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: | ---: | --- | --- |");
        foreach (var scenario in report.StressScenarios)
        {
            builder.AppendLine(
                $"| {EscapeMarkdown(scenario.Name)} | {FormatDate(scenario.StartDate)} | {FormatDate(scenario.EndDate)} | {scenario.WindowLengthObservations?.ToString(CultureInfo.InvariantCulture) ?? "N/A"} | {FormatPercent(scenario.PeakLoss)} | {FormatPercent(scenario.TerminalReturn)} | {EscapeMarkdown(scenario.SelectionCriterion ?? "N/A")} | {EscapeMarkdown(scenario.Diagnostic)} |");
        }

        builder.AppendLine();
    }

    private static void AppendWarnings(
        StringBuilder builder,
        FundResearchReport report,
        MarketTimingAssessment? timing)
    {
        var warnings = report.Warnings
            .Concat(report.FundScore.Warnings)
            .Concat(report.DecisionSignal.Warnings)
            .Concat(timing?.Warnings ?? Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        AppendBullets(builder, "Warnings", warnings);
    }

    private static void AppendProvenance(
        StringBuilder builder,
        FundResearchReport report,
        FundWorkspace workspace)
    {
        builder.AppendLine("## Provenance");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("| --- | --- |");
        foreach (var pair in report.Provenance.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"| {EscapeMarkdown(pair.Key)} | {EscapeMarkdown(pair.Value)} |");
        }

        builder.AppendLine($"| Arena attached | {(workspace.Arena is null ? "No" : "Yes")} |");
        builder.AppendLine();
    }

    private static void AppendBullets(
        StringBuilder builder,
        string title,
        IReadOnlyList<string> values)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        if (values.Count == 0)
        {
            builder.AppendLine("None.");
            builder.AppendLine();
            return;
        }

        foreach (var value in values)
        {
            builder.AppendLine($"- {EscapeMarkdown(value)}");
        }

        builder.AppendLine();
    }

    private static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatPercent(double value) => value.ToString("P2", CultureInfo.InvariantCulture);

    private static string FormatNullablePercent(double? value) => value.HasValue ? FormatPercent(value.Value) : "N/A";

    private static string FormatDate(DateOnly? value) => value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "N/A";

    private static string EscapeMarkdown(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static MarketTimingArenaResult? FindPrimaryTimingArena(MarketTimingAssessment timing)
    {
        var primary = timing.Decision.PrimaryHorizon;
        if (primary is not null)
        {
            var match = timing.ModelArenaResults.FirstOrDefault(result => result.Definition.Horizon.Equals(primary));
            if (match is not null)
            {
                return match;
            }
        }

        return timing.ModelArenaResults.FirstOrDefault();
    }

    private static string Shorten(string value, int maximumLength)
    {
        if (value.Length <= maximumLength)
        {
            return value;
        }

        return maximumLength <= 1 ? value[..maximumLength] : $"{value[..(maximumLength - 1)]}…";
    }

    private void MainFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.O)
        {
            e.Handled = true;
            _ = this.OpenCsvAsync();
        }
        else if (e.Control && e.KeyCode == Keys.L)
        {
            e.Handled = true;
            this.ShowStartPage();
        }
        else if (e.KeyCode == Keys.F5 && this.workspace is not null)
        {
            e.Handled = true;
            this.ApplyWorkspace();
            this.SetState(this.ResolveAvailableState(), BuildDatasetCaption(this.workspace.Analysis.Dataset));
        }
    }
}
