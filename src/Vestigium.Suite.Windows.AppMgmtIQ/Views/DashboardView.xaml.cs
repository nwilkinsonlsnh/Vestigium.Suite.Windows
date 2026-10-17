using System.Windows;
using System.Windows.Controls;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class DashboardView : UserControl
{
    private MainViewModel? _packages;
    private FeaturesViewModel? _features;

    public DashboardView()
    {
        InitializeComponent();
    }

    public void Bind(MainViewModel packages, FeaturesViewModel features)
    {
        _packages = packages;
        _features = features;
        Refresh();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        if (_packages is null || _features is null)
            return;

        var packages = _packages.AllPackages;
        var features = _features.AllFeatures;
        var enabled = features.Count(row => row.IsEnabled);
        var x64 = packages.Count(row => row.IsX64);

        PackagesCard.Child = Card("Packages", packages.Count.ToString());
        FeaturesCard.Child = Card("Features", features.Count.ToString());
        EnabledCard.Child = Card("Enabled", enabled.ToString());
        X64Card.Child = Card("x64", packages.Count == 0 ? "\u2014" : Percent(x64, packages.Count));

        SizeLine.Text = SizeLineText(packages);
        Publishers.Child = Chart(TopSlices(packages.Select(row => Blank(row.Publisher)), 8), "Publishers", pareto: true);
        Mix.Child = Chart(
        [
            new ChartSlice { Label = "Global", Value = packages.Count(row => row.IsGlobal) },
            new ChartSlice { Label = "Per user", Value = packages.Count(row => !row.IsGlobal) },
            new ChartSlice { Label = "System", Value = packages.Count(row => row.SystemComponent) },
            new ChartSlice { Label = "x86", Value = packages.Count(row => !row.IsX64) }
        ], "Package mix");
        States.Child = Chart(
        [
            new ChartSlice { Label = "Enabled", Value = enabled },
            new ChartSlice { Label = "Disabled", Value = features.Count(row => row.State == "Disabled") },
            new ChartSlice { Label = "Absent", Value = features.Count(row => row.State == "Absent") }
        ], "Feature state");
        Years.Child = Chart(TopSlices(packages.Select(Year), 10), "Install year", pareto: true);
        Sizes.Child = SizeChart(packages);
        Parents.Child = Chart(TopSlices(features.Select(row => Blank(_features.ParentOf(row.Name), "(root)")), 8), "Feature parents", pareto: true);
    }

    private static UIElement SizeChart(IReadOnlyList<PackageRow> packages)
    {
        var sizes = packages.Select(Megabytes).Where(value => value > 0).ToArray();
        if (sizes.Length < 2)
            return Empty("Not enough sizes");
        var series = NumericSeries.From(sizes, "size-mb");
        return ChartView.Histogram(series, new ChartOptions { Title = "Size, MB", ShowBellCurve = true });
    }

    private static string SizeLineText(IReadOnlyList<PackageRow> packages)
    {
        var sizes = packages.Select(Megabytes).Where(value => value > 0).ToArray();
        if (sizes.Length < 2)
            return "Size. Not enough values to summarize.";
        var series = NumericSeries.From(sizes, "size-mb");
        return $"Size. Mean {series.Full.Mean:0.0} MB. Median {series.Full.Median:0.0} MB. {series.Count} packages with a size.";
    }

    private static UIElement Chart(IReadOnlyList<ChartSlice> slices, string title, bool pareto = false)
    {
        var kept = slices.Where(slice => slice.Value > 0).ToArray();
        if (kept.Length == 0)
            return Empty("Nothing to chart");
        var options = new ChartOptions { Title = title };
        return pareto ? ChartView.Pareto(kept, options) : ChartView.Pie(kept, options);
    }

    private static IReadOnlyList<ChartSlice> TopSlices(IEnumerable<string> labels, int take)
    {
        var groups = labels
            .GroupBy(label => label, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ChartSlice { Label = group.Key, Value = group.Count() })
            .OrderByDescending(slice => slice.Value)
            .ToArray();
        if (groups.Length <= take)
            return groups;
        var head = groups.Take(take).ToList();
        head.Add(new ChartSlice { Label = "Other", Value = groups.Skip(take).Sum(slice => slice.Value) });
        return head;
    }

    private static UIElement Card(string label, string value) => new StackPanel
    {
        Margin = new Thickness(12),
        Children =
        {
            new TextBlock { Text = label, Style = (Style)Application.Current.FindResource("TextBlock.Caption") },
            new TextBlock { Text = value, FontSize = 28, Margin = new Thickness(0, 4, 0, 0), Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("Vestigium.Brushes.Text.Primary") }
        }
    };

    private static UIElement Empty(string text) => new TextBlock
    {
        Text = text,
        Margin = new Thickness(12),
        Style = (Style)Application.Current.FindResource("TextBlock.Caption")
    };

    private static string Percent(int part, int whole) => whole == 0 ? "\u2014" : $"{part * 100.0 / whole:0}%";

    private static string Blank(string? value, string fallback = "(none)") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Year(PackageRow row)
    {
        var text = row.InstallDate.Trim();
        return text.Length >= 4 && text.Take(4).All(char.IsDigit) ? text[..4] : "Unknown";
    }

    private static double Megabytes(PackageRow row) =>
        double.TryParse(row.EstimatedSize, out var kb) ? kb / 1024d : 0;
}
