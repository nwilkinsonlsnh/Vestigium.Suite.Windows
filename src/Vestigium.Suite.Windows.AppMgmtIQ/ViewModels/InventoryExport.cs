using Vestigium.Helpers.ClosedXml;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public enum ExportScope
{
    Packages,
    Features,
    Both
}

public enum ExportRows
{
    Current,
    All
}

public static class InventoryExport
{
    public static string Write(MainViewModel packages, FeaturesViewModel features, ExportScope scope, ExportRows rows, bool details)
    {
        var includePackages = scope is ExportScope.Packages or ExportScope.Both;
        var includeFeatures = scope is ExportScope.Features or ExportScope.Both;
        var packageRows = includePackages
            ? rows == ExportRows.Current ? packages.Packages.ToArray() : packages.AllPackages.ToArray()
            : [];
        var featureRows = includeFeatures ? FeatureRows(features, rows) : [];

        using var book = WorkbookHelper.Create("Summary", HostIds.AppMgmtIQ);
        WriteSummary(book, packages, scope, rows, details, packageRows.Length, featureRows.Count);
        if (includePackages)
            WritePackages(book, packageRows);
        if (includePackages && details)
            WritePackageDetails(book, packageRows);
        if (includeFeatures)
            WriteFeatures(book, featureRows);

        var path = book.SaveAs(WorkbookHelper.NewExportPath(HostIds.AppMgmtIQ, "inventory"));
        HostLog.Information(AppMgmtCatalog.ExportWritten, "Export", $"Workbook written. {path}");
        return path;
    }

    private static void WriteSummary(
        WorkbookSession book,
        MainViewModel packages,
        ExportScope scope,
        ExportRows rows,
        bool details,
        int packageCount,
        int featureCount)
    {
        var table = SheetTable.KeyValue("Item", "Value",
        [
            ("Machine", Environment.MachineName),
            ("When", DateTime.Now.ToString("dd MMM yyyy HH:mm")),
            ("Scope", ScopeText(scope)),
            ("Rows", rows == ExportRows.Current ? "Current view" : "Everything loaded"),
            ("Details", details ? "Yes" : "No"),
            ("Package query", packages.Query),
            ("Packages", packageCount),
            ("Features", featureCount)
        ], "Summary");
        book.Sheet("Summary").WriteTable(table, new SheetWriteOptions { TabColor = "4C8DFF" });
    }

    private static void WritePackages(WorkbookSession book, IReadOnlyList<PackageRow> rows)
    {
        var table = SheetTable.Create(
            ["Display Name", "Publisher", "Version", "Install Date", "Size", "x64", "Global", "System Component", "Windows Installer", "No Modify", "No Remove", "No Repair", "Hive", "Key"],
            rows.Select(row => (IReadOnlyList<object?>)
            [
                row.DisplayName, row.Publisher, row.DisplayVersion, row.DisplayInstallDate, row.DisplaySize,
                row.IsX64, row.IsGlobal, row.SystemComponent, row.WindowsInstaller, row.NoModify, row.NoRemove, row.NoRepair,
                row.Hive, row.KeyPath
            ]),
            "Packages");
        book.Sheet("Packages").WriteTable(table, new SheetWriteOptions { TabColor = "2F6FED" });
    }

    private static void WritePackageDetails(WorkbookSession book, IReadOnlyList<PackageRow> rows)
    {
        var lines = new List<IReadOnlyList<object?>>();
        foreach (var row in rows)
        {
            foreach (var value in row.Values)
                lines.Add([row.DisplayName, row.DisplayVersion, value.Name, value.Type.ToString(), value.DataText ?? ""]);
        }

        var table = SheetTable.Create(["Display Name", "Version", "Value", "Type", "Data"], lines, "PackageDetails");
        book.Sheet("Package Details").WriteTable(table, new SheetWriteOptions { TabColor = "7AA2FF" });
    }

    private static void WriteFeatures(WorkbookSession book, IReadOnlyList<(string Caption, string Name, string State, string Parent, string Description)> rows)
    {
        var table = SheetTable.Create(
            ["Display Name", "Feature Name", "State", "Parent", "Description"],
            rows.Select(row => (IReadOnlyList<object?>)[row.Caption, row.Name, row.State, row.Parent, row.Description]),
            "Features");
        book.Sheet("Features").WriteTable(table, new SheetWriteOptions { TabColor = "1F8A4C" });
    }

    private static List<(string Caption, string Name, string State, string Parent, string Description)> FeatureRows(FeaturesViewModel features, ExportRows rows)
    {
        if (rows == ExportRows.Current)
        {
            return features.VisibleNodes()
                .Select(node => (node.Caption, node.Name, node.State, node.Parent, node.Feature?.Description ?? ""))
                .ToList();
        }

        return features.AllFeatures
            .Select(row => (row.Caption, row.Name, row.State, features.ParentOf(row.Name), row.Description))
            .ToList();
    }

    private static string ScopeText(ExportScope scope) => scope switch
    {
        ExportScope.Packages => "Packages",
        ExportScope.Features => "Features",
        _ => "Packages and features"
    };
}
