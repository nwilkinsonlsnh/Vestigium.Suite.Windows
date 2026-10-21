namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed record SnapshotDrift(string Key, string Status, string Saved, string Current);

public static class SnapshotCompare
{
    public static IReadOnlyList<SnapshotDrift> Compare(IReadOnlyList<SnapshotRow> saved, IReadOnlyList<SnapshotRow> current)
    {
        var before = saved
            .Where(row => !string.IsNullOrWhiteSpace(row.Key))
            .GroupBy(row => row.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);
        var after = current
            .Where(row => !string.IsNullOrWhiteSpace(row.Key))
            .GroupBy(row => row.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);

        var drifts = new List<SnapshotDrift>();
        foreach (var key in before.Keys.Union(after.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
        {
            var had = before.TryGetValue(key, out var oldValue);
            var has = after.TryGetValue(key, out var newValue);
            var status = (had, has) switch
            {
                (true, true) when string.Equals(oldValue, newValue, StringComparison.OrdinalIgnoreCase) => "match",
                (true, true) => "changed",
                (false, true) => "added",
                (true, false) => "removed",
                _ => "none"
            };
            drifts.Add(new SnapshotDrift(key, status, oldValue ?? "", newValue ?? ""));
        }
        return drifts;
    }

    public static string For(IReadOnlyList<SnapshotDrift> drifts, string key)
        => drifts.FirstOrDefault(drift => drift.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Status ?? "none";
}
