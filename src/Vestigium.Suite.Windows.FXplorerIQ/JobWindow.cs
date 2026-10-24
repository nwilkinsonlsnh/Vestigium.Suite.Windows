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
    private string _report = "overview";
    private string _chart = "story";
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
        _cancel = Button("Cancel", "Button.Ghost", () =>
        {
            Safe(() => _job.Cancel());
            Close();
        });
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
        sub.Children.Add(Radio("Overview", "report", true, () => Pick("overview", "story")));
        sub.Children.Add(Radio("Shape", "report", false, () => Pick("shape", "types")));
        sub.Children.Add(Radio("Throughput", "report", false, () => Pick("rate", "rate")));
        sub.Children.Add(Radio("Time", "report", false, () => Pick("time", "share")));
        var head = new StackPanel();
        head.Children.Add(sub);
        head.Children.Add(_charts);
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(_story, Dock.Bottom);
        dashboard.Children.Add(head);
        dashboard.Children.Add(_story);
        dashboard.Children.Add(_stage);
        Pick("overview", "story");

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
            "shape" => new[] { ("types", "Types"), ("mix", "Mix"), ("weight", "Weight"), ("tiny", "Tiny"), ("small", "Small"), ("medium", "Medium"), ("large", "Large"), ("huge", "Huge"), ("iqr", "IQR") },
            "rate" => new[] { ("rate", "Rate"), ("bucket", "By bucket") },
            "time" => new[] { ("share", "Share"), ("duration", "Duration"), ("curve", "Curve") },
            _ => Array.Empty<(string, string)>()
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
            ("overview", _) => (Overview(stats), Sentence(stats)),
            ("shape", "weight") => (Weight(stats), "Where the bytes went. This often disagrees with the count pie."),
            ("shape", "types") => (Types(stats), "The file types in this job. Count is on the bar, size is in the label."),
            ("shape", "tiny") => (BucketSize(stats, "Tiny"), "Tiny files, doubled ranges from 0 to 256 KB. The count is on each bar."),
            ("shape", "small") => (BucketSize(stats, "Small"), "Small files, doubled ranges up to 4 MB."),
            ("shape", "medium") => (BucketSize(stats, "Medium"), "Medium files, doubled ranges up to 32 MB."),
            ("shape", "large") => (BucketSize(stats, "Large"), "Large files, doubled ranges up to 256 MB."),
            ("shape", "huge") => (BucketSize(stats, "Huge"), "Huge files, doubled ranges above 256 MB."),
            ("shape", "iqr") => (Iqr(stats), "The middle half of each bucket. Outliers are the files that stuck out."),
            ("shape", _) => (Mix(stats), "Where the files went. Five slices, one for each bucket."),
            ("rate", "bucket") => (BucketRate(stats), "Mean rate by bucket, in MB/s. The tall bar set the pace."),
            ("rate", _) => (Rate(stats), "How fast each file moved, in doubled MB/s ranges. The count is on each bar."),
            ("time", "duration") => (Duration(stats), "Median time per bucket, in milliseconds."),
            ("time", "curve") => (Curve(stats), "How long files took, smallest to largest."),
            ("time", _) => (Share(stats), "Which bucket held the clock."),
            _ => (Overview(stats), Sentence(stats))
        };

    private FrameworkElement Overview(FileIoJobStats stats)
    {
        var files = Mix(stats);
        var bytes = Weight(stats);
        files.VerticalAlignment = VerticalAlignment.Stretch;
        bytes.VerticalAlignment = VerticalAlignment.Stretch;
        var line = new TextBlock
        {
            Text = Summary(stats),
            Style = TryStyle("TextBlock.Body"),
            Foreground = Foreground,
            Margin = new Thickness(0, 4, 0, 4)
        };
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(line, 0);
        Grid.SetRow(files, 1);
        Grid.SetRow(bytes, 2);
        grid.Children.Add(line);
        grid.Children.Add(files);
        grid.Children.Add(bytes);
        return grid;
    }

    private FrameworkElement Types(FileIoJobStats stats)
    {
        var rows = stats.Extensions.Where(e => e.Count > 0).ToArray();
        if (rows.Length == 0)
            return Empty();
        return ChartView.From(new ChartSpec
        {
            Kind = ChartKind.Bar,
            Options = ChartTheme.Apply(new ChartOptions { Title = "Top extensions", XLabel = "Files", YLabel = "Type" }),
            Series =
            [
                new ChartSeries
                {
                    Y = rows.Select(e => (double)e.Count).ToArray(),
                    Labels = rows.Select(e => e.Name + "   " + e.Count.ToString("N0") + "   " + Size(e.Bytes)).ToArray()
                }
            ]
        });
    }

    private FrameworkElement Mix(FileIoJobStats stats)
        => Pie(stats, b => b.FileSizes.Count, b => b.Name + "  " + b.FileSizes.Count.ToString("N0"), "Files");

    private FrameworkElement Weight(FileIoJobStats stats)
        => Pie(stats, b => Sum(b.FileSizes), b => b.Name + "  " + Size((long)Sum(b.FileSizes)), "Bytes");

    private FrameworkElement Share(FileIoJobStats stats)
        => Pie(stats, b => Sum(b.Durations), b => b.Name + "  " + Millis(Sum(b.Durations)), "Time");

    private FrameworkElement Pie(FileIoJobStats stats, Func<FileIoBucketAnalytics, double> value, Func<FileIoBucketAnalytics, string> label, string title)
    {
        var slices = stats.Buckets
            .Select(b => new ChartSlice { Label = label(b), Value = value(b) })
            .Where(s => s.Value > 0)
            .ToList();
        return slices.Count == 0
            ? Empty()
            : ChartView.Pie(slices, ChartTheme.Apply(new ChartOptions { Title = title }));
    }

    private static string Millis(double ms)
        => ms >= 1000 ? (ms / 1000d).ToString("0.#") + " s" : ms.ToString("0") + " ms";

    private FrameworkElement BucketSize(FileIoJobStats stats, string name)
    {
        var bucket = stats.Buckets.FirstOrDefault(b => b.Name == name);
        if (bucket?.FileSizes.Series is not { Count: > 0 } series)
            return Empty();
        var kb = name == "Tiny";
        var scale = kb ? 1024m : 1024m * 1024m;
        var unit = kb ? "KB" : "MB";
        var values = series.Values.Select(v => (double)(v / scale)).ToArray();
        var (counts, labels) = LogBins(values, name);
        return ChartView.From(new ChartSpec
        {
            Kind = ChartKind.Bar,
            Options = ChartTheme.Apply(new ChartOptions { Title = name + " (" + unit + ")", XLabel = "Files", YLabel = unit }),
            Series = [new ChartSeries { Y = counts, Labels = labels }]
        });
    }

    private static (double[] Counts, string[] Labels) LogBins(double[] values, string name)
    {
        var edges = Edges(name, values.Length == 0 ? 0 : values.Max());
        var counts = new double[edges.Length - 1];
        foreach (var value in values)
        {
            var bin = edges.Length - 2;
            for (var i = 0; i < edges.Length - 1; i++)
            {
                if (value <= edges[i + 1])
                {
                    bin = i;
                    break;
                }
            }
            counts[bin]++;
        }

        var labels = new string[counts.Length];
        for (var i = 0; i < labels.Length; i++)
            labels[i] = Range(edges[i], edges[i + 1]) + "   " + counts[i].ToString("N0");
        return (counts, labels);
    }

    private static double[] Edges(string name, double max)
    {
        var (start, end) = name switch
        {
            "Tiny" => (0d, 256d),
            "Small" => (0.25d, 4d),
            "Medium" => (4d, 32d),
            "Large" => (32d, 256d),
            _ => (256d, Math.Max(512d, max))
        };
        var edges = new List<double>();
        if (start == 0)
        {
            edges.Add(0);
            for (var edge = 1d; edge < end; edge *= 2)
                edges.Add(edge);
        }
        else
        {
            for (var edge = start; edge < end; edge *= 2)
                edges.Add(edge);
        }
        edges.Add(Math.Max(end, max));
        return edges.ToArray();
    }

    private static string Range(double low, double high)
        => (low >= 10 ? low.ToString("0") : low.ToString("0.##"))
            + "–"
            + (high >= 10 ? high.ToString("0") : high.ToString("0.##"));

    private FrameworkElement Iqr(FileIoJobStats stats)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        var accent = TryBrush("Vestigium.Brushes.Accent.Primary") ?? SystemColors.HighlightBrush;
        var track = TryBrush("Vestigium.Brushes.Surface.Card") ?? SystemColors.ControlBrush;
        foreach (var bucket in stats.Buckets)
            panel.Children.Add(BoxRow(bucket, accent, track));
        return panel;
    }

    private FrameworkElement BoxRow(FileIoBucketAnalytics bucket, Brush accent, Brush track)
    {
        var sizes = bucket.FileSizes;
        var name = new TextBlock
        {
            Text = bucket.Name,
            Width = 72,
            Style = TryStyle("TextBlock.Body"),
            Foreground = Foreground,
            VerticalAlignment = VerticalAlignment.Center
        };
        var plot = new Grid { Height = 28, Margin = new Thickness(8, 0, 12, 0) };
        if (sizes.Count > 0 && sizes.Min is { } min && sizes.Max is { } max && max > min
            && sizes.Q1 is { } q1 && sizes.Median is { } median && sizes.Q3 is { } q3)
        {
            var span = (double)(max - min);
            double At(decimal value) => (double)(value - min) / span;
            plot.Children.Add(new Border
            {
                Height = 2,
                Background = track,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0)
            });
            var box = new Border
            {
                Height = 16,
                Background = accent,
                Opacity = 0.85,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0)
            };
            box.Loaded += (_, _) =>
            {
                var width = plot.ActualWidth;
                if (width <= 0)
                    return;
                var left = At(q1) * width;
                var right = At(q3) * width;
                box.Margin = new Thickness(left, 0, 0, 0);
                box.Width = Math.Max(4, right - left);
            };
            plot.Children.Add(box);
            var mark = new Border
            {
                Width = 2,
                Background = Foreground,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            mark.Loaded += (_, _) =>
            {
                var width = plot.ActualWidth;
                if (width <= 0)
                    return;
                mark.Margin = new Thickness(At(median) * width, 4, 0, 4);
            };
            plot.Children.Add(mark);
        }

        var caption = new TextBlock
        {
            Text = sizes.Count == 0
                ? "—"
                : "n " + sizes.Count
                    + "   " + Human(sizes.Q1)
                    + " · " + Human(sizes.Median)
                    + " · " + Human(sizes.Q3)
                    + "   outliers " + sizes.HighOutlierCount,
            Style = TryStyle("TextBlock.Caption"),
            Foreground = Foreground,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        };
        var row = new DockPanel { Margin = new Thickness(0, 10, 0, 10) };
        DockPanel.SetDock(name, Dock.Left);
        DockPanel.SetDock(caption, Dock.Right);
        row.Children.Add(name);
        row.Children.Add(caption);
        row.Children.Add(plot);
        return row;
    }

    private FrameworkElement Rate(FileIoJobStats stats)
    {
        if (stats.TransferRates.Series is not { Count: > 0 } series)
            return Empty();
        var values = series.Values.Select(v => (double)v / (1024d * 1024d)).ToArray();
        var (counts, labels) = RateBins(values);
        return ChartView.From(new ChartSpec
        {
            Kind = ChartKind.Bar,
            Options = ChartTheme.Apply(new ChartOptions { Title = "Rate (MB/s)", XLabel = "Files", YLabel = "MB/s" }),
            Series = [new ChartSeries { Y = counts, Labels = labels }]
        });
    }

    private FrameworkElement BucketRate(FileIoJobStats stats)
    {
        var rows = stats.Buckets.Where(b => b.TransferRates.Mean is > 0).ToArray();
        if (rows.Length == 0)
            return Empty();
        return ChartView.From(new ChartSpec
        {
            Kind = ChartKind.Column,
            Options = ChartTheme.Apply(new ChartOptions { Title = "Mean rate (MB/s)", XLabel = "Bucket", YLabel = "MB/s" }),
            Series =
            [
                new ChartSeries
                {
                    Y = rows.Select(b => (b.TransferRates.Mean ?? 0) / (1024d * 1024d)).ToArray(),
                    Labels = rows.Select(b => b.Name).ToArray()
                }
            ]
        });
    }

    private static (double[] Counts, string[] Labels) RateBins(double[] values)
    {
        var max = values.Length == 0 ? 1 : Math.Max(1, values.Max());
        var edges = new List<double> { 0, 1 };
        for (var edge = 2d; edge < max; edge *= 2)
            edges.Add(edge);
        if (edges[^1] < max)
            edges.Add(NextPow2(max));
        var counts = new double[edges.Count - 1];
        foreach (var value in values)
        {
            var bin = counts.Length - 1;
            for (var i = 0; i < counts.Length; i++)
            {
                if (value <= edges[i + 1])
                {
                    bin = i;
                    break;
                }
            }
            counts[bin]++;
        }

        var labels = new string[counts.Length];
        for (var i = 0; i < labels.Length; i++)
            labels[i] = Range(edges[i], edges[i + 1]) + "   " + counts[i].ToString("N0");
        return (counts, labels);
    }

    private static double NextPow2(double value)
    {
        var edge = 1d;
        while (edge < value)
            edge *= 2;
        return edge;
    }

    private FrameworkElement Duration(FileIoJobStats stats)
    {
        var rows = stats.Buckets.Where(b => b.Durations.Median is > 0).ToArray();
        if (rows.Length == 0)
            return Empty();
        return ChartView.Column(
            NumericSeries.FromDecimal(rows.Select(b => b.Durations.Median ?? 0).ToArray(), "ms"),
            ChartTheme.Apply(new ChartOptions { Title = "Median ms" }));
    }

    private FrameworkElement Curve(FileIoJobStats stats)
        => stats.Durations.Series is { } s
            ? ChartView.Ecdf(s, ChartTheme.Apply(new ChartOptions { Title = "Duration" }))
            : Empty();

    private static double Sum(FileIoSeriesSnapshot snap)
        => snap.Series is { } s ? (double)s.Values.Sum() : 0;

    private static string Human(decimal? bytes)
        => bytes is { } n ? Size((long)n) : "—";

    private static string Summary(FileIoJobStats stats)
        => stats.ObservationCount + " files   "
            + Size((long)Sum(stats.FileSizes)) + "   "
            + stats.JobElapsedMs + " ms   "
            + Size((long)stats.JobRateBytesPerSec) + "/s";

    private static string Sentence(FileIoJobStats stats)
    {
        var counted = stats.Buckets.OrderByDescending(b => b.FileSizes.Count).FirstOrDefault();
        var heavy = stats.Buckets.OrderByDescending(b => Sum(b.FileSizes)).FirstOrDefault();
        if (counted is null || heavy is null || stats.ObservationCount == 0)
            return "No files in this job.";
        var files = stats.ObservationCount;
        var countShare = counted.FileSizes.Count * 100 / files;
        var bytes = Sum(stats.FileSizes);
        var weightShare = bytes > 0 ? (int)(Sum(heavy.FileSizes) * 100 / bytes) : 0;
        return counted.Name + " is " + countShare + "% of the files. "
            + heavy.Name + " is " + weightShare + "% of the bytes.";
    }

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
