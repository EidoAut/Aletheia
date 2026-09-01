using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Controls;

/// <summary>
/// Creates consistently styled analytical grids.
/// </summary>
internal static class GridFactory
{
    /// <summary>
    /// Creates a styled grid.
    /// </summary>
    /// <returns>The grid.</returns>
    public static DataGridView Create()
    {
        var grid = new BufferedDataGridView { Dock = DockStyle.Fill };
        ControlStyler.StyleGrid(grid);
        return grid;
    }

    /// <summary>
    /// Replaces grid rows with name-value pairs.
    /// </summary>
    /// <param name="grid">The grid.</param>
    /// <param name="rows">The rows.</param>
    public static void SetNameValueRows(DataGridView grid, IReadOnlyList<(string Name, string Value)> rows)
    {
        grid.SuspendLayout();
        try
        {
            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add("name", "Metric");
            grid.Columns.Add("value", "Value");
            grid.Columns["name"]!.FillWeight = 42;
            grid.Columns["value"]!.FillWeight = 58;
            foreach (var row in rows)
            {
                var index = grid.Rows.Add(row.Name, row.Value);
                if (string.IsNullOrWhiteSpace(row.Value))
                {
                    grid.Rows[index].DefaultCellStyle.BackColor = ThemePalette.PanelAlt;
                    grid.Rows[index].DefaultCellStyle.ForeColor = ThemePalette.Accent;
                    grid.Rows[index].DefaultCellStyle.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
                }
            }
        }
        finally
        {
            grid.ResumeLayout();
        }
    }

    private sealed class BufferedDataGridView : DataGridView
    {
        public BufferedDataGridView()
        {
            this.DoubleBuffered = true;
        }
    }
}
