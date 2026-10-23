using System.IO;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public enum JobVerb
{
    Copy,
    Move,
    Delete,
    Mirror,
}

public sealed class JobGuardResult
{
    public bool Ok { get; init; }

    public string Reason { get; init; } = "";

    public static JobGuardResult Pass() => new() { Ok = true };

    public static JobGuardResult Refuse(string reason) => new() { Ok = false, Reason = reason };
}

public static class JobGuard
{
    public static JobGuardResult Check(JobVerb verb, string? source, string? destination)
    {
        var src = Clean(source);
        if (src.Length == 0)
            return JobGuardResult.Refuse("Source is missing.");
        if (!Directory.Exists(src) && !File.Exists(src))
            return JobGuardResult.Refuse("Source was not found.");

        if (verb == JobVerb.Delete)
            return JobGuardResult.Pass();

        var dest = Clean(destination);
        if (dest.Length == 0)
            return JobGuardResult.Refuse("Destination is missing.");
        if (Same(src, dest))
            return JobGuardResult.Refuse("Source and destination are the same path.");
        if (Inside(dest, src))
            return JobGuardResult.Refuse("Destination is inside the source.");
        if (!CanCreate(dest))
            return JobGuardResult.Refuse("Destination cannot be created.");

        return JobGuardResult.Pass();
    }

    private static string Clean(string? path)
        => string.IsNullOrWhiteSpace(path) ? "" : path.Trim().TrimEnd('\\', '/');

    private static bool Same(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool Inside(string path, string root)
    {
        var folder = root.EndsWith(':') ? root + "\\" : root + "\\";
        return path.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanCreate(string destination)
    {
        if (Directory.Exists(destination) || File.Exists(destination))
            return true;

        var parent = Path.GetDirectoryName(destination);
        if (string.IsNullOrWhiteSpace(parent))
            return false;
        if (Directory.Exists(parent))
            return true;

        var root = Path.GetPathRoot(parent);
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }
}
