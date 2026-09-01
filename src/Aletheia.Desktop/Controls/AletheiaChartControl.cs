#pragma warning disable SA1204 // Existing rendering helpers are grouped by drawing workflow.

using Aletheia.Application;
using Aletheia.Desktop.Infrastructure;
using Aletheia.Validation;
using ScottPlot.WinForms;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Provides shared ScottPlot rendering helpers inside a consistent analytical card.
/// </summary>
internal class AletheiaChartControl : UserControl
{
    private static readonly System.Drawing.Color[] SeriesColors =
    [
        ThemePalette.Accent,
        ThemePalette.AccentSecondary,
        ThemePalette.Positive,
        ThemePalette.Warning,
        ThemePalette.Negative,
        ThemePalette.MutedText,
    ];

    private readonly FormsPlot plot = new();
    private readonly System.Windows.Forms.Label titleLabel = new();
    private readonly System.Windows.Forms.Label subtitleLabel = new();
    private readonly System.Windows.Forms.Label emptyLabel = new();
    private bool refreshPending;
    private bool refreshScheduled;

    /// <summary>
    /// Initializes a new instance of the <see cref="AletheiaChartControl"/> class.
    /// </summary>
    public AletheiaChartControl()
    {
        this.Dock = DockStyle.Fill;
        this.Margin = new Padding(7);
        this.BackColor = ThemePalette.Background;

        var card = new SurfacePanel
        {
            Dock = DockStyle.Fill,
            FillColor = ThemePalette.Surface,
            BorderColor = ThemePalette.Border,
            CornerRadius = 8,
            Padding = new Padding(1),
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.Surface,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(this.BuildHeader(), 0, 0);
        layout.Controls.Add(this.BuildPlotHost(), 0, 1);
        card.Controls.Add(layout);
        this.Controls.Add(card);
    }

    /// <summary>
    /// Clears the plot and displays an empty analytical frame.
    /// </summary>
    /// <param name="title">The chart title.</param>
    /// <param name="xLabel">The x-axis label.</param>
    /// <param name="yLabel">The y-axis label.</param>
    public void ShowEmpty(string title, string xLabel = "", string yLabel = "")
    {
        this.Prepare(title, xLabel, yLabel);
        this.Finish(false);
    }

    /// <summary>
    /// Plots a single date-aligned line.
    /// </summary>
    /// <param name="title">The chart title.</param>
    /// <param name="series">The series.</param>
    /// <param name="yLabel">The y-axis label.</param>
    public void PlotLine(string title, IReadOnlyList<DatedValue> series, string yLabel)
    {
        this.Prepare(title, "Date", yLabel);
        var hasData = series.Count > 0;
        if (hasData)
        {
            var xs = series.Select(item => ToOaDate(item.Date)).ToArray();
            var ys = series.Select(item => item.Value).ToArray();
            var scatter = this.plot.Plot.Add.Scatter(xs, ys, ToPlotColor(ThemePalette.Accent));
            scatter.LineWidth = 2.1f;
            scatter.MarkerSize = 0;
            this.plot.Plot.Axes.DateTimeTicksBottom();
        }

        this.Finish(hasData);
    }

    /// <summary>
    /// Plots multiple date-aligned lines.
    /// </summary>
    /// <param name="title">The chart title.</param>
    /// <param name="series">The named series.</param>
    /// <param name="yLabel">The y-axis label.</param>
    public void PlotLines(string title, IReadOnlyList<(string Name, IReadOnlyList<DatedValue> Values)> series, string yLabel)
    {
        var coloredSeries = series
            .Select((item, index) => (item.Name, item.Values, SeriesColors[index % SeriesColors.Length]))
            .ToArray();
        this.PlotLines(title, coloredSeries, yLabel);
    }

    /// <summary>
    /// Plots multiple date-aligned lines with explicit colors.
    /// </summary>
    /// <param name="title">The chart title.</param>
    /// <param name="series">The named, colored series.</param>
    /// <param name="yLabel">The y-axis label.</param>
    public void PlotLines(
        string title,
        IReadOnlyList<(string Name, IReadOnlyList<DatedValue> Values, System.Drawing.Color Color)> series,
        string yLabel)
    {
        this.Prepare(title, "Date", yLabel);
        var populatedSeries = series.Where(item => item.Values.Count > 0).ToArray();
        foreach (var item in populatedSeries)
        {
            var xs = item.Values.Select(value => ToOaDate(value.Date)).ToArray();
            var ys = item.Values.Select(value => value.Value).ToArray();
            var scatter = this.plot.Plot.Add.Scatter(xs, ys, ToPlotColor(item.Color));
            scatter.LegendText = item.Name;
            scatter.LineWidth = 2f;
            scatter.MarkerSize = 0;
        }

        if (populatedSeries.Length > 0)
        {
            this.plot.Plot.Axes.DateTimeTicksBottom();
            this.plot.Plot.ShowLegend();
        }

        this.Finish(populatedSeries.Length > 0);
    }

    /// <summary>
    /// Plots a numeric XY line.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values.</param>
    /// <param name="xLabel">The x-axis label.</param>
    /// <param name="yLabel">The y-axis label.</param>
    public void PlotXYLine(string title, double[] x, double[] y, string xLabel, string yLabel)
    {
        this.Prepare(title, xLabel, yLabel);
        var hasData = x.Length > 0 && x.Length == y.Length;
        if (hasData)
        {
            var scatter = this.plot.Plot.Add.Scatter(x, y, ToPlotColor(ThemePalette.Accent));
            scatter.LineWidth = 2f;
            scatter.MarkerSize = 4;
        }

        this.Finish(hasData);
    }

    /// <summary>
    /// Plots a numeric scatter chart.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="points">The state projection points.</param>
    /// <param name="xSelector">The x value selector.</param>
    /// <param name="ySelector">The y value selector.</param>
    /// <param name="xLabel">The x-axis label.</param>
    /// <param name="yLabel">The y-axis label.</param>
    public void PlotStateScatter(
        string title,
        IReadOnlyList<StateProjectionPoint> points,
        Func<StateProjectionPoint, double> xSelector,
        Func<StateProjectionPoint, double> ySelector,
        string xLabel,
        string yLabel)
    {
        this.Prepare(title, xLabel, yLabel);
        var historical = points.Where(point => !point.IsCurrent).ToArray();
        if (historical.Length > 0)
        {
            var scatter = this.plot.Plot.Add.Scatter(
                historical.Select(xSelector).ToArray(),
                historical.Select(ySelector).ToArray(),
                ToPlotColor(ThemePalette.MutedText));
            scatter.LineWidth = 1.15f;
            scatter.MarkerSize = 2.5f;
            scatter.LegendText = "State trajectory";
        }

        var current = points.LastOrDefault(point => point.IsCurrent);
        if (current is not null)
        {
            var marker = this.plot.Plot.Add.Scatter(
                new[] { xSelector(current) },
                new[] { ySelector(current) },
                ToPlotColor(ThemePalette.Warning));
            marker.MarkerSize = 9;
            marker.LineWidth = 0;
            marker.LegendText = "Current state";
        }

        if (historical.Length > 0 || current is not null)
        {
            this.plot.Plot.ShowLegend();
        }

        this.Finish(historical.Length > 0 || current is not null);
    }

    /// <summary>
    /// Plots a return distribution.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="bins">The histogram bins.</param>
    public void PlotHistogram(string title, IReadOnlyList<HistogramBin> bins)
    {
        this.Prepare(title, "Return", "Count");
        var hasData = bins.Count > 0;
        if (hasData)
        {
            var positions = bins.Select(bin => (bin.LowerBoundInclusive + bin.UpperBoundExclusive) / 2d).ToArray();
            var counts = bins.Select(bin => (double)bin.Count).ToArray();
            var bars = this.plot.Plot.Add.Bars(positions, counts);
            bars.Color = ToPlotColor(ThemePalette.Accent);
        }

        this.Finish(hasData);
    }

    /// <summary>
    /// Plots the power spectrum.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="frequencies">The x-axis frequencies.</param>
    /// <param name="power">The y-axis power values.</param>
    /// <param name="dominantFrequency">The dominant frequency, if available.</param>
    public void PlotSpectrum(string title, double[] frequencies, double[] power, double? dominantFrequency)
    {
        this.Prepare(title, "Frequency (cycles / observation)", "Power");
        var hasData = frequencies.Length > 0 && frequencies.Length == power.Length;
        if (hasData)
        {
            var spectrum = this.plot.Plot.Add.Scatter(frequencies, power, ToPlotColor(ThemePalette.Accent));
            spectrum.LineWidth = 2f;
            spectrum.MarkerSize = 0;
            if (dominantFrequency.HasValue)
            {
                this.plot.Plot.Add.Line(
                    dominantFrequency.Value,
                    0d,
                    dominantFrequency.Value,
                    power.Max());
            }
        }

        this.Finish(hasData);
    }

    /// <summary>
    /// Plots analogue future paths and aggregate bands.
    /// </summary>
    /// <param name="paths">The analogue paths.</param>
    /// <param name="aggregate">The aggregate path.</param>
    public void PlotAnaloguePaths(
        IReadOnlyList<AnaloguePath> paths,
        IReadOnlyList<AnalogueAggregatePoint> aggregate)
    {
        this.Prepare("Analogue future trajectories", "Observation offset", "Return from t = 0");
        foreach (var path in paths.Take(20))
        {
            var scatter = this.plot.Plot.Add.Scatter(
                path.Points.Select(point => (double)point.ObservationOffset).ToArray(),
                path.Points.Select(point => point.Return).ToArray(),
                ToPlotColor(System.Drawing.Color.FromArgb(83, 105, 122)));
            scatter.MarkerSize = 0;
            scatter.LineWidth = 1;
        }

        if (aggregate.Count > 0)
        {
            var median = this.plot.Plot.Add.Scatter(
                aggregate.Select(point => (double)point.ObservationOffset).ToArray(),
                aggregate.Select(point => point.Median).ToArray(),
                ToPlotColor(ThemePalette.Accent));
            median.LineWidth = 2.4f;
            median.MarkerSize = 0;
            median.LegendText = "Median";
            var p25 = this.plot.Plot.Add.Scatter(
                aggregate.Select(point => (double)point.ObservationOffset).ToArray(),
                aggregate.Select(point => point.P25).ToArray(),
                ToPlotColor(ThemePalette.MutedText));
            p25.MarkerSize = 0;
            p25.LineWidth = 1.2f;
            p25.LegendText = "25th percentile";
            var p75 = this.plot.Plot.Add.Scatter(
                aggregate.Select(point => (double)point.ObservationOffset).ToArray(),
                aggregate.Select(point => point.P75).ToArray(),
                ToPlotColor(ThemePalette.MutedText));
            p75.MarkerSize = 0;
            p75.LineWidth = 1.2f;
            p75.LegendText = "75th percentile";
            this.plot.Plot.ShowLegend();
        }

        this.plot.Plot.Add.HorizontalLine(0d);
        this.Finish(paths.Count > 0 || aggregate.Count > 0);
    }

    /// <summary>
    /// Plots probability calibration bins.
    /// </summary>
    /// <param name="bins">The calibration bins.</param>
    public void PlotCalibration(IReadOnlyList<CalibrationBin> bins)
    {
        this.Prepare("Probability calibration", "Predicted probability", "Observed frequency");
        this.plot.Plot.Add.Line(0d, 0d, 1d, 1d);
        var populated = bins.Where(bin => bin.MeanPredictedProbability.HasValue && bin.ObservedPositiveFrequency.HasValue).ToArray();
        if (populated.Length > 0)
        {
            var scatter = this.plot.Plot.Add.Scatter(
                populated.Select(bin => bin.MeanPredictedProbability!.Value).ToArray(),
                populated.Select(bin => bin.ObservedPositiveFrequency!.Value).ToArray(),
                ToPlotColor(ThemePalette.Accent));
            scatter.MarkerSize = 8;
            scatter.LineWidth = 0;
        }

        this.Finish(populated.Length > 0);
    }

    /// <summary>
    /// Plots predicted return versus actual return.
    /// </summary>
    /// <param name="samples">The forecast samples.</param>
    public void PlotPredictedVsActual(IReadOnlyList<ForecastEvaluationSample> samples)
    {
        this.Prepare("Predicted vs actual", "Predicted return", "Actual return");
        var pointSamples = samples
            .Where(sample => sample.Prediction.Prediction.Supports(Aletheia.Core.ForecastCapabilities.PointForecast))
            .ToArray();
        if (pointSamples.Length > 0)
        {
            var scatter = this.plot.Plot.Add.Scatter(
                pointSamples.Select(sample => sample.Prediction.Prediction.PointForecastReturn).ToArray(),
                pointSamples.Select(sample => sample.Evaluation.ActualReturn).ToArray(),
                ToPlotColor(ThemePalette.Accent));
            scatter.LineWidth = 0;
            scatter.MarkerSize = 6;
            var minimum = pointSamples.Min(sample => Math.Min(sample.Prediction.Prediction.PointForecastReturn, sample.Evaluation.ActualReturn));
            var maximum = pointSamples.Max(sample => Math.Max(sample.Prediction.Prediction.PointForecastReturn, sample.Evaluation.ActualReturn));
            this.plot.Plot.Add.Line(minimum, minimum, maximum, maximum);
        }

        this.Finish(pointSamples.Length > 0);
    }

    /// <summary>
    /// Plots point-forecast error through time.
    /// </summary>
    /// <param name="samples">The forecast samples.</param>
    public void PlotErrors(IReadOnlyList<ForecastEvaluationSample> samples)
    {
        this.Prepare("Forecast error through time", "Cutoff", "Actual - prediction");
        var pointSamples = samples
            .Where(sample => sample.Prediction.Prediction.Supports(Aletheia.Core.ForecastCapabilities.PointForecast))
            .OrderBy(sample => sample.Prediction.Prediction.DataCutoffDate)
            .ToArray();
        if (pointSamples.Length > 0)
        {
            var scatter = this.plot.Plot.Add.Scatter(
                pointSamples.Select(sample => ToOaDate(sample.Prediction.Prediction.DataCutoffDate)).ToArray(),
                pointSamples.Select(sample => sample.Evaluation.ActualReturn - sample.Prediction.Prediction.PointForecastReturn).ToArray(),
                ToPlotColor(ThemePalette.Accent));
            scatter.LineWidth = 1.8f;
            scatter.MarkerSize = 3;
            this.plot.Plot.Axes.DateTimeTicksBottom();
        }

        this.plot.Plot.Add.HorizontalLine(0d);
        this.Finish(pointSamples.Length > 0);
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        this.ScheduleRefresh();
    }

    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (this.Visible)
        {
            this.ScheduleRefresh();
        }
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (this.Visible)
        {
            this.ScheduleRefresh();
        }
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        this.ScheduleRefresh();
    }

