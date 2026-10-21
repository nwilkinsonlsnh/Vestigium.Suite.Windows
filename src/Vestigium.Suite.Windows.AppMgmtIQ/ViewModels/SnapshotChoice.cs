using System.IO;
using System.Text.Json;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class SnapshotChoice
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public bool Packages { get; set; } = true;
    public bool Features { get; set; } = true;
    public bool BuiltIn { get; set; } = true;
    public bool ActiveSetup { get; set; } = true;
    public bool Startup { get; set; } = true;
    public bool Footprints { get; set; } = true;

    public static SnapshotChoice Load()
    {
        var path = Path.Combine(SnapshotStore.Root, "choice.json");
        if (!File.Exists(path))
            return new SnapshotChoice();
        return JsonSerializer.Deserialize<SnapshotChoice>(File.ReadAllText(path), Json) ?? new SnapshotChoice();
    }

    public static void Save(SnapshotChoice choice)
    {
        Directory.CreateDirectory(SnapshotStore.Root);
        File.WriteAllText(Path.Combine(SnapshotStore.Root, "choice.json"), JsonSerializer.Serialize(choice, Json));
    }
}
