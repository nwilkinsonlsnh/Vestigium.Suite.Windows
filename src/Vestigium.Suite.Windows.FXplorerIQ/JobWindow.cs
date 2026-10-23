using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class JobWindow : Window
{
    private readonly FileIoJob _job;
    private readonly TextBlock _phase = new();
    private readonly ProgressBar _bar = new();
    private readonly TextBlock _rate = new();
    private readonly TextBlock[] _bins = new TextBlock[5];
    private readonly Border[] _cards = new Border[5];
    private readonly StackPanel _files = new();
    private readonly Button _pause;
    private readonly Button _resume;
    private readonly Button _cancel;
    private readonly Button _close;
    private DateTimeOffset _painted = DateTimeOffset.MinValue;
    private bool _queued;

    public JobWindow(string title, FileIoJob job, string source, string? destination)
    {
        _job = job;
        Title = title;
        Width = 760;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = TryBrush("Vestigium.Brushes.Surface.Window") ?? SystemColors.WindowBrush;
        Foreground = TryBrush("Vestigium.Brushes.Text.Primary") ?? SystemColors.WindowTextBrush;

        _phase.Text = "Starting";
        _phase.Style = TryStyle("TextBlock.Title");
        _phase.Margin = new Thickness(0, 0, 0, 12);

        _bar.Height = 16;
        _bar.Minimum = 0;
        _bar.Maximum = 100;
        _bar.Margin = new Thickness(0, 0, 0, 8);
        if (TryStyle("ProgressBar.Standard") is { } barStyle)
            _bar.Style = barStyle;

        _rate.Text = "Waiting for the first files.";
        _rate.Style = TryStyle("TextBlock.Caption");
        _rate.Margin = new Thickness(0, 0, 0, 16);

        var bins = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 16) };
        var cardBrush = TryBrush("Vestigium.Brushes.Surface.Card") ?? SystemColors.ControlBrush;
        for (var i = 0; i < 5; i++)
        {
            _bins[i] = new TextBlock { Text = NameOf(i), Margin = new Thickness(8), Style = TryStyle("TextBlock.Caption") };
            _cards[i] = new Border
            {
                Margin = new Thickness(4),
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(6),
                Background = cardBrush,
                Child = _bins[i]
            };
            bins.Children.Add(_cards[i]);
        }

        var scroller = new ScrollViewer { Height = 180, Content = _files, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        _pause = Button("Pause", "Button.Secondary", () => Safe(() => _job.Pause()));
        _resume = Button("Resume", "Button.Secondary", () => Safe(() => _job.Resume()));
        _cancel = Button("Cancel", "Button.Ghost", () => Safe(() => _job.Cancel()));
        _close = Button("Close", "Button.Primary", Close);
        _close.IsEnabled = false;
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(_pause);
        actions.Children.Add(_resume);
        actions.Children.Add(_cancel);
        actions.Children.Add(_close);

        var root = new StackPanel { Margin = new Thickness(20), Background = Background };
        root.Children.Add(_phase);
        root.Children.Add(Path("Source", source));
        if (!string.IsNullOrWhiteSpace(destination))
            root.Children.Add(Path("Destination", destination));
        root.Children.Add(_bar);
        root.Children.Add(_rate);
        root.Children.Add(bins);
        root.Children.Add(scroller);
        root.Children.Add(actions);
        Content = root;

        _job.ProgressChanged += (_, progress) => Queue(progress);
        Loaded += async (_, _) => await Run();
    }

    public FileIoJobResult? Result { get; private set; }

    private void Queue(FileIoProgress progress)
    {
        if (_queued)
            return;
        _queued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _queued = false;
            _painted = DateTimeOffset.UtcNow;
            Show(progress);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private async Task Run()
    {
        try
        {
            Result = await _job.RunAsync().ConfigureAwait(true);
            _phase.Text = Result.Status;
            _pause.IsEnabled = false;
            _resume.IsEnabled = false;
            _cancel.IsEnabled = false;
            _close.IsEnabled = true;
        }
        catch (Exception ex)
        {
            if (IsCatalog(ex))
            {
                _rate.Text = "Log skipped. The job is still running.";
                return;
            }

            _phase.Text = "Failed";
            _rate.Text = ex.Message;
            _close.IsEnabled = true;
        }
    }

    private void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (IsCatalog(ex))
        {
            _rate.Text = "Log skipped. The job is still running.";
        }
    }

    private static bool IsCatalog(Exception ex)
        => ex.Message.Contains("EVENTID", StringComparison.OrdinalIgnoreCase)
           && ex.Message.Contains("catalog", StringComparison.OrdinalIgnoreCase);

    private void Show(FileIoProgress progress)
    {
        _phase.Text = progress.IsPaused ? "Paused  " + progress.Phase : progress.Phase;
        var found = Math.Max(progress.FilesFound, 1);
        _bar.Value = Math.Min(100, progress.FilesDone * 100d / found);
        var rate = progress.RateBytesPerSec > 0 ? Size(progress.RateBytesPerSec) + "/s" : "—";
        var eta = progress.EtaUtc is { } when ? when.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture) : "—";
        _rate.Text = progress.FilesDone + " / " + progress.FilesFound
            + "   " + Size(progress.BytesDone) + " / " + Size(progress.BytesFound)
            + "   " + rate + "   eta " + eta;

        for (var i = 0; i < progress.Buckets.Length && i < 5; i++)
        {
            var bin = progress.Buckets[i];
            var closed = bin.Found > 0 && bin.Done + bin.Skipped + bin.Failed >= bin.Found;
            _bins[i].Text = bin.Name + "\n" + bin.Done + " / " + bin.Found + "\nactive " + bin.Active;
            _cards[i].Opacity = closed ? 0.45 : 1;
        }

        _files.Children.Clear();
        var shown = 0;
        foreach (var file in progress.Active)
        {
            if (shown++ >= 30)
                break;
            _files.Children.Add(Row(file));
        }
    }

    private static Grid Row(FileIoFileProgress file)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        var name = new TextBlock
        {
            Text = file.RelativePath,
            Style = TryStyle("TextBlock.Caption"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        var bucket = new TextBlock { Text = file.Bucket, Style = TryStyle("TextBlock.Caption"), VerticalAlignment = VerticalAlignment.Center };
        var size = new TextBlock { Text = Size(file.Size), Style = TryStyle("TextBlock.Caption"), VerticalAlignment = VerticalAlignment.Center };
        var bar = new ProgressBar { Height = 10, Minimum = 0, Maximum = Math.Max(file.Size, 1), Value = file.BytesDone, VerticalAlignment = VerticalAlignment.Center };
        if (TryStyle("ProgressBar.Standard") is { } style)
            bar.Style = style;
        var status = new TextBlock { Text = file.Reason ?? file.Status, Style = TryStyle("TextBlock.Caption"), VerticalAlignment = VerticalAlignment.Center };

        Grid.SetColumn(name, 0);
        Grid.SetColumn(bucket, 1);
        Grid.SetColumn(size, 2);
        Grid.SetColumn(bar, 3);
        Grid.SetColumn(status, 4);
        grid.Children.Add(name);
        grid.Children.Add(bucket);
        grid.Children.Add(size);
        grid.Children.Add(bar);
        grid.Children.Add(status);
        return grid;
    }

    private static StackPanel Path(string caption, string value)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(new TextBlock { Text = caption, Style = TryStyle("TextBlock.Caption") });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            Style = TryStyle("TextBlock.Body"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        return panel;
    }

    private static string NameOf(int index)
        => index switch
        {
            0 => "Tiny",
            1 => "Small",
            2 => "Medium",
            3 => "Large",
            _ => "Huge"
        };

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

    private static Brush? TryBrush(string key)
        => Application.Current.TryFindResource(key) as Brush;

    private static Style? TryStyle(string key)
        => Application.Current.TryFindResource(key) as Style;

    private static Button Button(string text, string styleKey, Action click)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(12, 6, 12, 6),
            MinWidth = 88
        };
        if (TryStyle(styleKey) is { } style)
            button.Style = style;
        button.Click += (_, _) => click();
        return button;
    }
}
