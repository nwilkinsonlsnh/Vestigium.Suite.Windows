using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;
using Vestigium.Helpers.WinReg;

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
    private readonly KqlSession _session = AppMgmtKql.Packages();
    private KqlBoundQuery? _bound;
    private bool _showGlobal = true;
    private bool _showPerUser = true;
    private bool _showX64 = true;
    private bool _showX86 = true;
    private bool _showSystemComponents;
    private bool _showWindowsInstaller = true;
    private bool _showOtherInstallers = true;
    private string _datePattern = DisplayFormats.DefaultDateFormat.Pattern;
    private int _decimals = 4;

    public ObservableCollection<PackageRow> Packages { get; } = [];
    public ObservableCollection<string> Queries { get; } = [];
    public KqlSession Session => _session;

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

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading installed packages\u2026";
        try
        {
            var rows = await Task.Run(ReadAll);
            _all.Clear();
            _all.AddRange(rows);
            ApplyFilter();
            HostLog.Information(AppMgmtCatalog.InventoryLoaded, "Inventory", $"Inventory loaded. {_all.Count} packages.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    public async Task UninstallAsync(PackageRow package, Window owner)
    {
        var plan = UninstallPlan.Build(package);
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

        var note = plan.Rewritten ? $"\n\n{plan.Note}" : string.Empty;
        var go = MessageBox.Show(
            owner,
            $"Uninstall {package.DisplayName}?\n\nCommand:\n{plan.Display}{note}\n\nActor: {actor}",
            "Uninstall",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (go != MessageBoxResult.Yes)
            return;

        HostLog.Information(
            AppMgmtCatalog.UninstallStarted,
            "Uninstall",
            $"Uninstall started. package={package.DisplayName} command={plan.Display} rewritten={plan.Rewritten} actor={actor}");
        Status = $"Uninstalling {package.DisplayName}\u2026";
        try
        {
            var process = Process.Start(new ProcessStartInfo(plan.FileName, plan.Arguments) { UseShellExecute = true });
            if (process is not null)
                await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, ex.Message, "Uninstall", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        await LoadAsync();
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
            MessageBox.Show($"Could not clear NoRemove.\n{result.Status} {result.Message}", "NoRemove", MessageBoxButton.OK, MessageBoxImage.Error);
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
        _showGlobal = showGlobal;
        _showPerUser = showPerUser;
        _showX64 = showX64;
        _showX86 = showX86;
        _showSystemComponents = showSystemComponents;
        _showWindowsInstaller = showWindowsInstaller;
        _showOtherInstallers = showOtherInstallers;
        _datePattern = string.IsNullOrWhiteSpace(datePattern) ? DisplayFormats.DefaultDateFormat.Pattern : datePattern;
        _decimals = Math.Clamp(decimals, 0, 8);
        foreach (var row in _all)
            row.ApplyDisplay(_datePattern, _decimals);
        ApplyFilter();
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
        if (row.IsGlobal && !_showGlobal)
            return false;
        if (!row.IsGlobal && !_showPerUser)
            return false;
        if (row.IsX64 && !_showX64)
            return false;
        if (!row.IsX64 && !_showX86)
            return false;
        if (row.SystemComponent && !_showSystemComponents)
            return false;
        if (row.WindowsInstaller && !_showWindowsInstaller)
            return false;
        if (!row.WindowsInstaller && !_showOtherInstallers)
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

        return new PackageRow
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
    }
}
