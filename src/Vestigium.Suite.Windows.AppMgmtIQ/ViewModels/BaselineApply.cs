using System.IO;
using System.Text.Json;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public static class BaselineApply
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static void Run(IReadOnlyList<SnapshotRow> current, List<SnapshotRow> saved, Action<string, string> set)
    {
        var drifts = saved.Count == 0
            ? current.Select(row => new SnapshotDrift(row.Key, "none", "", row.Value)).ToList()
            : SnapshotCompare.Compare(saved, current).ToList();
        foreach (var drift in drifts)
            set(drift.Key, drift.Status);
    }

    public static string? Selected
    {
        get
        {
            var path = ChoicePath();
            if (!File.Exists(path))
                return null;
            return System.Text.Json.JsonSerializer.Deserialize<string>(File.ReadAllText(path));
        }
        set
        {
            var path = ChoicePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(value ?? "", Json));
        }
    }

    public static Snapshot? Latest()
    {
        var store = new SnapshotStore();
        var names = store.List();
        var chosen = Selected;
        var name = chosen is not null && names.Contains(chosen) ? chosen : names.FirstOrDefault();
        return name is null ? null : store.Load(name);
    }

    public static void DeleteSelected()
    {
        var name = Selected ?? new SnapshotStore().List().FirstOrDefault();
        if (name is null)
            return;
        new SnapshotStore().Delete(name);
        Selected = null;
    }

    private static string ChoicePath()
        => Path.Combine(AppMgmtSettingsStore.DefaultRoot, "snapshot.txt");
}
