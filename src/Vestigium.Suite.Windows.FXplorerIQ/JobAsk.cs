using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class JobAsk : Window
{
    public JobAsk(string title, string message, IReadOnlyList<string>? paths)
    {
        Title = title;
        Width = 520;
        Height = paths is { Count: > 0 } ? 420 : 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = Brush("Vestigium.Brushes.Surface.Window") ?? SystemColors.WindowBrush;
        Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush;

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Style = Application.Current.TryFindResource("TextBlock.Standard") as Style,
            Foreground = Foreground,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var ok = Button("OK", true);
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

        var root = new DockPanel { Margin = new Thickness(20), Background = Background };
        DockPanel.SetDock(text, Dock.Top);
        DockPanel.SetDock(actions, Dock.Bottom);
        root.Children.Add(text);
        root.Children.Add(actions);
        if (paths is { Count: > 0 })
            root.Children.Add(List(paths));
        Content = root;
    }

    public static bool Ask(Window? owner, string title, string message, IReadOnlyList<string>? paths = null)
    {
        var window = new JobAsk(title, message, paths) { Owner = owner };
        return window.ShowDialog() == true;
    }

    private static ScrollViewer List(IReadOnlyList<string> paths)
    {
        var panel = new StackPanel();
        foreach (var path in paths)
        {
            panel.Children.Add(new TextBlock
            {
                Text = path,
                Style = Application.Current.TryFindResource("TextBlock.Caption") as Style,
                Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush,
                Margin = new Thickness(0, 2, 0, 2)
            });
        }
        return new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brush("Vestigium.Brushes.Surface.Card") ?? SystemColors.ControlBrush,
            Padding = new Thickness(8)
        };
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
