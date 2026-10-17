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
            Build("Uninstall", package, UninstallNames),
            Build("Links", package, LinkNames),
            Build("Raw", package, null)
        ];
    }

    public string Title { get; }

    public ObservableCollection<DetailsTab> Tabs { get; }

    private static readonly string[] GeneralNames =
    [
        "DisplayName", "DisplayVersion", "Publisher", "InstallDate", "EstimatedSize",
        "InstallLocation", "InstallSource", "Language", "Version", "VersionMajor", "VersionMinor"
    ];

    private static readonly string[] UninstallNames =
    [
        "UninstallString", "QuietUninstallString", "ModifyPath", "NoModify", "NoRepair", "NoRemove"
    ];

    private static readonly string[] LinkNames =
    [
        "HelpLink", "HelpTelephone", "URLInfoAbout", "URLUpdateInfo", "Readme", "Contact"
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
