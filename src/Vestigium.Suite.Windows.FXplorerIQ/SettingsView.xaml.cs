using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Vestigium.Controls.NumericUpDown;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class SettingsView : UserControl
{
    private static readonly string[] Collisions = ["UniqueName", "Skip", "Overwrite"];
    private static readonly string[] Patterns = [".#", ".##", ".###", ".####", ".#####", "A#", "A##", "A###", "A####", "A#####"];
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
        var algorithms = Enum.GetValues<HashingAlgorithm>()
            .Where(HashingHelper.IsSupported)
            .Select(algorithm => new HashChoice(algorithm.ToString(), HashingHelper.AlgorithmName(algorithm)))
            .ToList();
        HashBox.ItemsSource = algorithms;
        VerifyBox.ItemsSource = algorithms;
        CollisionBox.ItemsSource = Collisions;
        PatternBox.ItemsSource = Patterns;
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
        HideEmptyDrives.IsChecked = settings.HideEmptyDrives;
        ShowAllFolders.IsChecked = settings.ShowAllFolders;
        HideJunctions.IsChecked = settings.HideJunctions;
        ShowHiddenFiles.IsChecked = settings.ShowHiddenFiles;
        ShowFileExtensions.IsChecked = settings.ShowFileExtensions;
        ShowSystemFiles.IsChecked = settings.ShowSystemFiles;
        CollisionBox.SelectedItem = Collisions.Contains(settings.Collision) ? settings.Collision : "UniqueName";
        var pattern = Patterns.Contains(settings.UniqueNamePattern) ? settings.UniqueNamePattern : ".##";
        PatternBox.SelectedItem = pattern;
        AskOverwrite.IsChecked = settings.AskBeforeOverwrite == false;
        ApplyCollision();
        VerifyBox.SelectedValue = settings.Verify().ToString();
        VerifyRetries.Value = settings.VerifyReadRetries;
        VerifyWait.Value = settings.VerifyReadWaitMs;
        FileMasks.Text = settings.ExcludeFileMasks;
        DirectoryMasks.Text = settings.ExcludeDirectoryMasks;
        MinSize.Text = settings.MinSizeBytes?.ToString() ?? "";
        MaxSize.Text = settings.MaxSizeBytes?.ToString() ?? "";
        MinAge.Text = settings.MinAgeDays?.ToString() ?? "";
        MaxAge.Text = settings.MaxAgeDays?.ToString() ?? "";
        Depth.Text = settings.MaxDepth?.ToString() ?? "";
        Retries.Value = settings.RetryCount;
        RetryWait.Value = settings.RetryWaitSeconds;
        EmptyDirectories.IsChecked = settings.IncludeEmptyDirectories;
        Timestamps.IsChecked = settings.CopyTimestamps;
        CreateMissing.IsChecked = settings.CreateMissingDestination;
        SecureBox.ItemsSource = new[] { 3, 7 };
        SecureBox.SelectedItem = settings.SecurePasses == 7 ? 7 : 3;
        SecureLine.Text = Line(settings.SecurePasses);
        ThreadBox.ItemsSource = Choices;
        ThreadBox.SelectedItem = Choices.Contains(settings.MaxThreads) ? settings.MaxThreads : 32;
        ShowThreads(ThreadBox.SelectedItem is int picked ? picked : 32);
    }

    private void Page_Checked(object sender, RoutedEventArgs e)
    {
        if (ThemesPage is null || ColumnsPage is null || HashPage is null || ViewPage is null || CollisionPage is null || JobPage is null || SecurePage is null || ThreadsPage is null)
            return;
        var tag = sender is RadioButton button ? button.Tag as string : "Themes";
        ThemesPage.Visibility = tag == "Themes" ? Visibility.Visible : Visibility.Collapsed;
        ColumnsPage.Visibility = tag == "Columns" ? Visibility.Visible : Visibility.Collapsed;
        HashPage.Visibility = tag == "Hash" ? Visibility.Visible : Visibility.Collapsed;
        ViewPage.Visibility = tag == "View" ? Visibility.Visible : Visibility.Collapsed;
        CollisionPage.Visibility = tag == "Collision" ? Visibility.Visible : Visibility.Collapsed;
        JobPage.Visibility = tag == "Job" ? Visibility.Visible : Visibility.Collapsed;
        SecurePage.Visibility = tag == "Secure" ? Visibility.Visible : Visibility.Collapsed;
        ThreadsPage.Visibility = tag == "Threads" ? Visibility.Visible : Visibility.Collapsed;
    }

    private static readonly int[] Choices = [1, 8, 16, 32, 48, 64, 80, 96, 112, 128];

    private void Threads_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ThreadBox.SelectedItem is not int count)
            return;
        ShowThreads(count);
        Save(settings => settings.MaxThreads = count);
    }

    private void ShowThreads(int count)
    {
        var shares = Split(count);
        ThreadLine.Text = count + " threads. Tiny " + shares[0]
            + ", Small " + shares[1]
            + ", Medium " + shares[2]
            + ", Large " + shares[3]
            + ", Huge " + shares[4] + ".";
        ThreadCards.Children.Clear();
        var names = new[] { "Tiny", "Small", "Medium", "Large", "Huge" };
        for (var i = 0; i < names.Length; i++)
            ThreadCards.Children.Add(Card(names[i], shares[i]));
    }

    private static int[] Split(int total)
    {
        if (total < 5)
            return [total, 0, 0, 0, 0];
        var raw = new[] { total * 0.70, total * 0.10, total * 0.10, total * 0.05, total * 0.05 };
        var shares = raw.Select(value => Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero))).ToArray();
        shares[0] += total - shares.Sum();
        if (shares[0] < 1)
            shares[0] = 1;
        return shares;
    }

    private FrameworkElement Card(string name, int threads)
    {
        var panel = new StackPanel { Margin = new Thickness(8) };
        panel.Children.Add(new TextBlock { Text = name, Style = Try("TextBlock.Caption") });
        panel.Children.Add(new TextBlock { Text = threads + (threads == 1 ? " thread" : " threads"), Margin = new Thickness(0, 4, 0, 0) });
        return new Border
        {
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = TryBrush("Vestigium.Brushes.Stroke.Default"),
            Background = TryBrush("Vestigium.Brushes.Surface.Card"),
            Child = panel
        };
    }

    private static Style? Try(string key) => Application.Current?.TryFindResource(key) as Style;

    private static System.Windows.Media.Brush TryBrush(string key)
        => Application.Current?.TryFindResource(key) as System.Windows.Media.Brush
           ?? System.Windows.Media.Brushes.Transparent;

    private void Secure_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || SecureBox.SelectedItem is not int passes)
            return;
        SecureLine.Text = Line(passes);
        Save(settings => settings.SecurePasses = passes);
    }

    private static string Line(int passes)
        => (passes == 7 ? "Seven" : "Three") + " cryptographic random passes, then one zero pass, then delete.";

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

    private void Collision_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || CollisionBox.SelectedItem is not string collision)
            return;
        ApplyCollision();
        Save(settings => settings.Collision = collision);
    }

    private void Pattern_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || PatternBox.SelectedItem is not string pattern)
            return;
        Save(settings => settings.UniqueNamePattern = pattern);
    }

    private void CollisionFlag_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        var automatic = AskOverwrite.IsChecked == true;
        ApplyCollision();
        Save(settings => settings.AskBeforeOverwrite = !automatic);
    }

    private void ApplyCollision()
    {
        var automatic = AskOverwrite.IsChecked == true;
        var unique = CollisionBox.SelectedItem as string == "UniqueName";
        CollisionLabel.Visibility = automatic ? Visibility.Visible : Visibility.Collapsed;
        CollisionBox.Visibility = automatic ? Visibility.Visible : Visibility.Collapsed;
        var showPattern = automatic && unique;
        PatternLabel.Visibility = showPattern ? Visibility.Visible : Visibility.Collapsed;
        PatternBox.Visibility = showPattern ? Visibility.Visible : Visibility.Collapsed;
    }

    private void VerifyRetry_Changed(object sender, EventArgs e)
    {
        if (_loading)
            return;
        Save(settings =>
        {
            settings.VerifyReadRetries = (int)VerifyRetries.Value;
            settings.VerifyReadWaitMs = (int)VerifyWait.Value;
        });
    }

    private void Verify_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || VerifyBox.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
            return;
        Save(settings => settings.VerifyAlgorithm = id);
    }

    private void Width_Changed(object sender, RoutedPropertyChangedEventArgs<decimal> e)
    {
        if (_loading || sender is not VestigiumNumericUpDown box || box.Tag is not string header)
            return;
        Save(settings => settings.SetWidth(header, (double)box.Value));
    }

    private void View_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        Save(settings =>
        {
            settings.HideEmptyDrives = HideEmptyDrives.IsChecked == true;
            settings.ShowAllFolders = ShowAllFolders.IsChecked == true;
            settings.HideJunctions = HideJunctions.IsChecked == true;
            settings.ShowHiddenFiles = ShowHiddenFiles.IsChecked == true;
            settings.ShowFileExtensions = ShowFileExtensions.IsChecked == true;
            settings.ShowSystemFiles = ShowSystemFiles.IsChecked == true;
        });
    }

    private void Job_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        Save(settings =>
        {
            settings.ExcludeFileMasks = FileMasks.Text.Trim();
            settings.ExcludeDirectoryMasks = DirectoryMasks.Text.Trim();
            settings.MinSizeBytes = LongOrNull(MinSize.Text);
            settings.MaxSizeBytes = LongOrNull(MaxSize.Text);
            settings.MinAgeDays = IntOrNull(MinAge.Text);
            settings.MaxAgeDays = IntOrNull(MaxAge.Text);
            settings.MaxDepth = IntOrNull(Depth.Text);
        });
    }

    private void Retry_Changed(object sender, EventArgs e)
    {
        if (_loading)
            return;
        Save(settings =>
        {
            settings.RetryCount = (int)Retries.Value;
            settings.RetryWaitSeconds = (int)RetryWait.Value;
        });
    }

    private void JobFlag_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        Save(settings =>
        {
            settings.IncludeEmptyDirectories = EmptyDirectories.IsChecked == true;
            settings.CopyTimestamps = Timestamps.IsChecked == true;
            settings.CreateMissingDestination = CreateMissing.IsChecked == true;
        });
    }

    private static long? LongOrNull(string text)
        => long.TryParse(text.Trim(), out var value) ? value : null;

    private static int? IntOrNull(string text)
        => int.TryParse(text.Trim(), out var value) ? value : null;

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
