using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public enum ElevationChoice
{
    Cancelled,
    Denied,
    Finished,
}

public sealed class ElevationOutcome
{
    public ElevationChoice Choice { get; init; }

    public string Status { get; init; } = "";
}

public static class JobHandoff
{
    public static ElevationOutcome Run(
        Window? owner,
        JobVerb verb,
        string source,
        string? destination,
        string collision,
        string pattern,
        bool secure,
        bool audit)
    {
        var shown = string.IsNullOrWhiteSpace(destination) ? source : destination;
        var verbName = audit ? "Audit" : Label(verb, secure);
        if (!ElevationPrompt.Ask(owner, verbName, shown))
            return new ElevationOutcome { Choice = ElevationChoice.Cancelled };

        Directory.CreateDirectory(JobVerify.Root);
        var path = Path.Combine(JobVerify.Root, "job-" + Guid.NewGuid().ToString("N") + ".json");
        var job = new ElevatedJob
        {
            Verb = verb.ToString(),
            Source = source,
            Destination = destination,
            Collision = collision,
            Pattern = string.IsNullOrWhiteSpace(pattern) ? ".##" : pattern,
            Purge = verb == JobVerb.Mirror,
            Audit = audit,
            Secure = secure,
            SecurePasses = App.Settings.Load().SecurePasses == 7 ? 7 : 3
        };
        File.WriteAllText(path, JsonSerializer.Serialize(job));

        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
            return new ElevationOutcome { Choice = ElevationChoice.Denied, Status = "This executable could not be found." };

        try
        {
            var process = Process.Start(new ProcessStartInfo(exe, "--job \"" + path + "\"")
            {
                UseShellExecute = true,
                Verb = "runas"
            });
            if (process is null)
                return new ElevationOutcome { Choice = ElevationChoice.Denied, Status = "Elevation was denied." };
            process.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new ElevationOutcome { Choice = ElevationChoice.Denied, Status = "Elevation was denied." };
        }

        var resultPath = Path.ChangeExtension(path, ".result.json");
        var status = "Finished";
        if (File.Exists(resultPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(resultPath));
            if (doc.RootElement.TryGetProperty("Status", out var value))
                status = value.GetString() ?? status;
        }

        return new ElevationOutcome { Choice = ElevationChoice.Finished, Status = status };
    }

    private static string Label(JobVerb verb, bool secure)
        => verb switch
        {
            JobVerb.Delete => secure ? "Delete (Secure)" : "Delete",
            JobVerb.Move => "Move",
            JobVerb.Mirror => "Mirror (Verify)",
            _ => "Copy"
        };
}
