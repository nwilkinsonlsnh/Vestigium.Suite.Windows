using System.IO;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public static class JobAccess
{
    public static bool CanRun(JobVerb verb, string source, string? destination)
    {
        try
        {
            if (!File.Exists(source) && !Directory.Exists(source))
                return false;

            if (verb == JobVerb.Delete)
                return CanDelete(source);

            return CanWrite(destination);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool CanDelete(string source)
    {
        if (File.Exists(source))
        {
            using var stream = new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return true;
        }

        var probe = Path.Combine(source, ".fxplorer-probe");
        File.WriteAllText(probe, "probe");
        File.Delete(probe);
        return true;
    }

    private static bool CanWrite(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return false;

        var folder = Directory.Exists(destination) ? destination : Path.GetDirectoryName(destination);
        if (string.IsNullOrWhiteSpace(folder))
            return false;

        Directory.CreateDirectory(folder);
        var probe = Path.Combine(folder, ".fxplorer-probe");
        File.WriteAllText(probe, "probe");
        File.Delete(probe);
        return true;
    }
}
