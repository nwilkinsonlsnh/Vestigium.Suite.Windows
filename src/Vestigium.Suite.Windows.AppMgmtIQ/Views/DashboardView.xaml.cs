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
    private BloatwareViewModel? _bloat;
    private ActiveSetupViewModel? _setup;
    private List<(string Title, UIElement Chart)> _charts = [];

    public DashboardView()
    {
        InitializeComponent();
    }

    public void Bind(MainViewModel packages, FeaturesViewModel features, BloatwareViewModel bloat, ActiveSetupViewModel setup)
    {
        _packages = packages;
        _features = features;
        _bloat = bloat;
        _setup = setup;
        Refresh();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void Section_Checked(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        if (_packages is null || _features is null || _bloat is null || _setup is null || PackagesTab is null)
            return;
        if (PackagesTab.IsChecked == true)
            ShowPackages();
        else if (FeaturesTab.IsChecked == true)
            ShowFeatures();
        else if (BloatTab.IsChecked == true)
            ShowBloat();
        else
            ShowSetup();
    }

    private void ShowPackages()
    {
        var rows = _packages!.AllPackages;
        Story.Text = "What is installed, who published it, and how large it is.";
        SetCards(
            ("Packages", rows.Count.ToString()),
            ("Global", rows.Count(row => row.IsGlobal).ToString()),
            ("Per user", rows.Count(row => !row.IsGlobal).ToString()),
            ("x64", Percent(rows.Count(row => row.IsX64), rows.Count)));
        ShowCharts(
        [
            ("Publishers", Chart(TopSlices(rows.Select(row => Blank(row.Publisher)), 8), "Publishers", true)),
            ("Mix", Chart([Slice("Global", rows.Count(row => row.IsGlobal)), Slice("Per user", rows.Count(row => !row.IsGlobal)), Slice("System", rows.Count(row => row.SystemComponent)), Slice("x86", rows.Count(row => !row.IsX64))], "Package mix")),
            ("Install year", Chart(TopSlices(rows.Select(Year), 10), "Install year", true)),
            ("Size", SizeChart(rows))
        ]);
    }

    private void ShowFeatures()
    {
        var rows = _features!.AllFeatures;
        var enabled = rows.Count(row => row.IsEnabled);
        Story.Text = "Which Windows features are on, and which parents own them.";
        SetCards(
            ("Features", rows.Count.ToString()),
            ("Enabled", enabled.ToString()),
            ("Disabled", rows.Count(row => row.State == "Disabled").ToString()),
            ("Absent", rows.Count(row => row.State == "Absent").ToString()));
        ShowCharts(
        [
            ("State", Chart([Slice("Enabled", enabled), Slice("Disabled", rows.Count(row => row.State == "Disabled")), Slice("Absent", rows.Count(row => row.State == "Absent"))], "Feature state")),
            ("Parents", Chart(TopSlices(rows.Select(row => Blank(_features.ParentOf(row.Name), "(root)")), 8), "Feature parents", true))
        ]);
    }

    private void ShowBloat()
    {
        var rows = _bloat!.All;
        var installed = rows.Count(row => row.Installed);
        Story.Text = "Which catalog entries are actually on this machine.";
        SetCards(
            ("Catalog", rows.Count.ToString()),
            ("Installed", installed.ToString()),
            ("Provisioned", rows.Count(row => row.Provisioned).ToString()),
            ("Missing", (rows.Count - installed).ToString()));
        ShowCharts(
        [
            ("Category", Chart(TopSlices(rows.Select(row => row.Category), 8), "By category", true)),
            ("Presence", Chart([Slice("Installed", installed), Slice("Not installed", rows.Count - installed)], "Presence")),
            ("Provisioned", Chart([Slice("Provisioned", rows.Count(row => row.Provisioned)), Slice("Not provisioned", rows.Count(row => !row.Provisioned))], "Provisioned"))
        ]);
    }

    private void ShowSetup()
    {
        var rows = _setup!.All;
        Story.Text = "What is armed to run at the next logon.";
        SetCards(
            ("Components", rows.Count.ToString()),
            ("Pending", rows.Count(row => row.Pending).ToString()),
            ("With command", rows.Count(row => !string.IsNullOrWhiteSpace(row.StubPath)).ToString()),
            ("Machine", rows.Count(row => row.IsGlobal).ToString()));
        ShowCharts(
        [
            ("Pending", Chart([Slice("Pending", rows.Count(row => row.Pending)), Slice("Current", rows.Count(row => !row.Pending))], "Pending")),
            ("Scope", Chart([Slice("Machine", rows.Count(row => row.IsGlobal)), Slice("User", rows.Count(row => !row.IsGlobal))], "Scope")),
            ("Bitness", Chart([Slice("x64", rows.Count(row => row.IsX64)), Slice("x86", rows.Count(row => !row.IsX64))], "Bitness")),
            ("Command", Chart([Slice("Has command", rows.Count(row => !string.IsNullOrWhiteSpace(row.StubPath))), Slice("No command", rows.Count(row => string.IsNullOrWhiteSpace(row.StubPath)))], "Command"))
        ]);
    }

    private void ShowCharts(IReadOnlyList<(string Title, UIElement Chart)> charts)
    {
        _charts = charts.ToList();
        Selector.Children.Clear();
        var many = charts.Count > 2;
        SelectorHost.Visibility = many ? Visibility.Visible : Visibility.Collapsed;
        Pair.Visibility = many ? Visibility.Collapsed : Visibility.Visible;
        SingleChart.Visibility = many ? Visibility.Visible : Visibility.Collapsed;
        if (!many)
        {
            LeftChart.Child = charts.Count > 0 ? charts[0].Chart : Empty("Nothing to chart");
            RightChart.Child = charts.Count > 1 ? charts[1].Chart : Empty("Nothing to chart");
            return;
        }
        for (var i = 0; i < charts.Count; i++)
        {
            var index = i;
            var button = new RadioButton
            {
                Content = charts[i].Title,
                GroupName = "DashboardChart",
                Style = (Style)FindResource("RadioButton.Standard"),
                Margin = new Thickness(0, 0, 0, 8),
                IsChecked = i == 0
            };
            button.Checked += (_, _) => SingleChart.Child = _charts[index].Chart;
            Selector.Children.Add(button);
        }
        SingleChart.Child = charts[0].Chart;
    }

    private void SetCards(params (string Label, string Value)[] cards)
    {
        var hosts = new[] { Card1, Card2, Card3, Card4 };
        for (var i = 0; i < hosts.Length; i++)
            hosts[i].Child = i < cards.Length ? Card(cards[i].Label, cards[i].Value) : null;
    }

    private static UIElement SizeChart(IReadOnlyList<PackageRow> packages)
    {
        var sizes = packages.Select(Megabytes).Where(value => value > 0).ToArray();
        if (sizes.Length < 2)
            return Empty("Not enough sizes");
        return ChartView.Histogram(NumericSeries.From(sizes, "size-mb"), new ChartOptions { Title = "Size, MB", ShowBellCurve = true });
    }

    private static UIElement Chart(IReadOnlyList<ChartSlice> slices, string title, bool pareto = false)
    {
        var kept = slices.Where(slice => slice.Value > 0).ToArray();
        if (kept.Length == 0)
            return Empty("Nothing to chart");
        var options = new ChartOptions { Title = title };
        return pareto ? ChartView.Pareto(kept, options) : ChartView.Pie(kept, options);
    }

    private static ChartSlice Slice(string label, int value) => new() { Label = label, Value = value };

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
