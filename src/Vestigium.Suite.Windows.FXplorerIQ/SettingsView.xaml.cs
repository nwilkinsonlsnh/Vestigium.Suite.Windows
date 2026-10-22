using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class SettingsView : UserControl
{
    private bool _loading = true;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadThemes();
    }

    private void LoadThemes()
    {
        var themes = App.Themes;
        ThemeBox.ItemsSource = themes.AvailableThemes;
        ThemeBox.SelectedValue = themes.Current?.Id ?? themes.AvailableThemes.FirstOrDefault()?.Id;
        _loading = false;
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ThemeBox.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
            return;

        var themes = App.Themes;
        if (!string.Equals(themes.Current?.Id, id, StringComparison.OrdinalIgnoreCase))
            themes.SwitchTheme(id);

        var settings = App.Settings.Load();
        settings.ThemeId = id;
        App.Settings.Save(settings);
    }
}
