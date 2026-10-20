using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

/// <summary>
/// Grid projection of one ARP package.
/// Temporary: the four-root walk and computed fields live here until a Helper door owns them.
/// </summary>
public sealed partial class PackageRow : ObservableObject
{
    public required string DisplayName { get; init; }
    public required string Publisher { get; init; }
    public required string DisplayVersion { get; init; }
    public required string InstallDate { get; init; }
    public required string EstimatedSize { get; init; }
    public required bool IsX64 { get; init; }
    public required bool IsGlobal { get; init; }
    public required bool SystemComponent { get; init; }
    public required bool WindowsInstaller { get; init; }
    public required bool NoModify { get; init; }
    public required bool NoRemove { get; init; }
    public required bool NoRepair { get; init; }
    public required string UninstallString { get; init; }
    public string ModifyPath { get; init; } = "";
    public string RepairCommand { get; init; } = "";
    public bool HasModify => !NoModify && !string.IsNullOrWhiteSpace(ModifyPath);
    public bool HasRepair => !string.IsNullOrWhiteSpace(RepairCommand);
    public required string KeyPath { get; init; }
    public required string Hive { get; init; }
    public required IReadOnlyList<RegistryValueInfo> Values { get; init; }
    public ImageSource? Icon { get; init; }

    [ObservableProperty]
    private string _displayInstallDate = "";

    [ObservableProperty]
    private string _displaySize = "";

    public void ApplyDisplay(string datePattern, int decimals)
    {
        DisplayInstallDate = DisplayFormats.Date(InstallDate, datePattern);
        DisplaySize = DisplayFormats.Size(EstimatedSize, decimals);
    }
}
