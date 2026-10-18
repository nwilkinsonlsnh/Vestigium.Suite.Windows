using System.Collections.ObjectModel;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class FeaturesViewModel : ObservableObject
{
    private readonly List<FeatureRow> _all = [];
    private readonly KqlSession _session = AppMgmtKql.Features();
    private KqlBoundQuery? _bound;
    private Dictionary<string, string> _parents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _intent = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FeatureNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private bool _expandOnLoad;
    private string _source = "flat";
    private bool _showEnabled = true;
    private bool _showDisabled = true;
    private bool _showAbsent;

    public ObservableCollection<FeatureNode> Roots { get; } = [];
    public ObservableCollection<string> Queries { get; } = [];
    public KqlSession Session => _session;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private FeatureRow? _selectedFeature;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading Windows features\u2026";

    [ObservableProperty]
    private bool _showEnabledFlag = true;

    [ObservableProperty]
    private bool _showDisabledFlag = true;

    [ObservableProperty]
    private bool _showAbsentFlag;

    [ObservableProperty]
    private bool _hasPlan;

    public ObservableCollection<FeatureChange> Enables { get; } = [];
    public ObservableCollection<FeatureChange> Disables { get; } = [];

    public void ApplyQuery()
    {
        _bound = KqlHost.Compile(Query, _session, _bound, out var error);
        ApplyFilter();
        if (!string.IsNullOrEmpty(error))
            Status = error;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading Windows features\u2026";
        try
        {
            var rows = await Task.Run(ReadAll);
            var dism = await Task.Run(() => DismParents.Read());
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Description))
                    continue;
                var text = DismParents.BestText(row.Name);
                if (!string.IsNullOrWhiteSpace(text))
                    row.Description = text;
            }
            _all.Clear();
            _all.AddRange(rows);
            if (dism is { Count: > 0 })
            {
                _parents = dism;
                _source = "DISM";
            }
            else
            {
                _parents = FallbackParents(rows);
                _source = "dialog map";
            }
            ApplyFilter();
            SetExpanded(_expandOnLoad);
            HostLog.Information(AppMgmtCatalog.FeaturesLoaded, "Features", $"Windows features loaded. {_all.Count} features. Tree from {_source}.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    partial void OnShowEnabledFlagChanged(bool value)
    {
        _showEnabled = value;
        ApplyFilter();
    }

    partial void OnShowDisabledFlagChanged(bool value)
    {
        _showDisabled = value;
        ApplyFilter();
    }

    partial void OnShowAbsentFlagChanged(bool value)
    {
        _showAbsent = value;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _nodes.Clear();
        Roots.Clear();
        var byName = _all.ToDictionary(row => row.Name, StringComparer.OrdinalIgnoreCase);
        var children = new Dictionary<string, List<FeatureRow>>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<FeatureRow>();
        foreach (var row in _all)
        {
            if (_parents.TryGetValue(row.Name, out var parent) && byName.ContainsKey(parent) && !string.Equals(parent, row.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!children.TryGetValue(parent, out var list))
                    children[parent] = list = [];
                list.Add(row);
            }
            else
            {
                roots.Add(row);
            }
        }

        foreach (var root in roots.OrderBy(row => row.Caption, StringComparer.OrdinalIgnoreCase))
        {
            var node = Build(root, children);
            foreach (var visible in TakeVisible(node))
                Roots.Add(visible);
        }
        if (string.IsNullOrWhiteSpace(Query) || _bound is not null)
            Status = $"{Roots.Count} groups, {_all.Count} features ({_source})";
    }

    private FeatureNode? Build(FeatureRow row, Dictionary<string, List<FeatureRow>> children)
    {
        var parent = _parents.TryGetValue(row.Name, out var name) ? name : "";
        var node = new FeatureNode
        {
            Name = row.Name,
            Caption = row.Caption,
            State = row.State,
            IsEnabled = row.IsEnabled,
            Parent = parent,
            Feature = row
        };
        node.ResetDesired();
        _nodes[node.Name] = node;
        if (children.TryGetValue(row.Name, out var kids))
        {
            foreach (var kid in kids.OrderBy(k => k.Caption, StringComparer.OrdinalIgnoreCase))
            {
                var child = Build(kid, children);
                if (child is not null)
                    node.Children.Add(child);
            }
        }
        return node;
    }

    private List<FeatureNode> TakeVisible(FeatureNode node)
    {
        var kids = node.Children.ToList();
        node.Children.Clear();
        var kept = new List<FeatureNode>();
        foreach (var child in kids)
            kept.AddRange(TakeVisible(child));
        var parent = _parents.TryGetValue(node.Name, out var name) ? name : "";
        if (node.Feature is null || !Matches(node.Feature, parent))
            return kept;
        foreach (var child in kept)
            node.Children.Add(child);
        return [node];
    }

    private bool Matches(FeatureRow row, string parent)
    {
        if (!Visible(row))
            return false;
        if (_bound is null)
            return true;
        return _bound.Matches(FeatureAdapter.ToRow(_session, new FeatureFacts
        {
            Caption = row.Caption,
            Name = row.Name,
            State = row.State,
            IsEnabled = row.IsEnabled,
            Parent = parent
        }));
    }

    private bool Visible(FeatureRow row)
    {
        return row.InstallState switch
        {
            1 => _showEnabled,
            2 => _showDisabled,
            3 => _showAbsent,
            _ => true
        };
    }

    private static Dictionary<string, string> FallbackParents(List<FeatureRow> rows)
    {
        var names = rows.Select(row => row.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var parents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (child, parent) in DialogParents)
        {
            if (names.Contains(child) && names.Contains(parent))
                parents[child] = parent;
        }
        foreach (var row in rows)
        {
            if (parents.ContainsKey(row.Name))
                continue;
            var parent = LongestPrefix(row.Name, names);
            if (parent is not null)
                parents[row.Name] = parent;
        }
        return parents;
    }

    private static string? LongestPrefix(string name, HashSet<string> names)
    {
        string? best = null;
        foreach (var candidate in names)
        {
            if (candidate.Length >= name.Length || string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!name.StartsWith(candidate + "-", StringComparison.OrdinalIgnoreCase))
                continue;
            if (best is null || candidate.Length > best.Length)
                best = candidate;
        }
        return best;
    }

    private static readonly (string Child, string Parent)[] DialogParents =
    [
        ("WCF-HTTP-Activation", "NetFx3"),
        ("WCF-NonHTTP-Activation", "NetFx3"),
        ("NetFx4Extended-ASPNET45", "NetFx4-AdvSrvs"),
        ("WCF-Services45", "NetFx4-AdvSrvs"),
        ("WCF-HTTP-Activation45", "WCF-Services45"),
        ("WCF-MSMQ-Activation45", "WCF-Services45"),
        ("WCF-Pipe-Activation45", "WCF-Services45"),
        ("WCF-TCP-Activation45", "WCF-Services45"),
        ("WCF-TCP-PortSharing45", "WCF-Services45"),
        ("Microsoft-Hyper-V-Management-Clients", "Microsoft-Hyper-V-All"),
        ("Microsoft-Hyper-V-Management-PowerShell", "Microsoft-Hyper-V-All"),
        ("Microsoft-Hyper-V", "Microsoft-Hyper-V-All"),
        ("Microsoft-Hyper-V-Hypervisor", "Microsoft-Hyper-V"),
        ("Microsoft-Hyper-V-Services", "Microsoft-Hyper-V"),
        ("IIS-WebServer", "IIS-WebServerRole"),
        ("IIS-FTPServer", "IIS-WebServerRole"),
        ("IIS-ManagementScriptingTools", "IIS-WebServerRole"),
        ("IIS-ManagementService", "IIS-WebServerRole"),
        ("IIS-IIS6ManagementCompatibility", "IIS-WebServerRole"),
        ("WAS-WindowsActivationService", "WAS-ProcessModel"),
        ("WAS-NetFxEnvironment", "WAS-WindowsActivationService"),
        ("WAS-ConfigurationAPI", "WAS-WindowsActivationService"),
        ("SMB1Protocol-Client", "SMB1Protocol"),
        ("SMB1Protocol-Server", "SMB1Protocol"),
        ("SMB1Protocol-Deprecation", "SMB1Protocol"),
        ("Printing-InternetPrinting-Client", "Printing-Foundation-Features"),
        ("Printing-LPDPrintService", "Printing-Foundation-Features"),
        ("Printing-LPRPortMonitor", "Printing-Foundation-Features"),
        ("FaxServicesClientPackage", "Printing-Foundation-Features"),
        ("MediaPlayback", "MediaPlayback")
    ];

    private static List<FeatureRow> ReadAll()
    {
        var rows = new List<FeatureRow>();
        using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name, Caption, InstallState, Description, Status FROM Win32_OptionalFeature");
        foreach (ManagementObject item in searcher.Get())
        {
            using (item)
            {
                var name = item["Name"]?.ToString() ?? "";
                var caption = item["Caption"]?.ToString();
                rows.Add(new FeatureRow
                {
                    Name = name,
                    Caption = string.IsNullOrWhiteSpace(caption) ? name : caption,
                    InstallState = item["InstallState"] is uint state ? (int)state : 4,
                    Description = item["Description"]?.ToString() ?? "",
                    Status = item["Status"]?.ToString() ?? ""
                });
            }
        }
        return rows;
    }

    public void SetDesired(FeatureNode node, bool desired)
    {
        if (desired == node.IsEnabled)
            _intent.Remove(node.Name);
        else
            _intent[node.Name] = desired;
        RebuildPlan();
    }

    public void SetExpanded(bool expanded)
    {
        _expandOnLoad = expanded;
        foreach (var node in _nodes.Values)
            node.IsExpanded = expanded && node.Children.Count > 0;
    }

    public bool? UniformExpansion
    {
        get
        {
            var branches = _nodes.Values.Where(node => node.Children.Count > 0).ToList();
            if (branches.Count == 0)
                return null;
            if (branches.All(node => node.IsExpanded))
                return true;
            return branches.All(node => !node.IsExpanded) ? false : null;
        }
    }

    public void SetBranch(FeatureNode node, bool desired)
    {
        foreach (var item in node.SelfAndDescendants())
        {
            if (desired == item.IsEnabled)
                _intent.Remove(item.Name);
            else
                _intent[item.Name] = desired;
        }
        RebuildPlan();
    }

    private void RebuildPlan()
    {
        foreach (var node in _nodes.Values)
            node.ResetDesired();
        Enables.Clear();
        Disables.Clear();

        foreach (var (name, desired) in _intent)
        {
            if (!_nodes.TryGetValue(name, out var node))
                continue;
            if (desired)
                MarkEnable(node, "");
            else
                MarkDisable(node, "");
        }

        foreach (var node in _nodes.Values)
            node.NotifyCheckState();
        HasPlan = Enables.Count + Disables.Count > 0;
        Status = HasPlan
            ? $"{Enables.Count} to enable, {Disables.Count} to disable"
            : $"{Roots.Count} groups, {_all.Count} features ({_source})";
    }

    private void MarkEnable(FeatureNode node, string reason)
    {
        if (!string.IsNullOrEmpty(node.Parent) && _nodes.TryGetValue(node.Parent, out var parent) && !parent.IsEnabled)
            MarkEnable(parent, $"required by {node.Caption}");
        if (node.IsEnabled || Enables.Any(change => string.Equals(change.Name, node.Name, StringComparison.OrdinalIgnoreCase)))
            return;
        node.DesiredEnabled = true;
        node.Reason = reason;
        Enables.Add(new FeatureChange(node.Caption, node.Name, reason, node.IsAbsent));
    }

    private void MarkDisable(FeatureNode node, string reason)
    {
        foreach (var child in node.Children)
        {
            if (child.IsEnabled)
                MarkDisable(child, $"required by {node.Caption}");
        }
        if (!node.IsEnabled || Disables.Any(change => string.Equals(change.Name, node.Name, StringComparison.OrdinalIgnoreCase)))
            return;
        node.DesiredEnabled = false;
        node.Reason = reason;
        Disables.Add(new FeatureChange(node.Caption, node.Name, reason, false));
    }
}

public sealed record FeatureChange(string Caption, string Name, string Reason, bool IsAbsent);
