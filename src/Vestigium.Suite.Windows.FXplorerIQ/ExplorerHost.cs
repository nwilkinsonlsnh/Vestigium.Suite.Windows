using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ExplorerHost : DockPanel
{
    private readonly Grid _body = new();
    private readonly GridSplitter _split = new();
    private readonly StackPanel _jobBar;
    private readonly TextBlock _sourceText;
    private readonly TextBlock _destinationText;
    private readonly MenuItem _splitItem;
    private readonly MenuItem _verticalItem;
    private readonly MenuItem _horizontalItem;
    private readonly MenuItem _swapItem;
    private readonly Action<string> _status;
    private ScrollViewer? _viewport;
    private bool _applying;
    private bool _busy;

    public ExplorerHost(Action<string> status)
    {
        _status = status;
        LastChildFill = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = true;
        First = new ExplorerPane { Status = status, FillCell = true, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        Second = new ExplorerPane { Status = status, FillCell = true, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        First.Navigated += _ => RefreshMarks();
        Second.Navigated += _ => RefreshMarks();
        Loaded += (_, _) => BindViewport();
        SizeChanged += (_, _) => Fit();

        _splitItem = Item("Split", Split_Click);
        _verticalItem = Item("Vertical", () => SetVertical(true));
        _horizontalItem = Item("Horizontal", () => SetVertical(false));
        _swapItem = Item("Swap source and destination", Swap_Click);
        var menu = new ContextMenu();
        menu.Items.Add(Item("Copy", () => Verb(JobVerb.Copy, false)));
        menu.Items.Add(Item("Copy (Verify)", () => Verb(JobVerb.Copy, true)));
        menu.Items.Add(Item("Move", () => Verb(JobVerb.Move, false)));
        menu.Items.Add(Item("Move (Verify)", () => Verb(JobVerb.Move, true)));
        menu.Items.Add(Item("Delete", () => Verb(JobVerb.Delete, false)));
        menu.Items.Add(Item("Delete (Secure)", () => Verb(JobVerb.Delete, true)));
        menu.Items.Add(Item("Mirror (Verify)", () => Verb(JobVerb.Mirror, true)));
        menu.Items.Add(Item("Audit", Audit_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(_splitItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(_verticalItem);
        menu.Items.Add(_horizontalItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(_swapItem);
        menu.Opened += (_, _) => RefreshMenu();
        ContextMenu = menu;
        PreviewMouseRightButtonUp += OpenMenu;

        _sourceText = Mark("Source");
        _destinationText = Mark("Destination");
        _jobBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 8, 12, 0),
            Visibility = Visibility.Collapsed
        };
        _jobBar.Children.Add(_sourceText);
        _jobBar.Children.Add(_destinationText);
        Children.Add(_jobBar);
        DockPanel.SetDock(_jobBar, Dock.Top);

        _body.HorizontalAlignment = HorizontalAlignment.Stretch;
        _body.VerticalAlignment = VerticalAlignment.Stretch;
        _split.HorizontalAlignment = HorizontalAlignment.Stretch;
        _split.VerticalAlignment = VerticalAlignment.Stretch;
        _split.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
        _split.ShowsPreview = false;
        _split.DragCompleted += Split_DragCompleted;
        Children.Add(_body);

        Apply(false, App.Settings.Load());
    }

    public ExplorerPane First { get; }

    public ExplorerPane Second { get; }

    public bool IsSplit { get; private set; }

    public bool Vertical { get; private set; } = true;

    public double Ratio { get; private set; } = 0.5;

    public bool SourceIsFirst { get; private set; } = true;

    public string SourcePath => (SourceIsFirst ? First : Second).Folder;

    public string DestinationPath => (SourceIsFirst ? Second : First).Folder;

    public event Action? Changed;

    public void Apply(bool split, FXplorerSettings settings, string? secondPath = null, bool? sourceIsFirst = null)
    {
        _applying = true;
        try
        {
            Vertical = settings.SplitVertical;
            Ratio = settings.SplitRatio is > 0.1 and < 0.9 ? settings.SplitRatio : 0.5;
            IsSplit = split;
            if (sourceIsFirst is not null)
                SourceIsFirst = sourceIsFirst.Value;
            if (!string.IsNullOrWhiteSpace(secondPath))
                Second.Seed(secondPath);
            Layout();
            RefreshMarks();
            Fit();
        }
        finally
        {
            _applying = false;
        }
    }

    private void BindViewport()
    {
        _viewport = FindViewport();
        if (_viewport is not null)
            _viewport.SizeChanged += (_, _) => Fit();

        var window = Window.GetWindow(this);
        if (window is not null)
            window.SizeChanged += (_, _) => Fit();
        Fit();
    }

    private void Fit()
    {
        var width = _viewport?.ViewportWidth ?? 0;
        var height = _viewport?.ViewportHeight ?? 0;
        if (width < 100 || height < 100)
        {
            var window = Window.GetWindow(this);
            if (window is null)
                return;
            width = Math.Max(width, window.ActualWidth - 24);
            height = Math.Max(height, window.ActualHeight - 120);
        }

        if (width < 100 || height < 100)
            return;

        Width = width;
        Height = height;
        _body.Width = double.NaN;
        _body.Height = double.NaN;
    }

    private ScrollViewer? FindViewport()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is ScrollViewer viewer)
                return viewer;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void OpenMenu(object sender, MouseButtonEventArgs e)
    {
        if (ContextMenu is null)
            return;
        RefreshMenu();
        ContextMenu.PlacementTarget = this;
        ContextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void Verb(JobVerb verb, bool verify)
    {
        if (_busy)
        {
            _status("A job is already running.");
            return;
        }

        var sourcePane = SourceIsFirst ? First : Second;
        var destPane = SourceIsFirst ? Second : First;
        var source = sourcePane.SelectionOrFolder();
        string? destination = null;

        if (verb != JobVerb.Delete)
        {
            var suggested = IsSplit && !string.IsNullOrWhiteSpace(destPane.Folder) ? destPane.Folder : null;
            var picked = JobConfirm.Ask(Window.GetWindow(this), Label(verb, verify), source, suggested);
            if (picked is null)
            {
                _status("Cancelled.");
                return;
            }

            destination = picked;
        }

        var guard = JobGuard.Check(verb, source, destination);
        if (!guard.Ok)
        {
            _status(guard.Reason);
            return;
        }

        if (!JobAccess.CanRun(verb, source, destination))
        {
            var elevated = JobHandoff.Offer(verb, source, destination, verify && verb == JobVerb.Delete, false);
            _status(elevated ? "Elevated job started." : "Elevation cancelled.");
            return;
        }

        if (verb == JobVerb.Copy)
        {
            _ = Copy(source, destination!, destPane, verify);
            return;
        }

        if (verb == JobVerb.Move)
        {
            _ = Move(source, destination!, sourcePane, destPane, verify);
            return;
        }

        if (verb == JobVerb.Delete)
        {
            _ = Delete(source, sourcePane, verify);
            return;
        }

        _ = Mirror(source, destination!, destPane);
    }

    private void Audit_Click()
    {
        if (_busy)
        {
            _status("A job is already running.");
            return;
        }

        var sourcePane = SourceIsFirst ? First : Second;
        var destPane = SourceIsFirst ? Second : First;
        var source = sourcePane.SelectionOrFolder();
        var suggested = IsSplit && !string.IsNullOrWhiteSpace(destPane.Folder) ? destPane.Folder : null;
        var destination = JobConfirm.Ask(Window.GetWindow(this), "Audit", source, suggested);
        if (destination is null)
        {
            _status("Cancelled.");
            return;
        }

        var guard = JobGuard.Check(JobVerb.Copy, source, destination);
        if (!guard.Ok)
        {
            _status(guard.Reason);
            return;
        }

        if (!JobAccess.CanRun(JobVerb.Copy, source, destination))
        {
            var elevated = JobHandoff.Offer(JobVerb.Copy, source, destination, false, true);
            _status(elevated ? "Elevated audit started." : "Elevation cancelled.");
            return;
        }

        _ = Audit(source, destination);
    }

    private static string Label(JobVerb verb, bool verify)
        => verb switch
        {
            JobVerb.Copy => verify ? "Copy (Verify)" : "Copy",
            JobVerb.Move => verify ? "Move (Verify)" : "Move",
            JobVerb.Mirror => "Mirror (Verify)",
            _ => verb.ToString()
        };

    private FileIoJobResult? ShowJob(string title, FileIoJob job, string source, string? destination)
    {
        JobLog.Start(title, job.JobId, source, destination);
        var window = new JobWindow(title, job, source, destination) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
        JobLog.End(title, job.JobId, source, destination, window.Result?.Status ?? "Failed");
        return window.Result;
    }

    private async Task Audit(string source, string destination)
    {
        _busy = true;
        try
        {
            var settings = App.Settings.Load();
            var options = Options(settings, CollisionOf(settings.Collision));
            options.AuditMode = true;
            var result = ShowJob("Audit", FileIoHelper.Copy(source, destination, options), source, destination);
            if (result is null)
            {
                _status("Audit failed.");
                return;
            }
            _status("Audit " + result.Status + ". Would copy " + result.Copied + ", skip " + result.Skipped + ", fail " + result.Failed + ". No disk change.");
        }
        catch (Exception ex)
        {
            _status("Audit failed. " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
        await Task.CompletedTask;
    }

    private async Task Copy(string source, string destination, ExplorerPane destPane, bool verify)
    {
        var settings = App.Settings.Load();
        var collision = CollisionOf(settings.Collision);
        if (collision == FileIoCollision.Overwrite && settings.AskBeforeOverwrite)
        {
            var answer = MessageBox.Show(
                "Overwrite will replace files that already exist in " + destination + ".",
                verify ? "Copy (Verify)" : "Copy",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.OK)
            {
                _status("Copy cancelled.");
                return;
            }
        }

        _busy = true;
        try
        {
            var label = verify ? "Copy (Verify)" : "Copy";
            var result = ShowJob(label, FileIoHelper.Copy(source, destination, Options(settings, collision)), source, destination);
            if (result is null)
            {
                _status(label + " failed.");
                return;
            }
            destPane.Open(destination);
            if (!verify)
            {
                _status("Copy " + result.Status + ". " + result.Copied + " copied, " + result.Skipped + " skipped, " + result.Failed + " failed.");
                return;
            }

            var verified = await Task.Run(() => JobVerify.Run(result.JobId, source, destination, settings.Verify())).ConfigureAwait(true);
            HostLog.Information(
                FXplorerCatalog.VerifyCompleted,
                "Lifecycle",
                "Copy verify " + result.JobId + " matched=" + verified.Matched + " failed=" + verified.Failed + " log=" + verified.Path);
            _status("Copy (Verify) " + result.Status + ". " + verified.Matched + " matched, " + verified.Failed + " failed. Source kept.");
        }
        catch (Exception ex)
        {
            _status("Copy failed. " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task Move(string source, string destination, ExplorerPane sourcePane, ExplorerPane destPane, bool verify)
    {
        var settings = App.Settings.Load();
        var collision = CollisionOf(settings.Collision);
        if (collision == FileIoCollision.Overwrite && settings.AskBeforeOverwrite)
        {
            var answer = MessageBox.Show(
                verify
                    ? "Overwrite will replace files that already exist in " + destination + ". The source is removed only if the verify matches."
                    : "Overwrite will replace files that already exist in " + destination + ". The source is removed after the write.",
                verify ? "Move (Verify)" : "Move",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.OK)
            {
                _status("Move cancelled.");
                return;
            }
        }

        _busy = true;
        try
        {
            if (!verify)
            {
                var result = ShowJob("Move", FileIoHelper.Move(source, destination, Options(settings, collision)), source, destination);
                if (result is null)
                {
                    _status("Move failed.");
                    return;
                }
                destPane.Open(destination);
                sourcePane.Open(Remain(sourcePane, source));
                _status("Move " + result.Status + ". " + result.Copied + " moved, " + result.Skipped + " skipped, " + result.Failed + " failed.");
                return;
            }

            var copied = ShowJob("Move (Verify)", FileIoHelper.Copy(source, destination, Options(settings, collision)), source, destination);
            if (copied is null)
            {
                _status("Move (Verify) failed.");
                return;
            }
            destPane.Open(destination);
            var verified = await Task.Run(() => JobVerify.Run(copied.JobId, source, destination, settings.Verify())).ConfigureAwait(true);
            if (verified.Failed > 0)
            {
                HostLog.Information(
                    FXplorerCatalog.VerifyCompleted,
                    "Lifecycle",
                    "Move verify " + copied.JobId + " matched=" + verified.Matched + " failed=" + verified.Failed + " source kept log=" + verified.Path);
                _status("Move (Verify) failed. " + verified.Failed + " did not match. Source kept.");
                return;
            }

            ShowJob("Move (Verify)", FileIoHelper.Delete(source, Options(settings, collision)), source, null);
            sourcePane.Open(Remain(sourcePane, source));
            HostLog.Information(
                FXplorerCatalog.VerifyCompleted,
                "Lifecycle",
                "Move verify " + copied.JobId + " matched=" + verified.Matched + " source removed log=" + verified.Path);
            _status("Move (Verify) matched. " + verified.Matched + " verified. Source removed.");
        }
        catch (Exception ex)
        {
            _status("Move failed. " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task Delete(string source, ExplorerPane sourcePane, bool secure)
    {
        var answer = MessageBox.Show(
            secure
                ? "Shred and delete " + source + "? The file is overwritten, then removed. This cannot be undone."
                : "Delete " + source + "? This cannot be undone.",
            secure ? "Delete (Secure)" : "Delete",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.OK)
        {
            _status("Delete cancelled.");
            return;
        }

        _busy = true;
        try
        {
            var settings = App.Settings.Load();
            var options = Options(settings, CollisionOf(settings.Collision));
            if (secure)
                options.Shred = FileIoShredRecipe.ZeroRandomZero;
            var label = secure ? "Delete (Secure)" : "Delete";
            var result = ShowJob(label, FileIoHelper.Delete(source, options), source, null);
            if (result is null)
            {
                _status(label + " failed.");
                return;
            }
            sourcePane.Open(Remain(sourcePane, source));
            _status(label + " " + result.Status + ". " + result.Deleted + " deleted, " + result.Failed + " failed.");
        }
        catch (Exception ex)
        {
            _status("Delete failed. " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
        await Task.CompletedTask;
    }

    private async Task Mirror(string source, string destination, ExplorerPane destPane)
    {
        var answer = MessageBox.Show(
            "Mirror will make " + destination + " match " + source + ". Files in the destination that are not in the source will be deleted. Then the result is verified.",
            "Mirror (Verify)",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.OK)
        {
            _status("Mirror cancelled.");
            return;
        }

        _busy = true;
        try
        {
            var settings = App.Settings.Load();
            var options = Options(settings, CollisionOf(settings.Collision));
            options.Purge = true;
            var result = ShowJob("Mirror (Verify)", FileIoHelper.Mirror(source, destination, options), source, destination);
            if (result is null)
            {
                _status("Mirror failed.");
                return;
            }
            destPane.Open(destination);
            var verified = await Task.Run(() => JobVerify.Run(result.JobId, source, destination, settings.Verify())).ConfigureAwait(true);
            HostLog.Information(
                FXplorerCatalog.VerifyCompleted,
                "Lifecycle",
                "Mirror verify " + result.JobId + " matched=" + verified.Matched + " failed=" + verified.Failed + " log=" + verified.Path);
            _status("Mirror (Verify) " + result.Status + ". " + verified.Matched + " matched, " + verified.Failed + " failed.");
        }
        catch (Exception ex)
        {
            _status("Mirror failed. " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private static string Remain(ExplorerPane pane, string source)
        => Directory.Exists(pane.Folder) ? pane.Folder : Path.GetDirectoryName(source) ?? pane.Folder;

    private static FileIoJobOptions Options(FXplorerSettings settings, FileIoCollision collision)
        => new()
        {
            Collision = collision,
            UniqueNamePattern = string.IsNullOrWhiteSpace(settings.UniqueNamePattern) ? ".##" : settings.UniqueNamePattern,
            Purge = false,
            HashAlgorithm = settings.Verify(),
            ReconLeadTime = TimeSpan.FromSeconds(2),
            ExcludeFileMasks = Split(settings.ExcludeFileMasks),
            ExcludeDirectoryMasks = Split(settings.ExcludeDirectoryMasks),
            MinSizeBytes = settings.MinSizeBytes,
            MaxSizeBytes = settings.MaxSizeBytes,
            MinAge = settings.MinAgeDays is { } min ? new FileIoAge { Days = min } : null,
            MaxAge = settings.MaxAgeDays is { } max ? new FileIoAge { Days = max } : null,
            MaxDepth = settings.MaxDepth,
            IncludeEmptyDirectories = settings.IncludeEmptyDirectories,
            CopyTimestampsAndAttributes = settings.CopyTimestamps,
            RetryCount = settings.RetryCount,
            RetryWait = TimeSpan.FromSeconds(settings.RetryWaitSeconds)
        };

    private static string[] Split(string masks)
        => string.IsNullOrWhiteSpace(masks)
            ? []
            : masks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static FileIoCollision CollisionOf(string name)
        => name switch
        {
            "Skip" => FileIoCollision.Skip,
            "Overwrite" => FileIoCollision.Overwrite,
            _ => FileIoCollision.UniqueName
        };

    private void Split_Click()
    {
        IsSplit = !IsSplit;
        if (IsSplit && string.IsNullOrWhiteSpace(Second.Folder))
            Second.Seed(First.Folder);
        Layout();
        RefreshMarks();
        Save();
        Fit();
        HostLog.Information(IsSplit ? FXplorerCatalog.SplitOpened : FXplorerCatalog.SplitClosed, "Lifecycle", IsSplit ? "Split opened." : "Split closed.");
        Changed?.Invoke();
    }

    private void Swap_Click()
    {
        if (!IsSplit)
            return;
        SourceIsFirst = !SourceIsFirst;
        RefreshMarks();
        Changed?.Invoke();
    }

    private void RefreshMarks()
    {
        _jobBar.Visibility = IsSplit ? Visibility.Visible : Visibility.Collapsed;
        _sourceText.Text = "Source  " + OrNone(SourcePath);
        _destinationText.Text = "Destination  " + OrNone(DestinationPath);
    }

    private void RefreshMenu()
    {
        _splitItem.Header = IsSplit ? "Close split" : "Split";
        _verticalItem.IsEnabled = IsSplit;
        _horizontalItem.IsEnabled = IsSplit;
        _swapItem.IsEnabled = IsSplit;
        _verticalItem.IsChecked = IsSplit && Vertical;
        _horizontalItem.IsChecked = IsSplit && !Vertical;
    }

    private static string OrNone(string path) => string.IsNullOrWhiteSpace(path) ? "(none)" : path;

    private void SetVertical(bool vertical)
    {
        if (!IsSplit || Vertical == vertical)
            return;
        Vertical = vertical;
        Layout();
        Save();
        Fit();
        Changed?.Invoke();
    }

    private void Split_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        var total = Vertical ? _body.ActualWidth : _body.ActualHeight;
        if (total < 100)
            return;
        var first = Vertical ? _body.ColumnDefinitions[0].ActualWidth : _body.RowDefinitions[0].ActualHeight;
        var ratio = first / total;
        if (ratio is < 0.15 or > 0.85)
            return;
        Ratio = Math.Round(ratio, 3);
        Save();
        Changed?.Invoke();
    }

    private void Layout()
    {
        _body.Children.Clear();
        _body.RowDefinitions.Clear();
        _body.ColumnDefinitions.Clear();
        Grid.SetRow(First, 0);
        Grid.SetColumn(First, 0);

        if (!IsSplit)
        {
            _body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _body.Children.Add(First);
            return;
        }

        var first = new GridLength(Ratio, GridUnitType.Star);
        var second = new GridLength(1 - Ratio, GridUnitType.Star);
        if (Vertical)
        {
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = first, MinWidth = 0 });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = second, MinWidth = 0 });
            _body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _split.ResizeDirection = GridResizeDirection.Columns;
            _split.Width = 8;
            _split.Height = double.NaN;
            Grid.SetColumn(_split, 1);
            Grid.SetRow(_split, 0);
            Grid.SetColumn(Second, 2);
            Grid.SetRow(Second, 0);
        }
        else
        {
            _body.RowDefinitions.Add(new RowDefinition { Height = first, MinHeight = 0 });
            _body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            _body.RowDefinitions.Add(new RowDefinition { Height = second, MinHeight = 0 });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _split.ResizeDirection = GridResizeDirection.Rows;
            _split.Height = 8;
            _split.Width = double.NaN;
            Grid.SetRow(_split, 1);
            Grid.SetColumn(_split, 0);
            Grid.SetRow(Second, 2);
            Grid.SetColumn(Second, 0);
        }

        _body.Children.Add(First);
        _body.Children.Add(_split);
        _body.Children.Add(Second);
        if (string.IsNullOrWhiteSpace(Second.Folder))
            Second.Open(First.Folder);
    }

    private void Save()
    {
        if (_applying)
            return;
        var settings = App.Settings.Load();
        settings.SplitVertical = Vertical;
        settings.SplitRatio = Ratio;
        App.Settings.Save(settings);
    }

    private MenuItem Item(string text, Action click)
    {
        var item = new MenuItem { Header = text };
        item.Click += (_, _) => click();
        return item;
    }

    private TextBlock Mark(string caption)
        => new()
        {
            Text = caption,
            Style = (Style)Application.Current.FindResource("TextBlock.Caption"),
            Margin = new Thickness(0, 0, 24, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
}
