using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Pages;

namespace Aletheia.Desktop.Tests;

public sealed class StartPageInteractionTests
{
    [Fact]
    public void StartPage_SearchSelectionAndLoadUseInjectedWorkflow()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var fund = CreateFund();
                string? searchedQuery = null;
                FundSearchResultSummary? loadedFund = null;
                StartPage? page = null;
                page = new StartPage(
                    query =>
                    {
                        searchedQuery = query;
                        page!.SetSearchResults([fund]);
                        return Task.CompletedTask;
                    },
                    selected =>
                    {
                        loadedFund = selected;
                        return Task.CompletedTask;
                    },
                    (_, _) => { },
                    (_, _) => { });

                using (page)
                {
                    page.CreateControl();
                    page.Size = new Size(1_200, 760);
                    page.PerformLayout();
                    var searchBox = FindControls<TextBox>(page).Single();
                    var searchButton = FindControls<AletheiaButton>(page).Single(button => button.Name == "SearchButton");
                    var loadButton = FindControls<AletheiaButton>(page).Single(button => button.Name == "LoadFundButton");

                    searchBox.Text = "santander";
                    searchButton.PerformClick();
                    loadButton.PerformClick();

                    Assert.Equal("santander", searchedQuery);
                    Assert.True(loadButton.Enabled);
                    Assert.Same(fund, loadedFund);
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

    private static FundSearchResultSummary CreateFund()
    {
        return new FundSearchResultSummary(
            "cnmv-iic",
            "CNMV IIC",
            new FundIdentifier(FundIdentifierKind.Isin, "ES0168845032"),
            "AURUM RENTA VARIABLE, FI",
            "ES0168845032",
            "SANTANDER ASSET MANAGEMENT, S.A., SGIIC",
            "EUR",
            "FI",
            "ES",
            true,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 5, 31),
            ObservationFrequency.Daily,
            "CNMV official IIC XML publication",
            "FONDMENS_202605.xml");
    }

    private static IEnumerable<TControl> FindControls<TControl>(Control parent)
        where TControl : Control
    {
        foreach (Control child in parent.Controls)
        {
            if (child is TControl match)
            {
                yield return match;
            }

            foreach (var descendant in FindControls<TControl>(child))
            {
                yield return descendant;
            }
        }
    }
}
