using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeManager _themes;
    private readonly MainViewModel _host;
    private readonly AppMgmtSettingsStore _store;
    private FeaturesViewModel? _features;
    private BloatwareViewModel? _bloatware;
    private ActiveSetupViewModel? _setup;
    private StartupViewModel? _startup;
    private FootprintsViewModel? _footprints;
    private bool _loading;

    public SettingsViewModel(ThemeManager themes, MainViewModel host, AppMgmtSettingsStore store)
    {
        _themes = themes;
        _host = host;
        _host.FiltersChanged += SyncFromHost;
        _store = store;
        _selectedThemeId = themes.Current?.Id ?? themes.AvailableThemes.FirstOrDefault()?.Id;
        _selectedDateFormat = DisplayFormats.DefaultDateFormat;
        themes.ThemeChanged += (_, _) =>
        {
            var id = themes.Current?.Id;
            if (_selectedThemeId != id)
            {
                _selectedThemeId = id;
                OnPropertyChanged(nameof(SelectedThemeId));
            }
        };
    }

    public MainViewModel Host => _host;

    public void Attach(FeaturesViewModel features) => _features = features;

    public void BindMrus(FeaturesViewModel features, BloatwareViewModel bloatware, ActiveSetupViewModel setup, StartupViewModel startup, FootprintsViewModel footprints)
    {
        _features = features;
        _bloatware = bloatware;
        _setup = setup;
        _startup = startup;
        _footprints = footprints;
        _host.MruChanged += SaveMrus;
        features.MruChanged += SaveMrus;
        bloatware.MruChanged += SaveMrus;
        setup.MruChanged += SaveMrus;
        startup.MruChanged += SaveMrus;
        footprints.MruChanged += SaveMrus;
    }

    public IReadOnlyList<ThemeDefinition> Themes => _themes.AvailableThemes;

    public IReadOnlyList<DateFormatOption> DateFormats => DisplayFormats.DateFormats;

    public IReadOnlyList<string> MruTabs { get; } = ["Packages", "Features", "Built-In (Packages)", "Active Setup", "Startup", "Footprints"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AppMgmtPageOpen))]
    [NotifyPropertyChangedFor(nameof(ThemesPageOpen))]
    [NotifyPropertyChangedFor(nameof(MrusPageOpen))]
    private string _settingsPage = "AppMgmtIQ";

    public bool AppMgmtPageOpen
    {
        get => SettingsPage == "AppMgmtIQ";
        set { if (value) SettingsPage = "AppMgmtIQ"; }
    }

    public bool ThemesPageOpen
    {
        get => SettingsPage == "Themes";
        set { if (value) SettingsPage = "Themes"; }
    }

    public bool MrusPageOpen
    {
        get => SettingsPage == "MRUs";
        set { if (value) SettingsPage = "MRUs"; }
    }

    [ObservableProperty]
    private string? _selectedThemeId;

    [ObservableProperty]
    private bool _showGlobal = true;

    [ObservableProperty]
    private bool _showPerUser = true;

    [ObservableProperty]
    private bool _showX64 = true;

    [ObservableProperty]
    private bool _showX86 = true;

    [ObservableProperty]
    private bool _showSystemComponents;

    [ObservableProperty]
    private bool _showWindowsInstaller = true;

    [ObservableProperty]
    private bool _showOtherInstallers = true;

    [ObservableProperty]
    private bool _featuresExpanded;

    [ObservableProperty]
    private DateFormatOption _selectedDateFormat;

    [ObservableProperty]
    private decimal _decimalPlaces = 4;

    [ObservableProperty]
    private decimal _mruLimit = 10;

    [ObservableProperty]
    private decimal _stickyLimit = 3;

    [ObservableProperty]
    private string _selectedMruTab = "Packages";

    [ObservableProperty]
    private string _newMruText = "";

    [ObservableProperty]
    private QueryMruRow? _selectedMru;

    public ObservableCollection<QueryMruRow> CurrentMrus => SelectedMruTab switch
    {
        "Features" => _features?.Queries ?? [],
        "Bloatware" => _bloatware?.Queries ?? [],
        "Active Setup" => _setup?.Queries ?? [],
        "Startup" => _startup?.Queries ?? [],
        "Footprints" => _footprints?.Queries ?? [],
        _ => _host.Queries
    };

    public bool HasSelection => _host.SelectedPackage is not null;

    private void SyncFromHost()
    {
        if (_loading)
            return;
        _loading = true;
        ShowGlobal = _host.ShowGlobal;
        ShowPerUser = _host.ShowPerUser;
        ShowX64 = _host.ShowX64;
        ShowX86 = _host.ShowX86;
        ShowSystemComponents = _host.ShowSystemComponents;
        ShowWindowsInstaller = _host.ShowWindowsInstaller;
        ShowOtherInstallers = _host.ShowOtherInstallers;
        _loading = false;
        Push();
        Save();
    }

    public void Load()
    {
        _loading = true;
        var data = _store.Load();
        ShowGlobal = data.ShowGlobal;
        ShowPerUser = data.ShowPerUser;
        ShowX64 = data.ShowX64;
        ShowX86 = data.ShowX86;
        ShowSystemComponents = data.ShowSystemComponents;
        ShowWindowsInstaller = data.ShowWindowsInstaller;
        ShowOtherInstallers = data.ShowOtherInstallers;
        FeaturesExpanded = data.FeaturesExpanded;
        DecimalPlaces = Math.Clamp(data.DecimalPlaces, 0, 8);
        MruLimit = Math.Clamp(data.MruLimit, 0, 50);
        StickyLimit = Math.Clamp(data.StickyLimit, 0, 20);
        SelectedDateFormat = DateFormats.FirstOrDefault(f => f.Label == data.DateFormat) ?? DisplayFormats.DefaultDateFormat;
        if (!string.IsNullOrWhiteSpace(data.ThemeId))
            SelectedThemeId = data.ThemeId;
        Fill(_host.Queries, data.PackageMrus);
        if (_features is not null)
            Fill(_features.Queries, data.FeatureMrus);
        if (_bloatware is not null)
            Fill(_bloatware.Queries, data.BloatwareMrus);
        if (_setup is not null)
            Fill(_setup.Queries, data.ActiveSetupMrus);
        if (_startup is not null)
            Fill(_startup.Queries, data.StartupMrus);
        if (_footprints is not null)
            Fill(_footprints.Queries, data.FootprintsMrus);
        ApplyLimits();
        _loading = false;
        Push();
        OnPropertyChanged(nameof(CurrentMrus));
    }

    private static void Fill(ObservableCollection<QueryMruRow> target, List<QueryMruEntry> source)
    {
        target.Clear();
        foreach (var entry in source)
            target.Add(new QueryMruRow { Text = entry.Text, IsSticky = entry.IsSticky });
    }

    private void ApplyLimits()
    {
        var n = (int)MruLimit;
        var x = (int)StickyLimit;
        _host.MruLimit = n;
        _host.StickyLimit = x;
        if (_features is not null) { _features.MruLimit = n; _features.StickyLimit = x; }
        if (_bloatware is not null) { _bloatware.MruLimit = n; _bloatware.StickyLimit = x; }
        if (_setup is not null) { _setup.MruLimit = n; _setup.StickyLimit = x; }
        if (_startup is not null) { _startup.MruLimit = n; _startup.StickyLimit = x; }
        if (_footprints is not null) { _footprints.MruLimit = n; _footprints.StickyLimit = x; }
        QueryMru.Trim(_host.Queries, n);
        if (_features is not null) QueryMru.Trim(_features.Queries, n);
        if (_bloatware is not null) QueryMru.Trim(_bloatware.Queries, n);
        if (_setup is not null) QueryMru.Trim(_setup.Queries, n);
        if (_startup is not null) QueryMru.Trim(_startup.Queries, n);
        if (_footprints is not null) QueryMru.Trim(_footprints.Queries, n);
    }

    public void RefreshSelection()
        => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    private void OpenDetails()
    {
        if (_host.SelectedPackage is null)
            return;
        OpenDetailsRequested?.Invoke(this, _host.SelectedPackage);
    }

    public event EventHandler<PackageRow>? OpenDetailsRequested;

    partial void OnSelectedThemeIdChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        var previous = _themes.Current?.Id;
        if (previous != value)
            _themes.SwitchTheme(value);
        Changed("Theme", previous, value);
    }

    partial void OnShowGlobalChanged(bool value) => Changed("Global packages", (!value).ToString(), value.ToString());
    partial void OnShowPerUserChanged(bool value) => Changed("Per-user packages", (!value).ToString(), value.ToString());
    partial void OnShowX64Changed(bool value) => Changed("x64", (!value).ToString(), value.ToString());
    partial void OnShowX86Changed(bool value) => Changed("x86", (!value).ToString(), value.ToString());
    partial void OnShowSystemComponentsChanged(bool value) => Changed("System components", (!value).ToString(), value.ToString());
    partial void OnShowWindowsInstallerChanged(bool value) => Changed("Windows Installer", (!value).ToString(), value.ToString());
    partial void OnShowOtherInstallersChanged(bool value) => Changed("Other installers", (!value).ToString(), value.ToString());
    partial void OnFeaturesExpandedChanged(bool value)
    {
        _features?.SetExpanded(value);
        Changed("Windows features", (!value) ? "Expanded" : "Collapsed", value ? "Expanded" : "Collapsed");
    }
    partial void OnSelectedDateFormatChanged(DateFormatOption value) => Changed("Date format", null, value?.Label);

    partial void OnDecimalPlacesChanged(decimal value)
    {
        var clamped = Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 8);
        if (clamped != value)
        {
            DecimalPlaces = clamped;
            return;
        }
        Changed("Decimal places", null, clamped.ToString());
    }

    partial void OnMruLimitChanged(decimal value)
    {
        var clamped = Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 50);
        if (clamped != value)
        {
            MruLimit = clamped;
            return;
        }
        ApplyLimits();
        Changed("MRU limit", null, clamped.ToString());
    }

    partial void OnStickyLimitChanged(decimal value)
    {
        var clamped = Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 20);
        if (clamped != value)
        {
            StickyLimit = clamped;
            return;
        }
        ApplyLimits();
        Changed("Sticky limit", null, clamped.ToString());
    }

    partial void OnSelectedMruTabChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentMrus));
        SelectedMru = null;
    }

    [RelayCommand]
    private void AddMru()
    {
        var text = NewMruText.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;
        var list = CurrentMrus;
        QueryMru.Record(list, text, (int)MruLimit, (int)StickyLimit);
        NewMruText = "";
        SaveMrus();
    }

    [RelayCommand]
    private void DeleteMru()
    {
        if (SelectedMru is null)
            return;
        CurrentMrus.Remove(SelectedMru);
        SelectedMru = null;
        SaveMrus();
    }

    [RelayCommand]
    private void MoveMruUp() => Move(-1);

    [RelayCommand]
    private void MoveMruDown() => Move(1);

    [RelayCommand]
    private void ToggleSticky()
    {
        if (SelectedMru is null)
            return;
        if (!QueryMru.ToggleSticky(CurrentMrus, SelectedMru, (int)StickyLimit))
            return;
        SaveMrus();
    }

    private void Move(int delta)
    {
        if (SelectedMru is null)
            return;
        QueryMru.Move(CurrentMrus, SelectedMru, delta);
        SaveMrus();
    }

    private void SaveMrus()
    {
        if (_loading)
            return;
        var saved = _store.Load();
        saved.MruLimit = (int)MruLimit;
        saved.StickyLimit = (int)StickyLimit;
        saved.PackageMrus = ToEntries(_host.Queries);
        saved.FeatureMrus = ToEntries(_features?.Queries);
        saved.BloatwareMrus = ToEntries(_bloatware?.Queries);
        saved.ActiveSetupMrus = ToEntries(_setup?.Queries);
        saved.StartupMrus = ToEntries(_startup?.Queries);
        saved.FootprintsMrus = ToEntries(_footprints?.Queries);
        _store.Save(saved);
    }

    private static List<QueryMruEntry> ToEntries(ObservableCollection<QueryMruRow>? list)
        => list?.Select(row => new QueryMruEntry { Text = row.Text, IsSticky = row.IsSticky }).ToList() ?? [];

    private void Changed(string name, string? previous, string? next)
    {
        if (_loading)
            return;

        Push();

        HostLog.Information(
            AppMgmtCatalog.SettingsChanged,
            "Settings",
            $"{name}: current '{previous ?? "(unset)"}' new '{next ?? "(unset)"}'");

        Save();

        HostLog.Information(AppMgmtCatalog.SettingsSaved, "Settings", $"Settings saved. {_store.FilePath}");
    }

    private void Save()
    {
        var saved = _store.Load();
        saved.ThemeId = SelectedThemeId;
        saved.ShowGlobal = ShowGlobal;
        saved.ShowPerUser = ShowPerUser;
        saved.ShowX64 = ShowX64;
        saved.ShowX86 = ShowX86;
        saved.ShowSystemComponents = ShowSystemComponents;
        saved.ShowWindowsInstaller = ShowWindowsInstaller;
        saved.ShowOtherInstallers = ShowOtherInstallers;
        saved.FeaturesExpanded = FeaturesExpanded;
        saved.DateFormat = SelectedDateFormat?.Label ?? DisplayFormats.DefaultDateFormat.Label;
        saved.DecimalPlaces = (int)DecimalPlaces;
        saved.MruLimit = (int)MruLimit;
        saved.StickyLimit = (int)StickyLimit;
        saved.PackageMrus = ToEntries(_host.Queries);
        saved.FeatureMrus = ToEntries(_features?.Queries);
        saved.BloatwareMrus = ToEntries(_bloatware?.Queries);
        saved.ActiveSetupMrus = ToEntries(_setup?.Queries);
        saved.StartupMrus = ToEntries(_startup?.Queries);
        saved.FootprintsMrus = ToEntries(_footprints?.Queries);
        _store.Save(saved);
    }

    private void Push()
    {
        _host.ApplySettings(
            ShowGlobal,
            ShowPerUser,
            ShowX64,
            ShowX86,
            ShowSystemComponents,
            ShowWindowsInstaller,
            ShowOtherInstallers,
            SelectedDateFormat?.Pattern ?? DisplayFormats.DefaultDateFormat.Pattern,
            (int)DecimalPlaces);
    }
}
