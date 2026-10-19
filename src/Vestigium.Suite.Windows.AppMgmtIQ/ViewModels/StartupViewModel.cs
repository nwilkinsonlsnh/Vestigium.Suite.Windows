using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class StartupViewModel : ObservableObject
{
    private const string Run = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunOnce = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string RunOnceEx = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx";
    private const string UserRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string UserRunOnce = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";

    private readonly List<StartupRow> _all = [];

    public ObservableCollection<StartupRow> Rows { get; } = [];
    public IReadOnlyList<StartupRow> All => _all;

    [ObservableProperty]
    private StartupRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading startup entries\u2026";

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading startup entries\u2026";
        try
        {
            var found = await Task.Run(ReadAll);
            _all.Clear();
            _all.AddRange(found);
            Apply();
            HostLog.Information(AppMgmtCatalog.StartupLoaded, "Startup", $"Startup loaded. {_all.Count} entries.");
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
        Rows.Clear();
        foreach (var row in _all.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase))
            Rows.Add(row);
        Status = $"{Rows.Count} entries";
    }

    private static List<StartupRow> ReadAll()
    {
        var client = RegistryHelper.Local;
        var rows = new List<StartupRow>();

        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, Run, "Run", "HKLM", "64", rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, Run, "Run", "HKLM", "32", rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, RunOnce, "RunOnce", "HKLM", "64", rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, RunOnce, "RunOnce", "HKLM", "32", rows);
        ReadOnceEx(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, RunOnceEx, "HKLM", "64", rows);
        ReadOnceEx(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, RunOnceEx, "HKLM", "32", rows);
        ReadValues(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserRun, "Run", "HKCU", "64", rows);
        ReadValues(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserRunOnce, "RunOnce", "HKCU", "64", rows);

        return rows;
    }

    private static void ReadValues(
        RegistryClient client,
        RegistryHiveKind hive,
        RegistryViewKind view,
        string path,
        string source,
        string scope,
        string bit,
        List<StartupRow> rows)
    {
        var key = client.GetKey(hive, path, view, RegistryDetailLevel.Full);
        if (key is null)
            return;
        foreach (var value in key.Values)
            Add(rows, value.Name, value.IsDefault, value.DataText, source, scope, bit);
    }

    private static void ReadOnceEx(
        RegistryClient client,
        RegistryHiveKind hive,
        RegistryViewKind view,
        string path,
        string scope,
        string bit,
        List<StartupRow> rows)
    {
        ReadValues(client, hive, view, path, "RunOnceEx", scope, bit, rows);
        foreach (var child in client.ListSubKeys(hive, path, view, RegistryDetailLevel.Identity))
        {
            if (string.Equals(child.Name, "Depend", StringComparison.OrdinalIgnoreCase))
                continue;
            var snap = client.GetKey(hive, child.Path, view, RegistryDetailLevel.Full);
            if (snap is null)
                continue;
            var title = snap.Values.FirstOrDefault(value => value.IsDefault)?.DataText;
            foreach (var value in snap.Values)
            {
                if (value.IsDefault)
                    continue;
                var name = string.IsNullOrWhiteSpace(title)
                    ? $"{child.Name} / {value.Name}"
                    : $"{title} / {value.Name}";
                Add(rows, name, false, value.DataText, "RunOnceEx", scope, bit);
            }
        }
    }

    private static void Add(List<StartupRow> rows, string name, bool isDefault, string? command, string source, string scope, string bit)
    {
        var text = command?.Trim() ?? "";
        if (text.Length == 0)
            return;
        rows.Add(new StartupRow
        {
            Name = isDefault || string.IsNullOrWhiteSpace(name) ? "(Default)" : name,
            Command = text,
            Source = source,
            Scope = scope,
            Bit = bit,
            Package = ""
        });
    }
}
