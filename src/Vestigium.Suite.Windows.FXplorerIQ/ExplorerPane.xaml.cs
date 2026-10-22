using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Helpers.FileSystemPaths;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class ExplorerPane : UserControl
{
    private const double OpenWidth = 260;
    private const double ClosedWidth = 36;

    private readonly ObservableCollection<FolderNode> _roots = [];
    private string _folder = "";
    private bool _fromTree;
    private bool _treeOpen = true;
    private bool _applying;
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_roots.Count == 0)
            LoadRoots();
        if (_folder.Length == 0)
            Open(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        ApplyColumns(App.Settings.Load());
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
        Dispatcher.Invoke(() => ApplyColumns(App.Settings.Load()));
    }

    private void ApplyColumns(FXplorerSettings settings)
    {
        _applying = true;
        try
        {
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

        _applying = true;
        try
        {
            var settings = App.Settings.Load();
            settings.SetWidth(header, Math.Round(width));
            App.Settings.Save(settings);
        }
        finally
        {
            _applying = false;
        }
    }

    private void Fit()
    {
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
        TreeColumn.Width = new GridLength(_treeOpen ? OpenWidth : ClosedWidth);
        TreeHost.Visibility = _treeOpen ? Visibility.Visible : Visibility.Collapsed;
        TreeOpen.Visibility = _treeOpen ? Visibility.Collapsed : Visibility.Visible;
    }

    public void Open(string raw)
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
            Say("Not a folder: " + cleaned);
            PathBox.Text = cleaned;
            return;
        }

        try
        {
            var full = Path.GetFullPath(path);
            var rows = new List<PaneEntry>();
            foreach (var dir in Directory.EnumerateDirectories(full))
            {
                var info = new DirectoryInfo(dir);
                rows.Add(new PaneEntry(info.Name, info.FullName, true, Write(info.LastWriteTime), "File folder", ""));
            }

            foreach (var file in Directory.EnumerateFiles(full))
            {
                var info = new FileInfo(file);
                rows.Add(new PaneEntry(info.Name, info.FullName, false, Write(info.LastWriteTime), Kind(info.Extension), Size(info.Length)));
            }

            rows.Sort(static (a, b) =>
            {
                var kind = b.IsDirectory.CompareTo(a.IsDirectory);
                return kind != 0 ? kind : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            _folder = full;
            PathBox.Text = full;
            Listing.ItemsSource = rows;
            UpButton.IsEnabled = Path.GetDirectoryName(full) is not null;
            Say(rows.Count + " item" + (rows.Count == 1 ? "" : "s"));
        }
        catch (Exception ex)
        {
            Say(ex.Message);
        }
    }

    private void LoadRoots()
    {
        var pc = Group("This PC");
        pc.IsExpanded = true;

        var devices = Group("Devices & Drives");
        devices.IsExpanded = true;
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || drive.DriveType is not (DriveType.Fixed or DriveType.Removable or DriveType.CDRom))
                continue;
            devices.Children.Add(DriveNode(drive));
        }

        var network = Group("Network Locations");
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Network || !drive.IsReady)
                continue;
            network.Children.Add(DriveNode(drive));
        }

        pc.Children.Add(devices);
        pc.Children.Add(network);
        _roots.Add(pc);
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

    private void Folder_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem item || item.DataContext is not FolderNode node)
            return;
        if (node.Loaded || node.FullPath.Length == 0)
            return;

        node.Loaded = true;
        node.Children.Clear();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(node.FullPath))
            {
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
