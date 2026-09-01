using System.Runtime.ExceptionServices;
using Aletheia.Desktop;
using Aletheia.Desktop.Controls;

namespace Aletheia.Desktop.Tests;

public sealed class MainFormSmokeTests
{
    [Fact]
    public void MainForm_ConstructsCompleteVisualShellOnStaThread()
    {
        RunOnStaThread(form =>
        {
            Assert.Contains("Aletheia", form.Text, StringComparison.Ordinal);
            Assert.True(form.Width > 0);
            Assert.True(form.MinimumSize.Width > 0);
            Assert.True(form.MinimumSize.Height > 0);
            Assert.Single(FindControls<BrandMarkControl>(form));
            Assert.Equal(13, FindControls<NavigationButton>(form).Count());
            Assert.Single(FindControls<ActivityBarControl>(form));
            Assert.True(FindControls<AletheiaButton>(form).Count() >= 4);
            Assert.Equal(6, FindControls<Button>(form).Count(button => button.Name is "ChangeFundButton" or "LoadSampleButton" or "OpenCsvButton" or "GenerateReportButton" or "RunArenaButton" or "CancelButton"));
            Assert.True(FindControls<SurfacePanel>(form).Count() >= 6);

            form.CreateControl();
            form.ClientSize = new Size(1_180, 720);
            form.PerformLayout();
            var headerFunds = FindControls<Button>(form).Single(button => button.Name == "ChangeFundButton");
            var headerSample = FindControls<Button>(form).Single(button => button.Name == "LoadSampleButton");
            var headerCsv = FindControls<Button>(form).Single(button => button.Name == "OpenCsvButton");
            var report = FindControls<Button>(form).Single(button => button.Name == "GenerateReportButton");
            var runArena = FindControls<Button>(form).Single(button => button.Name == "RunArenaButton");
            var loadSample = FindControls<AletheiaButton>(form).Single(button => button.Text == "Load");
            var pageTitle = FindControls<Label>(form).Single(label => label.Text == "Market Simulator");
            var pageSubtitle = FindControls<Label>(form).Single(
                label => label.Text == "Search official funds, load CSV data and build investor guidance");
            var datasetMeta = FindControls<Label>(form).Single(label => label.Text == "Search CNMV or open a local CSV");
            var header = FindControls<Control>(form).Single(control => control.Name == "ShellHeader");
            var brand = FindControls<BrandMarkControl>(form).Single();

            AssertHeaderActionHasRenderableBounds(headerFunds, "FUNDS");
            AssertHeaderActionHasRenderableBounds(headerSample, "SAMPLE");
            AssertHeaderActionHasRenderableBounds(headerCsv, "OPEN CSV");
            AssertHeaderActionHasRenderableBounds(report, "REPORT");
            AssertHeaderActionHasRenderableBounds(runArena);
            Assert.StartsWith("RUN ", runArena.Text, StringComparison.Ordinal);
            Assert.EndsWith("D", runArena.Text, StringComparison.Ordinal);
            Assert.True(GetAbsoluteRight(runArena) <= form.ClientSize.Width);
            Assert.Equal(36, loadSample.Height);
            AssertSingleLineHeight(pageTitle);
            AssertSingleLineHeight(pageSubtitle);
            AssertSingleLineHeight(datasetMeta);
            Assert.True(header.Height >= 120);
            Assert.True(GetAbsoluteBottom(pageSubtitle) < GetAbsoluteBottom(header));
            Assert.True(GetAbsoluteBottom(datasetMeta) < GetAbsoluteBottom(header));
            Assert.True(brand.Width >= brand.MinimumSize.Width);
            Assert.True(brand.Height >= brand.MinimumSize.Height);
        });
    }

    private static void AssertHeaderActionHasRenderableBounds(Button button, string? expectedText = null)
    {
        Assert.NotNull(button.Parent);
        if (expectedText is not null)
        {
            Assert.Equal(expectedText, button.Text);
        }

        Assert.Equal(AccessibleRole.PushButton, button.AccessibleRole);
        Assert.True(button.TabStop);
        var measured = TextRenderer.MeasureText(
            button.Text,
            button.Font,
            new Size(10_000, 1_000),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        Assert.True(button.ClientSize.Width >= measured.Width + 12);
        Assert.True(button.ClientSize.Height >= measured.Height + 4);
        AssertCaptionPaintsVisibleGlyphs(button);
    }

    private static void AssertCaptionPaintsVisibleGlyphs(Button caption)
    {
        caption.CreateControl();
        using var bitmap = new Bitmap(Math.Max(1, caption.ClientSize.Width), Math.Max(1, caption.ClientSize.Height));
        caption.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        var background = caption.BackColor;
        var changedPixels = 0;
        for (var y = 4; y < bitmap.Height - 4; y++)
        {
            for (var x = 4; x < bitmap.Width - 4; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                var distance = Math.Abs(pixel.R - background.R) +
                    Math.Abs(pixel.G - background.G) +
                    Math.Abs(pixel.B - background.B);
                if (distance >= 24)
                {
                    changedPixels++;
                }
            }
        }

        Assert.True(changedPixels >= 20, $"Header caption '{caption.Text}' did not render visible glyphs.");
    }

    private static void AssertSingleLineHeight(Label label)
    {
        var measured = TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new Size(10_000, 1_000),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        Assert.True(
            label.ClientSize.Height >= measured.Height,
            $"Label '{label.Text}' needs {measured.Height}px but has {label.ClientSize.Height}px.");
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

    private static int GetAbsoluteRight(Control control)
    {
        var x = control.Right;
        var parent = control.Parent;
        while (parent is not null && parent is not Form)
        {
            x += parent.Left;
            parent = parent.Parent;
        }

        return x;
    }

    private static int GetAbsoluteBottom(Control control)
    {
        var y = control.Bottom;
        var parent = control.Parent;
        while (parent is not null && parent is not Form)
        {
            y += parent.Top;
            parent = parent.Parent;
        }

        return y;
    }

    private static void RunOnStaThread(Action<MainForm> assertion)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new MainForm();
                assertion(form);
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
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
