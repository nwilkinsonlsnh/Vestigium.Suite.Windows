using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private bool _showGlobal = true;
    private bool _showPerUser = true;
    private string _datePattern = DisplayFormats.DefaultDateFormat.Pattern;
    private int _decimals = 4;

    public ObservableCollection<PackageRow> Packages { get; } = [];

    [ObservableProperty]
    private PackageRow? _selectedPackage;

    [ObservableProperty]
    private string _status = "Idle";

    public void Load()
    {
        _all.Clear();
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
                _all.Add(row);
            }
        }
        ApplyFilter();
        HostLog.Information(AppMgmtCatalog.InventoryLoaded, "Inventory", $"Inventory loaded. {_all.Count} packages.");
    }

    public void ApplySettings(bool showGlobal, bool showPerUser, string datePattern, int decimals)
    {
        _showGlobal = showGlobal;
        _showPerUser = showPerUser;
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
        foreach (var row in _all)
        {
            if (row.IsGlobal && !_showGlobal)
                continue;
            if (!row.IsGlobal && !_showPerUser)
                continue;
            Packages.Add(row);
        }
        SelectedPackage = Packages.Contains(keep!) ? keep : null;
        Status = $"{Packages.Count} packages";
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
            Icon = IconLoader.Load(Value("DisplayIcon"))
        };
    }
}
