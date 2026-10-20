using System.IO;
using System.Text.Json;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class BaselineEntry
{
    public string Key { get; set; } = "";
    public string Version { get; set; } = "";
}

public sealed class BaselineStore
{
    private static readonly JsonSerializerOptions Json =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, string> _versions = new(StringComparer.OrdinalIgnoreCase);

    public BaselineStore(string rootDirectory)
    {
        FilePath = Path.Combine(rootDirectory, "baselines.json");
    }

    public string FilePath { get; }

    public void Load()
    {
        _versions.Clear();
        if (!File.Exists(FilePath))
            return;
        var entries = JsonSerializer.Deserialize<List<BaselineEntry>>(File.ReadAllText(FilePath), Json) ?? [];
        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Key))
                _versions[entry.Key] = entry.Version;
        }
    }

    public string? Expected(string key)
        => _versions.TryGetValue(key, out var version) ? version : null;

    public void Set(string key, string version)
    {
        _versions[key] = version;
        Save();
    }

    public void Clear(string key)
    {
        if (_versions.Remove(key))
            Save();
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var entries = _versions
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new BaselineEntry { Key = pair.Key, Version = pair.Value })
            .ToList();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(entries, Json));
    }
}
