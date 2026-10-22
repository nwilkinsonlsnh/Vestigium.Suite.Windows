using System.Windows;
using System.Windows.Controls;
using Vestigium.Controls.NumericUpDown;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class SettingsView : UserControl
{
    private bool _loading = true;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += (_, _) => Load();
        Unloaded += (_, _) => FXplorerSettingsStore.Changed -= OnSettingsChanged;
        FXplorerSettingsStore.Changed += OnSettingsChanged;
    }

    private void Load()
    {
        var themes = App.Themes;
        ThemeBox.ItemsSource = themes.AvailableThemes;
        ThemeBox.SelectedValue = themes.Current?.Id ?? themes.AvailableThemes.FirstOrDefault()?.Id;
        Show(App.Settings.Load());
        _loading = false;
    }

    private void OnSettingsChanged()
    {
        if (_loading)
            return;
        Dispatcher.Invoke(() =>
        {
            _loading = true;
            Show(App.Settings.Load());
            _loading = false;
        });
    }

    private void Show(FXplorerSettings settings)
    {
        FoldersWidth.Value = (decimal)settings.TreeWidth;
        NameWidth.Value = (decimal)settings.NameWidth;
        DateModifiedWidth.Value = (decimal)settings.DateModifiedWidth;
        TypeWidth.Value = (decimal)settings.TypeWidth;
        SizeWidth.Value = (decimal)settings.SizeWidth;
        CategoryWidth.Value = (decimal)settings.CategoryWidth;
        TagWidth.Value = (decimal)settings.TagWidth;
    }

    private void Page_Checked(object sender, RoutedEventArgs e)
    {
        if (ThemesPage is null || ColumnsPage is null)
            return;
        var columns = sender is RadioButton button && button.Tag as string == "Columns";
        ThemesPage.Visibility = columns ? Visibility.Collapsed : Visibility.Visible;
        ColumnsPage.Visibility = columns ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ThemeBox.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
            return;

        var themes = App.Themes;
        if (!string.Equals(themes.Current?.Id, id, StringComparison.OrdinalIgnoreCase))
            themes.SwitchTheme(id);

        Save(settings => settings.ThemeId = id);
    }

    private void Width_Changed(object sender, RoutedPropertyChangedEventArgs<decimal> e)
    {
        if (_loading || sender is not VestigiumNumericUpDown box || box.Tag is not string header)
            return;
        Save(settings => settings.SetWidth(header, (double)box.Value));
    }

    private void Defaults_Click(object sender, RoutedEventArgs e)
    {
        Save(settings => settings.ResetColumns());
    }

    private void Save(Action<FXplorerSettings> change)
    {
        _loading = true;
        try
        {
            var settings = App.Settings.Load();
            change(settings);
            App.Settings.Save(settings);
            Show(settings);
        }
        finally
        {
            _loading = false;
        }
    }
}
