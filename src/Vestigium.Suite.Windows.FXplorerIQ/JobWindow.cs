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

    private void Queue(FileIoProgress progress)
    {
        if (_queued)
            return;
        var wait = 200 - (int)(DateTimeOffset.UtcNow - _painted).TotalMilliseconds;
        if (wait < 0)
            wait = 0;
        _queued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _queued = false;
            _painted = DateTimeOffset.UtcNow;
            Show(progress);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }