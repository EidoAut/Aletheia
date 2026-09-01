using Aletheia.Desktop.Pages;

namespace Aletheia.Desktop.Tests;

public sealed class DesignerBackedPageSmokeTests
{
    [Fact]
    public void WorkspacePageBase_IsConcreteAndDesignerInstantiable()
    {
        Assert.False(typeof(WorkspacePageBase).IsAbstract);

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var page = new WorkspacePageBase();
                page.CreateControl();
                page.Size = new Size(1_200, 760);
                page.PerformLayout();
                Assert.Equal(DockStyle.Fill, page.Dock);
                Assert.Equal("Workspace", page.PageTitle);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    [Fact]
    public void AllDesignerBackedPages_ConstructOnStaThread()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                UserControl[] pages =
                [
                    new StartPage(),
                    new OverviewPage(),
                    new PerformancePage(),
                    new RiskPage(),
                    new SimulationPage(),
                    new DynamicsPage(),
                    new SpectralPage(),
                    new AnaloguesPage(),
                    new ForecastPage(),
                    new ModelArenaPage(),
                    new ValidationPage(),
                    new PredictionsPage(),
                    new LabPage(),
                ];

                foreach (var page in pages)
                {
                    using (page)
                    {
                        page.CreateControl();
                        page.Size = new Size(1_200, 760);
                        page.PerformLayout();
                        Assert.Equal(DockStyle.Fill, page.Dock);
                        Assert.NotEmpty(page.Controls.Cast<Control>());
                    }
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }
}
