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
    private readonly Button _pause;
    private readonly Button _resume;
    private readonly Button _cancel;
    private readonly Button _close;

    public JobWindow(string title, FileIoJob job)
    {
        _job = job;
        Title = title;
        Width = 760;
        Height = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = TryBrush("Brush.Surface.Background") ?? SystemColors.WindowBrush;

        _phase.Text = "Starting";
        _phase.FontSize = 18;
        _phase.Margin = new Thickness(0, 0, 0, 8);

        _bar.Height = 16;
        _bar.Minimum = 0;
        _bar.Maximum = 100;
        _bar.Margin = new Thickness(0, 0, 0, 8);

        _rate.Text = "Waiting for the first files.";
        _rate.Margin = new Thickness(0, 0, 0, 16);

        var bins = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 16) };
        var cardBrush = TryBrush("Brush.Surface.Card") ?? SystemColors.ControlBrush;
        for (var i = 0; i < 5; i++)
        {
            _bins[i] = new TextBlock { Text = NameOf(i), Margin = new Thickness(8) };
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

        _pause = Button("Pause", () => Safe(() => _job.Pause()));
        _resume = Button("Resume", () => Safe(() => _job.Resume()));
        _cancel = Button("Cancel", () => Safe(() => _job.Cancel()));
        _close = Button("Close", Close);
        _close.IsEnabled = false;
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(_pause);
        actions.Children.Add(_resume);
        actions.Children.Add(_cancel);
        actions.Children.Add(_close);

        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(_phase);
        root.Children.Add(_bar);
        root.Children.Add(_rate);
        root.Children.Add(bins);
        root.Children.Add(actions);
        Content = root;

        _job.ProgressChanged += (_, progress) => Dispatcher.Invoke(() => Show(progress));
        Loaded += async (_, _) => await Run();
    }

    public FileIoJobResult? Result { get; private set; }

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

    private static Button Button(string text, Action click)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(12, 6, 12, 6)
        };
        if (Application.Current.TryFindResource("Button.Ghost") is Style style)
            button.Style = style;
        button.Click += (_, _) => click();
        return button;
    }
}
