using SetupKql = Vestigium.Helpers.Kql.ActiveSetup;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class ActiveSetupViewModel : ObservableObject
{
    private const string Root = @"SOFTWARE\Microsoft\Active Setup\Installed Components";

    private readonly List<ActiveSetupRow> _all = [];
    private readonly KqlSession _session = SetupKql.ActiveSetupKql.Components();
    private KqlBoundQuery? _bound;

    public ObservableCollection<ActiveSetupRow> Rows { get; } = [];
    public ObservableCollection<string> Queries { get; } = [];
    public KqlSession Session => _session;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading Active Setup\u2026";

    public void ApplyQuery()
    {
        _bound = KqlHost.Compile(Query, _session, _bound, out var error);
        Apply();
        if (!string.IsNullOrEmpty(error))
            Status = error;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading Active Setup\u2026";
        try
        {
            var found = await Task.Run(ReadAll);
            _all.Clear();
            _all.AddRange(found);
            Apply();
            HostLog.Information(AppMgmtCatalog.ActiveSetupLoaded, "ActiveSetup", $"Active Setup loaded. {_all.Count} components. {_all.Count(row => row.Pending)} pending.");
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
        foreach (var row in _all.Where(Matches).OrderBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase))
            Rows.Add(row);
        var pending = _all.Count(row => row.Pending);
        if (string.IsNullOrWhiteSpace(Query) || _bound is not null)
            Status = $"{Rows.Count} shown. {pending} pending of {_all.Count}";
    }

    private bool Matches(ActiveSetupRow row)
    {
        if (_bound is null)
            return true;
        return _bound.Matches(SetupKql.ActiveSetupAdapter.ToRow(_session, new SetupKql.ActiveSetupFacts
        {
            DisplayName = row.DisplayName,
            Component = row.Component,
            StubPath = row.StubPath,
            Version = row.Version,
            Locale = row.Locale,
            ComponentId = row.ComponentId,
            Installed = row.Installed,
            IsGlobal = row.IsGlobal,
            IsX64 = row.IsX64,
            Pending = row.Pending
        }));
    }

    private static List<ActiveSetupRow> ReadAll()
    {
        var client = RegistryHelper.Local;
        var raw = new List<Raw>();
        Read(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, true, true, raw);
        Read(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, true, false, raw);
        Read(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, false, true, raw);
        Read(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry32, false, false, raw);

        var user = raw.Where(row => !row.IsGlobal)
            .GroupBy(row => (row.IsX64, row.Component))
            .ToDictionary(group => group.Key, group => group.First().Version, comparer: PairComparer.Instance);

        return raw.Select(row => new ActiveSetupRow
        {
            DisplayName = row.DisplayName,
            Component = row.Component,
            StubPath = row.StubPath,
            Version = row.Version,
            Locale = row.Locale,
            ComponentId = row.ComponentId,
            Installed = row.Installed,
            IsGlobal = row.IsGlobal,
            IsX64 = row.IsX64,
            Pending = row.IsGlobal && (!user.TryGetValue((row.IsX64, row.Component), out var seen) || Newer(row.Version, seen))
        }).ToList();
    }

    private static void Read(RegistryClient client, RegistryHiveKind hive, RegistryViewKind view, bool global, bool x64, List<Raw> into)
    {
        foreach (var key in client.ListSubKeys(hive, Root, view, RegistryDetailLevel.Identity))
        {
            var path = key.Path;
            into.Add(new Raw
            {
                Component = key.Name,
                DisplayName = Text(client, hive, view, path, null) is { Length: > 0 } name ? name : key.Name,
                StubPath = Text(client, hive, view, path, "StubPath"),
                Version = Text(client, hive, view, path, "Version"),
                Locale = Text(client, hive, view, path, "Locale"),
                ComponentId = Text(client, hive, view, path, "ComponentID"),
                Installed = Installed(client, hive, view, path),
                IsGlobal = global,
                IsX64 = x64
            });
        }
    }

    private static string Text(RegistryClient client, RegistryHiveKind hive, RegistryViewKind view, string path, string? name)
        => client.GetValue(hive, path, name, view)?.DataText ?? "";

    private static bool Installed(RegistryClient client, RegistryHiveKind hive, RegistryViewKind view, string path)
    {
        var value = client.GetValue(hive, path, "IsInstalled", view);
        if (value?.Data is int flag)
            return flag != 0;
        return true;
    }

    private static bool Newer(string machine, string user)
    {
        var left = Parts(machine);
        var right = Parts(user);
        var count = Math.Max(left.Length, right.Length);
        for (var i = 0; i < count; i++)
        {
            var a = i < left.Length ? left[i] : 0;
            var b = i < right.Length ? right[i] : 0;
            if (a != b)
                return a > b;
        }
        return false;
    }

    private static int[] Parts(string version)
        => version.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .ToArray();

    private sealed class Raw
    {
        public required string Component { get; init; }
        public required string DisplayName { get; init; }
        public string StubPath { get; init; } = "";
        public string Version { get; init; } = "";
        public string Locale { get; init; } = "";
        public string ComponentId { get; init; } = "";
        public bool Installed { get; init; }
        public bool IsGlobal { get; init; }
        public bool IsX64 { get; init; }
    }

    private sealed class PairComparer : IEqualityComparer<(bool IsX64, string Component)>
    {
        public static readonly PairComparer Instance = new();
        public bool Equals((bool IsX64, string Component) x, (bool IsX64, string Component) y)
            => x.IsX64 == y.IsX64 && string.Equals(x.Component, y.Component, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((bool IsX64, string Component) obj)
            => HashCode.Combine(obj.IsX64, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Component));
    }
}
