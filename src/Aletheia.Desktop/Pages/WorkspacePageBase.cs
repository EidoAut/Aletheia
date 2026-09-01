using Aletheia.Application;
using Aletheia.Desktop.Infrastructure;
using Aletheia.Validation;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Base class for workspace-aware analytical pages.
/// </summary>
public class WorkspacePageBase : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspacePageBase"/> class.
    /// </summary>
    public WorkspacePageBase()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ThemePalette.Background;
        this.ForeColor = ThemePalette.Text;
    }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    public virtual string PageTitle => "Workspace";

    /// <summary>
    /// Updates the page from the current workspace.
    /// </summary>
    /// <param name="workspace">The workspace, if loaded.</param>
    public virtual void SetWorkspace(FundWorkspace? workspace)
    {
    }

    /// <summary>
    /// Updates the page from Model Arena results.
    /// </summary>
    /// <param name="arena">The arena result, if available.</param>
    public virtual void SetArena(ModelArenaResult? arena)
    {
    }

    /// <summary>
    /// Creates a standard empty-state label.
    /// </summary>
    /// <param name="text">The label text.</param>
    /// <returns>The label.</returns>
    protected static Label CreateEmptyLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = ThemePalette.MutedText,
            BackColor = ThemePalette.Background,
        };
    }
}
