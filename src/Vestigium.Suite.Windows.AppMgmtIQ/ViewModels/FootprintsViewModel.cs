using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class FootprintsViewModel : ObservableObject
{
    private const string AppPaths = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
    private const string AppPathsWow = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths";
    private const string UserAppPaths = @"Software\Microsoft\Windows\CurrentVersion\App Paths";

    private readonly List<FootprintRow> _all = [];

    public ObservableCollection<FootprintRow> Rows { get; } = [];
    public IReadOnlyList<FootprintRow> All => _all;

    [ObservableProperty]
    private FootprintRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading footprints\u2026";

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading App Paths\u2026";
        try
        {
            var found = await Task.Run(ReadAppPaths);
            _all.Clear();
            _all.AddRange(found);
            Apply();
            HostLog.Information(AppMgmtCatalog.FootprintsLoaded, "Footprints", $"Footprints loaded. {_all.Count} App Paths.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    private void Apply()
    {
        var keep = Selected;
        Rows.Clear();
        foreach (var row in _all.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase))
            Rows.Add(row);
        Selected = keep is null ? null : Rows.FirstOrDefault(row => row.Kind == keep.Kind && row.Name == keep.Name && row.Scope == keep.Scope);
        Status = $"{Rows.Count} entries";
    }

    private static List<FootprintRow> ReadAppPaths()
    {
        var client = RegistryHelper.Local;
        var rows = new List<FootprintRow>();
        Read(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, AppPaths, "HKLM", rows);
        Read(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, AppPathsWow, "Wow", rows);
        Read(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserAppPaths, "HKCU", rows);
        return rows;
    }

    private static void Read(RegistryClient client, RegistryHiveKind hive, RegistryViewKind view, string path, string scope, List<FootprintRow> rows)
    {
        foreach (var child in client.ListSubKeys(hive, path, view, RegistryDetailLevel.Identity))
        {
            var key = client.GetKey(hive, child.Path, view, RegistryDetailLevel.Full);
            if (key is null)
                continue;
            var target = key.Values.FirstOrDefault(value => value.IsDefault)?.DataText ?? "";
            rows.Add(new FootprintRow
            {
                Kind = "AppPath",
                Name = child.Name,
                Path = target,
                Scope = scope
            });
        }
    }
}
