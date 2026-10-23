using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class FolderPicker : Window
{
    private readonly TreeView _tree = new();

    public FolderPicker(string? current)
    {
        Title = "Choose a folder";
        Width = 420;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush("Brush.Surface.Background") ?? SystemColors.WindowBrush;
        Foreground = Brush("Brush.Text.Primary") ?? SystemColors.WindowTextBrush;

        _tree.Margin = new Thickness(0, 0, 0, 12);
        _tree.Background = Brushes.Transparent;
        _tree.BorderThickness = new Thickness(0);
        _tree.Foreground = Foreground;
        if (Application.Current.TryFindResource("TreeView.Standard") is Style style)
            _tree.Style = style;

        var settings = App.Settings.Load();
        var pc = Group("This PC");
        var devices = Group("Devices & Drives");
        var network = Group("Network Locations");

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!Show(drive, settings))
                continue;
            var node = Folder(drive.RootDirectory.FullName, Label(drive));
            if (drive.DriveType == DriveType.Network)
                network.Items.Add(node);
            else if (drive.DriveType is DriveType.Fixed or DriveType.Removable or DriveType.CDRom)
                devices.Items.Add(node);
        }

        pc.Items.Add(devices);
        pc.Items.Add(network);
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

        var root = new DockPanel { Margin = new Thickness(16), Background = Background };
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

    private static TreeViewItem Group(string name)
        => new()
        {
            Header = name,
            Tag = "",
            IsExpanded = true,
            Foreground = Brush("Brush.Text.Primary") ?? SystemColors.WindowTextBrush
        };

    private TreeViewItem Folder(string path, string label)
    {
        var item = new TreeViewItem
        {
            Header = label,
            Tag = path,
            Foreground = Brush("Brush.Text.Primary") ?? SystemColors.WindowTextBrush
        };
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
                var child = new TreeViewItem
                {
                    Header = System.IO.Path.GetFileName(folder),
                    Tag = folder,
                    Foreground = Brush("Brush.Text.Primary") ?? SystemColors.WindowTextBrush
                };
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
        if (string.IsNullOrWhiteSpace(current) || _tree.Items[0] is not TreeViewItem pc)
            return;

        foreach (TreeViewItem group in pc.Items)
        {
            foreach (TreeViewItem drive in group.Items)
            {
                if (drive.Tag is not string root)
                    continue;
                if (!current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    continue;
                group.IsExpanded = true;
                drive.IsExpanded = true;
                drive.IsSelected = true;
                return;
            }
        }
    }

    private static bool Show(DriveInfo drive, FXplorerSettings settings)
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

    private static string Label(DriveInfo drive)
    {
        var letter = drive.Name.TrimEnd('\\');
        var kind = drive.DriveType switch
        {
            DriveType.Fixed => "Local Disk",
            DriveType.Removable => "Removable Disk",
            DriveType.CDRom => "CD Drive",
            DriveType.Network => "Network",
            _ => "Drive"
        };
        var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? kind : drive.VolumeLabel;
        return label + " (" + letter + ")";
    }

    private static Brush? Brush(string key)
        => Application.Current.TryFindResource(key) as Brush;

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
