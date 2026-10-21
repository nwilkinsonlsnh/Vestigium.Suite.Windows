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
    private StartupViewModel? _startup;
    private FootprintsViewModel? _footprints;
    private List<(string Title, UIElement Chart)> _charts = [];

    public DashboardView()
    {
        InitializeComponent();
    }

    public void Bind(MainViewModel packages, FeaturesViewModel features, BloatwareViewModel bloat, ActiveSetupViewModel setup, StartupViewModel startup, FootprintsViewModel footprints)
    {
        _packages = packages;
        _features = features;
        _bloat = bloat;
        _setup = setup;
        _startup = startup;
        _footprints = footprints;
        LoadSnapshots();
        Refresh();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void LoadSnapshots()
    {
        var names = new SnapshotStore(AppMgmtSettingsStore.DefaultRoot).List();
        Snapshots.SelectionChanged -= Snapshot_Changed;
        Snapshots.ItemsSource = names;
        var chosen = BaselineApply.Selected;
        Snapshots.SelectedItem = chosen is not null && names.Contains(chosen) ? chosen : names.FirstOrDefault();
        Snapshots.SelectionChanged += Snapshot_Changed;
    }

    private void Snapshot_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Snapshots.SelectedItem is string name)
            BaselineApply.Selected = name;
        Refresh();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        BaselineApply.DeleteSelected();
        LoadSnapshots();
        Refresh();
    }

    private void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (_packages is null || _features is null || _bloat is null || _setup is null || _startup is null || _footprints is null)
            return;
        var taken = DateTime.Now;
        var snapshot = new Snapshot
        {
            Name = SnapshotStore.NameFor(taken),
            Machine = Environment.MachineName,
            Taken = taken,
            Packages = _packages.All.Select(row => new SnapshotRow { Key = row.Hive + "|" + row.KeyPath, Value = row.DisplayVersion }).ToList(),
            Features = _features.All.Select(row => new SnapshotRow { Key = row.Name, Value = row.IsEnabled ? "enabled" : "disabled" }).ToList(),
            BuiltIn = _bloat.All.Select(row => new SnapshotRow { Key = row.Family, Value = (row.Installed ? "installed" : "absent") + "|" + (row.Provisioned ? "provisioned" : "not") }).ToList(),
            ActiveSetup = _setup.All.Select(row => new SnapshotRow { Key = row.ComponentId, Value = row.Version + "|" + (row.Installed ? "installed" : "absent") }).ToList(),
            Startup = _startup.All.Select(row => new SnapshotRow { Key = row.Scope + "|" + row.Key, Value = (row.Enabled ? "enabled" : "disabled") + "|" + row.Command }).ToList(),
            Footprints = _footprints.All.Select(row => new SnapshotRow { Key = row.Kind + "|" + row.Scope + "|" + row.Path, Value = row.Exists switch { true => "yes", false => "no", _ => "" } }).ToList()
        };
        new SnapshotStore(AppMgmtSettingsStore.DefaultRoot).Save(snapshot);
        BaselineApply.Selected = snapshot.Name;
        LoadSnapshots();
        Story.Text = "Baseline captured. " + snapshot.Name;
    }

    private void Section_Checked(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        if (_packages is null || _features is null || _bloat is null || _setup is null || _startup is null || _footprints is null || PackagesTab is null)
            return;
        if (PackagesTab.IsChecked == true)
            ShowPackages();
        else if (FeaturesTab.IsChecked == true)
            ShowFeatures();
        else if (BloatTab.IsChecked == true)
            ShowBloat();
        else if (SetupTab.IsChecked == true)
            ShowSetup();
        else if (StartupTab.IsChecked == true)
            ShowStartup();
        else
            ShowFootprints();
    }

    private void ShowPackages()
    {
        var rows = _packages!.All;
        var orphans = rows.Count(row => row.IsOrphan == true);
        var repair = rows.Count(row => row.HasRepair);
        var modify = rows.Count(row => row.HasModify);
        Story.Text = "What is installed, and how healthy it is: orphans, repair, modify, and baseline.";
        SetCards(
            ("Packages", rows.Count.ToString()),
            ("Orphans", orphans.ToString()),
            ("Repair", repair.ToString()),
            ("Modify", modify.ToString()));
        ShowCharts(
        [
            ("Orphans", Chart([Slice("Orphan", orphans), Slice("Clean", rows.Count - orphans)], "Orphans")),
            ("Baseline", BaselineChart(_packages.All.Select(row => new SnapshotRow { Key = row.Hive + "|" + row.KeyPath, Value = row.DisplayVersion }), snap => snap.Packages)),
            ("Actions", Chart([Slice("Repair", repair), Slice("Modify", modify), Slice("Neither", rows.Count(row => !row.HasRepair && !row.HasModify))], "Actions")),
            ("Publishers", Chart(TopSlices(rows.Select(row => Blank(row.Publisher)), 8), "Publishers", true))
        ]);
    }

    private void ShowFeatures()
    {
        var rows = _features!.All;
        var enabled = rows.Count(row => row.IsEnabled);
        Story.Text = "Which Windows features are on, and which parents own them.";
        SetCards(
            ("Features", rows.Count.ToString()),
            ("Enabled", enabled.ToString()),
            ("Disabled", rows.Count(row => row.State == "Disabled").ToString()),
            ("Absent", rows.Count(row => row.State == "Absent").ToString()));
        ShowCharts(
        [
            ("Baseline", BaselineChart(_features.All.Select(row => new SnapshotRow { Key = row.Name, Value = row.IsEnabled ? "enabled" : "disabled" }), snap => snap.Features)),
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
            ("Baseline", BaselineChart(_bloat.All.Select(row => new SnapshotRow { Key = row.Family, Value = (row.Installed ? "installed" : "absent") + "|" + (row.Provisioned ? "provisioned" : "not") }), snap => snap.BuiltIn)),
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
            ("Baseline", BaselineChart(_setup.All.Select(row => new SnapshotRow { Key = row.ComponentId, Value = row.Version + "|" + (row.Installed ? "installed" : "absent") }), snap => snap.ActiveSetup)),
            ("Pending", Chart([Slice("Pending", rows.Count(row => row.Pending)), Slice("Current", rows.Count(row => !row.Pending))], "Pending")),
            ("Scope", Chart([Slice("Machine", rows.Count(row => row.IsGlobal)), Slice("User", rows.Count(row => !row.IsGlobal))], "Scope")),
            ("Architecture", Chart([Slice("x64", rows.Count(row => row.IsX64)), Slice("x86", rows.Count(row => !row.IsX64))], "Architecture")),
            ("Command", Chart([Slice("Has command", rows.Count(row => !string.IsNullOrWhiteSpace(row.StubPath))), Slice("No command", rows.Count(row => string.IsNullOrWhiteSpace(row.StubPath)))], "Command"))
        ]);
    }

    private void ShowStartup()
    {
        var rows = _startup!.All;
        var linked = rows.Count(row => !string.IsNullOrWhiteSpace(row.Package));
        var enabled = rows.Count(row => row.Enabled);
        Story.Text = "What starts with Windows, which of those are actually enabled, and which package they belong to.";
        SetCards(
            ("Entries", rows.Count.ToString()),
            ("Enabled", enabled.ToString()),
            ("Disabled", (rows.Count - enabled).ToString()),
            ("Linked", Percent(linked, rows.Count)));
        ShowCharts(
        [
            ("Baseline", BaselineChart(_startup.All.Select(row => new SnapshotRow { Key = row.Scope + "|" + row.Key, Value = (row.Enabled ? "enabled" : "disabled") + "|" + row.Command }), snap => snap.Startup)),
            ("State", Chart([Slice("Enabled", enabled), Slice("Disabled", rows.Count - enabled)], "State")),
            ("Source", Chart(TopSlices(rows.Select(row => Blank(row.Source)), 6), "Source", true)),
            ("Scope", Chart([Slice("HKLM", rows.Count(row => row.Scope == "HKLM")), Slice("HKCU", rows.Count(row => row.Scope == "HKCU")), Slice("Common", rows.Count(row => row.Scope == "Common")), Slice("User", rows.Count(row => row.Scope == "User"))], "Scope")),
            ("Linked", Chart([Slice("Linked", linked), Slice("Unlinked", rows.Count - linked)], "Linked"))
        ]);
    }

    private void ShowFootprints()
    {
        var rows = _footprints!.All;
        var shared = rows.Where(row => row.Kind == "SharedDll").ToList();
        var sharedMany = shared.Count(row => (row.RefCount ?? 0) > 1);
        var products = rows.Count(row => row.Kind == "Product");
        Story.Text = "App Paths, associations, installer products, and shared DLLs left behind by installers.";
        SetCards(
            ("Entries", rows.Count.ToString()),
            ("App Paths", rows.Count(row => row.Kind == "AppPath").ToString()),
            ("Shared > 1", sharedMany.ToString()),
            ("Products", products.ToString()));
        var exists = rows.Count(row => row.Exists == true);
        var missing = rows.Count(row => row.Exists == false);
        var pending = rows.Count - exists - missing;
        ShowCharts(
        [
            ("Baseline", BaselineChart(_footprints.All.Select(row => new SnapshotRow { Key = row.Kind + "|" + row.Scope + "|" + row.Path, Value = row.Exists switch { true => "yes", false => "no", _ => "" } }), snap => snap.Footprints)),
            ("Exists", Chart([Slice("Exists", exists), Slice("Missing", missing), Slice("No status", pending)], "File status")),
            ("Type", Chart([Slice("App Paths", rows.Count(row => row.Kind == "AppPath")), Slice("Shared DLLs", shared.Count), Slice("Products", products), Slice("Associations", rows.Count(row => row.Kind == "Association"))], "By type")),
            ("Ref count", Chart([Slice("1", shared.Count(row => row.RefCount == 1)), Slice("2", shared.Count(row => row.RefCount == 2)), Slice("3-5", shared.Count(row => row.RefCount is >= 3 and <= 5)), Slice("6+", shared.Count(row => (row.RefCount ?? 0) >= 6))], "SharedDLL ref count")),
            ("Products", Chart([Slice("Products", products), Slice("Other", rows.Count - products)], "Product presence")),
            ("Linked", Chart([Slice("Linked", rows.Count(row => !string.IsNullOrWhiteSpace(row.Package))), Slice("Unlinked", rows.Count(row => string.IsNullOrWhiteSpace(row.Package)))], "Linked"))
        ]);
    }


    private UIElement BaselineChart(IEnumerable<SnapshotRow> current, Func<Snapshot, List<SnapshotRow>> pick)
    {
        var store = new SnapshotStore(AppMgmtSettingsStore.DefaultRoot);
        var name = store.List().FirstOrDefault();
        var saved = name is null ? null : store.Load(name);
        if (saved is null)
            return Chart([Slice("None", current.Count())], "Baseline");
        var drifts = SnapshotCompare.Compare(pick(saved), current.ToList());
        return Chart([
            Slice("Match", drifts.Count(drift => drift.Status == "match")),
            Slice("Changed", drifts.Count(drift => drift.Status == "changed")),
            Slice("Added", drifts.Count(drift => drift.Status == "added")),
            Slice("Removed", drifts.Count(drift => drift.Status == "removed"))
        ], "Baseline");
    }

    private void ShowCharts(IReadOnlyList<(string Title, UIElement Chart)> charts)
    {
        _charts = charts.ToList();
        Selector.Children.Clear();
        var many = charts.Count > 1;
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
