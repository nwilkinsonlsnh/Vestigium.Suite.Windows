using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Helpers.FileSystemPaths;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class ExplorerPane : UserControl
{
    private const double ClosedWidth = 36;

    private readonly ObservableCollection<FolderNode> _roots = [];
    private string _folder = "";
    private string _algorithm = HashingAlgorithm.Sha256.ToString();
    private bool _fromTree;
    private bool _treeOpen = true;
    private bool _applying;
    private int _load;
    private ScrollViewer? _host;

    public ExplorerPane()
    {
        InitializeComponent();
        Folders.ItemsSource = _roots;
        Folders.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(Folder_Expanded));
        Loaded += OnLoaded;
        Unloaded += (_, _) => FXplorerSettingsStore.Changed -= OnSettingsChanged;
    }

    public Action<string>? Status { get; set; }

    public string Folder => _folder;

    public string SelectionOrFolder()
        => Listing.SelectedItem is PaneEntry entry && entry.FullPath.Length > 0 ? entry.FullPath : _folder;

    public IReadOnlyList<string> SelectedPaths()
    {
        var paths = new List<string>();
        foreach (var item in Listing.SelectedItems)
        {
            if (item is PaneEntry entry && entry.FullPath.Length > 0)
                paths.Add(entry.FullPath);
        }
        if (paths.Count == 0 && _folder.Length > 0)
            paths.Add(_folder);
        return paths;
    }

    public bool FillCell { get; set; }

    public event Action<string>? Navigated;

    public void Seed(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
            _folder = path;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_roots.Count == 0)
            LoadRoots();
        Open(_folder.Length == 0
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : _folder);

        ApplyLayout(App.Settings.Load());
        HookColumns();
        FXplorerSettingsStore.Changed += OnSettingsChanged;

        _host = FindHost();
        if (_host is not null)
            _host.SizeChanged += (_, _) => Fit();

        var window = Window.GetWindow(this);
        if (window is not null)
            window.SizeChanged += (_, _) => Fit();

        Fit();
    }

    private void OnSettingsChanged()
    {
        if (_applying)
            return;
        Dispatcher.Invoke(() =>
        {
            var settings = App.Settings.Load();
            ApplyLayout(settings);
            LoadRoots();
            if (_folder.Length > 0)
                Open(_folder);
        });
    }

    private void ApplyLayout(FXplorerSettings settings)
    {
        _applying = true;
        try
        {
            if (_treeOpen && settings.TreeWidth >= 160)
                TreeColumn.Width = new GridLength(settings.TreeWidth);

            foreach (var column in Listing.Columns)
            {
                var width = settings.Width(column.Header?.ToString() ?? "");
                if (width >= 40)
                    column.Width = new DataGridLength(width);
            }
        }
        finally
        {
            _applying = false;
        }
    }

    private void HookColumns()
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
        foreach (var column in Listing.Columns)
            descriptor.AddValueChanged(column, OnColumnWidthChanged);
    }

    private void OnColumnWidthChanged(object? sender, EventArgs e)
    {
        if (_applying || sender is not DataGridColumn column)
            return;

        var header = column.Header?.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return;

        var width = column.ActualWidth;
        if (width < 40)
            return;

        Save(settings => settings.SetWidth(header, Math.Round(width)));
    }

    private void TreeSplit_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (!_treeOpen)
            return;

        var width = TreeColumn.ActualWidth;
        if (width < 160)
            return;

        Save(settings => settings.TreeWidth = Math.Round(width));
    }

    private void Save(Action<FXplorerSettings> change)
    {
        _applying = true;
        try
        {
            var settings = App.Settings.Load();
            change(settings);
            App.Settings.Save(settings);
        }
        finally
        {
            _applying = false;
        }
    }

    private void Fit()
    {
        if (FillCell)
        {
            Height = double.NaN;
            VerticalAlignment = VerticalAlignment.Stretch;
            return;
        }

        var height = _host?.ActualHeight ?? 0;
        if (height < 100)
        {
            var window = Window.GetWindow(this);
            if (window is null || window.ActualHeight < 200)
                return;
            height = window.ActualHeight - 96;
        }

        Height = Math.Max(240, height);
    }

    private ScrollViewer? FindHost()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is ScrollViewer viewer)
                return viewer;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void TreeToggle_Click(object sender, RoutedEventArgs e)
    {
        _treeOpen = !_treeOpen;
        var open = App.Settings.Load().TreeWidth;
        if (open < 160)
            open = FXplorerSettings.DefaultTreeWidth;

        TreeColumn.Width = new GridLength(_treeOpen ? open : ClosedWidth);
        TreeColumn.MinWidth = _treeOpen ? 160 : 0;
        TreeHost.Visibility = _treeOpen ? Visibility.Visible : Visibility.Collapsed;
        TreeOpenHost.Visibility = _treeOpen ? Visibility.Collapsed : Visibility.Visible;
        TreeSplit.Visibility = _treeOpen ? Visibility.Visible : Visibility.Collapsed;
        SplitColumn.Width = new GridLength(_treeOpen ? 8 : 0);
    }

    public async void Open(string raw)
    {
        var cleaned = PathResolve.Clean(PathTokens.Expand(raw));
        if (cleaned.Length == 0)
        {
            Say("Path is empty.");
            return;
        }

        var path = PathLong.Apply(cleaned);
        if (!Directory.Exists(path))
        {
            _folder = cleaned;
            PathBox.Text = cleaned;
            Listing.ItemsSource = null;
            Say("Folder is gone: " + cleaned);
            Navigated?.Invoke(cleaned);
            return;
        }

        var token = ++_load;
        var settings = App.Settings.Load();
        var algorithm = settings.Algorithm();
        ShowBusy("Loading " + cleaned);
        try
        {
            var full = Path.GetFullPath(path);
            var rows = await Task.Run(() => Read(full, settings)).ConfigureAwait(true);
            if (token != _load)
                return;

            if (rows is null)
            {
                _folder = full;
                PathBox.Text = full;
                Listing.ItemsSource = null;
                Say("Access denied: " + full);
                Navigated?.Invoke(full);
                return;
            }

            _algorithm = algorithm.ToString();
            _folder = full;
            PathBox.Text = full;
            Listing.ItemsSource = rows;
            UpButton.IsEnabled = Path.GetDirectoryName(full) is not null;
            Say(rows.Count + " item" + (rows.Count == 1 ? "" : "s") + " · " + HashingHelper.AlgorithmName(algorithm));
            Navigated?.Invoke(full);
            RefreshTree(full);
            _ = FillHashes(rows, algorithm, token);
        }
        catch (Exception ex)
        {
            if (token == _load)
                Say(ex.Message);
        }
        finally
        {
            if (token == _load)
                HideBusy();
        }
    }

    private static List<PaneEntry>? Read(string full, FXplorerSettings settings)
    {
        var rows = new List<PaneEntry>();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(full))
            {
                if (!ShowFolder(dir, settings))
                    continue;
                var info = new DirectoryInfo(dir);
                rows.Add(new PaneEntry(info.Name, info.FullName, true, Write(info.LastWriteTime), "File folder", ""));
            }

            foreach (var file in Directory.EnumerateFiles(full))
            {
                var info = new FileInfo(file);
                if (!ShowFile(info, settings))
                    continue;
                rows.Add(new PaneEntry(DisplayName(info, settings), info.FullName, false, Write(info.LastWriteTime), Kind(info.Extension), Size(info.Length)));
            }
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }

        rows.Sort(static (a, b) =>
        {
            var kind = b.IsDirectory.CompareTo(a.IsDirectory);
            return kind != 0 ? kind : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        return rows;
    }

    private async Task FillHashes(List<PaneEntry> rows, HashingAlgorithm algorithm, int token)
    {
        var files = rows.Where(row => !row.IsDirectory).ToList();
        foreach (var row in files)
        {
            if (token != _load)
                return;
            var hash = await Task.Run(() => Hash(row.FullPath, algorithm)).ConfigureAwait(true);
            if (token != _load)
                return;
            row.Hash = hash;
        }
    }

    private void ShowBusy(string message)
    {
        BusyMessage.Text = message;
        BusyOverlay.Visibility = Visibility.Visible;
    }

    private void HideBusy() => BusyOverlay.Visibility = Visibility.Collapsed;

    private void LoadRoots()
    {
        _roots.Clear();
        var settings = App.Settings.Load();
        var pc = Group("This PC");
        pc.IsExpanded = true;

        var devices = Group("Devices & Drives");
        devices.IsExpanded = true;
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!ShowDrive(drive, settings))
                continue;
            if (drive.DriveType is not (DriveType.Fixed or DriveType.Removable or DriveType.CDRom))
                continue;
            devices.Children.Add(DriveNode(drive));
        }

        var network = Group("Network Locations");
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Network || !ShowDrive(drive, settings))
                continue;
            network.Children.Add(DriveNode(drive));
        }

        pc.Children.Add(devices);
        pc.Children.Add(network);
        _roots.Add(pc);
    }

    private static bool ShowDrive(DriveInfo drive, FXplorerSettings settings)
    {
        if (!drive.IsReady)
            return !settings.HideEmptyDrives;
        if (!settings.HideEmptyDrives)
            return true;
        try
        {
            return drive.TotalSize > 0;
        }
        catch
        {
            return false;
        }
    }

    private static FolderNode Group(string name)
    {
        var node = new FolderNode(name, "");
        node.Loaded = true;
        return node;
    }

    private static FolderNode DriveNode(DriveInfo drive)
    {
        var node = new FolderNode(DriveLabel(drive), drive.RootDirectory.FullName);
        node.Children.Add(new FolderNode("", ""));
        return node;
    }

    private static string DriveLabel(DriveInfo drive)
    {
        var letter = drive.Name.TrimEnd('\\');
        var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? DriveKind(drive.DriveType) : drive.VolumeLabel;
        return label + " (" + letter + ")";
    }

    private static string DriveKind(DriveType type)
        => type switch
        {
            DriveType.Fixed => "Local Disk",
            DriveType.Removable => "Removable Disk",
            DriveType.CDRom => "CD Drive",
            DriveType.Network => "Network",
            _ => "Drive"
        };

    public void RefreshTree(string path)
    {
        var full = PathResolve.Clean(PathTokens.Expand(path));
        if (full.Length == 0)
            return;
        var node = Find(_roots, full);
        if (node is null || !node.Loaded)
            return;
        Fill(node);
    }

    private static FolderNode? Find(IEnumerable<FolderNode> nodes, string path)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.FullPath, path, StringComparison.OrdinalIgnoreCase))
                return node;
            var child = Find(node.Children, path);
            if (child is not null)
                return child;
        }
        return null;
    }

    private void Folder_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem item || item.DataContext is not FolderNode node)
            return;
        if (node.Loaded || node.FullPath.Length == 0)
            return;

        node.Loaded = true;
        Fill(node);
    }

    private void Fill(FolderNode node)
    {
        node.Children.Clear();
        var settings = App.Settings.Load();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(node.FullPath))
            {
                if (!ShowFolder(dir, settings))
                    continue;
                var info = new DirectoryInfo(dir);
                var child = new FolderNode(info.Name, info.FullName);
                if (HasSubfolder(info.FullName))
                    child.Children.Add(new FolderNode("", ""));
                node.Children.Add(child);
            }
        }
        catch (Exception ex)
        {
            Say(ex.Message);
        }
    }

    private static bool ShowFolder(string path, FXplorerSettings settings)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if (!attributes.HasFlag(FileAttributes.Directory))
                return false;
            if (settings.HideJunctions && attributes.HasFlag(FileAttributes.ReparsePoint))
                return false;
            if (!settings.ShowAllFolders && (attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System)))
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ShowFile(FileInfo info, FXplorerSettings settings)
    {
        try
        {
            var attributes = info.Attributes;
            if (!settings.ShowHiddenFiles && attributes.HasFlag(FileAttributes.Hidden))
                return false;
            if (!settings.ShowSystemFiles && attributes.HasFlag(FileAttributes.System))
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string DisplayName(FileInfo info, FXplorerSettings settings)
    {
        if (settings.ShowFileExtensions || string.IsNullOrEmpty(info.Extension))
            return info.Name;
        var name = Path.GetFileNameWithoutExtension(info.Name);
        return string.IsNullOrEmpty(name) ? info.Name : name;
    }

    private void Folders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_fromTree || e.NewValue is not FolderNode node || node.FullPath.Length == 0)
            return;
        if (string.Equals(node.FullPath, _folder, StringComparison.OrdinalIgnoreCase))
            return;

        _fromTree = true;
        try
        {
            Open(node.FullPath);
        }
        finally
        {
            _fromTree = false;
        }
    }

    private void Pane_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject source)
            return;

        var scroll = FindScroll(source);
        if (scroll is null)
            return;

        var lines = SystemParameters.WheelScrollLines;
        if (lines <= 0)
            lines = 3;
        var step = e.Delta > 0 ? -lines * 18 : lines * 18;
        scroll.ScrollToVerticalOffset(scroll.VerticalOffset + step);
        e.Handled = true;
    }

    private static ScrollViewer? FindScroll(DependencyObject root)
    {
        if (root is ScrollViewer found)
            return found;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var hit = FindScroll(VisualTreeHelper.GetChild(root, i));
            if (hit is not null)
                return hit;
        }

        return null;
    }

    private static bool HasSubfolder(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).Any();
        }
        catch
        {
            return false;
        }
    }

    private void Go_Click(object sender, RoutedEventArgs e) => Open(PathBox.Text);

    private void Up_Click(object sender, RoutedEventArgs e)
    {
        var parent = Path.GetDirectoryName(_folder);
        if (parent is not null)
            Open(parent);
    }

    private void PathBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        Open(PathBox.Text);
        e.Handled = true;
    }

    private void Listing_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Listing.SelectedItem is not PaneEntry entry || !entry.IsDirectory)
            return;
        Open(entry.FullPath);
    }

    private void Say(string message) => Status?.Invoke(message);

    private static string Write(DateTime when)
        => when.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string Kind(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return "File";
        return extension.TrimStart('.').ToUpperInvariant() + " File";
    }

    private static string Hash(string path, HashingAlgorithm algorithm)
    {
        try
        {
            return HashingHelper.HashFile(path, algorithm);
        }
        catch
        {
            return "";
        }
    }

    private static string Size(long bytes)
    {
        const long kilo = 1024;
        if (bytes < kilo)
            return bytes + " B";
        var value = (double)bytes;
        string unit;
        if (bytes < kilo * kilo)
        {
            value /= kilo;
            unit = "KB";
        }
        else if (bytes < kilo * kilo * kilo)
        {
            value /= kilo * kilo;
            unit = "MB";
        }
        else
        {
            value /= kilo * kilo * kilo;
            unit = "GB";
        }

        return value.ToString("0.#", CultureInfo.InvariantCulture) + " " + unit;
    }
}
