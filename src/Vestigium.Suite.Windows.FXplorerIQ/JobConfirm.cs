using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class JobConfirm : Window
{
    private readonly TextBox _destination;

    public JobConfirm(string title, string source, string? suggested)
    {
        Title = title;
        Width = 560;
        Height = 280;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = Application.Current.TryFindResource("Brush.Surface.Background") as System.Windows.Media.Brush
                     ?? SystemColors.WindowBrush;

        var sourceBox = Box(source, true);
        _destination = Box(suggested ?? "", false);

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

        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(Label("Source"));
        root.Children.Add(sourceBox);
        root.Children.Add(Label("Destination"));
        root.Children.Add(row);
        root.Children.Add(actions);
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
        DialogResult = true;
    }

    private static TextBlock Label(string text)
        => new()
        {
            Text = text,
            Style = Application.Current.TryFindResource("TextBlock.Caption") as Style,
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
