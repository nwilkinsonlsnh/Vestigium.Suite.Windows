using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class ExportViewModel : ObservableObject
{
    private readonly MainViewModel _packages;
    private readonly FeaturesViewModel _features;

    public ExportViewModel(MainViewModel packages, FeaturesViewModel features)
    {
        _packages = packages;
        _features = features;
    }

    [ObservableProperty]
    private bool _packagesOnly = true;

    [ObservableProperty]
    private bool _featuresOnly;

    [ObservableProperty]
    private bool _both;

    [ObservableProperty]
    private bool _currentView = true;

    [ObservableProperty]
    private bool _everything;

    [ObservableProperty]
    private bool _includeDetails;

    [ObservableProperty]
    private string _status = "Writes to Desktop\\Vestigium\\Exports\\AppMgmtIQ.";

    [ObservableProperty]
    private string _lastPath = "";

    [RelayCommand]
    private void Export()
    {
        try
        {
            var scope = Both ? ExportScope.Both : FeaturesOnly ? ExportScope.Features : ExportScope.Packages;
            var rows = Everything ? ExportRows.All : ExportRows.Current;
            LastPath = InventoryExport.Write(_packages, _features, scope, rows, IncludeDetails);
            Status = "Wrote " + LastPath;
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }
}
