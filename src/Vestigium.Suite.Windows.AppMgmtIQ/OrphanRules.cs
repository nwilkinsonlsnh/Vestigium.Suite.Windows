using Vestigium.Helpers.FileSystemPaths;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public static class OrphanRules
{
    public static (bool Orphan, string Reason)? Check(PackageRow row, Func<string, bool> exists)
    {
        var candidates = Candidates(row).ToArray();
        if (candidates.Length == 0)
            return null;

        if (candidates.Any(exists))
            return (false, "");

        var location = PathResolve.Clean(Text(row, "InstallLocation"));
        var reason = LooksLikePath(location) ? "Install location missing" : "Key file missing";
        return (true, reason);
    }

    private static IEnumerable<string> Candidates(PackageRow row)
    {
        var location = PathResolve.Clean(Text(row, "InstallLocation"));
        if (LooksLikePath(location))
            yield return location;

        foreach (var name in new[] { "DisplayIcon", "UninstallString", "ModifyPath" })
        {
            var file = PathResolve.Clean(FirstPath(Text(row, name)));
            if (LooksLikePath(file))
                yield return file;
        }
    }

    private static string FirstPath(string raw)
    {
        var text = raw.Trim().Trim('"');
        var comma = text.IndexOf(',');
        if (comma > 0 && text[..comma].Contains('\\'))
            text = text[..comma];
        foreach (var extension in new[] { ".exe", ".dll", ".msi" })
        {
            var at = text.IndexOf(extension, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
                continue;
            return text[..(at + extension.Length)].Trim('"');
        }
        return text;
    }

    private static string Text(PackageRow row, string name)
        => row.Values.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.DataText ?? "";

    private static bool LooksLikePath(string? path)
        => !string.IsNullOrWhiteSpace(path) && (path.Contains('\\') || path.Contains(':'));
}
