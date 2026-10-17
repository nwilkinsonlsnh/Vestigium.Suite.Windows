using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeManager _themes;
    private readonly MainViewModel _host;
    private readonly AppMgmtSettingsStore _store;
    private bool _loading;

    public SettingsViewModel(ThemeManager themes, MainViewModel host, AppMgmtSettingsStore store)
    {
        _themes = themes;
        _host = host;
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

    public IReadOnlyList<ThemeDefinition> Themes => _themes.AvailableThemes;

    public IReadOnlyList<DateFormatOption> DateFormats => DisplayFormats.DateFormats;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AppMgmtPageOpen))]
    [NotifyPropertyChangedFor(nameof(ThemesPageOpen))]
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
    private DateFormatOption _selectedDateFormat;

    [ObservableProperty]
    private decimal _decimalPlaces = 4;

    public bool HasSelection => _host.SelectedPackage is not null;

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
        DecimalPlaces = Math.Clamp(data.DecimalPlaces, 0, 8);
        SelectedDateFormat = DateFormats.FirstOrDefault(f => f.Label == data.DateFormat) ?? DisplayFormats.DefaultDateFormat;
        if (!string.IsNullOrWhiteSpace(data.ThemeId))
            SelectedThemeId = data.ThemeId;
        _loading = false;
        Push();
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

    private void Changed(string name, string? previous, string? next)
    {
        if (_loading)
            return;

        Push();

        HostLog.Information(
            AppMgmtCatalog.SettingsChanged,
            "Settings",
            $"{name}: current '{previous ?? "(unset)"}' new '{next ?? "(unset)"}'");

        _store.Save(new AppMgmtSettings
        {
            ThemeId = SelectedThemeId,
            ShowGlobal = ShowGlobal,
            ShowPerUser = ShowPerUser,
            ShowX64 = ShowX64,
            ShowX86 = ShowX86,
            ShowSystemComponents = ShowSystemComponents,
            ShowWindowsInstaller = ShowWindowsInstaller,
            ShowOtherInstallers = ShowOtherInstallers,
            DateFormat = SelectedDateFormat?.Label ?? DisplayFormats.DefaultDateFormat.Label,
            DecimalPlaces = (int)DecimalPlaces
        });

        HostLog.Information(AppMgmtCatalog.SettingsSaved, "Settings", $"Settings saved. {_store.FilePath}");
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
