using System.Collections.ObjectModel;
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
    private const double OpenWidth = 308;
    private const double ClosedWidth = 28;

    private readonly ObservableCollection<FolderNode> _roots = [];
    private string _folder = "";
    private bool _fromTree;
    private bool _treeOpen = true;

    public ExplorerPane()
    {
        InitializeComponent();
        Folders.ItemsSource = _roots;
        Folders.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(Folder_Expanded));
        Loaded += OnLoaded;
    }

    public Action<string>? Status { get; set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_roots.Count == 0)
            LoadRoots();
        if (_folder.Length == 0)
            Open(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        var window = Window.GetWindow(this);
        if (window is null)
            return;

        window.SizeChanged += (_, _) => Fit();
        Fit();
    }

    private void Fit()
    {
        var window = Window.GetWindow(this);
        if (window is null || window.ActualHeight < 200)
            return;

        // Shell wraps the page in a ScrollViewer, so the pane must take a real height
        // or the tree and grid grow instead of scrolling.
        Height = Math.Max(240, window.ActualHeight - 140);
    }

    private void TreeToggle_Click(object sender, RoutedEventArgs e)
    {
        _treeOpen = !_treeOpen;
        TreeColumn.Width = new GridLength(_treeOpen ? OpenWidth : ClosedWidth);
        TreeHost.Visibility = _treeOpen ? Visibility.Visible : Visibility.Collapsed;
        TreeToggle.Content = _treeOpen ? "◀" : "▶";
        TreeToggle.ToolTip = _treeOpen ? "Hide folders" : "Show folders";
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
                rows.Add(new PaneEntry(info.Name, info.FullName, true, "", Write(info.LastWriteTime)));
            }

            foreach (var file in Directory.EnumerateFiles(full))
            {
                var info = new FileInfo(file);
                rows.Add(new PaneEntry(info.Name, info.FullName, false, Size(info.Length), Write(info.LastWriteTime)));
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
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;
            var root = new FolderNode(drive.Name, drive.RootDirectory.FullName);
            root.Children.Add(new FolderNode("", ""));
            _roots.Add(root);
        }
    }

    private void Folder_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem item || item.DataContext is not FolderNode node)
            return;
        if (node.Loaded)
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
