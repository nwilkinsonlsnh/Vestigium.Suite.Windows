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

    public ObservableCollection<PackageRow> Packages { get; } = [];

    [ObservableProperty]
    private PackageRow? _selectedPackage;

    [ObservableProperty]
    private string _status = "Idle";

    public void Load()
    {
        Packages.Clear();
        var client = RegistryHelper.Local;
        foreach (var (hive, path, isX64, isGlobal) in Roots)
        {
            var children = client.ListSubKeys(hive, path, RegistryViewKind.Default, RegistryDetailLevel.Identity);
            foreach (var child in children)
            {
                var full = client.GetKey(hive, child.Path, RegistryViewKind.Default, RegistryDetailLevel.Full);
                if (full is null)
                    continue;
                Packages.Add(Project(full, isX64, isGlobal));
            }
        }
        Status = $"{Packages.Count} packages";
    }

    private static PackageRow Project(RegistryKeyInfo key, bool isX64, bool isGlobal)
    {
        string Value(string name)
            => key.Values.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))?.DataText ?? "";

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
            UninstallString = Value("UninstallString"),
            KeyPath = key.Path,
            Hive = key.Hive.ToString()
        };
    }
}
