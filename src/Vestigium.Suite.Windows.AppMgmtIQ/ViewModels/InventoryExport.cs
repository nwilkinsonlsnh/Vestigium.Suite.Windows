using Vestigium.Helpers.ClosedXml;
using Vestigium.Helpers.FileSystemPaths;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public enum ExportRows
{
    Current,
    All
}

public sealed record ExportRequest(
    MainViewModel Packages,
    FeaturesViewModel Features,
    BloatwareViewModel Bloatware,
    ActiveSetupViewModel Setup,
    StartupViewModel Startup,
    FootprintsViewModel Footprints,
    bool IncludePackages,
    bool PackagesCurrent,
    bool IncludeFeatures,
    bool FeaturesCurrent,
    bool IncludeBloatware,
    bool BloatwareCurrent,
    bool IncludeActiveSetup,
    bool ActiveSetupCurrent,
    bool IncludeStartup,
    bool StartupCurrent,
    bool IncludeFootprints,
    bool FootprintsCurrent,
    bool Details);

public static class InventoryExport
{
    public static string Write(ExportRequest request)
    {
        var packageRows = request.IncludePackages
            ? request.PackagesCurrent ? request.Packages.Packages.ToArray() : request.Packages.AllPackages.ToArray()
            : [];
        var featureRows = request.IncludeFeatures ? FeatureRows(request.Features, request.FeaturesCurrent) : [];
        var bloatRows = request.IncludeBloatware
            ? request.BloatwareCurrent ? request.Bloatware.Rows.ToArray() : request.Bloatware.All.ToArray()
            : [];
        var setupRows = request.IncludeActiveSetup
            ? request.ActiveSetupCurrent ? request.Setup.Rows.ToArray() : request.Setup.All.ToArray()
            : [];
        var startupRows = request.IncludeStartup
            ? request.StartupCurrent ? request.Startup.Rows.ToArray() : request.Startup.All.ToArray()
            : [];
        var footprintRows = request.IncludeFootprints
            ? request.FootprintsCurrent ? request.Footprints.Rows.ToArray() : request.Footprints.All.ToArray()
            : [];

        using var book = WorkbookHelper.Create("Summary", HostIds.AppMgmtIQ);
        WriteSummary(book, request, packageRows.Length, featureRows.Count, bloatRows.Length, setupRows.Length, startupRows.Length, footprintRows.Length);
        if (request.IncludePackages)
            WritePackages(book, packageRows);
        if (request.IncludePackages && request.Details)
            WritePackageDetails(book, packageRows);
        if (request.IncludeFeatures)
            WriteFeatures(book, featureRows);
        if (request.IncludeBloatware)
            WriteBloatware(book, bloatRows);
        if (request.IncludeActiveSetup)
            WriteActiveSetup(book, setupRows);
        if (request.IncludeStartup)
            WriteStartup(book, startupRows);
        if (request.IncludeFootprints)
            WriteFootprints(book, footprintRows);

        var path = book.SaveAs(WorkbookHelper.NewExportPath(HostIds.AppMgmtIQ, "inventory"));
        HostLog.Information(AppMgmtCatalog.ExportWritten, "Export", $"Workbook written. {path}");
        return path;
    }

    private static void WriteSummary(WorkbookSession book, ExportRequest request, int packageCount, int featureCount, int bloatCount, int setupCount, int startupCount, int footprintCount)
    {
        var table = SheetTable.KeyValue("Item", "Value",
        [
            ("Machine", Environment.MachineName),
            ("When", DateTime.Now.ToString("dd MMM yyyy HH:mm")),
            ("AppMgmtIQ", request.IncludePackages ? (request.PackagesCurrent ? "Current view" : "Everything") : "No"),
            ("Windows Features", request.IncludeFeatures ? (request.FeaturesCurrent ? "Current view" : "Everything") : "No"),
            ("Bloatware", request.IncludeBloatware ? (request.BloatwareCurrent ? "Current view" : "Everything") : "No"),
            ("Active Setup", request.IncludeActiveSetup ? (request.ActiveSetupCurrent ? "Current view" : "Everything") : "No"),
            ("Startup", request.IncludeStartup ? (request.StartupCurrent ? "Current view" : "Everything") : "No"),
            ("Footprints", request.IncludeFootprints ? (request.FootprintsCurrent ? "Current view" : "Everything") : "No"),
            ("Package details", request.Details ? "Yes" : "No"),
            ("Packages", packageCount),
            ("Features", featureCount),
            ("Bloatware rows", bloatCount),
            ("Active Setup rows", setupCount),
            ("Startup rows", startupCount),
            ("Footprint rows", footprintCount)
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

    private static List<(string Caption, string Name, string State, string Parent, string Description)> FeatureRows(FeaturesViewModel features, bool current)
    {
        if (current)
        {
            return features.VisibleNodes()
                .Select(node => (node.Caption, node.Name, node.State, node.Parent, node.Feature?.Description ?? ""))
                .ToList();
        }

        return features.AllFeatures
            .Select(row => (row.Caption, row.Name, row.State, features.ParentOf(row.Name), row.Description))
            .ToList();
    }

    private static void WriteBloatware(WorkbookSession book, IReadOnlyList<BloatwareRow> rows)
    {
        var table = SheetTable.Create(
            ["Name", "Family", "Category", "Installed", "Provisioned", "Package"],
            rows.Select(row => (IReadOnlyList<object?>)[row.Name, row.Family, row.Category, row.Installed, row.Provisioned, row.Package]),
            "Bloatware");
        book.Sheet("Built-In (Packages)").WriteTable(table, new SheetWriteOptions { TabColor = "C47B2B" });
    }

    private static void WriteActiveSetup(WorkbookSession book, IReadOnlyList<ActiveSetupRow> rows)
    {
        var table = SheetTable.Create(
            ["Display Name", "Component", "Stub Path", "Version", "Scope", "Architecture", "Pending", "Installed"],
            rows.Select(row => (IReadOnlyList<object?>)[row.DisplayName, row.Component, row.StubPath, row.Version, row.Scope, row.Bit, row.Pending, row.Installed]),
            "ActiveSetup");
        book.Sheet("Active Setup").WriteTable(table, new SheetWriteOptions { TabColor = "8B5CF6" });
    }

    private static void WriteStartup(WorkbookSession book, IReadOnlyList<StartupRow> rows)
    {
        var table = SheetTable.Create(
            ["Name", "Source", "Scope", "Bit", "State", "Command", "Package"],
            rows.Select(row => (IReadOnlyList<object?>)[row.Name, row.Source, row.Scope, row.Bit, row.State, row.Command, row.Package]),
            "Startup");
        book.Sheet("Startup").WriteTable(table, new SheetWriteOptions { TabColor = "0E7C86" });
    }

    private static void WriteFootprints(WorkbookSession book, IReadOnlyList<FootprintRow> rows)
    {
        var table = SheetTable.Create(
            ["Kind", "Name", "Path", "Scope", "Ref count", "Exists", "Package"],
            rows.Select(row => (IReadOnlyList<object?>)[row.Kind, row.Name, PathResolve.Clean(row.Path), row.Scope, row.RefCountText, row.ExistsText, row.Package]),
            "Footprints");
        book.Sheet("Footprints").WriteTable(table, new SheetWriteOptions { TabColor = "B45309" });
    }
}
