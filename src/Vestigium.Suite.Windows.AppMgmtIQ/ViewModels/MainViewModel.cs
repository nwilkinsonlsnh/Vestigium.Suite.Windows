using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;
using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ;
using Vestigium.Suite.Windows.AppMgmtIQ.Views;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

/// <summary>
/// Inventory grid. Walks the four ARP roots through RegistryHelper.Local.
/// Debt: the model (isX64, isGlobal, DisplayName fallback) belongs in a Helper door.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private static readonly (RegistryHiveKind Hive, string Path, bool IsX64, bool IsGlobal)[] Roots =
    [
        (RegistryHiveKind.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true, true),
        (RegistryHiveKind.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", false, true),
        (RegistryHiveKind.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", true, false),
        (RegistryHiveKind.CurrentUser, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", false, false)
    ];

    private readonly List<PackageRow> _all = [];
    private readonly BaselineStore _baselines = new(AppMgmtSettingsStore.DefaultRoot);
    private readonly KqlSession _session = AppMgmtKql.Packages();
    private KqlBoundQuery? _bound;
    private int _orphan;
    [ObservableProperty]
    private bool _showGlobal = true;

    [ObservableProperty]
    private bool _showPerUser = true;

    [ObservableProperty]
    private bool _showX64 = true;

    [ObservableProperty]
    private bool _showX86 = true;

    [ObservableProperty]
    private bool _showSystemComponents;

    [ObservableProperty]
    private bool _showWindowsInstaller = true;

    [ObservableProperty]
    private bool _showOtherInstallers = true;

    private bool _applying;
    public event Action? FiltersChanged;
    public event Action? MruChanged;
    private string _datePattern = DisplayFormats.DefaultDateFormat.Pattern;
    private int _decimals = 4;

    public ObservableCollection<PackageRow> Packages { get; } = [];
    public ObservableCollection<QueryMruRow> Queries { get; } = [];
    public KqlSession Session => _session;

    public int MruLimit { get; set; } = 10;
    public int StickyLimit { get; set; } = 3;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private PackageRow? _selectedPackage;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading installed packages\u2026";

    public void ApplyQuery()
    {
        _bound = KqlHost.Compile(Query, _session, _bound, out var error);
        ApplyFilter();
        if (!string.IsNullOrEmpty(error))
            Status = error;
    }

    public void CommitQuery()
    {
        ApplyQuery();
        if (_bound is not null)
        {
            QueryMru.Record(Queries, Query, MruLimit, StickyLimit);
            MruChanged?.Invoke();
        }
    }

    public void Pin(object? row)
    {
        if (row is not QueryMruRow mru)
            return;
        if (!QueryMru.ToggleSticky(Queries, mru, StickyLimit))
            Status = $"Sticky limit is {StickyLimit}.";
        MruChanged?.Invoke();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading installed packages\u2026";
        try
        {
            var rows = await Task.Run(ReadAll);
            _all.Clear();
            _all.AddRange(rows);
            _baselines.Load();
            ApplyBaselines();
            ApplyPending();
            ApplyFilter();
            HostLog.Information(AppMgmtCatalog.InventoryLoaded, "Inventory", $"Inventory loaded. {_all.Count} packages.");
            _ = CheckOrphans();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CheckOrphans()
    {
        var generation = Interlocked.Increment(ref _orphan);
        var rows = _all.ToList();
        var found = await Task.Run(() => Probe(rows, generation));
        if (generation != _orphan)
            return;
        var orphans = found.Count(item => item.Orphan);
        var pending = _all.Count(row => row.PendingReboot);
        var status = $"{Packages.Count} packages. {orphans} orphans.";
        if (pending > 0)
            status += $" {pending} pending reboot.";
        Status = status;
    }

    private List<(PackageRow Row, bool Orphan, string Reason)> Probe(IReadOnlyList<PackageRow> rows, int generation)
    {
        var found = new List<(PackageRow Row, bool Orphan, string Reason)>();
        var batch = new List<(PackageRow Row, bool Orphan, string Reason)>();
        foreach (var row in rows)
        {
            if (generation != _orphan)
                return found;
            var result = OrphanCheck.Check(row);
            if (result is null)
                continue;
            var item = (Row: row, Orphan: result.Orphan, Reason: result.Reason);
            found.Add(item);
            batch.Add(item);
            if (batch.Count < 50)
                continue;
            Publish(batch);
            batch = [];
        }
        Publish(batch);
        return found;
    }

    private static void Publish(List<(PackageRow Row, bool Orphan, string Reason)> batch)
    {
        if (batch.Count == 0)
            return;
        var snapshot = batch.ToArray();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            foreach (var (row, orphan, reason) in snapshot)
            {
                row.IsOrphan = orphan;
                row.OrphanReason = reason;
            }
        });
    }

    private void ApplyPending()
    {
        var state = PendingRebootState.Read();
        var hits = 0;
        foreach (var row in _all)
        {
            row.PendingReboot = state.Mentions(row);
            if (row.PendingReboot)
                hits++;
        }
        if (state.Pending)
            Status = hits == 0
                ? "A reboot is pending."
                : $"A reboot is pending. {hits} packages are named in a file rename.";
    }

    public void SetBaseline(PackageRow package)
    {
        _baselines.Set(Key(package), package.DisplayVersion);
        ApplyBaseline(package);
        Status = $"Baseline set for {package.DisplayName}.";
    }

    public void ClearBaseline(PackageRow package)
    {
        _baselines.Clear(Key(package));
        ApplyBaseline(package);
        Status = $"Baseline cleared for {package.DisplayName}.";
    }

    private void ApplyBaselines()
    {
        foreach (var row in _all)
            ApplyBaseline(row);
    }

    private void ApplyBaseline(PackageRow row)
    {
        var expected = _baselines.Expected(Key(row)) ?? "";
        row.ExpectedVersion = expected;
        row.BaselineStatus = BaselineCompare.Status(row.DisplayVersion, expected);
    }

    private static string Key(PackageRow row) => row.Hive + "\\" + row.KeyPath;

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    public async Task UninstallAsync(PackageRow package, Window owner, UninstallMode mode)
    {
        var plan = UninstallPlan.Build(package, mode);
        if (plan is null)
        {
            MessageBox.Show(owner, $"{package.DisplayName} has no UninstallString.", "Uninstall", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var actor = WindowsIdentity.GetCurrent().Name;
        if (package.NoRemove)
        {
            var clear = MessageBox.Show(
                owner,
                $"{package.DisplayName} is marked NoRemove.\n\nClearing it writes NoRemove = 0 on\n{package.Hive}\\{package.KeyPath}\n\nThis is logged and attributed to {actor}.\n\nClear the flag and continue?",
                "NoRemove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (clear != MessageBoxResult.Yes)
                return;
            if (!ClearNoRemove(package, actor))
                return;
        }

        var confirm = new UninstallConfirmWindow(package.DisplayName, plan.Display, actor, plan.Note) { Owner = owner };
        if (confirm.ShowDialog() != true)
            return;

        HostLog.Information(
            AppMgmtCatalog.UninstallStarted,
            "Uninstall",
            $"Uninstall started. package={package.DisplayName} command={plan.Display} rewritten={plan.Rewritten} actor={actor}");
        Status = $"Uninstalling {package.DisplayName}\u2026";
        int? code = null;
        try
        {
            var process = Process.Start(new ProcessStartInfo(plan.FileName, plan.Arguments) { UseShellExecute = true });
            if (process is not null)
            {
                await process.WaitForExitAsync();
                code = process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, ex.Message, "Uninstall", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Status = $"Uninstall finished for {package.DisplayName}. Refreshing\u2026";
        await LoadAsync();
        new ActiveSetupSummaryWindow("Uninstall", code is 0 or 3010 or null ? $"{package.DisplayName} was uninstalled." : $"{package.DisplayName} exited {code}.", plan.Display, code is null ? "The process did not report an exit code." : $"Exit code {code}.") { Owner = owner }.ShowDialog();
    }

    public Task ModifyAsync(PackageRow package, Window owner)
    {
        if (!package.HasModify)
        {
            MessageBox.Show(owner, $"{package.DisplayName} has no ModifyPath.", "Modify", MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }
        var (file, args) = Split(package.ModifyPath);
        return LaunchAsync(package, owner, "Modify", file, args, package.ModifyPath);
    }

    public Task RepairAsync(PackageRow package, Window owner)
    {
        var plan = RepairPlan.Build(package);
        if (plan is null)
        {
            MessageBox.Show(owner, $"{package.DisplayName} has no repair command.", "Repair", MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }
        return LaunchAsync(package, owner, "Repair", plan.FileName, plan.Arguments, plan.Display);
    }

    private async Task LaunchAsync(PackageRow package, Window owner, string verb, string file, string args, string display)
    {
        var actor = WindowsIdentity.GetCurrent().Name;
        var confirm = new UninstallConfirmWindow(package.DisplayName, display, actor, null, verb) { Owner = owner };
        if (confirm.ShowDialog() != true)
            return;

        HostLog.Information(
            AppMgmtCatalog.LifecycleRequested,
            "Lifecycle",
            $"{verb} started. package={package.DisplayName} command={display} actor={actor}");
        Status = $"{verb} {package.DisplayName}\u2026";
        int? code = null;
        try
        {
            var process = Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = true });
            if (process is not null)
            {
                await process.WaitForExitAsync();
                code = process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, ex.Message, verb, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        HostLog.Information(
            AppMgmtCatalog.LifecycleCompleted,
            "Lifecycle",
            $"{verb} finished. package={package.DisplayName} code={code}");
        Status = $"{verb} finished for {package.DisplayName}. Refreshing\u2026";
        await LoadAsync();
        var summary = code is 0 or 3010 or null ? $"{package.DisplayName} {verb.ToLower()} finished." : $"{package.DisplayName} exited {code}.";
        var detail = code is null ? "The process did not report an exit code." : $"Exit code {code}.";
        new ActiveSetupSummaryWindow(verb, summary, display, detail) { Owner = owner }.ShowDialog();
    }

    private static (string File, string Args) Split(string command)
    {
        var text = command.Trim();
        if (text.StartsWith('"'))
        {
            var end = text.IndexOf('"', 1);
            if (end > 1)
                return (text[1..end], text[(end + 1)..].Trim());
        }
        foreach (var extension in new[] { ".exe", ".bat", ".cmd", ".com", ".msi" })
        {
            var at = text.IndexOf(extension, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
                continue;
            var end = at + extension.Length;
            return (text[..end], text[end..].Trim());
        }
        var space = text.IndexOf(' ');
        return space < 0 ? (text, string.Empty) : (text[..space], text[(space + 1)..].Trim());
    }

    private static bool ClearNoRemove(PackageRow package, string actor)
    {
        if (!Enum.TryParse<RegistryHiveKind>(package.Hive, out var hive))
        {
            MessageBox.Show($"Unknown hive {package.Hive}.", "NoRemove", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        var result = RegistryHelper.Local.SetValue(
            hive,
            package.KeyPath,
            "NoRemove",
            0,
            RegistryValueKind.DWord,
            RegistryViewKind.Default,
            confirm: true);
        if (result.Status != RegistryWriteStatus.Ok)
        {
            MessageBox.Show($"Could not clear NoRemove.\n{result.Status} {result.Reason}", "NoRemove", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        HostLog.Information(
            AppMgmtCatalog.NoRemoveCleared,
            "Uninstall",
            $"NoRemove cleared. package={package.DisplayName} key={package.Hive}\\{package.KeyPath} old=1 new=0 actor={actor}");
        return true;
    }

    public void ApplySettings(
        bool showGlobal,
        bool showPerUser,
        bool showX64,
        bool showX86,
        bool showSystemComponents,
        bool showWindowsInstaller,
        bool showOtherInstallers,
        string datePattern,
        int decimals)
    {
        _applying = true;
        ShowGlobal = showGlobal;
        ShowPerUser = showPerUser;
        ShowX64 = showX64;
        ShowX86 = showX86;
        ShowSystemComponents = showSystemComponents;
        ShowWindowsInstaller = showWindowsInstaller;
        ShowOtherInstallers = showOtherInstallers;
        _applying = false;
        _datePattern = string.IsNullOrWhiteSpace(datePattern) ? DisplayFormats.DefaultDateFormat.Pattern : datePattern;
        _decimals = Math.Clamp(decimals, 0, 8);
        foreach (var row in _all)
            row.ApplyDisplay(_datePattern, _decimals);
        ApplyFilter();
    }

    partial void OnShowGlobalChanged(bool value) => FilterChanged();
    partial void OnShowPerUserChanged(bool value) => FilterChanged();
    partial void OnShowX64Changed(bool value) => FilterChanged();
    partial void OnShowX86Changed(bool value) => FilterChanged();
    partial void OnShowSystemComponentsChanged(bool value) => FilterChanged();
    partial void OnShowWindowsInstallerChanged(bool value) => FilterChanged();
    partial void OnShowOtherInstallersChanged(bool value) => FilterChanged();

    private void FilterChanged()
    {
        if (_applying)
            return;
        ApplyFilter();
        FiltersChanged?.Invoke();
    }

    private void ApplyFilter()
    {
        var keep = SelectedPackage;
        Packages.Clear();
        var rows = _all
            .Where(Passes)
            .OrderBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
            Packages.Add(row);
        SelectedPackage = Packages.Contains(keep!) ? keep : null;
        if (string.IsNullOrWhiteSpace(Query) || _bound is not null)
            Status = $"{Packages.Count} packages";
    }

    private bool Passes(PackageRow row)
    {
        if (row.IsGlobal && !ShowGlobal)
            return false;
        if (!row.IsGlobal && !ShowPerUser)
            return false;
        if (row.IsX64 && !ShowX64)
            return false;
        if (!row.IsX64 && !ShowX86)
            return false;
        if (row.SystemComponent && !ShowSystemComponents)
            return false;
        if (row.WindowsInstaller && !ShowWindowsInstaller)
            return false;
        if (!row.WindowsInstaller && !ShowOtherInstallers)
            return false;
        if (_bound is null)
            return true;
        return _bound.Matches(PackageAdapter.ToRow(_session, Facts(row)));
    }

    private static PackageFacts Facts(PackageRow row)
    {
        double? size = double.TryParse(row.EstimatedSize, NumberStyles.Number, CultureInfo.InvariantCulture, out var kb) ? kb : null;
        return new PackageFacts
        {
            DisplayName = row.DisplayName,
            Publisher = row.Publisher,
            Version = row.DisplayVersion,
            MajorVersion = IntValue(row, "VersionMajor", "MajorVersion"),
            MinorVersion = IntValue(row, "VersionMinor", "MinorVersion"),
            InstallDate = row.DisplayInstallDate,
            EstimatedSizeKb = size,
            IsX64 = row.IsX64,
            IsGlobal = row.IsGlobal,
            IsSystemComponent = row.SystemComponent,
            WindowsInstaller = row.WindowsInstaller,
            NoModify = row.NoModify,
            NoRemove = row.NoRemove,
            NoRepair = row.NoRepair,
            HasIcon = row.Icon is not null
        };
    }

    private static int? IntValue(PackageRow row, params string[] names)
    {
        foreach (var name in names)
        {
            var text = row.Values.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))?.DataText;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;
        }

        return null;
    }

    private List<PackageRow> ReadAll()
    {
        var rows = new List<PackageRow>();
        var client = RegistryHelper.Local;
        foreach (var (hive, path, isX64, isGlobal) in Roots)
        {
            var children = client.ListSubKeys(hive, path, RegistryViewKind.Default, RegistryDetailLevel.Identity);
            foreach (var child in children)
            {
                var full = client.GetKey(hive, child.Path, RegistryViewKind.Default, RegistryDetailLevel.Full);
                if (full is null)
                    continue;
                var row = Project(full, isX64, isGlobal);
                row.ApplyDisplay(_datePattern, _decimals);
                rows.Add(row);
            }
        }
        return rows;
    }

    private static PackageRow Project(RegistryKeyInfo key, bool isX64, bool isGlobal)
    {
        string Value(string name)
            => key.Values.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))?.DataText ?? "";

        bool Flag(string name)
        {
            var text = Value(name);
            return text is "1" or "true" or "True" or "yes" or "Yes";
        }

        var display = Value("DisplayName");
        if (string.IsNullOrWhiteSpace(display))
            display = key.Name;

        var product = key.Name.StartsWith('{') ? key.Name : Value("ProductCode");

        var row = new PackageRow
        {
            DisplayName = display,
            Publisher = Value("Publisher"),
            DisplayVersion = Value("DisplayVersion"),
            InstallDate = Value("InstallDate"),
            EstimatedSize = Value("EstimatedSize"),
            IsX64 = isX64,
            IsGlobal = isGlobal,
            SystemComponent = Flag("SystemComponent"),
            WindowsInstaller = Flag("WindowsInstaller"),
            NoModify = Flag("NoModify"),
            NoRemove = Flag("NoRemove"),
            NoRepair = Flag("NoRepair"),
            UninstallString = Value("UninstallString"),
            ModifyPath = Value("ModifyPath"),
            KeyPath = key.Path,
            Hive = key.Hive.ToString(),
            Values = key.Values,
            Icon = IconLoader.Load(
                Value("DisplayIcon"),
                Value("InstallLocation"),
                Value("UninstallString"),
                Value("QuietUninstallString"),
                Value("ModifyPath"),
                display,
                product)
        };
        row.RepairCommand = RepairPlan.Build(row)?.Display ?? "";
        return row;
    }
}
