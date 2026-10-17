using System.Collections.ObjectModel;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class FeaturesViewModel : ObservableObject
{
    private readonly List<FeatureRow> _all = [];
    private Dictionary<string, string> _parents = new(StringComparer.OrdinalIgnoreCase);
    private bool _showEnabled = true;
    private bool _showDisabled = true;
    private bool _showAbsent;

    public ObservableCollection<FeatureNode> Roots { get; } = [];

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

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading Windows features\u2026";
        try
        {
            var rows = await Task.Run(ReadAll);
            var parents = await Task.Run(() => DismParents.Read());
            _all.Clear();
            _all.AddRange(rows);
            _parents = parents ?? FamilyParents(rows);
            ApplyFilter();
            var source = parents is null ? "name family" : "DISM parents";
            HostLog.Information(AppMgmtCatalog.FeaturesLoaded, "Features", $"Windows features loaded. {_all.Count} features. Tree from {source}.");
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
        Roots.Clear();
        var byName = _all.ToDictionary(row => row.Name, StringComparer.OrdinalIgnoreCase);
        var children = new Dictionary<string, List<FeatureRow>>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<FeatureRow>();
        foreach (var row in _all)
        {
            if (_parents.TryGetValue(row.Name, out var parent) && byName.ContainsKey(parent))
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
            if (node is not null)
                Roots.Add(node);
        }
        Status = $"{Roots.Count} groups";
    }

    private FeatureNode? Build(FeatureRow row, Dictionary<string, List<FeatureRow>> children)
    {
        var node = new FeatureNode
        {
            Name = row.Name,
            Caption = row.Caption,
            State = row.State,
            IsEnabled = row.IsEnabled,
            Feature = row
        };
        if (children.TryGetValue(row.Name, out var kids))
        {
            foreach (var kid in kids.OrderBy(k => k.Caption, StringComparer.OrdinalIgnoreCase))
            {
                var child = Build(kid, children);
                if (child is not null)
                    node.Children.Add(child);
            }
        }
        return Visible(row) || node.Children.Count > 0 ? node : null;
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

    private static Dictionary<string, string> FamilyParents(List<FeatureRow> rows)
    {
        var names = rows.Select(row => row.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var parents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var dash = row.Name.LastIndexOf('-');
            if (dash <= 0)
                continue;
            var family = row.Name[..dash];
            if (names.Contains(family))
                parents[row.Name] = family;
        }
        return parents;
    }

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
}
