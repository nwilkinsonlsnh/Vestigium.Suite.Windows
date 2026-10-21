namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public static class BaselineApply
{
    public static void Run(IReadOnlyList<SnapshotRow> current, List<SnapshotRow> saved, Action<string, string> set)
    {
        var drifts = saved.Count == 0
            ? current.Select(row => new SnapshotDrift(row.Key, "none", "", row.Value)).ToList()
            : SnapshotCompare.Compare(saved, current).ToList();
        foreach (var drift in drifts)
            set(drift.Key, drift.Status);
    }

    public static Snapshot? Latest()
    {
        var store = new SnapshotStore(AppMgmtSettingsStore.DefaultRoot);
        var name = store.List().FirstOrDefault();
        return name is null ? null : store.Load(name);
    }
}
