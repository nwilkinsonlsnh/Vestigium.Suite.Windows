using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;
using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;
using Xunit;

namespace Vestigium.Suite.Windows.Tests;

public class FootprintsTests
{
    [Fact]
    public void Path_match_links_the_package()
    {
        var row = Entry(@"C:\Program Files\Contoso\contoso.exe");
        var name = FootprintLink.Match(row, [Package("Contoso", @"C:\Program Files\Contoso")]);
        Assert.Equal("Contoso", name);
    }

    [Fact]
    public void Product_code_links_the_package()
    {
        var row = Entry("{11111111-2222-3333-4444-555555555555}", "Contoso");
        var name = FootprintLink.Match(row, [Package("Contoso", key: "Uninstall\\{11111111-2222-3333-4444-555555555555}")]);
        Assert.Equal("Contoso", name);
    }

    [Fact]
    public void Name_match_when_no_path()
    {
        var row = Entry("", "OneDrive");
        var name = FootprintLink.Match(row, [Package("OneDrive")]);
        Assert.Equal("OneDrive", name);
    }

    [Fact]
    public void Short_names_are_ignored()
    {
        var row = Entry("", "ab");
        var name = FootprintLink.Match(row, [Package("ab")]);
        Assert.Equal("", name);
    }

    [Fact]
    public void No_signal_stays_unlinked()
    {
        var row = Entry(@"C:\Other\tool.exe", "tool");
        var name = FootprintLink.Match(row, [Package("Contoso", @"C:\Program Files\Contoso")]);
        Assert.Equal("", name);
    }

    [Fact]
    public void Catalog_projects_kind_path_and_package()
    {
        using var session = AppMgmtKql.Footprints();
        var row = FootprintsAdapter.ToRow(session, new FootprintsFacts
        {
            Kind = "SharedDll",
            Name = "contoso.dll",
            Path = @"C:\Program Files\Contoso\contoso.dll",
            Scope = "HKLM",
            RefCount = 3,
            Package = "Contoso"
        });

        Assert.Equal("SharedDll", row.Get("footprint.kind").Raw);
        Assert.Equal(@"C:\Program Files\Contoso\contoso.dll", row.Get("footprint.path").Raw);
        Assert.Equal(3, row.Get("footprint.refcount").Raw);
        Assert.Equal("Contoso", row.Get("footprint.package").Raw);
    }

    [Fact]
    public void Alias_binds_the_same_field()
    {
        using var session = AppMgmtKql.Footprints();
        Assert.True(session.TryGetField("footprint.kind", out var dotted));
        Assert.True(session.TryGetField("kind", out var bare));
        Assert.Same(dotted, bare);
    }

    [Fact]
    public void Missing_refcount_is_unknown()
    {
        using var session = AppMgmtKql.Footprints();
        var row = FootprintsAdapter.ToRow(session, new FootprintsFacts { Kind = "AppPath" });
        Assert.True(row.Get("footprint.refcount").IsUnknown);
    }

    [Fact]
    public void Bound_query_is_recorded()
    {
        var vm = new FootprintsViewModel { Query = "footprint.kind == 'AppPath'" };
        vm.CommitQuery();
        Assert.Single(vm.Queries);
        Assert.Equal("footprint.kind == 'AppPath'", vm.Queries[0].Text);
    }

    [Fact]
    public void Unbound_query_is_not_recorded()
    {
        var vm = new FootprintsViewModel { Query = "footprint.missing == true" };
        vm.CommitQuery();
        Assert.Empty(vm.Queries);
    }

    private static FootprintRow Entry(string path, string name = "Contoso")
        => new() { Kind = "AppPath", Name = name, Path = path, Scope = "HKLM" };

    private static PackageRow Package(string name, string? location = null, string? key = null)
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
            KeyPath = key ?? @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + name,
            Hive = "LocalMachine",
            Values = location is null
                ? []
                : [new RegistryValueInfo { Name = "InstallLocation", Type = RegistryValueKind.String, DataText = location }]
        };
}
