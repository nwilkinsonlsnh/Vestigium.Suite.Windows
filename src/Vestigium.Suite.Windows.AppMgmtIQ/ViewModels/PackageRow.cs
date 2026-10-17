using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

/// <summary>
/// Grid projection of one ARP package.
/// Temporary: the four-root walk and computed fields live here until a Helper door owns them.
/// </summary>
public sealed class PackageRow
{
    public required string DisplayName { get; init; }
    public required string Publisher { get; init; }
    public required string DisplayVersion { get; init; }
    public required string InstallDate { get; init; }
    public required string EstimatedSize { get; init; }
    public required bool IsX64 { get; init; }
    public required bool IsGlobal { get; init; }
    public required string UninstallString { get; init; }
    public required string KeyPath { get; init; }
    public required string Hive { get; init; }
    public required IReadOnlyList<RegistryValueInfo> Values { get; init; }

    public string DisplayInstallDate => DisplayFormats.Date(InstallDate);
    public string DisplaySize => DisplayFormats.Size(EstimatedSize);
}
