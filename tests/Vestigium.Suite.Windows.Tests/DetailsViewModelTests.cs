using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;
using Xunit;

namespace Vestigium.Suite.Windows.Tests;

public class DetailsViewModelTests
{
    [Fact]
    public void Title_includes_version_when_present()
    {
        var vm = new DetailsViewModel(Package("Contoso", "1.2.3"));
        Assert.Equal("Contoso (1.2.3)", vm.Title);
    }

    [Fact]
    public void Title_is_name_only_when_version_blank()
    {
        var vm = new DetailsViewModel(Package("Contoso", ""));
        Assert.Equal("Contoso", vm.Title);
    }

    [Fact]
    public void General_tab_shows_computed_fields()
    {
        var vm = new DetailsViewModel(Package("Contoso", "1.0", isX64: false, isGlobal: true));
        var general = vm.Tabs.Single(t => t.Header == "General");

        Assert.Contains(general.Rows, r => r.Name == "isX64" && r.Data == "False");
        Assert.Contains(general.Rows, r => r.Name == "isGlobal" && r.Data == "True");
        Assert.Contains(general.Rows, r => r.Name == "Hive" && r.Data == "LocalMachine");
    }

    [Fact]
    public void Raw_tab_lists_every_value()
    {
        var values = new[]
        {
            Value("DisplayName", "Contoso"),
            Value("UninstallString", "msiexec /x {guid}"),
            Value("HelpLink", "https://example")
        };
        var vm = new DetailsViewModel(Package("Contoso", "1.0", values));
        var raw = vm.Tabs.Single(t => t.Header == "Raw");

        Assert.Equal(3, raw.Rows.Count);
        Assert.Contains(raw.Rows, r => r.Name == "DisplayName");
        Assert.Contains(raw.Rows, r => r.Name == "UninstallString");
        Assert.Contains(raw.Rows, r => r.Name == "HelpLink");
    }

    [Fact]
    public void Uninstall_tab_only_matching_names()
    {
        var values = new[]
        {
            Value("DisplayName", "Contoso"),
            Value("UninstallString", "msiexec /x {guid}"),
            Value("QuietUninstallString", "msiexec /x {guid} /qn")
        };
        var vm = new DetailsViewModel(Package("Contoso", "1.0", values));
        var uninstall = vm.Tabs.Single(t => t.Header == "Uninstall");

        Assert.Equal(2, uninstall.Rows.Count);
        Assert.DoesNotContain(uninstall.Rows, r => r.Name == "DisplayName");
    }

    private static PackageRow Package(
        string name,
        string version,
        IReadOnlyList<RegistryValueInfo>? values = null,
        bool isX64 = true,
        bool isGlobal = true)
        => new()
        {
            DisplayName = name,
            Publisher = "",
            DisplayVersion = version,
            InstallDate = "",
            EstimatedSize = "",
            IsX64 = isX64,
            IsGlobal = isGlobal,
            UninstallString = "",
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Contoso",
            Hive = "LocalMachine",
            Values = values ?? []
        };

    private static RegistryValueInfo Value(string name, string data)
        => new()
        {
            Name = name,
            Type = RegistryValueKind.String,
            DataText = data
        };
}
