using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;
using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;
using Xunit;

namespace Vestigium.Suite.Windows.Tests;

public class StartupTests
{
    [Fact]
    public void Path_match_links_the_package()
    {
        var row = Entry(@"C:\Program Files\Contoso\contoso.exe");
        var name = StartupLink.Match(row, [Package("Contoso", @"C:\Program Files\Contoso")]);
        Assert.Equal("Contoso", name);
    }

    [Fact]
    public void Quoted_command_still_matches()
    {
        var row = Entry("\"C:\\Program Files\\Contoso\\contoso.exe\" /silent");
        var name = StartupLink.Match(row, [Package("Contoso", @"C:\Program Files\Contoso")]);
        Assert.Equal("Contoso", name);
    }

    [Fact]
    public void Name_match_when_no_path()
    {
        var row = Entry("OneDrive.exe", "OneDrive");
        var name = StartupLink.Match(row, [Package("OneDrive")]);
        Assert.Equal("OneDrive", name);
    }

    [Fact]
    public void Short_names_are_ignored()
    {
        var row = Entry("ab.exe", "ab");
        var name = StartupLink.Match(row, [Package("ab")]);
        Assert.Equal("", name);
    }

    [Fact]
    public void No_signal_stays_unlinked()
    {
        var row = Entry("C:\\Other\\tool.exe", "tool");
        var name = StartupLink.Match(row, [Package("Contoso", @"C:\Program Files\Contoso")]);
        Assert.Equal("", name);
    }

    [Fact]
    public void Catalog_projects_bit_and_package()
    {
        using var session = AppMgmtKql.Startup();
        var row = StartupAdapter.ToRow(session, new StartupFacts
        {
            Name = "Contoso",
            Command = @"C:\Program Files\Contoso\contoso.exe",
            Source = "Run",
            Scope = "HKLM",
            IsX64 = true,
            Package = "Contoso"
        });

        Assert.Equal("Contoso", row.Get("startup.name").Raw);
        Assert.Equal("Run", row.Get("startup.source").Raw);
        Assert.Equal(true, row.Get("startup.isX64").Raw);
        Assert.Equal(false, row.Get("startup.isX86").Raw);
        Assert.Equal("Contoso", row.Get("startup.package").Raw);
    }

    [Fact]
    public void Folder_leaves_bit_unknown()
    {
        using var session = AppMgmtKql.Startup();
        var row = StartupAdapter.ToRow(session, new StartupFacts { Source = "Startup folder", Scope = "User" });
        Assert.True(row.Get("startup.isX64").IsUnknown);
        Assert.True(row.Get("startup.isX86").IsUnknown);
    }

    [Fact]
    public void Bound_query_is_recorded()
    {
        var vm = new StartupViewModel { Query = "startup.source == 'Run'" };
        vm.CommitQuery();
        Assert.Single(vm.Queries);
        Assert.Equal("startup.source == 'Run'", vm.Queries[0].Text);
    }

    [Fact]
    public void Unbound_query_is_not_recorded()
    {
        var vm = new StartupViewModel { Query = "startup.missing == true" };
        vm.CommitQuery();
        Assert.Empty(vm.Queries);
    }

    private static StartupRow Entry(string command, string name = "Contoso")
        => new() { Name = name, Command = command, Source = "Run", Scope = "HKLM", Bit = "64" };

    private static PackageRow Package(string name, string? location = null)
        => new()
        {
            DisplayName = name,
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
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + name,
            Hive = "LocalMachine",
            Values = location is null
                ? []
                : [new RegistryValueInfo { Name = "InstallLocation", Type = RegistryValueKind.String, DataText = location }]
        };
}
