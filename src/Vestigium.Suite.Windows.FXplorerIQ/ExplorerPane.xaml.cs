using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Vestigium.Helpers.FileSystemPaths;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class ExplorerPane : UserControl
{
    private string _folder = "";

    public ExplorerPane()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (_folder.Length == 0)
                Open(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        };
    }

    public Action<string>? Status { get; set; }

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
