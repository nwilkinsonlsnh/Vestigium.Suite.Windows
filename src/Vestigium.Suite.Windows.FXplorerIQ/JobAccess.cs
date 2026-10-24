using System.IO;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public enum JobAccessDecision
{
    Run,
    Elevate,
    AskCreate,
    Refuse,
}

public sealed class JobAccessResult
{
    public JobAccessDecision Decision { get; init; }

    public string Reason { get; init; } = "";

    public static JobAccessResult Ok() => new() { Decision = JobAccessDecision.Run };

    public static JobAccessResult NeedsAdmin(string reason) => new() { Decision = JobAccessDecision.Elevate, Reason = reason };

    public static JobAccessResult Ask(string reason) => new() { Decision = JobAccessDecision.AskCreate, Reason = reason };

    public static JobAccessResult Stop(string reason) => new() { Decision = JobAccessDecision.Refuse, Reason = reason };
}

public static class JobAccess
{
    public static JobAccessResult Check(JobVerb verb, string source, string? destination)
    {
        if (!File.Exists(source) && !Directory.Exists(source))
            return JobAccessResult.Stop("Source was not found.");

        if (verb == JobVerb.Delete)
            return ProbeDelete(source);

        if (string.IsNullOrWhiteSpace(destination))
            return JobAccessResult.Stop("Destination is missing.");

        var read = ProbeRead(source);
        if (read.Decision != JobAccessDecision.Run)
            return read;

        return ProbeWrite(destination);
    }

    private static JobAccessResult ProbeRead(string source)
    {
        try
        {
            if (File.Exists(source))
            {
                using var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return JobAccessResult.Ok();
            }

            _ = Directory.EnumerateFileSystemEntries(source).Take(1).ToArray();
            return JobAccessResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return JobAccessResult.NeedsAdmin("Access denied on " + source + ".");
        }
        catch (IOException ex)
        {
            return JobAccessResult.Stop(ex.Message);
        }
    }

    private static JobAccessResult ProbeDelete(string source)
    {
        try
        {
            if (File.Exists(source))
            {
                using var stream = new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return JobAccessResult.Ok();
            }

            var probe = Path.Combine(source, ".fxplorer-probe");
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
            return JobAccessResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return JobAccessResult.NeedsAdmin("Access denied on " + source + ".");
        }
        catch (IOException ex)
        {
            return JobAccessResult.Stop(ex.Message);
        }
    }

    private static JobAccessResult ProbeWrite(string destination)
    {
        try
        {
            var folder = Directory.Exists(destination) ? destination : destination;
            if (!Directory.Exists(folder))
            {
                if (!App.Settings.Load().CreateMissingDestination)
                    return JobAccessResult.Ask("Destination does not exist: " + destination);
                Directory.CreateDirectory(folder);
            }

            var probe = Path.Combine(folder, ".fxplorer-probe");
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
            return JobAccessResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return JobAccessResult.NeedsAdmin("Access denied on " + destination + ".");
        }
        catch (IOException ex)
        {
            return JobAccessResult.Stop(ex.Message);
        }
    }
}
