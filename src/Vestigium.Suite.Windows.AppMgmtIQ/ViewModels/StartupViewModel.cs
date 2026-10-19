using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class StartupViewModel : ObservableObject
{
    private const string Run = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunOnce = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string RunOnceEx = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx";
    private const string UserRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string UserRunOnce = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string Approved = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved";

    private readonly List<StartupRow> _all = [];
    private readonly KqlSession _session = AppMgmtKql.Startup();
    private KqlBoundQuery? _bound;
    private MainViewModel? _inventory;

    public ObservableCollection<StartupRow> Rows { get; } = [];
    public IReadOnlyList<StartupRow> All => _all;
    public ObservableCollection<QueryMruRow> Queries { get; } = [];
    public KqlSession Session => _session;

    public int MruLimit { get; set; } = 10;
    public int StickyLimit { get; set; } = 3;
    public event Action? MruChanged;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnableCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisableCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSourceCommand))]
    [NotifyCanExecuteChangedFor(nameof(JumpToPackageCommand))]
    private StartupRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading startup entries\u2026";

    public void Attach(MainViewModel inventory) => _inventory = inventory;

    public void ApplyQuery()
    {
        _bound = KqlHost.Compile(Query, _session, _bound, out var error);
        Apply();
        if (!string.IsNullOrEmpty(error))
            Status = error;
    }

    public void CommitQuery()
    {
        ApplyQuery();
        if (_bound is not null)
        {
            QueryMru.Record(Queries, Query, MruLimit, StickyLimit);
            MruChanged?.Invoke();
        }
    }

    public void Pin(object? row)
    {
        if (row is not QueryMruRow mru)
            return;
        if (!QueryMru.ToggleSticky(Queries, mru, StickyLimit))
            Status = $"Sticky limit is {StickyLimit}.";
        MruChanged?.Invoke();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading startup entries\u2026";
        try
        {
            var found = await Task.Run(ReadAll);
            _all.Clear();
            _all.AddRange(found);
            if (_inventory is not null)
                Link(_inventory.AllPackages);
            else
                Apply();
            HostLog.Information(AppMgmtCatalog.StartupLoaded, "Startup", $"Startup loaded. {_all.Count} entries.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Link(IReadOnlyList<PackageRow> packages)
    {
        foreach (var row in _all)
            row.Package = StartupLink.Match(row, packages);
        Apply();
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private void CopyName()
    {
        if (Selected is not null)
            Clipboard.SetText(Selected.Name);
    }

    [RelayCommand]
    private void CopyCommand()
    {
        if (Selected is not null)
            Clipboard.SetText(Selected.Command);
    }

    [RelayCommand(CanExecute = nameof(CanToggle))]
    private void Enable() => Later("Enable");

    [RelayCommand(CanExecute = nameof(CanToggle))]
    private void Disable() => Later("Disable");

    [RelayCommand]
    private void EnableAll() => Later("Enable All");

    [RelayCommand]
    private void DisableAll() => Later("Disable All");

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove() => Later("Remove");

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenSource() => Later("Open source");

    [RelayCommand(CanExecute = nameof(CanJump))]
    private void JumpToPackage() => Later("Jump to package");

    private bool HasSelection() => Selected is not null;

    private bool CanToggle() => Selected is not null && Selected.Source is not ("RunOnce" or "RunOnceEx");

    private bool CanJump() => Selected is not null && !string.IsNullOrWhiteSpace(Selected.Package);

    private void Later(string action) => Status = $"{action} is not wired yet.";

    private void Apply()
    {
        var keep = Selected;
        Rows.Clear();
        foreach (var row in _all.Where(Matches).OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase))
            Rows.Add(row);
        Selected = keep is null ? null : Rows.FirstOrDefault(row => row.Name == keep.Name && row.Command == keep.Command && row.Source == keep.Source);
        if (string.IsNullOrWhiteSpace(Query) || _bound is not null)
            Status = $"{Rows.Count} entries";
    }

    private bool Matches(StartupRow row)
    {
        if (_bound is null)
            return true;
        return _bound.Matches(StartupAdapter.ToRow(_session, new StartupFacts
        {
            Name = row.Name,
            Command = row.Command,
            Source = row.Source,
            Scope = row.Scope,
            IsX64 = row.Bit switch { "64" => true, "32" => false, _ => null },
            Package = row.Package,
            Enabled = row.Enabled
        }));
    }

    private static List<StartupRow> ReadAll()
    {
        var client = RegistryHelper.Local;
        var approved = ReadApproved(client);
        var rows = new List<StartupRow>();

        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, Run, "Run", "HKLM", "64", approved, rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, Run, "Run", "HKLM", "32", approved, rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, RunOnce, "RunOnce", "HKLM", "64", approved, rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, RunOnce, "RunOnce", "HKLM", "32", approved, rows);
        ReadOnceEx(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, RunOnceEx, "HKLM", "64", approved, rows);
        ReadOnceEx(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, RunOnceEx, "HKLM", "32", approved, rows);
        ReadValues(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserRun, "Run", "HKCU", "64", approved, rows);
        ReadValues(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserRunOnce, "RunOnce", "HKCU", "64", approved, rows);
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "User", approved, rows);
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Common", approved, rows);

        return rows;
    }

    private static Dictionary<string, bool> ReadApproved(RegistryClient client)
    {
        var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        ReadApproved(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, "HKLM", map);
        ReadApproved(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, "HKLM", map);
        ReadApproved(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, "HKCU", map);
        return map;
    }

    private static void ReadApproved(RegistryClient client, RegistryHiveKind hive, RegistryViewKind view, string scope, Dictionary<string, bool> map)
    {
        foreach (var branch in new[] { "Run", "Run32", "StartupFolder" })
        {
            var key = client.GetKey(hive, Approved + "\\" + branch, view, RegistryDetailLevel.Full);
            if (key is null)
                continue;
            foreach (var value in key.Values)
                map[$"{scope}|{branch}|{value.Name}"] = StartupApproved.IsEnabled(value.Data ?? value.DataText);
        }
    }

    private static bool Flag(Dictionary<string, bool> approved, string scope, string branch, string name)
        => !approved.TryGetValue($"{scope}|{branch}|{name}", out var enabled) || enabled;

    private static void ReadValues(
        RegistryClient client,
        RegistryHiveKind hive,
        RegistryViewKind view,
        string path,
        string source,
        string scope,
        string bit,
        Dictionary<string, bool> approved,
        List<StartupRow> rows)
    {
        var key = client.GetKey(hive, path, view, RegistryDetailLevel.Full);
        if (key is null)
            return;
        var branch = bit == "32" ? "Run32" : "Run";
        foreach (var value in key.Values)
            Add(rows, value.Name, value.IsDefault, value.DataText, source, scope, bit, Flag(approved, scope, branch, value.Name));
    }

    private static void ReadOnceEx(
        RegistryClient client,
        RegistryHiveKind hive,
        RegistryViewKind view,
        string path,
        string scope,
        string bit,
        Dictionary<string, bool> approved,
        List<StartupRow> rows)
    {
        ReadValues(client, hive, view, path, "RunOnceEx", scope, bit, approved, rows);
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
                Add(rows, name, false, value.DataText, "RunOnceEx", scope, bit, true);
            }
        }
    }

    private static void ReadFolder(string folder, string scope, Dictionary<string, bool> approved, List<StartupRow> rows)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;
        var hiveScope = scope == "Common" ? "HKLM" : "HKCU";
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase))
                continue;
            var display = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(display))
                display = name;
            var command = string.Equals(Path.GetExtension(file), ".lnk", StringComparison.OrdinalIgnoreCase)
                ? ResolveShortcut(file)
                : file;
            Add(rows, display, false, command, "Startup folder", scope, "", Flag(approved, hiveScope, "StartupFolder", name));
        }
    }

    private static string ResolveShortcut(string path)
    {
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type is null)
                return path;
            dynamic shell = Activator.CreateInstance(type)!;
            dynamic link = shell.CreateShortcut(path);
            string target = link.TargetPath ?? "";
            string args = link.Arguments ?? "";
            if (string.IsNullOrWhiteSpace(target))
                return path;
            return string.IsNullOrWhiteSpace(args) ? target : $"{target} {args}".Trim();
        }
        catch
        {
            return path;
        }
    }

    private static void Add(List<StartupRow> rows, string name, bool isDefault, string? command, string source, string scope, string bit, bool enabled)
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
            Package = "",
            Enabled = enabled
        });
    }
}
