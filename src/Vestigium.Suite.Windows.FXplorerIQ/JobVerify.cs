using System.IO;
using System.Linq;
using System.Text.Json;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class VerifyResult
{
    public string Path { get; init; } = "";

    public int Matched { get; init; }

    public int Failed { get; init; }
}

public static class JobVerify
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Vestigium",
        "FXplorerIQ",
        "Operations");

    public static VerifyResult Run(string jobId, string source, string destination, HashingAlgorithm algorithm, IProgress<(int Done, int Total, string Name)>? progress = null, int expected = 0)
    {
        Directory.CreateDirectory(Root);
        var file = Path.Combine(Root, jobId + ".jsonl");
        var matched = 0;
        var failed = 0;
        var sourceIsFile = File.Exists(source);
        IEnumerable<string> files = sourceIsFile
            ? new[] { source }
            : Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories);
        var matched = 0;
        var failed = 0;
        var done = 0;

        foreach (var left in files)
        {
            done++;
            var relative = sourceIsFile ? Path.GetFileName(left) : Path.GetRelativePath(source, left);
            var right = Path.Combine(destination, relative);
            var compared = FileIoHelper.CompareFiles(left, right, algorithm);
            if (compared.Equal)
                matched++;
            else
                failed++;

            var line = JsonSerializer.Serialize(new
            {
                jobId,
                relative,
                equal = compared.Equal,
                source = compared.LeftDigest,
                destination = compared.RightDigest,
                sourceMissing = compared.LeftMissing,
                destinationMissing = compared.RightMissing,
                at = DateTimeOffset.UtcNow
            }, Json);
            File.AppendAllText(file, line + Environment.NewLine);
            if (done == 1 || done % 25 == 0)
                progress?.Report((done, expected > 0 ? expected : done, relative));
        }

        progress?.Report((done, done, ""));
        return new VerifyResult { Path = file, Matched = matched, Failed = failed };
    }
}
