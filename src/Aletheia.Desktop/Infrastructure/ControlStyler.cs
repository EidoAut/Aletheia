namespace Aletheia.Desktop.Infrastructure;

/// <summary>
/// Applies consistent dark technical styling to WinForms controls.
/// </summary>
internal static class ControlStyler
{
    /// <summary>
    /// Styles an analytical grid.
    /// </summary>
    /// <param name="grid">The grid.</param>
    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = ThemePalette.Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = ThemePalette.PanelAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ThemePalette.MutedText;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemePalette.PanelAlt;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = ThemePalette.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Consolas", 8f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.DefaultCellStyle.BackColor = ThemePalette.Surface;
        grid.DefaultCellStyle.ForeColor = ThemePalette.Text;
        grid.DefaultCellStyle.SelectionBackColor = ThemePalette.Selection;
        grid.DefaultCellStyle.SelectionForeColor = ThemePalette.TextStrong;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 8.8f, FontStyle.Regular);
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = ThemePalette.Panel;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = ThemePalette.Selection;
        grid.GridColor = ThemePalette.Border;
        grid.RowHeadersVisible = false;
        grid.RowTemplate.Height = 30;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoGenerateColumns = false;
        grid.ShowCellErrors = false;
        grid.ShowRowErrors = false;
        grid.ShowCellToolTips = true;
        grid.Margin = new Padding(0);
    }

    /// <summary>
    /// Styles a text input for use on an elevated surface.
    /// </summary>
    /// <param name="textBox">The text input.</param>
    public static void StyleTextInput(TextBox textBox)
    {
        textBox.BackColor = ThemePalette.Input;
        textBox.ForeColor = ThemePalette.Text;
        textBox.BorderStyle = BorderStyle.None;
        textBox.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
    }

    /// <summary>
    /// Styles a numeric input for use on an elevated surface.
    /// </summary>
    /// <param name="numericInput">The numeric input.</param>
    public static void StyleNumericInput(NumericUpDown numericInput)
    {
        numericInput.BackColor = ThemePalette.Input;
        numericInput.ForeColor = ThemePalette.Text;
        numericInput.BorderStyle = BorderStyle.None;
        numericInput.Font = new Font("Consolas", 9f, FontStyle.Bold);
        numericInput.TextAlign = HorizontalAlignment.Right;
    }

    /// <summary>
    /// Creates a compact heading label.
    /// </summary>
    /// <param name="text">The label text.</param>
    /// <returns>The label.</returns>
    public static Label CreateHeading(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Height = 30,
            Dock = DockStyle.Top,
            ForeColor = ThemePalette.TextStrong,
            Font = new Font("Consolas", 10f, FontStyle.Bold),
        };
    }

    /// <summary>
    /// Creates a compact uppercase section label.
    /// </summary>
    /// <param name="text">The section text.</param>
    /// <returns>The label.</returns>
    public static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            ForeColor = ThemePalette.SubtleText,
            Font = new Font("Consolas", 7.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft,
        };
    }

    /// <summary>
    /// Applies shared layout properties to an analytical table layout.
    /// </summary>
    /// <param name="layout">The analytical layout.</param>
    public static void ConfigureAnalyticsLayout(TableLayoutPanel layout)
    {
        layout.Dock = DockStyle.Fill;
        layout.BackColor = ThemePalette.Background;
        layout.Margin = new Padding(0);
        layout.Padding = new Padding(0);
    }
}
