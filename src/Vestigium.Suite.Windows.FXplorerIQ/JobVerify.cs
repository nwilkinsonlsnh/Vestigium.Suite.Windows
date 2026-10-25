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

    public static VerifyResult Run(string jobId, string source, string destination, HashingAlgorithm algorithm, IProgress<(int Done, int Total, string Name)>? progress = null)
    {
        Directory.CreateDirectory(Root);
        var file = Path.Combine(Root, jobId + ".jsonl");
        var matched = 0;
        var failed = 0;
        var sourceIsFile = File.Exists(source);
        var files = sourceIsFile
            ? new[] { source }
            : Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).ToArray();
        var total = files.Length;

        for (var i = 0; i < files.Length; i++)
        {
            var left = files[i];
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
            if (i % 25 == 0 || i == files.Length - 1)
                progress?.Report((i + 1, total, relative));
        }

        return new VerifyResult { Path = file, Matched = matched, Failed = failed };
    }
}
