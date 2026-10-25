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
        Threads.Value = settings.MaxThreads;
        ThreadLine.Text = ThreadText(settings.MaxThreads);
    }

    private void Page_Checked(object sender, RoutedEventArgs e)
    {
        if (ThemesPage is null || ColumnsPage is null || HashPage is null || ViewPage is null || CollisionPage is null || JobPage is null || SecurePage is null)
            return;
        var tag = sender is RadioButton button ? button.Tag as string : "Themes";
        ThemesPage.Visibility = tag == "Themes" ? Visibility.Visible : Visibility.Collapsed;
        ColumnsPage.Visibility = tag == "Columns" ? Visibility.Visible : Visibility.Collapsed;
        HashPage.Visibility = tag == "Hash" ? Visibility.Visible : Visibility.Collapsed;
        ViewPage.Visibility = tag == "View" ? Visibility.Visible : Visibility.Collapsed;
        CollisionPage.Visibility = tag == "Collision" ? Visibility.Visible : Visibility.Collapsed;
        JobPage.Visibility = tag == "Job" ? Visibility.Visible : Visibility.Collapsed;
        SecurePage.Visibility = tag == "Secure" ? Visibility.Visible : Visibility.Collapsed;
        JobPage.Visibility = tag == "Job" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Threads_Changed(object sender, EventArgs e)
    {
        if (_loading)
            return;
        var requested = (int)Threads.Value;
        var current = App.Settings.Load().MaxThreads;
        var next = requested == current
            ? current
            : requested > current ? Next(current, 1) : Next(current, -1);
        if (next != requested)
        {
            Threads.Value = next;
            return;
        }
        ThreadLine.Text = ThreadText(next);
        Save(settings => settings.MaxThreads = next);
    }

    private static int Next(int current, int direction)
    {
        var allowed = new[] { 1, 4, 8, 16, 24, 32, 45, 64, 96, 128 };
        if (direction > 0)
        {
            foreach (var count in allowed)
                if (count > current)
                    return count;
            return allowed[^1];
        }
        for (var i = allowed.Length - 1; i >= 0; i--)
            if (allowed[i] < current)
                return allowed[i];
        return allowed[0];
    }

    private static string ThreadText(int count)
        => count + " threads. Tiny 70%, Small 10%, Medium 10%, Large 5%, Huge 5%.";

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
