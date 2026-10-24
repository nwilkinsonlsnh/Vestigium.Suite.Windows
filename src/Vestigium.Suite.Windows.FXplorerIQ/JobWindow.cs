using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
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
    private readonly ContentControl _page = new();
    private readonly StackPanel _charts = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
    private readonly ContentControl _stage = new();
    private readonly TextBlock _story = new();
    private readonly Brush _surface;
    private FileIoJobResult? _result;
    private string _report = "shape";
    private string _chart = "size";
    private readonly DispatcherTimer _pulse;
    private bool _queued;
    private DateTimeOffset _lastPaint = DateTimeOffset.MinValue;

    public JobWindow(string title, FileIoJob job, string source, string? destination)
    {
        _job = job;
        Title = title;
        Width = 860;
        Height = 740;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        _surface = TryBrush("Vestigium.Brushes.Surface.Window") ?? SystemColors.WindowBrush;
        Background = _surface;
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

        var bins = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 12) };
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

        var scroller = new ScrollViewer { Content = _files, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = _surface };

        _pause = Button("Pause", "Button.Secondary", () => Safe(() => _job.Pause()));
        _resume = Button("Resume", "Button.Secondary", () => Safe(() => _job.Resume()));
        _cancel = Button("Cancel", "Button.Ghost", () => Safe(() => _job.Cancel()));
        _close = Button("Close", "Button.Primary", Close);
        _close.IsEnabled = false;
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 12, 0, 0)
        };
        actions.Children.Add(_pause);
        actions.Children.Add(_resume);
        actions.Children.Add(_cancel);
        actions.Children.Add(_close);

        var body = new StackPanel();
        body.Children.Add(_phase);
        body.Children.Add(Path("Source", source));
        if (!string.IsNullOrWhiteSpace(destination))
            body.Children.Add(Path("Destination", destination));
        body.Children.Add(_bar);
        body.Children.Add(_rate);
        body.Children.Add(bins);

        var action = new DockPanel { Background = _surface };
        DockPanel.SetDock(actions, Dock.Bottom);
        DockPanel.SetDock(body, Dock.Top);
        action.Children.Add(actions);
        action.Children.Add(body);
        action.Children.Add(scroller);

        _story.Style = TryStyle("TextBlock.Body");
        _story.Foreground = Foreground;
        _story.TextWrapping = TextWrapping.Wrap;
        _story.Margin = new Thickness(0, 8, 0, 0);
        _story.Text = "The report fills when the job finishes.";

        var dashboard = new DockPanel { Background = _surface };
        var sub = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        sub.Children.Add(Radio("Shape", "report", true, () => Pick("shape", "size")));
        sub.Children.Add(Radio("Throughput", "report", false, () => Pick("rate", "rate")));
        sub.Children.Add(Radio("Time", "report", false, () => Pick("time", "perfile")));
        var head = new StackPanel();
        head.Children.Add(sub);
        head.Children.Add(_charts);
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(_story, Dock.Bottom);
        dashboard.Children.Add(head);
        dashboard.Children.Add(_story);
        dashboard.Children.Add(_stage);
        Pick("shape", "size");

        var main = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        main.Children.Add(Radio("Action", "main", true, () => _page.Content = action));
        main.Children.Add(Radio("Dashboard", "main", false, () => _page.Content = dashboard));

        _page.Content = action;
        var root = new DockPanel { Margin = new Thickness(16), Background = _surface };
        DockPanel.SetDock(main, Dock.Top);
        root.Children.Add(main);
        root.Children.Add(_page);
        Content = root;

        Closing += (_, _) =>
        {
            if (_result is null)
                Safe(() => _job.Cancel());
        };

        _job.ProgressChanged += (_, progress) => Queue(progress);
        _pulse = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _pulse.Tick += (_, _) => Show(_job.Progress);
        ContentRendered += async (_, _) => await Run();
    }

    public FileIoJobResult? Result => _result;

    public bool AutoClose { get; set; }

    private void Pick(string report, string chart)
    {
        _report = report;
        _chart = chart;
        BuildMenu();
        ShowChart();
    }

    private void BuildMenu()
    {
        _charts.Children.Clear();
        var items = _report switch
        {
            "rate" => new[] { ("rate", "Rate"), ("bucket", "By bucket") },
            "time" => new[] { ("perfile", "Per file"), ("curve", "Curve") },
            _ => new[] { ("size", "Size"), ("spread", "Spread"), ("buckets", "Buckets") }
        };
        foreach (var (key, label) in items)
            _charts.Children.Add(Radio(label, "chart", key == _chart, () => { _chart = key; ShowChart(); }));
    }

    private void ShowChart()
    {
        if (_result?.Stats is not { } stats)
        {
            _stage.Content = null;
            _story.Text = "The report fills when the job finishes.";
            return;
        }

        try
        {
            var (view, story) = ChartOf(stats);
            _stage.Content = view;
            _story.Text = story;
        }
        catch (Exception ex)
        {
            _stage.Content = null;
            _story.Text = ex.Message;
        }
    }

    private (FrameworkElement View, string Story) ChartOf(FileIoJobStats stats)
        => (_report, _chart) switch
        {
            ("rate", "bucket") => (BucketRate(stats), "Mean rate by bucket. Tall bars are the fast lanes."),
            ("rate", _) => (Rate(stats), "How fast each file moved. The peak is the common rate."),
            ("time", "curve") => (Curve(stats), "How long files took, smallest to largest. A steep curve means most finished together."),
            ("time", _) => (Duration(stats), "Time per file. A long tail means a few files held the job."),
            (_, "spread") => (Spread(stats), "The size spread. The box is the middle half. Dots past the whiskers are outliers."),
            (_, "buckets") => (Buckets(stats), "How the files split across the five size lanes."),
            _ => (Size(stats), "The shape of the files. Most of the work sits under the tallest bars.")
        };

    private FrameworkElement Size(FileIoJobStats stats)
        => stats.FileSizes.Series is { } s
            ? ChartView.Histogram(s, ChartTheme.Apply(new ChartOptions { Title = "File size" }))
            : Empty();

    private FrameworkElement Spread(FileIoJobStats stats)
        => stats.FileSizes.Series is { } s
            ? ChartView.Box(s, ChartTheme.Apply(new ChartOptions { Title = "Size" }))
            : Empty();

    private FrameworkElement Buckets(FileIoJobStats stats)
    {
        var slices = stats.Buckets.Where(b => b.FileSizes.Count > 0).Select(b => new ChartSlice { Label = b.Name, Value = b.FileSizes.Count }).ToList();
        return slices.Count == 0 ? Empty() : ChartView.Pareto(slices, ChartTheme.Apply(new ChartOptions { Title = "Files by bucket" }));
    }

    private FrameworkElement Rate(FileIoJobStats stats)
        => stats.TransferRates.Series is { } s
            ? ChartView.Histogram(s, ChartTheme.Apply(new ChartOptions { Title = "Rate" }))
            : Empty();

    private FrameworkElement BucketRate(FileIoJobStats stats)
    {
        var rows = stats.Buckets.Where(b => b.TransferRates.Mean is > 0).ToArray();
        if (rows.Length == 0)
            return Empty();
        return ChartView.Column(
            NumericSeries.FromDecimal(rows.Select(b => (decimal)(b.TransferRates.Mean ?? 0)).ToArray(), "rate"),
            ChartTheme.Apply(new ChartOptions { Title = "Mean rate" }));
    }

    private FrameworkElement Duration(FileIoJobStats stats)
        => stats.Durations.Series is { } s
            ? ChartView.Histogram(s, ChartTheme.Apply(new ChartOptions { Title = "Duration ms" }))
            : Empty();

    private FrameworkElement Curve(FileIoJobStats stats)
        => stats.Durations.Series is { } s
            ? ChartView.Ecdf(s, ChartTheme.Apply(new ChartOptions { Title = "Duration" }))
            : Empty();

    private DateTimeOffset _lastPaint = DateTimeOffset.MinValue;

    private void Queue(FileIoProgress progress)
    {
        if (_queued)
            return;
        _queued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _queued = false;
            _lastPaint = DateTimeOffset.UtcNow;
            Show(progress);
        }, DispatcherPriority.Render);
    }

    private async Task Run()
    {
        _pulse.Start();
        try
        {
            _result = await _job.RunAsync().ConfigureAwait(true);
            _pulse.Stop();
            _phase.Text = _result.Status;
            _pause.IsEnabled = false;
            _resume.IsEnabled = false;
            _cancel.IsEnabled = false;
            _close.IsEnabled = true;
            _files.Children.Clear();
            Show(_job.Progress);
            ShowChart();
            if (AutoClose)
                Close();
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
        finally
        {
            _pulse.Stop();
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
        Set(_bar, Math.Min(100, progress.FilesDone * 100d / found), 100);
        var rate = progress.RateBytesPerSec > 0 ? Size(progress.RateBytesPerSec) + "/s" : "—";
        var eta = progress.EtaUtc is { } when ? when.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture) : "—";
        _rate.Text = progress.FilesDone + " / " + progress.FilesFound
            + "   " + Size(progress.BytesDone) + " / " + Size(progress.BytesFound)
            + "   " + rate + "   eta " + eta;

        for (var i = 0; i < progress.Buckets.Length && i < 5; i++)
        {
            var bin = progress.Buckets[i];
            var closed = bin.Found > 0 && bin.Done + bin.Skipped + bin.Failed >= bin.Found;
            _bins[i].Text = bin.Name + "\n" + bin.Done + " / " + bin.Found
                + (bin.Failed > 0 ? "\nfailed " + bin.Failed : "")
                + "\nactive " + bin.Active;
            _cards[i].Opacity = closed ? 0.45 : 1;
        }

        _files.Children.Clear();
        if (_result is not null)
            return;
        var shown = 0;
        foreach (var file in progress.Active)
        {
            if (file.Status == "Done")
                continue;
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
        var max = Math.Max(file.Size, 1);
        var bar = new ProgressBar { Height = 10, Minimum = 0, Maximum = max, VerticalAlignment = VerticalAlignment.Center };
        Set(bar, file.BytesDone, max);
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

    private static void Set(ProgressBar bar, double value, double max)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            value = 0;
        if (value < bar.Minimum)
            value = bar.Minimum;
        if (value > max)
            value = max;
        bar.Value = value;
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

    private RadioButton Radio(string text, string group, bool check, Action click)
    {
        var radio = new RadioButton
        {
            Content = text,
            GroupName = group,
            IsChecked = check,
            Style = TryStyle("RadioButton.UnderlineTab"),
            Foreground = Foreground
        };
        radio.Checked += (_, _) => click();
        return radio;
    }

    private TextBlock Empty()
        => new()
        {
            Text = "Nothing to chart.",
            Style = TryStyle("TextBlock.Body"),
            Foreground = Foreground,
            Margin = new Thickness(0, 12, 0, 0)
        };

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
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(12, 6, 12, 6),
            MinWidth = 88
        };
        if (TryStyle(styleKey) is { } style)
            button.Style = style;
        button.Click += (_, _) => click();
        return button;
    }
}
