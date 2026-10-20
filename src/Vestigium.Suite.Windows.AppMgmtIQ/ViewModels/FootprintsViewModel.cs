using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class FootprintsViewModel : ObservableObject
{
    private const string AppPaths = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
    private const string AppPathsWow = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths";
    private const string UserAppPaths = @"Software\Microsoft\Windows\CurrentVersion\App Paths";
    private const string SharedDlls = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs";
    private const string SharedDllsWow = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs";
    private const string UserData = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";
    private const string MachineClasses = @"SOFTWARE\Classes";
    private const string UserClasses = @"Software\Classes";

    private readonly List<FootprintRow> _all = [];
    private readonly KqlSession _session = AppMgmtKql.Footprints();
    private KqlBoundQuery? _bound;

    public ObservableCollection<FootprintRow> Rows { get; } = [];
    public IReadOnlyList<FootprintRow> All => _all;
    public ObservableCollection<QueryMruRow> Queries { get; } = [];
    public KqlSession Session => _session;

    public int MruLimit { get; set; } = 10;
    public int StickyLimit { get; set; } = 3;
    public event Action? MruChanged;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private FootprintRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading footprints\u2026";

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
        Status = "Reading footprints\u2026";
        try
        {
            var found = await Task.Run(ReadAll);
            _all.Clear();
            _all.AddRange(found);
            Apply();
            HostLog.Information(AppMgmtCatalog.FootprintsLoaded, "Footprints", $"Footprints loaded. {_all.Count} entries.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Link(IReadOnlyList<PackageRow> packages)
    {
        foreach (var row in _all)
            row.Package = FootprintLink.Match(row, packages);
        Apply();
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    private void Apply()
    {
        var keep = Selected;
        Rows.Clear();
        foreach (var row in _all.Where(Matches).OrderBy(row => row.Kind, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase))
            Rows.Add(row);
        Selected = keep is null ? null : Rows.FirstOrDefault(row => row.Kind == keep.Kind && row.Name == keep.Name && row.Scope == keep.Scope && row.Path == keep.Path);
        if (string.IsNullOrWhiteSpace(Query) || _bound is not null)
            Status = $"{Rows.Count} entries";
    }

    private bool Matches(FootprintRow row)
    {
        if (_bound is null)
            return true;
        return _bound.Matches(FootprintsAdapter.ToRow(_session, new FootprintsFacts
        {
            Kind = row.Kind,
            Name = row.Name,
            Path = row.Path,
            Scope = row.Scope,
            RefCount = row.RefCount,
            Package = row.Package
        }));
    }

    private static List<FootprintRow> ReadAll()
    {
        var client = RegistryHelper.Local;
        var rows = new List<FootprintRow>();
        ReadAppPaths(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, AppPaths, "HKLM", rows);
        ReadAppPaths(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, AppPathsWow, "Wow", rows);
        ReadAppPaths(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserAppPaths, "HKCU", rows);
        ReadShared(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, SharedDlls, "HKLM", rows);
        ReadShared(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, SharedDllsWow, "Wow", rows);
        ReadProducts(client, rows);
        ReadAssociations(client, RegistryHiveKind.LocalMachine, MachineClasses, "HKLM", rows);
        ReadAssociations(client, RegistryHiveKind.CurrentUser, UserClasses, "HKCU", rows);
        return rows;
    }

    private static void ReadAppPaths(RegistryClient client, RegistryHiveKind hive, RegistryViewKind view, string path, string scope, List<FootprintRow> rows)
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

    private static void ReadShared(RegistryClient client, RegistryHiveKind hive, RegistryViewKind view, string path, string scope, List<FootprintRow> rows)
    {
        var key = client.GetKey(hive, path, view, RegistryDetailLevel.Full);
        if (key is null)
            return;
        foreach (var value in key.Values)
        {
            if (value.IsDefault || string.IsNullOrWhiteSpace(value.Name))
                continue;
            rows.Add(new FootprintRow
            {
                Kind = "SharedDll",
                Name = SafeName(value.Name),
                Path = value.Name,
                Scope = scope,
                RefCount = Count(value)
            });
        }
    }

    private static void ReadProducts(RegistryClient client, List<FootprintRow> rows)
    {
        foreach (var sid in client.ListSubKeys(RegistryHiveKind.LocalMachine, UserData, RegistryViewKind.Registry64, RegistryDetailLevel.Identity))
        {
            var products = sid.Path + "\\Products";
            foreach (var product in client.ListSubKeys(RegistryHiveKind.LocalMachine, products, RegistryViewKind.Registry64, RegistryDetailLevel.Identity))
            {
                var props = client.GetKey(RegistryHiveKind.LocalMachine, product.Path + "\\InstallProperties", RegistryViewKind.Registry64, RegistryDetailLevel.Full);
                var display = Text(props, "DisplayName");
                var code = Unpack(product.Name);
                rows.Add(new FootprintRow
                {
                    Kind = "Product",
                    Name = string.IsNullOrWhiteSpace(display) ? code : display,
                    Path = code,
                    Scope = sid.Name.Equals("S-1-5-18", StringComparison.OrdinalIgnoreCase) ? "Machine" : "User"
                });
            }
        }
    }

    private static void ReadAssociations(RegistryClient client, RegistryHiveKind hive, string root, string scope, List<FootprintRow> rows)
    {
        var codes = rows.Where(row => row.Kind == "Product" && !string.IsNullOrWhiteSpace(row.Path))
            .Select(row => row.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var extensions = new List<(string Ext, string ProgId)>();
        foreach (var child in client.ListSubKeys(hive, root, RegistryViewKind.Registry64, RegistryDetailLevel.Identity))
        {
            if (!child.Name.StartsWith('.'))
                continue;
            var key = client.GetKey(hive, child.Path, RegistryViewKind.Registry64, RegistryDetailLevel.Full);
            var progId = key?.Values.FirstOrDefault(value => value.IsDefault)?.DataText;
            if (!string.IsNullOrWhiteSpace(progId))
                extensions.Add((child.Name, progId));
        }

        var kept = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var progId in extensions.Select(item => item.ProgId).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var key = client.GetKey(hive, root + "\\" + progId, RegistryViewKind.Registry64, RegistryDetailLevel.Full);
            if (key is null)
                continue;
            var source = Text(key, "InstallSource");
            if (string.IsNullOrWhiteSpace(source))
                source = Text(key, "ProductCode");
            if (string.IsNullOrWhiteSpace(source))
                source = codes.FirstOrDefault(code => key.Values.Any(value => (value.DataText ?? "").Contains(code, StringComparison.OrdinalIgnoreCase))) ?? "";
            if (string.IsNullOrWhiteSpace(source))
                continue;
            kept[progId] = source;
            rows.Add(new FootprintRow
            {
                Kind = "Association",
                Name = progId,
                Path = source,
                Scope = scope
            });
        }

        foreach (var (ext, progId) in extensions)
        {
            if (!kept.ContainsKey(progId))
                continue;
            rows.Add(new FootprintRow
            {
                Kind = "Association",
                Name = ext,
                Path = progId,
                Scope = scope
            });
        }
    }

    private static string Text(RegistryKeyInfo? key, string name)
        => key?.Values.FirstOrDefault(value => value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.DataText ?? "";

    private static string Unpack(string packed)
    {
        if (packed.Length != 32)
            return packed;
        var a = Reverse(packed, 0, 8);
        var b = Reverse(packed, 8, 4);
        var c = Reverse(packed, 12, 4);
        var d = Reverse(packed, 16, 2) + Reverse(packed, 18, 2);
        var e = Reverse(packed, 20, 2) + Reverse(packed, 22, 2) + Reverse(packed, 24, 2) + Reverse(packed, 26, 2) + Reverse(packed, 28, 2) + Reverse(packed, 30, 2);
        return "{" + a + "-" + b + "-" + c + "-" + d + "-" + e + "}";
    }

    private static string Reverse(string text, int start, int length)
    {
        var chars = text.Substring(start, length).ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    private static string SafeName(string path)
    {
        try { return Path.GetFileName(path); }
        catch (ArgumentException) { return path; }
    }

    private static int? Count(RegistryValueInfo value)
    {
        if (value.Data is int number)
            return number;
        if (value.Data is long wide)
            return wide > int.MaxValue ? int.MaxValue : (int)wide;
        return int.TryParse(value.DataText, out var parsed) ? parsed : null;
    }
}
