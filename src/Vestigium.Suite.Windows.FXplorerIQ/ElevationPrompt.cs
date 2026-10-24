using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ElevationPrompt : Window
{
    private ElevationPrompt(string verb, string path)
    {
        Title = "Elevation required";
        Width = 520;
        Height = 280;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Background = Brush("Vestigium.Brushes.Surface.Window") ?? SystemColors.WindowBrush;
        Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush;

        var body = new TextBlock
        {
            Text = verb + " needs administrator permission for this path. Continue and Windows will ask once. The job then runs in its own elevated window.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Foreground,
            Margin = new Thickness(0, 0, 0, 16)
        };
        var pathBox = new TextBox
        {
            Text = path,
            IsReadOnly = true,
            Style = Application.Current.TryFindResource("TextBox.Standard") as Style,
            MinHeight = 36
        };

        var ok = Button("Continue", true);
        ok.IsDefault = true;
        ok.Click += (_, _) => DialogResult = true;
        var cancel = Button("Cancel", false);
        cancel.IsCancel = true;
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        actions.Children.Add(ok);
        actions.Children.Add(cancel);

        var root = new StackPanel { Margin = new Thickness(20), Background = Background };
        root.Children.Add(body);
        root.Children.Add(Label("Path"));
        root.Children.Add(pathBox);
        root.Children.Add(actions);
        Content = root;
    }

    public static bool Ask(Window? owner, string verb, string path)
    {
        var window = new ElevationPrompt(verb, path);
        if (owner is not null)
            window.Owner = owner;
        return window.ShowDialog() == true;
    }

    private static TextBlock Label(string text)
        => new()
        {
            Text = text,
            Style = Application.Current.TryFindResource("TextBlock.Caption") as Style,
            Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush,
            Margin = new Thickness(0, 0, 0, 4)
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
