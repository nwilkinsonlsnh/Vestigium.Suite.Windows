using System.IO;
using System.Text.Json;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class SnapshotRow
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class Snapshot
{
    public string Name { get; set; } = "";
    public string Machine { get; set; } = "";
    public DateTime Taken { get; set; }
    public List<SnapshotRow> Packages { get; set; } = [];
    public List<SnapshotRow> Features { get; set; } = [];
    public List<SnapshotRow> BuiltIn { get; set; } = [];
    public List<SnapshotRow> ActiveSetup { get; set; } = [];
    public List<SnapshotRow> Startup { get; set; } = [];
    public List<SnapshotRow> Footprints { get; set; } = [];
}

public sealed class SnapshotStore
{
    private static readonly JsonSerializerOptions Json =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "Vestigium", "Snapshots", "AppMgmtIQ");

    public SnapshotStore()
    {
        DirectoryPath = Root;
    }

    public SnapshotStore(string rootDirectory)
    {
        DirectoryPath = string.IsNullOrWhiteSpace(rootDirectory) ? Root : rootDirectory;
    }

    public string DirectoryPath { get; }

    public IReadOnlyList<string> List()
    {
        if (!Directory.Exists(DirectoryPath))
            return [];
        return Directory.GetFiles(DirectoryPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderByDescending(name => name, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray();
    }

    public Snapshot? Load(string name)
    {
        var path = FilePath(name);
        if (!File.Exists(path))
            return null;
        return JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path), Json);
    }

    public void Save(Snapshot snapshot)
    {
        Directory.CreateDirectory(DirectoryPath);
        if (string.IsNullOrWhiteSpace(snapshot.Name))
            snapshot.Name = NameFor(snapshot.Taken == default ? DateTime.Now : snapshot.Taken);
        File.WriteAllText(FilePath(snapshot.Name), JsonSerializer.Serialize(snapshot, Json));
    }

    public void Delete(string name)
    {
        var path = FilePath(name);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static string NameFor(DateTime taken)
        => Environment.MachineName + "-" + taken.ToString("yyyyMMdd-HHmmss");

    private string FilePath(string name)
        => Path.Combine(DirectoryPath, name + ".json");
}