    /// <inheritdoc />
    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        this.ScheduleRefresh();
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        this.refreshScheduled = false;
        base.OnHandleDestroyed(e);
    }

    private static double ToOaDate(DateOnly date) => date.ToDateTime(TimeOnly.MinValue).ToOADate();

    private static ScottPlot.Color ToPlotColor(System.Drawing.Color color) => ScottPlot.Color.FromSDColor(color);

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.Surface,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14, 7, 12, 5),
            Margin = new Padding(0),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));

        var textPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.Surface,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        this.titleLabel.Dock = DockStyle.Fill;
        this.titleLabel.ForeColor = ThemePalette.TextStrong;
        this.titleLabel.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
        this.titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        this.titleLabel.AutoEllipsis = true;
        this.subtitleLabel.Dock = DockStyle.Fill;
        this.subtitleLabel.ForeColor = ThemePalette.SubtleText;
        this.subtitleLabel.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
        this.subtitleLabel.TextAlign = ContentAlignment.TopLeft;
        this.subtitleLabel.AutoEllipsis = true;
        textPanel.Controls.Add(this.titleLabel, 0, 0);
        textPanel.Controls.Add(this.subtitleLabel, 0, 1);

        var chartTag = new System.Windows.Forms.Label
        {
            Text = "CHART",
            Dock = DockStyle.Fill,
            ForeColor = ThemePalette.SubtleText,
            Font = new Font("Consolas", 7.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight,
        };
        header.Controls.Add(textPanel, 0, 0);
        header.Controls.Add(chartTag, 1, 0);
        return header;
    }

    private Control BuildPlotHost()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemePalette.ChartBackground,
            Margin = new Padding(0),
            Padding = new Padding(2, 0, 2, 2),
        };
        this.plot.Dock = DockStyle.Fill;
        this.plot.Margin = new Padding(0);
        this.emptyLabel.Dock = DockStyle.Fill;
        this.emptyLabel.Text = "No observations available for this view.";
        this.emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        this.emptyLabel.ForeColor = ThemePalette.SubtleText;
        this.emptyLabel.BackColor = ThemePalette.ChartBackground;
        this.emptyLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.emptyLabel.Visible = false;
        host.Controls.Add(this.plot);
        host.Controls.Add(this.emptyLabel);
        return host;
    }

    private void Prepare(string title, string xLabel, string yLabel)
    {
        this.titleLabel.Text = title;
        this.subtitleLabel.Text = BuildAxisCaption(xLabel, yLabel);
        this.emptyLabel.Visible = false;
        this.plot.Plot.Clear();
        this.plot.Plot.FigureBackground.Color = ToPlotColor(ThemePalette.ChartBackground);
        this.plot.Plot.DataBackground.Color = ToPlotColor(ThemePalette.ChartBackground);
        this.plot.Plot.Title(string.Empty);
        this.plot.Plot.XLabel(xLabel);
        this.plot.Plot.YLabel(yLabel);
        this.plot.Plot.Axes.Color(ToPlotColor(ThemePalette.MutedText));
        this.plot.Plot.Axes.Title.Label.ForeColor = ToPlotColor(ThemePalette.Text);
        this.plot.Plot.Axes.Title.Label.FontName = "Consolas";
        this.plot.Plot.Axes.Title.Label.FontSize = 12;
        this.plot.Plot.Grid.MajorLineColor = ToPlotColor(ThemePalette.Grid);
        this.plot.Plot.Grid.MinorLineColor = ToPlotColor(ThemePalette.ChartBackground);
        this.plot.Plot.Grid.MajorLineWidth = 1;
        this.plot.Plot.Legend.BackgroundColor = ToPlotColor(ThemePalette.PanelAlt);
        this.plot.Plot.Legend.OutlineColor = ToPlotColor(ThemePalette.Border);
        this.plot.Plot.Legend.FontColor = ToPlotColor(ThemePalette.Text);
        this.plot.Plot.Legend.FontName = "Consolas";
    }

    private void Finish(bool hasData)
    {
        this.plot.Plot.Axes.AutoScale();
        this.emptyLabel.Visible = !hasData;
        if (!hasData)
        {
            this.emptyLabel.BringToFront();
        }

        this.refreshPending = true;
        this.RefreshPlotIfReady();
    }

    private void ScheduleRefresh()
    {
        if (!this.refreshPending || this.refreshScheduled || !this.IsHandleCreated || this.IsDisposed || this.Disposing)
        {
            return;
        }

        this.refreshScheduled = true;
        try
        {
            this.BeginInvoke(new Action(() =>
            {
                this.refreshScheduled = false;
                this.RefreshPlotIfReady();
            }));
        }
        catch (InvalidOperationException)
        {
            this.refreshScheduled = false;
        }
    }

    private void RefreshPlotIfReady()
    {
        if (!this.refreshPending || this.IsDisposed || this.Disposing || !this.Visible || !this.IsHandleCreated || this.Parent is null)
        {
            return;
        }

        if (this.plot.IsDisposed || this.plot.Disposing)
        {
            return;
        }

        if (!this.plot.IsHandleCreated)
        {
            this.plot.CreateControl();
        }

        if (!this.plot.IsHandleCreated || this.plot.ClientSize.Width <= 0 || this.plot.ClientSize.Height <= 0)
        {
            return;
        }

        this.plot.Refresh();
        this.refreshPending = false;
    }

    private static string BuildAxisCaption(string xLabel, string yLabel)
    {
        if (string.IsNullOrWhiteSpace(xLabel))
        {
            return yLabel;
        }

        if (string.IsNullOrWhiteSpace(yLabel))
        {
            return xLabel;
        }

        return $"{yLabel} · {xLabel}";
    }
}
