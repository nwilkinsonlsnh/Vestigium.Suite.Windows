using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class ExportViewModel : ObservableObject
{
    private readonly MainViewModel _packages;
    private readonly FeaturesViewModel _features;
    private readonly BloatwareViewModel _bloatware;
    private readonly ActiveSetupViewModel _setup;
    private readonly StartupViewModel _startup;
    private readonly FootprintsViewModel _footprints;
    private readonly AppMgmtSettingsStore _store;
    private bool _loading;

    public ExportViewModel(
        MainViewModel packages,
        FeaturesViewModel features,
        BloatwareViewModel bloatware,
        ActiveSetupViewModel setup,
        StartupViewModel startup,
        FootprintsViewModel footprints,
        AppMgmtSettingsStore store)
    {
        _packages = packages;
        _features = features;
        _bloatware = bloatware;
        _setup = setup;
        _startup = startup;
        _footprints = footprints;
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
        IncludeStartup = data.ExportStartup;
        IncludeFootprints = data.ExportFootprints;
        PackagesCurrent = data.ExportPackagesCurrent;
        FeaturesCurrent = data.ExportFeaturesCurrent;
        BloatwareCurrent = data.ExportBloatwareCurrent;
        ActiveSetupCurrent = data.ExportActiveSetupCurrent;
        StartupCurrent = data.ExportStartupCurrent;
        FootprintsCurrent = data.ExportFootprintsCurrent;
        IncludeDetails = data.ExportDetails;
        OpenFileAfterExport = data.ExportOpenFile;
        OpenFolderAfterExport = data.ExportOpenFolder;
        _loading = false;
    }

    [ObservableProperty] private bool _includePackages = true;
    [ObservableProperty] private bool _includeFeatures = true;
    [ObservableProperty] private bool _includeBloatware = true;
    [ObservableProperty] private bool _includeActiveSetup = true;
    [ObservableProperty] private bool _includeStartup = true;
    [ObservableProperty] private bool _includeFootprints = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(PackagesEverything))] private bool _packagesCurrent = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FeaturesEverything))] private bool _featuresCurrent = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(BloatwareEverything))] private bool _bloatwareCurrent = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ActiveSetupEverything))] private bool _activeSetupCurrent = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(StartupEverything))] private bool _startupCurrent = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FootprintsEverything))] private bool _footprintsCurrent = true;
    [ObservableProperty] private bool _includeDetails;
    [ObservableProperty] private bool _openFileAfterExport;
    [ObservableProperty] private bool _openFolderAfterExport;
    [ObservableProperty] private string _status = "Writes to Desktop\\Vestigium\\Exports\\AppMgmtIQ.";
    [ObservableProperty] private string _lastPath = "";

    public bool PackagesEverything { get => !PackagesCurrent; set => PackagesCurrent = !value; }
    public bool FeaturesEverything { get => !FeaturesCurrent; set => FeaturesCurrent = !value; }
    public bool BloatwareEverything { get => !BloatwareCurrent; set => BloatwareCurrent = !value; }
    public bool ActiveSetupEverything { get => !ActiveSetupCurrent; set => ActiveSetupCurrent = !value; }
    public bool StartupEverything { get => !StartupCurrent; set => StartupCurrent = !value; }
    public bool FootprintsEverything { get => !FootprintsCurrent; set => FootprintsCurrent = !value; }

    partial void OnIncludePackagesChanged(bool value) => Persist();
    partial void OnIncludeFeaturesChanged(bool value) => Persist();
    partial void OnIncludeBloatwareChanged(bool value) => Persist();
    partial void OnIncludeActiveSetupChanged(bool value) => Persist();
    partial void OnIncludeStartupChanged(bool value) => Persist();
    partial void OnIncludeFootprintsChanged(bool value) => Persist();
    partial void OnPackagesCurrentChanged(bool value) => Persist();
    partial void OnFeaturesCurrentChanged(bool value) => Persist();
    partial void OnBloatwareCurrentChanged(bool value) => Persist();
    partial void OnActiveSetupCurrentChanged(bool value) => Persist();
    partial void OnStartupCurrentChanged(bool value) => Persist();
    partial void OnFootprintsCurrentChanged(bool value) => Persist();
    partial void OnIncludeDetailsChanged(bool value) => Persist();
    partial void OnOpenFileAfterExportChanged(bool value) => Persist();
    partial void OnOpenFolderAfterExportChanged(bool value) => Persist();

    private void Persist()
    {
        if (_loading)
            return;
        var saved = _store.Load();
        saved.ExportPackages = IncludePackages;
        saved.ExportFeatures = IncludeFeatures;
        saved.ExportBloatware = IncludeBloatware;
        saved.ExportActiveSetup = IncludeActiveSetup;
        saved.ExportStartup = IncludeStartup;
        saved.ExportFootprints = IncludeFootprints;
        saved.ExportPackagesCurrent = PackagesCurrent;
        saved.ExportFeaturesCurrent = FeaturesCurrent;
        saved.ExportBloatwareCurrent = BloatwareCurrent;
        saved.ExportActiveSetupCurrent = ActiveSetupCurrent;
        saved.ExportStartupCurrent = StartupCurrent;
        saved.ExportFootprintsCurrent = FootprintsCurrent;
        saved.ExportDetails = IncludeDetails;
        saved.ExportOpenFile = OpenFileAfterExport;
        saved.ExportOpenFolder = OpenFolderAfterExport;
        _store.Save(saved);
    }

    [RelayCommand]
    private void Export()
    {
        if (!IncludePackages && !IncludeFeatures && !IncludeBloatware && !IncludeActiveSetup && !IncludeStartup && !IncludeFootprints)
        {
            Status = "Choose at least one inventory.";
            return;
        }

        try
        {
            LastPath = InventoryExport.Write(new ExportRequest(
                _packages, _features, _bloatware, _setup, _startup, _footprints,
                IncludePackages, PackagesCurrent,
                IncludeFeatures, FeaturesCurrent,
                IncludeBloatware, BloatwareCurrent,
                IncludeActiveSetup, ActiveSetupCurrent,
                IncludeStartup, StartupCurrent,
                IncludeFootprints, FootprintsCurrent,
                IncludeDetails));
            Status = "Wrote " + LastPath;
            OpenAfterExport(LastPath);
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private void OpenAfterExport(string path)
    {
        if (OpenFolderAfterExport)
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        if (OpenFileAfterExport)
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
