using System.Windows;
using System.Windows.Controls;
using Vestigium.Controls.NumericUpDown;
using Vestigium.Helpers.Hashing;

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
        HashBox.ItemsSource = Enum.GetValues<HashingAlgorithm>()
            .Where(HashingHelper.IsSupported)
            .Select(algorithm => new HashChoice(algorithm.ToString(), HashingHelper.AlgorithmName(algorithm)))
            .ToList();
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
        HashWidth.Value = (decimal)settings.HashWidth;
        HashBox.SelectedValue = settings.Algorithm().ToString();
    }

    private void Page_Checked(object sender, RoutedEventArgs e)
    {
        if (ThemesPage is null || ColumnsPage is null || HashPage is null)
            return;
        var tag = sender is RadioButton button ? button.Tag as string : "Themes";
        ThemesPage.Visibility = tag == "Themes" ? Visibility.Visible : Visibility.Collapsed;
        ColumnsPage.Visibility = tag == "Columns" ? Visibility.Visible : Visibility.Collapsed;
        HashPage.Visibility = tag == "Hash" ? Visibility.Visible : Visibility.Collapsed;
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

    private void HashBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || HashBox.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
            return;
        Save(settings => settings.HashAlgorithm = id);
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

    private sealed record HashChoice(string Id, string Name);
}
