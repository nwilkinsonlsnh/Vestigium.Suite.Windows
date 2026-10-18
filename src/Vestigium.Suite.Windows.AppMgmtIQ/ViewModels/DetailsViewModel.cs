using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class ValueRow
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Data { get; init; }
}

public sealed class DetailsTab
{
    public required string Header { get; init; }
    public required ObservableCollection<ValueRow> Rows { get; init; }
}

public sealed partial class DetailsViewModel : ObservableObject
{
    public DetailsViewModel(PackageRow package)
    {
        Title = string.IsNullOrWhiteSpace(package.DisplayVersion)
            ? package.DisplayName
            : $"{package.DisplayName} ({package.DisplayVersion})";

        Tabs =
        [
            Build("General", package, GeneralNames),
            Build("Version", package, VersionNames),
            Build("Install", package, InstallNames),
            Build("Uninstall", package, UninstallNames),
            Build("Links", package, LinkNames),
            Build("Patch", package, PatchNames),
            Build("Raw", package, null)
        ];
        SelectedTab = Tabs[0];
    }

    public string Title { get; }

    public ObservableCollection<DetailsTab> Tabs { get; }

    [ObservableProperty]
    private DetailsTab? _selectedTab;

    [ObservableProperty]
    private ValueRow? _selectedRow;

    private static readonly string[] GeneralNames =
    [
        "DisplayName", "DisplayName_Localized", "DisplayVersion", "Publisher", "Comments",
        "Contact", "DisplayIcon", "Language", "Cpu", "ProductId", "ReleaseTrain", "ReleaseType",
        "SettingsIdentifier", "ParentDisplayName", "ParentKeyName"
    ];

    private static readonly string[] VersionNames =
    [
        "Version", "VersionMajor", "VersionMinor", "MajorVersion", "MinorVersion",
        "ProductVersion", "PatchProductVersion", "SPLevel"
    ];

    private static readonly string[] InstallNames =
    [
        "InstallDate", "EstimatedSize", "Size", "InstallLocation", "InstallSource",
        "InstanceId", "WindowsInstaller", "SystemComponent", "ClickToRunComponent",
        "AuthorizedCDFPrefix", "AssociatedHotfixBuild"
    ];

    private static readonly string[] UninstallNames =
    [
        "UninstallString", "UninstallString_Hidden", "QuietUninstallString", "ModifyPath",
        "ModifyPath_Hidden", "NoModify", "NoRepair", "NoRemove"
    ];

    private static readonly string[] LinkNames =
    [
        "HelpLink", "HelpTelephone", "URLInfoAbout", "URLUpdateInfo", "Readme"
    ];

    private static readonly string[] PatchNames =
    [
        "KBNumber", "PatchType", "InstalledPatchCodeList", "PatchedProductCodeList",
        "SQLProductFamilyCode", "SQLProductPatchFamilyCode"
    ];

    private static DetailsTab Build(string header, PackageRow package, string[]? names)
    {
        var rows = new ObservableCollection<ValueRow>();
        if (header == "General")
        {
            rows.Add(new ValueRow { Name = "isX64", Type = "bool", Data = package.IsX64.ToString() });
            rows.Add(new ValueRow { Name = "isGlobal", Type = "bool", Data = package.IsGlobal.ToString() });
            rows.Add(new ValueRow { Name = "Hive", Type = "string", Data = package.Hive });
            rows.Add(new ValueRow { Name = "KeyPath", Type = "string", Data = package.KeyPath });
        }

        var source = names is null
            ? package.Values
            : package.Values.Where(v => names.Contains(v.Name, StringComparer.OrdinalIgnoreCase));

        foreach (var value in source.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new ValueRow
            {
                Name = string.IsNullOrEmpty(value.Name) ? "(default)" : value.Name,
                Type = value.Type.ToString(),
                Data = value.DataText ?? ""
            });
        }

        return new DetailsTab { Header = header, Rows = rows };
    }
}
