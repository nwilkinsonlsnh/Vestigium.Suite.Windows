using System.IO;
using System.Windows;
using System.Windows.Controls;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class SnapshotView : UserControl
{
    private bool _loading;
    private MainViewModel? _packages;
    private FeaturesViewModel? _features;
    private BloatwareViewModel? _bloat;
    private ActiveSetupViewModel? _setup;
    private StartupViewModel? _startup;
    private FootprintsViewModel? _footprints;

    public SnapshotView()
    {
        InitializeComponent();
        Loaded += (_, _) => { LoadChoice(); LoadSnapshots(); };
    }

    public void Bind(MainViewModel packages, FeaturesViewModel features, BloatwareViewModel bloat, ActiveSetupViewModel setup, StartupViewModel startup, FootprintsViewModel footprints)
    {
        _packages = packages;
        _features = features;
        _bloat = bloat;
        _setup = setup;
        _startup = startup;
        _footprints = footprints;
    }

    private void LoadChoice()
    {
        _loading = true;
        var choice = SnapshotChoice.Load();
        Packages.IsChecked = choice.Packages;
        Features.IsChecked = choice.Features;
        BuiltIn.IsChecked = choice.BuiltIn;
        Setup.IsChecked = choice.ActiveSetup;
        Startup.IsChecked = choice.Startup;
        Footprints.IsChecked = choice.Footprints;
        _loading = false;
    }

    private void Choice_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        SnapshotChoice.Save(new SnapshotChoice
        {
            Packages = Packages.IsChecked == true,
            Features = Features.IsChecked == true,
            BuiltIn = BuiltIn.IsChecked == true,
            ActiveSetup = Setup.IsChecked == true,
            Startup = Startup.IsChecked == true,
            Footprints = Footprints.IsChecked == true
        });
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadSnapshots();

    private void LoadSnapshots()
    {
        var names = new SnapshotStore().List();
        Snapshots.SelectionChanged -= Snapshot_Changed;
        Snapshots.ItemsSource = names;
        var chosen = BaselineApply.Selected;
        Snapshots.SelectedItem = chosen is not null && names.Contains(chosen) ? chosen : names.FirstOrDefault();
        Snapshots.SelectionChanged += Snapshot_Changed;
        Story.Text = names.Count + " snapshots.";
    }

    private void Snapshot_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Snapshots.SelectedItem is string name)
            BaselineApply.Selected = name;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        BaselineApply.DeleteSelected();
        LoadSnapshots();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (Snapshots.SelectedItem is not string name)
            return;
        var source = Path.Combine(SnapshotStore.Root, name + ".json");
        if (!File.Exists(source))
            return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = name + ".json",
            Filter = "Snapshot (*.json)|*.json",
            Title = "Export snapshot"
        };
        if (dialog.ShowDialog() != true)
            return;
        File.Copy(source, dialog.FileName, true);
        Story.Text = "Exported " + name;
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Snapshot (*.json)|*.json",
            Title = "Import snapshot"
        };
        if (dialog.ShowDialog() != true)
            return;
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(dialog.FileName));
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.Name))
        {
            Story.Text = "That file is not a snapshot.";
            return;
        }
        new SnapshotStore().Save(snapshot);
        BaselineApply.Selected = snapshot.Name;
        LoadSnapshots();
        Story.Text = "Imported " + snapshot.Name;
    }

    private void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (_packages is null || _features is null || _bloat is null || _setup is null || _startup is null || _footprints is null)
            return;
        var choice = SnapshotChoice.Load();
        var taken = DateTime.Now;
        var snapshot = new Snapshot
        {
            Name = SnapshotStore.NameFor(taken),
            Machine = Environment.MachineName,
            Taken = taken,
            Packages = choice.Packages ? _packages.All.Select(row => new SnapshotRow { Key = row.Hive + "|" + row.KeyPath, Value = row.DisplayVersion }).ToList() : [],
            Features = choice.Features ? _features.All.Select(row => new SnapshotRow { Key = row.Name, Value = row.IsEnabled ? "enabled" : "disabled" }).ToList() : [],
            BuiltIn = choice.BuiltIn ? _bloat.All.Select(row => new SnapshotRow { Key = row.Family, Value = (row.Installed ? "installed" : "absent") + "|" + (row.Provisioned ? "provisioned" : "not") }).ToList() : [],
            ActiveSetup = choice.ActiveSetup ? _setup.All.Select(row => new SnapshotRow { Key = row.ComponentId, Value = row.Version + "|" + (row.Installed ? "installed" : "absent") }).ToList() : [],
            Startup = choice.Startup ? _startup.All.Select(row => new SnapshotRow { Key = row.Scope + "|" + row.Key, Value = (row.Enabled ? "enabled" : "disabled") + "|" + row.Command }).ToList() : [],
            Footprints = choice.Footprints ? _footprints.All.Select(row => new SnapshotRow { Key = row.Kind + "|" + row.Scope + "|" + row.Path, Value = row.Exists switch { true => "yes", false => "no", _ => "" } }).ToList() : []
        };
        new SnapshotStore().Save(snapshot);
        BaselineApply.Selected = snapshot.Name;
        LoadSnapshots();
        Story.Text = "Captured " + snapshot.Name;
    }
}
