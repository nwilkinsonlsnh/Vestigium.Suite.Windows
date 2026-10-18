using AppKql = Vestigium.Helpers.Kql.AppMgmt;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class BloatwareViewModel : ObservableObject
{
    private const string Applications = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications";
    private const string Inbox = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\InboxApplications";
    private const string UserPackages = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";

    private readonly List<BloatwareRow> _all = [];
    private readonly KqlSession _session = AppKql.AppMgmtKql.Bloatware();
    private KqlBoundQuery? _bound;

    public ObservableCollection<BloatwareRow> Rows { get; } = [];
    public IReadOnlyList<BloatwareRow> All => _all;
    public ObservableCollection<QueryMruRow> Queries { get; } = [];
    public KqlSession Session => _session;

    public int MruLimit { get; set; } = 10;
    public int StickyLimit { get; set; } = 3;
    public event Action? MruChanged;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private BloatwareRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading AppX packages\u2026";

    public void ApplyQuery()
    {
        _bound = KqlHost.Compile(Query, _session, _bound, out var error);
        Apply();
        QueryMru.Record(Queries, Query, MruLimit, StickyLimit);
        MruChanged?.Invoke();
        if (!string.IsNullOrEmpty(error))
            Status = error;
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
        Status = "Reading AppX packages\u2026";
        try
        {
            var found = await Task.Run(ReadInstalled);
            _all.Clear();
            _all.AddRange(found);
            Apply();
            HostLog.Information(AppMgmtCatalog.BloatwareLoaded, "Bloatware", $"Bloatware catalog matched. {_all.Count(row => row.Installed)} present of {_all.Count}.");
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
        foreach (var row in _all.Where(Matches).OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase))
            Rows.Add(row);
        var present = _all.Count(row => row.Installed);
        if (string.IsNullOrWhiteSpace(Query) || _bound is not null)
            Status = $"{Rows.Count} shown. {present} present of {_all.Count} catalog entries";
    }

    private bool Matches(BloatwareRow row)
    {
        if (_bound is null)
            return true;
        return _bound.Matches(AppKql.BloatwareAdapter.ToRow(_session, new AppKql.BloatwareFacts
        {
            Name = row.Name,
            Family = row.Family,
            Category = row.Category,
            Installed = row.Installed,
            Provisioned = row.Provisioned,
            Package = row.Package
        }));
    }

    private static List<BloatwareRow> ReadInstalled()
    {
        var client = RegistryHelper.Local;
        var installed = Families(client, RegistryHiveKind.LocalMachine, Applications)
            .Concat(Families(client, RegistryHiveKind.CurrentUser, UserPackages))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var provisioned = Families(client, RegistryHiveKind.LocalMachine, Inbox).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var packages = PackageNames(client);

        return BloatwareCatalog.Entries.Select(entry => new BloatwareRow
        {
            Name = entry.Name,
            Family = entry.Family,
            Category = entry.Category,
            Installed = installed.Any(entry.Matches),
            Provisioned = provisioned.Any(entry.Matches),
            Package = PackageFor(packages, entry)
        }).ToList();
    }

    private static IEnumerable<string> Families(RegistryClient client, RegistryHiveKind hive, string key)
    {
        foreach (var row in client.ListSubKeys(hive, key, RegistryViewKind.Registry64, RegistryDetailLevel.Identity))
            yield return Family(row.Name);
    }

    private static Dictionary<string, string> PackageNames(RegistryClient client)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in client.ListSubKeys(RegistryHiveKind.LocalMachine, Applications, RegistryViewKind.Registry64, RegistryDetailLevel.Identity))
            map.TryAdd(Family(row.Name), row.Name);
        foreach (var row in client.ListSubKeys(RegistryHiveKind.CurrentUser, UserPackages, RegistryViewKind.Default, RegistryDetailLevel.Identity))
            map.TryAdd(Family(row.Name), row.Name);
        return map;
    }

    private static string PackageFor(Dictionary<string, string> packages, BloatwareEntry entry)
    {
        if (packages.TryGetValue(entry.Family, out var full))
            return full;
        foreach (var alias in entry.Aliases)
            if (packages.TryGetValue(alias, out full))
                return full;
        return "";
    }

    private static string Family(string fullName)
    {
        var at = fullName.IndexOf('_');
        return at > 0 ? fullName[..at] : fullName;
    }
}
