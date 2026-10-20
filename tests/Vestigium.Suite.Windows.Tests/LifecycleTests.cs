using Vestigium.Helpers.Kql.AppMgmt;
using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;
using Xunit;

namespace Vestigium.Suite.Windows.Tests;

public class LifecycleTests
{
    [Fact]
    public void Baseline_is_none_without_an_expected_version()
        => Assert.Equal("none", BaselineCompare.Status("1.0", null));

    [Fact]
    public void Baseline_matches_equal_versions()
        => Assert.Equal("match", BaselineCompare.Status("1.2.3", "1.2.3"));

    [Fact]
    public void Baseline_is_newer_when_installed_is_higher()
        => Assert.Equal("newer", BaselineCompare.Status("2.0", "1.9"));

    [Fact]
    public void Baseline_is_older_when_installed_is_lower()
        => Assert.Equal("older", BaselineCompare.Status("1.0", "1.1"));

    [Fact]
    public void Baseline_is_unknown_when_versions_cannot_be_ordered()
        => Assert.Equal("unknown", BaselineCompare.Status("beta", "alpha"));

    [Fact]
    public void Missing_install_location_is_an_orphan()
    {
        var row = Package(Value("InstallLocation", @"C:\Program Files\Contoso"));
        var result = OrphanRules.Check(row, _ => false);
        Assert.True(result?.Orphan);
        Assert.Equal("Install location missing", result?.Reason);
    }

    [Fact]
    public void Missing_key_file_is_an_orphan()
    {
        var row = Package(Value("DisplayIcon", @"C:\Program Files\Contoso\contoso.exe,0"));
        var result = OrphanRules.Check(row, _ => false);
        Assert.True(result?.Orphan);
        Assert.Equal("Key file missing", result?.Reason);
    }

    [Fact]
    public void Present_location_is_not_an_orphan()
    {
        var row = Package(Value("InstallLocation", @"C:\Program Files\Contoso"));
        var result = OrphanRules.Check(row, _ => true);
        Assert.False(result?.Orphan);
    }

    [Fact]
    public void No_signal_stays_unknown()
    {
        var row = Package();
        Assert.Null(OrphanRules.Check(row, _ => false));
    }

    [Fact]
    public void Repair_command_builds_from_a_product_code()
    {
        var row = Package();
        row = new PackageRow
        {
            DisplayName = "Contoso",
            Publisher = "",
            DisplayVersion = "1.0",
            InstallDate = "",
            EstimatedSize = "",
            IsX64 = true,
            IsGlobal = true,
            SystemComponent = false,
            WindowsInstaller = true,
            NoModify = false,
            NoRemove = false,
            NoRepair = false,
            UninstallString = "",
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{11111111-2222-3333-4444-555555555555}",
            Hive = "LocalMachine",
            Values = [],
            Icon = null
        };
        var plan = RepairPlan.Build(row);
        Assert.NotNull(plan);
        Assert.Contains("{11111111-2222-3333-4444-555555555555}", plan!.Display);
    }

    [Fact]
    public void NoRepair_blocks_the_command()
    {
        var row = new PackageRow
        {
            DisplayName = "Contoso",
            Publisher = "",
            DisplayVersion = "1.0",
            InstallDate = "",
            EstimatedSize = "",
            IsX64 = true,
            IsGlobal = true,
            SystemComponent = false,
            WindowsInstaller = true,
            NoModify = false,
            NoRemove = false,
            NoRepair = true,
            UninstallString = "",
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{11111111-2222-3333-4444-555555555555}",
            Hive = "LocalMachine",
            Values = [],
            Icon = null
        };
        Assert.Null(RepairPlan.Build(row));
    }

    [Fact]
    public void Catalog_projects_lifecycle_fields()
    {
        using var session = AppMgmtKql.Packages();
        var row = PackageAdapter.ToRow(session, new PackageFacts
        {
            DisplayName = "Contoso",
            HasModify = true,
            HasRepair = false,
            IsOrphan = true,
            BaselineStatus = "older",
            PendingReboot = true
        });

        Assert.Equal(true, row.Get("app.HasModify").Raw);
        Assert.Equal(false, row.Get("app.HasRepair").Raw);
        Assert.Equal(true, row.Get("app.IsOrphan").Raw);
        Assert.Equal("older", row.Get("app.BaselineStatus").Raw);
        Assert.Equal(true, row.Get("app.PendingReboot").Raw);
    }

    private static PackageRow Package(params RegistryValueInfo[] values)
        => new()
        {
            DisplayName = "Contoso",
            Publisher = "",
            DisplayVersion = "1.0",
            InstallDate = "",
            EstimatedSize = "",
            IsX64 = true,
            IsGlobal = true,
            SystemComponent = false,
            WindowsInstaller = false,
            NoModify = false,
            NoRemove = false,
            NoRepair = false,
            UninstallString = "",
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Contoso",
            Hive = "LocalMachine",
            Values = values,
            Icon = null
        };

    private static RegistryValueInfo Value(string name, string data)
        => new() { Name = name, Type = RegistryValueKind.String, DataText = data };
}
