using System.Collections.ObjectModel;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class FeaturesViewModel : ObservableObject
{
    private readonly List<FeatureRow> _all = [];
    private bool _showEnabled = true;
    private bool _showDisabled = true;
    private bool _showAbsent;

    public ObservableCollection<FeatureRow> Features { get; } = [];

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
            _all.Clear();
            _all.AddRange(rows);
            ApplyFilter();
            HostLog.Information(AppMgmtCatalog.FeaturesLoaded, "Features", $"Windows features loaded. {_all.Count} features.");
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
        var keep = SelectedFeature;
        Features.Clear();
        foreach (var row in _all.Where(Passes).OrderBy(row => row.Caption, StringComparer.OrdinalIgnoreCase))
            Features.Add(row);
        SelectedFeature = Features.Contains(keep!) ? keep : null;
        Status = $"{Features.Count} features";
    }

    private bool Passes(FeatureRow row)
    {
        return row.InstallState switch
        {
            1 => _showEnabled,
            2 => _showDisabled,
            3 => _showAbsent,
            _ => true
        };
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
