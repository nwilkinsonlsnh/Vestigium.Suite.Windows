using System.IO;
using System.Text.Json;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ElevatedJob
{
    public string Verb { get; set; } = "Copy";

    public string Source { get; set; } = "";

    public string? Destination { get; set; }

    public string Collision { get; set; } = "UniqueName";

    public string Pattern { get; set; } = ".##";

    public bool Purge { get; set; }

    public bool Audit { get; set; }

    public bool Secure { get; set; }

    public int SecurePasses { get; set; } = 3;

    public int MaxThreads { get; set; } = 32;

    public static ElevatedJob? Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        return JsonSerializer.Deserialize<ElevatedJob>(File.ReadAllText(path));
    }

    public FileIoJob Build()
    {
        var options = new FileIoJobOptions
        {
            Collision = Enum.TryParse<FileIoCollision>(Collision, out var collision) ? collision : FileIoCollision.UniqueName,
            UniqueNamePattern = string.IsNullOrWhiteSpace(Pattern) ? ".##" : Pattern,
            Purge = Purge,
            AuditMode = Audit,
            Shred = Secure
                ? SecurePasses == 7 ? FileIoShredRecipe.SevenRandomThenZero : FileIoShredRecipe.ThreeRandomThenZero
                : null,
            ReconLeadTime = TimeSpan.FromSeconds(2),
            MaxThreads = MaxThreads
        };
        return Verb switch
        {
            "Move" => FileIoHelper.Move(Source, Destination ?? "", options),
            "Delete" => FileIoHelper.Delete(Source, options),
            "Mirror" => FileIoHelper.Mirror(Source, Destination ?? "", options),
            _ => FileIoHelper.Copy(Source, Destination ?? "", options)
        };
    }

    public static void WriteResult(string jobPath, FileIoJobResult? result)
    {
        var path = Path.ChangeExtension(jobPath, ".result.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            JobId = result?.JobId,
            Status = result?.Status ?? "Failed",
            Copied = result?.Copied ?? 0,
            Skipped = result?.Skipped ?? 0,
            Failed = result?.Failed ?? 0,
            Deleted = result?.Deleted ?? 0
        }));
    }
}
