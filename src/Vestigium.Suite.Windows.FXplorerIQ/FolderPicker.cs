using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class FolderPicker : Window
{
    private readonly TreeView _tree = new();

    public FolderPicker(string? current)
    {
        Title = "Choose a folder";
        Width = 420;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Application.Current.TryFindResource("Brush.Surface.Background") as System.Windows.Media.Brush
                     ?? SystemColors.WindowBrush;

        _tree.Margin = new Thickness(0, 0, 0, 12);
        if (Application.Current.TryFindResource("TreeView.Standard") is Style style)
            _tree.Style = style;

        var pc = new TreeViewItem { Header = "This PC", IsExpanded = true, Tag = "" };
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;
            pc.Items.Add(Folder(drive.Name, drive.Name.TrimEnd('\\')));
        }

        _tree.Items.Add(pc);
        Select(current);

        var ok = Button("OK", true);
        ok.IsDefault = true;
        ok.Click += (_, _) => Accept();
        var cancel = Button("Cancel", false);
        cancel.IsCancel = true;

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(ok);
        actions.Children.Add(cancel);

        var root = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(actions, Dock.Bottom);
        root.Children.Add(actions);
        root.Children.Add(_tree);
        Content = root;
    }

    public string Path { get; private set; } = "";

    public static string? Ask(Window? owner, string? current)
    {
        var window = new FolderPicker(current) { Owner = owner };
        return window.ShowDialog() == true ? window.Path : null;
    }

    private void Accept()
    {
        if (_tree.SelectedItem is not TreeViewItem item || item.Tag is not string path || path.Length == 0)
            return;
        Path = path;
        DialogResult = true;
    }

    private TreeViewItem Folder(string path, string label)
    {
        var item = new TreeViewItem { Header = label, Tag = path };
        item.Items.Add(new TreeViewItem { Header = "" });
        item.Expanded += (_, _) => Fill(item);
        return item;
    }

    private static void Fill(TreeViewItem item)
    {
        if (item.Items.Count != 1 || item.Items[0] is not TreeViewItem stub || stub.Tag is not null)
            return;

        item.Items.Clear();
        if (item.Tag is not string path)
            return;

        try
        {
            foreach (var folder in Directory.EnumerateDirectories(path))
            {
                var child = new TreeViewItem { Header = System.IO.Path.GetFileName(folder), Tag = folder };
                child.Items.Add(new TreeViewItem { Header = "" });
                child.Expanded += (_, _) => Fill(child);
                item.Items.Add(child);
            }
        }
        catch (Exception)
        {
            item.Items.Add(new TreeViewItem { Header = "(unavailable)", Tag = null });
        }
    }

    private void Select(string? current)
    {
        if (string.IsNullOrWhiteSpace(current))
            return;
        if (_tree.Items[0] is not TreeViewItem pc)
            return;

        foreach (TreeViewItem drive in pc.Items)
        {
            if (drive.Tag is not string root)
                continue;
            if (!current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                continue;
            drive.IsExpanded = true;
            drive.IsSelected = true;
            break;
        }
    }

    private static Button Button(string text, bool primary)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(16, 6, 16, 6),
            MinWidth = 96
        };
        var key = primary ? "Button.Primary" : "Button.Secondary";
        if (Application.Current.TryFindResource(key) is Style style)
            button.Style = style;
        return button;
    }
}
