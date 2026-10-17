using CommunityToolkit.Mvvm.ComponentModel;
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
    private DateFormatOption _selectedDateFormat;

    [ObservableProperty]
    private decimal _decimalPlaces = 4;

    public void Load()
    {
        _loading = true;
        var data = _store.Load();
        ShowGlobal = data.ShowGlobal;
        ShowPerUser = data.ShowPerUser;
        DecimalPlaces = Math.Clamp(data.DecimalPlaces, 0, 8);
        SelectedDateFormat = DateFormats.FirstOrDefault(f => f.Label == data.DateFormat) ?? DisplayFormats.DefaultDateFormat;
        if (!string.IsNullOrWhiteSpace(data.ThemeId))
            SelectedThemeId = data.ThemeId;
        _loading = false;
        _host.ApplySettings(ShowGlobal, ShowPerUser, SelectedDateFormat.Pattern, (int)DecimalPlaces);
    }

    partial void OnSelectedThemeIdChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        var previous = _themes.Current?.Id;
        if (previous != value)
            _themes.SwitchTheme(value);
        Changed("Theme", previous, value);
    }

    partial void OnShowGlobalChanged(bool value) => Changed("Show Global Packages", (!value).ToString(), value.ToString());
    partial void OnShowPerUserChanged(bool value) => Changed("Show Per User Packages", (!value).ToString(), value.ToString());
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

        _host.ApplySettings(
            ShowGlobal,
            ShowPerUser,
            SelectedDateFormat?.Pattern ?? DisplayFormats.DefaultDateFormat.Pattern,
            (int)DecimalPlaces);

        HostLog.Information(
            AppMgmtCatalog.SettingsChanged,
            "Settings",
            $"{name}: current '{previous ?? "(unset)"}' new '{next ?? "(unset)"}'");

        _store.Save(new AppMgmtSettings
        {
            ThemeId = SelectedThemeId,
            ShowGlobal = ShowGlobal,
            ShowPerUser = ShowPerUser,
            DateFormat = SelectedDateFormat?.Label ?? DisplayFormats.DefaultDateFormat.Label,
            DecimalPlaces = (int)DecimalPlaces
        });

        HostLog.Information(AppMgmtCatalog.SettingsSaved, "Settings", $"Settings saved. {_store.FilePath}");
    }
}
