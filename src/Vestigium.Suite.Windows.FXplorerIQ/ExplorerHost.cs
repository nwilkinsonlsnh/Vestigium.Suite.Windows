using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Helpers.Pdf;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ExplorerHost : DockPanel
{
    private readonly Grid _body = new();
    private readonly GridSplitter _split = new();
    private readonly StackPanel _jobBar;
    private readonly TextBlock _sourceText;
    private readonly TextBlock _destinationText;
    private MenuItem _splitItem = null!;
    private MenuItem _verticalItem = null!;
    private MenuItem _horizontalItem = null!;
    private readonly Action<string> _status;
    private ScrollViewer? _viewport;
    private bool _applying;
    private bool _busy;
    private Task? _running;

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

        var menu = new ContextMenu();
        menu.Items.Add(Files());
        menu.Items.Add(new Separator());
        menu.Items.Add(Clipboard());
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Open in Command Prompt", () => Prompt("cmd.exe")));
        menu.Items.Add(Item("Open in PowerShell", () => Prompt("powershell.exe")));
        menu.Items.Add(new Separator());
        menu.Items.Add(Split());
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

    private MenuItem Files()
    {
        var item = new MenuItem { Header = "File System Management" };
        item.Items.Add(Item("Audit", Audit_Click));
        item.Items.Add(new Separator());
        item.Items.Add(Item("Copy", () => Verb(JobVerb.Copy, false)));
        item.Items.Add(Item("Move", () => Verb(JobVerb.Move, false)));
        item.Items.Add(Item("Mirror", () => Verb(JobVerb.Mirror, false)));
        item.Items.Add(new Separator());
        var validation = new MenuItem { Header = "Validation" };
        validation.Items.Add(Item("Copy", () => Verb(JobVerb.Copy, true)));
        validation.Items.Add(Item("Move", () => Verb(JobVerb.Move, true)));
        validation.Items.Add(Item("Mirror", () => Verb(JobVerb.Mirror, true)));
        item.Items.Add(validation);
        item.Items.Add(new Separator());
        item.Items.Add(Item("Delete", () => Verb(JobVerb.Delete, false)));
        item.Items.Add(Item("Delete (Secure)", () => Verb(JobVerb.Delete, true)));
        return item;
    }

    private MenuItem Clipboard()
    {
        var item = new MenuItem { Header = "Clipboard" };
        item.Items.Add(Item("Copy Name", () => CopyText(Path.GetFileName(Target()))));
        item.Items.Add(Item("Copy Directory Path", () => CopyText(DirectoryOf(Target()))));
        item.Items.Add(Item("Copy Full Path", () => CopyText(string.Join(Environment.NewLine, Targets()))));
        return item;
    }

    private MenuItem Split()
    {
        _splitItem = Item("None", () => CloseSplit());
        _verticalItem = Item("Vertical", () => Orient(true));
        _horizontalItem = Item("Horizontal", () => Orient(false));
        var item = new MenuItem { Header = "Split" };
        item.Items.Add(_splitItem);
        item.Items.Add(_verticalItem);
        item.Items.Add(_horizontalItem);
        return item;
    }

    private void CloseSplit()
    {
        if (!IsSplit)
            return;
        Split_Click();
    }

    private void Orient(bool vertical)
    {
        if (!IsSplit)
            Split_Click();
        SetVertical(vertical);
    }

    private IReadOnlyList<string> Targets()
    {
        var pane = SourceIsFirst ? First : Second;
        var paths = pane.SelectedPaths();
        return paths.Count == 0 ? [pane.Folder] : paths;
    }

    private string Target() => Targets()[0];

    private static string DirectoryOf(string path)
        => Directory.Exists(path) ? path : Path.GetDirectoryName(path) ?? path;

    private void CopyText(string text)
    {
        if (text.Length == 0)
            return;
        System.Windows.Clipboard.SetText(text);
        _status("Copied to the clipboard.");
    }

    private void Prompt(string shell)
    {
        var folder = DirectoryOf(Target());
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            _status("No folder to open.");
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = shell,
            WorkingDirectory = folder,
            UseShellExecute = true
        });
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
        var sourcePane = SourceIsFirst ? First : Second;
        var destPane = SourceIsFirst ? Second : First;
        var paths = sourcePane.SelectedPaths();
        var source = paths.Count == 0 ? sourcePane.Folder : paths[0];
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

        var collision = FileIoCollision.UniqueName;
        var pattern = ".##";
        if (verb is JobVerb.Copy or JobVerb.Move)
        {
            var settings = App.Settings.Load();
            collision = CollisionOf(settings.Collision);
            pattern = string.IsNullOrWhiteSpace(settings.UniqueNamePattern) ? ".##" : settings.UniqueNamePattern;
            if (!settings.AskBeforeOverwrite)
            {
                _status("Using " + settings.Collision + ".");
            }
            else
            {
                _status("Automatic Collision Handling is off.");
                var choice = CollisionPrompt.Ask(Window.GetWindow(this), Label(verb, verify), settings.Collision, pattern);
                if (choice is null)
                {
                    _status("Cancelled.");
                    return;
                }
                collision = choice.Collision;
                pattern = choice.Pattern;
            }
        }

        var guard = JobGuard.Check(verb, source, destination);
        if (!guard.Ok)
        {
            _status(guard.Reason);
            return;
        }

        var access = JobAccess.Check(verb, source, destination ?? "");
        if (access.Decision == JobAccessDecision.Refuse)
        {
            _status(access.Reason);
            return;
        }

        access = ConfirmCreate(access, destination);
        if (access.Decision == JobAccessDecision.Refuse)
        {
            _status(access.Reason);
            return;
        }

        if (access.Decision == JobAccessDecision.Elevate)
        {
            var outcome = JobHandoff.Run(
                Window.GetWindow(this), verb, source, destination, collision.ToString(), pattern,
                verify && verb == JobVerb.Delete, false);
            FinishElevated(outcome, verb, source, destination, sourcePane, destPane);
            return;
        }

        if (Overlaps(source, destination))
        {
            _status("A job is already using that folder.");
            return;
        }

        if (verb == JobVerb.Copy)
        {
            var work = Run(CopyMany(paths, destination!, destPane, verify, collision, pattern));
            Track(source, destination, work);
            _running = work;
            return;
        }

        if (verb == JobVerb.Move)
        {
            var work = Run(MoveMany(paths, destination!, sourcePane, destPane, verify, collision, pattern));
            Track(source, destination, work);
            _running = work;
            return;
        }

        if (verb == JobVerb.Delete)
        {
            var work = DeleteMany(paths, sourcePane, verify);
            Track(source, null, work);
            _running = work;
            return;
        }

        var mirror = Mirror(source, destination!, destPane);
        Track(source, destination, mirror);
        _running = mirror;
    }

    private readonly List<RunningJob> _jobs = new();

    private bool Overlaps(string? source, string? destination)
    {
        _jobs.RemoveAll(job => job.Work.IsCompleted);
        foreach (var job in _jobs)
        {
            if (Hits(source, job.Source) || Hits(source, job.Destination) || Hits(destination, job.Source) || Hits(destination, job.Destination))
                return true;
        }
        return false;
    }

    private static bool Hits(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;
        var a = Path.GetFullPath(left).TrimEnd('\\');
        var b = Path.GetFullPath(right).TrimEnd('\\');
        return a.Equals(b, StringComparison.OrdinalIgnoreCase)
            || a.StartsWith(b + "\\", StringComparison.OrdinalIgnoreCase)
            || b.StartsWith(a + "\\", StringComparison.OrdinalIgnoreCase);
    }

    private void Track(string? source, string? destination, Task work)
        => _jobs.Add(new RunningJob(source, destination, work));

    private sealed record RunningJob(string? Source, string? Destination, Task Work);

    private bool Busy()
    {
        if (_openJob is { IsVisible: false })
        {
            _busy = false;
            _running = null;
            _openJob = null;
        }
        if (_running is { IsCompleted: true })
        {
            _busy = false;
            _running = null;
        }
        return _busy && _running is { IsCompleted: false };
    }

    private void Audit_Click()
    {
        if (Busy())
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

        var access = JobAccess.Check(JobVerb.Copy, source, destination);
        if (access.Decision == JobAccessDecision.Refuse)
        {
            _status(access.Reason);
            return;
        }

        access = ConfirmCreate(access, destination);
        if (access.Decision == JobAccessDecision.Refuse)
        {
            _status(access.Reason);
            return;
        }

        if (access.Decision == JobAccessDecision.Elevate)
        {
            var outcome = JobHandoff.Run(
                Window.GetWindow(this), JobVerb.Copy, source, destination, CollisionOf(App.Settings.Load().Collision).ToString(),
                App.Settings.Load().UniqueNamePattern, false, true);
            _status(outcome.Choice switch
            {
                ElevationChoice.Finished => "Elevated audit " + outcome.Status + ".",
                ElevationChoice.Denied => outcome.Status,
                _ => "Elevation cancelled."
            });
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

    private JobWindow? _openJob;

    private Task<FileIoJobResult?> ShowJob(string title, FileIoJob job, string source, string? destination, bool autoClose = false)
    {
        JobLog.Start(title, job.JobId, source, destination);
        var done = new TaskCompletionSource<FileIoJobResult?>();
        var window = new JobWindow(title, job, source, destination) { Owner = Window.GetWindow(this), AutoClose = autoClose };
        _openJob = window;
        window.Finished += (_, _) =>
        {
            JobLog.End(title, job.JobId, source, destination, window.Result?.Status ?? "Failed");
            done.TrySetResult(window.Result);
        };
        window.Closed += (_, _) => done.TrySetResult(window.Result);
        window.Show();
        return done.Task;
    }

    private async Task Audit(string source, string destination)
    {
        _busy = true;
        try
        {
            var settings = App.Settings.Load();
            var options = Options(settings, CollisionOf(settings.Collision));
            options.AuditMode = true;
            var result = await ShowJob("Audit", FileIoHelper.Copy(source, destination, options), source, destination);
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

    private JobAccessResult ConfirmCreate(JobAccessResult access, string? destination)
    {
        if (access.Decision != JobAccessDecision.AskCreate)
            return access;
        if (!JobAsk.Ask(Window.GetWindow(this), "Create destination", access.Reason + " Create it?"))
            return JobAccessResult.Stop("Cancelled.");
        try
        {
            Directory.CreateDirectory(destination!);
            return JobAccessResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return JobAccessResult.NeedsAdmin("Access denied on " + destination + ".");
        }
        catch (IOException ex)
        {
            return JobAccessResult.Stop(ex.Message);
        }
    }

    private void FinishElevated(
        ElevationOutcome outcome,
        JobVerb verb,
        string source,
        string? destination,
        ExplorerPane sourcePane,
        ExplorerPane destPane)
    {
        if (outcome.Choice == ElevationChoice.Cancelled)
        {
            _status("Elevation cancelled.");
            return;
        }

        if (outcome.Choice == ElevationChoice.Denied)
        {
            _status(outcome.Status);
            return;
        }

        if (!string.IsNullOrWhiteSpace(destination))
        {
            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(parent))
                RefreshShowing(parent);
            destPane.Open(destination);
        }
        if (verb is JobVerb.Move or JobVerb.Delete)
        {
            var parent = Path.GetDirectoryName(source);
            if (!string.IsNullOrWhiteSpace(parent))
                RefreshShowing(parent);
            sourcePane.Open(Remain(sourcePane, source));
        }
        _status("Elevated " + verb + " " + outcome.Status + ".");
    }

    private void RefreshShowing(string path)
    {
        foreach (var pane in new[] { First, Second })
        {
            pane.RefreshTree(path);
            if (string.Equals(pane.Folder, path, StringComparison.OrdinalIgnoreCase))
                pane.Open(path);
        }
    }

    private async Task Run(Task work)
    {
        try
        {
            await work.ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _status(ex.Message);
            _busy = false;
        }
    }

    private async Task Copy(string source, string destination, ExplorerPane destPane, bool verify, FileIoCollision collision, string pattern)
    {
        var settings = App.Settings.Load();

        _busy = true;
        try
        {
            var label = verify ? "Copy (Verify)" : "Copy";
            var job = FileIoHelper.Copy(source, destination, Options(settings, collision, pattern));
            JobLiveLog? live = null;
            if (verify)
                live = new JobLiveLog(job, source, destination, settings.Verify(), "Copy (Verify)", settings.VerifyReadRetries, settings.VerifyReadWaitMs, settings.VerifyRetryParallel);
            var result = await ShowJob(label, job, source, destination);
            if (result is null)
            {
                _status(label + " failed.");
                return;
            }
            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(parent))
                RefreshShowing(parent);
            destPane.Open(destination);
            if (!verify)
            {
                _status("Copy " + result.Status + ". " + result.Copied + " copied, " + result.Skipped + " skipped, " + result.Failed + " failed.");
                return;
            }

            var window = _openJob;
            var jobId = result.JobId;
            window?.OfferLog(live?.LogPath ?? JobLog.File(jobId));
            window?.ShowEvidence();
            window?.BeginVerify();
            var expected = result.Copied + result.Skipped + result.Failed;
            live?.Watch(expected, (done, total, name) => Dispatcher.Invoke(() => window?.VerifyProgress(done, total, name)));
            _ = Task.Run(() =>
            {
                live?.Drain();
                var rows = live?.Rows ?? Array.Empty<PdfRow>();
                var matched = rows.Count(row => row.Equal);
                var failed = rows.Count - matched;
                var pdf = WriteEvidence(jobId, "Copy (Verify)", source, destination, settings.Verify(), rows);
                Dispatcher.Invoke(() =>
                {
                    window?.OfferEvidence(pdf);
                    window?.VerifyDone(matched, failed);
                    _status("Copy (Verify) " + result.Status + ". " + matched + " matched, " + failed + " failed. Source kept.");
                });
            });
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

    private static string WriteEvidence(string jobId, string verb, string source, string destination, HashingAlgorithm algorithm, IReadOnlyList<PdfRow> rows)
    {
        var folder = Path.Combine(JobVerify.Root);
        Directory.CreateDirectory(folder);
        var pdf = Path.Combine(folder, jobId + ".pdf");
        var matched = rows.Where(row => row.Equal).Select(row => new[] { row.Relative, "Match", Short(row.SourceDigest) }).ToArray();
        var failed = rows.Where(row => !row.Equal).Select(row => new[] { row.Relative, "Fail", Short(row.SourceDigest) }).ToArray();
        var document = new PdfDocument
        {
            Title = "Vestigium (FXplorerIQ)",
            Subtitle = "Summary",
            Seal = true
        };
        document.AddFields("Summary",
            new PdfField { Name = "Job", Value = jobId },
            new PdfField { Name = "Verb", Value = verb },
            new PdfField { Name = "Source", Value = source },
            new PdfField { Name = "Destination", Value = destination },
            new PdfField { Name = "Algorithm", Value = algorithm.ToString() },
            new PdfField { Name = "Matched", Value = matched.Length.ToString() },
            new PdfField { Name = "Failed", Value = failed.Length.ToString() },
            new PdfField { Name = "Result", Value = failed.Length == 0 ? "Passed" : "Failed" });
        if (failed.Length > 0)
            document.AddTable("Failures  (" + failed.Length + ")", ["File", "Result", "Source digest"], failed);
        document.AddTable("Matches  (" + matched.Length + ")", ["File", "Result", "Source digest"], matched);
        var bytes = PdfEvidence.Build(document);
        File.WriteAllBytes(pdf, bytes);
        return pdf;
    }

    private static string Short(string? digest)
        => string.IsNullOrEmpty(digest) ? "" : digest.Length <= 16 ? digest : digest[..16];

    private async Task Move(string source, string destination, ExplorerPane sourcePane, ExplorerPane destPane, bool verify, FileIoCollision collision, string pattern)
    {
        var settings = App.Settings.Load();

        _busy = true;
        try
        {
            if (!verify)
            {
                var result = await ShowJob("Move", FileIoHelper.Move(source, destination, Options(settings, collision, pattern)), source, destination);
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

            var options = Options(settings, collision, pattern);
            JobLiveLog? live = null;
            var job = FileIoHelper.Copy(source, destination, options);
            if (verify)
                live = new JobLiveLog(job, source, destination, settings.Verify(), "Move (Verify)", settings.VerifyReadRetries, settings.VerifyReadWaitMs, settings.VerifyRetryParallel);
            var copied = await ShowJob("Move (Verify)", job, source, destination);
            if (copied is null)
            {
                _status("Move (Verify) failed.");
                return;
            }
            destPane.Open(destination);
            var window = _openJob;
            window?.OfferLog(live?.LogPath ?? JobLog.File(copied.JobId));
            window?.ShowEvidence();
            window?.BeginVerify();
            var expected = copied.Copied + copied.Skipped + copied.Failed;
            live?.Watch(expected, (done, total, name) => Dispatcher.Invoke(() => window?.VerifyProgress(done, total, name)));
            await Task.Run(() => live?.Drain());
            var rows = live?.Rows ?? Array.Empty<PdfRow>();
            var matched = rows.Count(row => row.Equal);
            var failed = rows.Count - matched;
            if (failed > 0)
            {
                var pdf = WriteEvidence(copied.JobId, "Move (Verify)", source, destination, settings.Verify(), rows);
                window?.OfferEvidence(pdf);
                window?.VerifyDone(matched, failed);
                _status("Move (Verify) failed. " + failed + " did not match. Source kept.");
                return;
            }
            window?.SetPhase("Removing source");
            var removed = await FileIoHelper.Delete(source, options).RunAsync();
            sourcePane.Open(Remain(sourcePane, source));
            var pdfOk = WriteEvidence(copied.JobId, "Move (Verify)", source, destination, settings.Verify(), rows);
            window?.OfferEvidence(pdfOk);
            window?.VerifyDone(matched, failed);
            _status("Move (Verify) matched. " + matched + " verified. Source removed. " + removed.Status + ".");
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

    private async Task CopyMany(IReadOnlyList<string> paths, string destination, ExplorerPane destPane, bool verify, FileIoCollision collision, string pattern)
    {
        var copied = 0;
        var skipped = 0;
        var failed = 0;
        foreach (var source in paths)
        {
            await Copy(source, destination, destPane, verify, collision, pattern);
            copied++;
        }
        if (paths.Count > 1)
            _status(paths.Count + " items sent to " + destination + ".");
    }

    private async Task MoveMany(IReadOnlyList<string> paths, string destination, ExplorerPane sourcePane, ExplorerPane destPane, bool verify, FileIoCollision collision, string pattern)
    {
        foreach (var source in paths)
            await Move(source, destination, sourcePane, destPane, verify, collision, pattern);
        if (paths.Count > 1)
            _status(paths.Count + " items moved.");
    }

    private async Task DeleteMany(IReadOnlyList<string> paths, ExplorerPane sourcePane, bool secure)
    {
        if (!JobAsk.Ask(Window.GetWindow(this), secure ? "Delete (Secure)" : "Delete",
            secure
                ? "Shred and delete " + paths.Count + " item(s)? This cannot be undone."
                : "Delete " + paths.Count + " item(s)? This cannot be undone.",
            paths))
        {
            _status("Delete cancelled.");
            return;
        }

        var deleted = 0;
        var failed = 0;
        _busy = true;
        try
        {
            var settings = App.Settings.Load();
            var options = Options(settings, CollisionOf(settings.Collision));
            options.ReconLeadTime = TimeSpan.Zero;
            if (secure)
                options.Shred = settings.SecurePasses == 7
                    ? FileIoShredRecipe.SevenRandomThenZero
                    : FileIoShredRecipe.ThreeRandomThenZero;
            var label = (secure ? "Delete (Secure)" : "Delete") + " (" + paths.Count + ")";
            var shown = paths.Count == 1 ? paths[0] : paths.Count + " items";
            var result = await ShowJob(label, FileIoHelper.Delete(paths, options), shown, null);
            if (result is null || result.Status is "Cancelled" or "Canceled")
            {
                _status(label + " cancelled.");
                return;
            }
            foreach (var path in paths)
                Remove(path);
            deleted = result.Deleted;
            failed = result.Failed;
            sourcePane.Open(Directory.Exists(sourcePane.Folder) ? sourcePane.Folder : Path.GetDirectoryName(paths[0]) ?? sourcePane.Folder);
            _status(label + " " + result.Status + ". " + deleted + " deleted, " + failed + " failed.");
        }
        catch (Exception ex)
        {
            _status("Delete failed. " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private static void Remove(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            else if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception)
        {
            /* the job already removed what it could */
        }
    }

    private async Task Delete(string source, ExplorerPane sourcePane, bool secure)
    {
        await DeleteMany([source], sourcePane, secure);
    }

    private async Task Mirror(string source, string destination, ExplorerPane destPane)
    {
        if (!JobAsk.Ask(Window.GetWindow(this), "Mirror (Verify)",
            "Mirror will make " + destination + " match " + source + ". Files in the destination that are not in the source will be deleted. Then the result is verified."))
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
            var result = await ShowJob("Mirror (Verify)", FileIoHelper.Mirror(source, destination, options), source, destination);
            if (result is null)
            {
                _status("Mirror failed.");
                return;
            }
            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(parent))
                RefreshShowing(parent);
            destPane.Open(destination);
            var verified = await Task.Run(() => JobVerify.Run(result.JobId, source, destination, settings.Verify(), verb: "Mirror (Verify)")).ConfigureAwait(true);
            HostLog.Information(
                FXplorerCatalog.VerifyCompleted,
                "Lifecycle",
                "Mirror verify " + result.JobId + " matched=" + verified.Matched + " failed=" + verified.Failed + " log=" + verified.Path);
            _status("Mirror (Verify) " + result.Status + ". " + verified.Matched + " matched, " + verified.Failed + " failed.");
            _openJob?.OfferEvidence(verified.Pdf);
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

    private static FileIoJobOptions Options(FXplorerSettings settings, FileIoCollision collision, string? pattern = null)
        => new()
        {
            Collision = collision,
            UniqueNamePattern = string.IsNullOrWhiteSpace(pattern) ? ".##" : pattern,
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
            RetryWait = TimeSpan.FromSeconds(settings.RetryWaitSeconds),
            MaxThreads = settings.MaxThreads
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
        _splitItem.IsChecked = !IsSplit;
        _verticalItem.IsEnabled = true;
        _horizontalItem.IsEnabled = true;
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
