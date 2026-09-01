using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Displays a responsive row of analytical metric cards.
/// </summary>
internal sealed class MetricStripControl : UserControl
{
    private readonly TableLayoutPanel layout = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricStripControl"/> class.
    /// </summary>
    public MetricStripControl()
    {
        this.Dock = DockStyle.Fill;
        this.Height = 92;
        this.Margin = new Padding(2, 0, 2, 4);
        this.BackColor = ThemePalette.Background;
        this.layout.Dock = DockStyle.Fill;
        this.layout.BackColor = ThemePalette.Background;
        this.layout.RowCount = 1;
        this.layout.Margin = new Padding(0);
        this.layout.Padding = new Padding(0);
        this.layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        this.Controls.Add(this.layout);
    }

    /// <summary>
    /// Replaces the displayed metrics.
    /// </summary>
    /// <param name="metrics">The metric name/value/color tuples.</param>
    public void SetMetrics(IReadOnlyList<(string Name, string Value, Color? Accent)> metrics)
    {
        this.layout.SuspendLayout();
        try
        {
            while (this.layout.Controls.Count > 0)
            {
                var control = this.layout.Controls[0];
                this.layout.Controls.RemoveAt(0);
                control.Dispose();
            }

            this.layout.ColumnStyles.Clear();
            this.layout.ColumnCount = Math.Max(1, metrics.Count);
            if (metrics.Count == 0)
            {
                this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                return;
            }

            var width = 100f / metrics.Count;
            for (var index = 0; index < metrics.Count; index++)
            {
                this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
                var metric = metrics[index];
                var control = new KpiControl { Dock = DockStyle.Fill };
                control.SetMetric(metric.Name, metric.Value, metric.Accent);
                this.layout.Controls.Add(control, index, 0);
            }
        }
        finally
        {
            this.layout.ResumeLayout(true);
        }
    }
}
