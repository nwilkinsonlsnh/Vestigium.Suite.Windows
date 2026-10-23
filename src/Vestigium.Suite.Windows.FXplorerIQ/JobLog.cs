using System.IO;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public static class JobLog
{
    public static string File(string jobId)
        => Path.Combine(JobVerify.Root, jobId + ".jsonl");

    public static void Start(string verb, string jobId, string source, string? destination)
    {
        Directory.CreateDirectory(JobVerify.Root);
        HostLog.Information(
            FXplorerCatalog.JobStarted,
            "Lifecycle",
            verb + " started id=" + jobId + " source=" + source + " dest=" + OrNone(destination) + " log=" + File(jobId));
    }

    public static void End(string verb, string jobId, string source, string? destination, string status)
        => HostLog.Information(
            FXplorerCatalog.JobCompleted,
            "Lifecycle",
            verb + " ended id=" + jobId + " status=" + status + " source=" + source + " dest=" + OrNone(destination) + " log=" + File(jobId));

    private static string OrNone(string? path) => string.IsNullOrWhiteSpace(path) ? "(none)" : path;
}
