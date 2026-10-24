using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed record CollisionChoice(FileIoCollision Collision, string Pattern);

public sealed class CollisionPrompt : Window
{
    private static readonly string[] Methods = ["UniqueName", "Skip", "Overwrite"];
    private static readonly string[] Patterns = [".#", ".##", ".###", ".####", ".#####", "A#", "A##", "A###", "A####", "A#####"];

    private readonly ComboBox _method;
    private readonly ComboBox _pattern;
    private readonly TextBlock _patternLabel;
    private readonly TextBlock _hint;

    private CollisionPrompt(string verb, string current, string pattern)
    {
        Title = "Automatic Collision Handling (OFF)";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = Brush("Vestigium.Brushes.Surface.Window") ?? SystemColors.WindowBrush;
        Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush;

        var title = Text(verb, "TextBlock.Caption");
        title.FontSize = 16;
        title.Margin = new Thickness(0, 0, 0, 8);

        var body = Text(
            "Automatic Collision Handling is turned off. Choose how this " + verb.ToLowerInvariant() + " should treat a name that already exists.",
            "TextBlock.Standard");
        body.TextWrapping = TextWrapping.Wrap;
        body.Margin = new Thickness(0, 0, 0, 16);

        _method = Combo(Methods, Methods.Contains(current) ? current : "UniqueName");
        _method.SelectionChanged += (_, _) => ShowPattern();

        _patternLabel = Text("Unique name pattern", "Label.Standard");
        _patternLabel.Margin = new Thickness(0, 12, 0, 4);
        _pattern = Combo(Patterns, Patterns.Contains(pattern) ? pattern : ".##");

        _hint = Text(HintOf("UniqueName"), "TextBlock.Caption");
        _hint.TextWrapping = TextWrapping.Wrap;
        _hint.Margin = new Thickness(0, 12, 0, 0);

        var form = new StackPanel();
        form.Children.Add(Text("Method", "Label.Standard"));
        form.Children.Add(_method);
        form.Children.Add(_patternLabel);
        form.Children.Add(_pattern);
        form.Children.Add(_hint);

        var ok = Button("Continue", true);
        ok.IsDefault = true;
        ok.Click += (_, _) => DialogResult = true;
        var cancel = Button("Cancel", false);
        cancel.IsCancel = true;
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        actions.Children.Add(ok);
        actions.Children.Add(cancel);

        var root = new DockPanel { Margin = new Thickness(24), Background = Background };
        DockPanel.SetDock(title, Dock.Top);
        DockPanel.SetDock(body, Dock.Top);
        DockPanel.SetDock(actions, Dock.Bottom);
        root.Children.Add(title);
        root.Children.Add(body);
        root.Children.Add(actions);
        root.Children.Add(form);
        Content = root;
        ShowPattern();
    }

    public CollisionChoice Choice
        => new(CollisionOf(_method.SelectedItem as string), _pattern.SelectedItem as string ?? ".##");

    public static CollisionChoice? Ask(Window? owner, string verb, string current, string pattern)
    {
        var window = new CollisionPrompt(verb, current, pattern) { Owner = owner };
        return window.ShowDialog() == true ? window.Choice : null;
    }

    private void ShowPattern()
    {
        var method = _method.SelectedItem as string ?? "UniqueName";
        var unique = method == "UniqueName";
        _patternLabel.Visibility = unique ? Visibility.Visible : Visibility.Collapsed;
        _pattern.Visibility = unique ? Visibility.Visible : Visibility.Collapsed;
        _hint.Text = HintOf(method);
    }

    private static string HintOf(string method)
        => method switch
        {
            "Skip" => "A name that already exists is left as it is. The new file is not written.",
            "Overwrite" => "A name that already exists is replaced. This cannot be undone.",
            _ => "The existing file is kept. The new file is written with a unique name."
        };

    private static FileIoCollision CollisionOf(string? name)
        => name switch
        {
            "Skip" => FileIoCollision.Skip,
            "Overwrite" => FileIoCollision.Overwrite,
            _ => FileIoCollision.UniqueName
        };

    private static ComboBox Combo(string[] items, string selected)
        => new()
        {
            ItemsSource = items,
            SelectedItem = selected,
            IsEditable = false,
            Width = 240,
            HorizontalAlignment = HorizontalAlignment.Left,
            Style = Application.Current.TryFindResource("ComboBox.Standard") as Style
        };

    private static TextBlock Text(string value, string style)
        => new()
        {
            Text = value,
            Style = Application.Current.TryFindResource(style) as Style,
            Foreground = Brush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush
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
