using CommunityToolkit.Mvvm.ComponentModel;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeManager _themes;
    private readonly MainViewModel _host;

    public SettingsViewModel(ThemeManager themes, MainViewModel host)
    {
        _themes = themes;
        _host = host;
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

    partial void OnSelectedThemeIdChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (_themes.Current?.Id != value)
            _themes.SwitchTheme(value);
    }

    partial void OnShowGlobalChanged(bool value) => Push();
    partial void OnShowPerUserChanged(bool value) => Push();
    partial void OnSelectedDateFormatChanged(DateFormatOption value) => Push();

    partial void OnDecimalPlacesChanged(decimal value)
    {
        var clamped = Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 8);
        if (clamped != value)
        {
            DecimalPlaces = clamped;
            return;
        }
        Push();
    }

    private void Push()
    {
        _host.ApplySettings(
            ShowGlobal,
            ShowPerUser,
            SelectedDateFormat?.Pattern ?? DisplayFormats.DefaultDateFormat.Pattern,
            (int)DecimalPlaces);
    }
}
