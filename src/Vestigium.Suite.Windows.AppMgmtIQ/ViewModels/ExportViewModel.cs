using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class ExportViewModel : ObservableObject
{
    private readonly MainViewModel _packages;
    private readonly FeaturesViewModel _features;
    private readonly BloatwareViewModel _bloatware;
    private readonly ActiveSetupViewModel _setup;

    public ExportViewModel(MainViewModel packages, FeaturesViewModel features, BloatwareViewModel bloatware, ActiveSetupViewModel setup)
    {
        _packages = packages;
        _features = features;
        _bloatware = bloatware;
        _setup = setup;
    }

    [ObservableProperty]
    private bool _includePackages = true;

    [ObservableProperty]
    private bool _includeFeatures = true;

    [ObservableProperty]
    private bool _includeBloatware;

    [ObservableProperty]
    private bool _includeActiveSetup;

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
        if (!IncludePackages && !IncludeFeatures && !IncludeBloatware && !IncludeActiveSetup)
        {
            Status = "Choose at least one inventory.";
            return;
        }

        try
        {
            var rows = Everything ? ExportRows.All : ExportRows.Current;
            LastPath = InventoryExport.Write(new ExportRequest(_packages, _features, _bloatware, _setup, IncludePackages, IncludeFeatures, IncludeBloatware, IncludeActiveSetup, rows, IncludeDetails));
            Status = "Wrote " + LastPath;
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }
}
