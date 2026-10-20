using System.IO;
using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public sealed record OrphanResult(bool Orphan, string Reason)
{
    public static OrphanResult? Check(PackageRow row)
    {
        var location = Text(row, "InstallLocation");
        if (LooksLikePath(location))
        {
            try
            {
                if (!Directory.Exists(location))
                    return new OrphanResult(true, "Install location missing");
            }
            catch (Exception)
            {
                return new OrphanResult(true, "Install location missing");
            }
        }

        var file = KeyFile(row);
        if (LooksLikePath(file))
        {
            try
            {
                if (!File.Exists(file))
                    return new OrphanResult(true, "Key file missing");
            }
            catch (Exception)
            {
                return new OrphanResult(true, "Key file missing");
            }
        }

        if (LooksLikePath(location) || LooksLikePath(file))
            return new OrphanResult(false, "");
        return null;
    }

    private static string? KeyFile(PackageRow row)
    {
        foreach (var name in new[] { "DisplayIcon", "UninstallString", "ModifyPath" })
        {
            var raw = Text(row, name);
            var file = FirstPath(raw);
            if (LooksLikePath(file))
                return file;
        }
        return null;
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
