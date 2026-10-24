using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class JobConfirm : Window
{
    private readonly TextBox _destination;
    private readonly string _source;
    private readonly TextBlock _hint;

    public JobConfirm(string title, string source, string? suggested)
    {
        Title = title;
        Width = 560;
        Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = Brush("Vestigium.Brushes.Surface.Window") ?? SystemColors.WindowBrush;
        Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush;

        _source = source;
        var sourceBox = Box(source, true);
        _destination = Box(suggested ?? "", false);
        _hint = new TextBlock
        {
            Foreground = Brush("Vestigium.Brushes.Status.Error") ?? Brushes.IndianRed,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var browse = Button("Browse", false);
        browse.Click += (_, _) => Browse();
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(browse, Dock.Right);
        row.Children.Add(browse);
        row.Children.Add(_destination);

        var ok = Button("OK", true);
        ok.IsDefault = true;
        ok.Click += (_, _) => Accept();
        var cancel = Button("Cancel", false);
        cancel.IsCancel = true;

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(ok);
        actions.Children.Add(cancel);

        var form = new StackPanel();
        form.Children.Add(Label("Source"));
        form.Children.Add(sourceBox);
        form.Children.Add(Label("Destination"));
        form.Children.Add(row);
        form.Children.Add(_hint);

        var root = new DockPanel { Margin = new Thickness(20), Background = Background };
        DockPanel.SetDock(actions, Dock.Bottom);
        root.Children.Add(actions);
        root.Children.Add(form);
        Content = root;
    }

    public string Destination => _destination.Text.Trim();

    public static string? Ask(Window? owner, string title, string source, string? suggested)
    {
        var window = new JobConfirm(title, source, suggested) { Owner = owner };
        return window.ShowDialog() == true ? window.Destination : null;
    }

    private void Browse()
    {
        var picked = FolderPicker.Ask(this, Destination);
        if (picked is null)
            return;
        _destination.Text = picked;
    }

    private void Accept()
    {
        if (Destination.Length == 0)
            return;
        if (Same(_source, Destination))
        {
            _hint.Text = "Source and destination are the same path.";
            return;
        }
        DialogResult = true;
    }

    private static bool Same(string left, string right)
    {
        var a = left.Trim().TrimEnd('\\', '/');
        var b = right.Trim().TrimEnd('\\', '/');
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static TextBlock Label(string text)
        => new()
        {
            Text = text,
            Style = Application.Current.TryFindResource("TextBlock.Caption") as Style,
            Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush,
            Margin = new Thickness(0, 8, 0, 4)
        };

    private static TextBox Box(string text, bool readOnly)
        => new()
        {
            Text = text,
            IsReadOnly = readOnly,
            Style = Application.Current.TryFindResource("TextBox.Standard") as Style,
            MinHeight = 36
        };

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
