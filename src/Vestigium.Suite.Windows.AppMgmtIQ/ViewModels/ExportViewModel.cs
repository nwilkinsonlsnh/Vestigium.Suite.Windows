using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class ExportViewModel : ObservableObject
{
    private readonly MainViewModel _packages;
    private readonly FeaturesViewModel _features;
    private readonly BloatwareViewModel _bloatware;
    private readonly ActiveSetupViewModel _setup;

    private readonly AppMgmtSettingsStore _store;
    private bool _loading;

    public ExportViewModel(MainViewModel packages, FeaturesViewModel features, BloatwareViewModel bloatware, ActiveSetupViewModel setup, AppMgmtSettingsStore store)
    {
        _packages = packages;
        _features = features;
        _bloatware = bloatware;
        _setup = setup;
        _store = store;
        Load();
    }

    public void Load()
    {
        _loading = true;
        var data = _store.Load();
        IncludePackages = data.ExportPackages;
        IncludeFeatures = data.ExportFeatures;
        IncludeBloatware = data.ExportBloatware;
        IncludeActiveSetup = data.ExportActiveSetup;
        CurrentView = data.ExportCurrentView;
        Everything = !data.ExportCurrentView;
        IncludeDetails = data.ExportDetails;
        _loading = false;
    }

    [ObservableProperty]
    private bool _includePackages = true;

    [ObservableProperty]
    private bool _includeFeatures = true;

    [ObservableProperty]
    private bool _includeBloatware = true;

    [ObservableProperty]
    private bool _includeActiveSetup = true;

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

    partial void OnIncludePackagesChanged(bool value) => Persist();
    partial void OnIncludeFeaturesChanged(bool value) => Persist();
    partial void OnIncludeBloatwareChanged(bool value) => Persist();
    partial void OnIncludeActiveSetupChanged(bool value) => Persist();
    partial void OnCurrentViewChanged(bool value) => Persist();
    partial void OnIncludeDetailsChanged(bool value) => Persist();

    private void Persist()
    {
        if (_loading)
            return;
        var saved = _store.Load();
        saved.ExportPackages = IncludePackages;
        saved.ExportFeatures = IncludeFeatures;
        saved.ExportBloatware = IncludeBloatware;
        saved.ExportActiveSetup = IncludeActiveSetup;
        saved.ExportCurrentView = CurrentView;
        saved.ExportDetails = IncludeDetails;
        _store.Save(saved);
    }

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
